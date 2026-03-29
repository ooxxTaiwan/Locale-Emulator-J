using System.IO;
using Microsoft.Win32;

namespace LEGUI;

/// <summary>
/// Shell Extension registry registrar.
///
/// This is the sole authoritative implementation of Shell Extension COM registration.
///
/// Design principles:
/// - Two install modes (AllUsers/CurrentUser) use the same key list, only root hive differs
/// - 64-bit OS forces RegistryView.Registry64
/// - No DllRegisterServer, no LoadLibrary, no regsvr32
/// - Responsible for detecting and cleaning old COM GUID residues
/// </summary>
public sealed class ShellExtensionRegistrar
{
    /// <summary>
    /// Old LEContextMenuHandler COM GUID (needs cleanup).
    /// </summary>
    public const string OldClsid = "{C52B9871-E5E9-41FD-B84D-C5ACADBEC7AE}";

    /// <summary>
    /// Shell Extension friendly name (written to ContextMenuHandlers Default value).
    /// </summary>
    private const string FriendlyName = "Locale Emulator Shell Extension";

    private readonly IRegistryOperations _registry;
    private readonly string _clsid;

    /// <summary>
    /// Install mode.
    /// </summary>
    public enum InstallMode
    {
        /// <summary>All users (HKLM, requires admin).</summary>
        AllUsers,

        /// <summary>Current user only (HKCU, no admin required).</summary>
        CurrentUser
    }

    /// <summary>
    /// Create a ShellExtensionRegistrar instance.
    /// </summary>
    /// <param name="registry">Registry operations abstraction (pass RegistryOperations in production, mock in tests).</param>
    /// <param name="clsid">New Shell Extension COM GUID (with braces, e.g. {XXXXXXXX-...}).</param>
    public ShellExtensionRegistrar(IRegistryOperations registry, string clsid)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _clsid = clsid ?? throw new ArgumentNullException(nameof(clsid));
    }

    /// <summary>
    /// Get the registry root hive for the given install mode.
    /// </summary>
    private static RegistryHive GetHive(InstallMode mode) =>
        mode == InstallMode.AllUsers ? RegistryHive.LocalMachine : RegistryHive.CurrentUser;

    /// <summary>
    /// Get the RegistryView based on OS bitness.
    /// 64-bit OS -> Registry64 (ensure writes to native 64-bit view, which Explorer.exe reads)
    /// 32-bit OS -> Default
    /// </summary>
    private static RegistryView GetView(bool is64BitOs) =>
        is64BitOs ? RegistryView.Registry64 : RegistryView.Default;

    /// <summary>
    /// Get the list of registry keys to write (shared by both modes).
    /// </summary>
    private (string subKey, Action<IRegistryKeyWrapper> setValues)[] GetKeyList(string dllPath) =>
    [
        // 1. COM server registration
        (
            subKey: $@"Software\Classes\CLSID\{_clsid}\InprocServer32",
            setValues: key =>
            {
                key.SetValue(null, dllPath);           // (Default) = DLL path
                key.SetValue("ThreadingModel", "Apartment");
            }
        ),
        // 2. Context Menu Handler registration
        (
            subKey: $@"Software\Classes\*\shellex\ContextMenuHandlers\{_clsid}",
            setValues: key =>
            {
                key.SetValue(null, FriendlyName);      // (Default) = friendly name
            }
        )
    ];

    /// <summary>
    /// Keys to delete (for uninstall).
    /// </summary>
    private string[] GetDeleteKeyList(string clsid) =>
    [
        $@"Software\Classes\CLSID\{clsid}",
        $@"Software\Classes\*\shellex\ContextMenuHandlers\{clsid}"
    ];

    /// <summary>
    /// Register the Shell Extension.
    /// </summary>
    /// <param name="mode">Install mode.</param>
    /// <param name="dllPath">Full path to ShellExtension.dll.</param>
    /// <param name="is64BitOs">Whether this is a 64-bit OS (defaults to auto-detect).</param>
    public void Register(InstallMode mode, string dllPath, bool? is64BitOs = null)
    {
        var hive = GetHive(mode);
        var view = GetView(is64BitOs ?? Environment.Is64BitOperatingSystem);

        foreach (var (subKey, setValues) in GetKeyList(dllPath))
        {
            using var key = _registry.CreateSubKey(hive, subKey, view);
            setValues(key);
        }
    }

    /// <summary>
    /// Unregister the Shell Extension.
    /// </summary>
    /// <param name="mode">Install mode.</param>
    /// <param name="is64BitOs">Whether this is a 64-bit OS (defaults to auto-detect).</param>
    public void Unregister(InstallMode mode, bool? is64BitOs = null)
    {
        var hive = GetHive(mode);
        var view = GetView(is64BitOs ?? Environment.Is64BitOperatingSystem);

        foreach (var subKey in GetDeleteKeyList(_clsid))
        {
            _registry.DeleteSubKeyTree(hive, subKey, view, throwOnMissing: false);
        }
    }

    /// <summary>
    /// Check if the Shell Extension is installed.
    /// </summary>
    /// <param name="mode">Install mode.</param>
    /// <param name="is64BitOs">Whether this is a 64-bit OS (defaults to auto-detect).</param>
    public bool IsInstalled(InstallMode mode, bool? is64BitOs = null)
    {
        var hive = GetHive(mode);
        var view = GetView(is64BitOs ?? Environment.Is64BitOperatingSystem);
        var subKey = $@"Software\Classes\*\shellex\ContextMenuHandlers\{_clsid}";

        return _registry.SubKeyExists(hive, subKey, view);
    }

    /// <summary>
    /// Auto-detect DLL path based on OS bitness.
    /// </summary>
    /// <param name="basePath">Build output root directory.</param>
    /// <param name="is64BitOs">Whether this is a 64-bit OS (defaults to auto-detect).</param>
    /// <returns>Full path to ShellExtension.dll.</returns>
    public static string AutoDetectDllPath(string basePath, bool? is64BitOs = null)
    {
        var is64 = is64BitOs ?? Environment.Is64BitOperatingSystem;
        var subDir = is64 ? "x64" : "x86";
        return Path.Combine(basePath, subDir, "ShellExtension.dll");
    }

    /// <summary>
    /// Check if old COM GUID registry residues exist (HKLM or HKCU).
    /// </summary>
    public bool HasOldRegistration(bool? is64BitOs = null)
    {
        var view = GetView(is64BitOs ?? Environment.Is64BitOperatingSystem);

        foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        {
            foreach (var subKey in GetDeleteKeyList(OldClsid))
            {
                if (_registry.SubKeyExists(hive, subKey, view))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Clean up old COM GUID registry residues (both HKLM and HKCU).
    /// </summary>
    public void CleanupOldRegistration(bool? is64BitOs = null)
    {
        var view = GetView(is64BitOs ?? Environment.Is64BitOperatingSystem);

        foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        {
            foreach (var subKey in GetDeleteKeyList(OldClsid))
            {
                _registry.DeleteSubKeyTree(hive, subKey, view, throwOnMissing: false);
            }
        }
    }
}
