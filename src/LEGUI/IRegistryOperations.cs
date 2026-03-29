using Microsoft.Win32;

namespace LEGUI;

/// <summary>
/// Registry operations abstraction for ShellExtensionRegistrar.
/// Test with NSubstitute mock instead of real registry operations.
/// </summary>
public interface IRegistryOperations
{
    /// <summary>
    /// Open or create the specified registry subkey.
    /// </summary>
    IRegistryKeyWrapper CreateSubKey(RegistryHive hive, string subKey, RegistryView view);

    /// <summary>
    /// Delete the specified registry subkey tree.
    /// </summary>
    void DeleteSubKeyTree(RegistryHive hive, string subKey, RegistryView view, bool throwOnMissing);

    /// <summary>
    /// Check if the specified registry subkey exists.
    /// </summary>
    bool SubKeyExists(RegistryHive hive, string subKey, RegistryView view);
}

/// <summary>
/// Wrapper interface for RegistryKey, for mock use.
/// </summary>
public interface IRegistryKeyWrapper : IDisposable
{
    void SetValue(string? name, object value);
}
