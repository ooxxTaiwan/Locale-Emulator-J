using Microsoft.Win32;

namespace LEGUI;

/// <summary>
/// Real Registry operations implementation wrapping .NET RegistryKey API.
/// </summary>
internal sealed class RegistryOperations : IRegistryOperations
{
    public IRegistryKeyWrapper CreateSubKey(RegistryHive hive, string subKey, RegistryView view)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        var key = baseKey.CreateSubKey(subKey)
            ?? throw new InvalidOperationException($"Failed to create registry key: {hive}\\{subKey}");
        return new RegistryKeyWrapper(key);
    }

    public void DeleteSubKeyTree(RegistryHive hive, string subKey, RegistryView view, bool throwOnMissing)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        try
        {
            baseKey.DeleteSubKeyTree(subKey, throwOnMissing);
        }
        catch (ArgumentException) when (!throwOnMissing)
        {
            // Key doesn't exist, ignore
        }
    }

    public bool SubKeyExists(RegistryHive hive, string subKey, RegistryView view)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        using var key = baseKey.OpenSubKey(subKey, false);
        return key != null;
    }
}

internal sealed class RegistryKeyWrapper : IRegistryKeyWrapper
{
    private readonly RegistryKey _key;

    public RegistryKeyWrapper(RegistryKey key) => _key = key;

    public void SetValue(string? name, object value) => _key.SetValue(name, value);

    public void Dispose() => _key.Dispose();
}
