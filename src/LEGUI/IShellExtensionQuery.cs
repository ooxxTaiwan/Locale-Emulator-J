namespace LEGUI;

public interface IShellExtensionQuery
{
    bool IsInstalled(ShellExtensionRegistrar.InstallMode mode);
    bool HasOldRegistration();
}

public interface IShellExtensionCommand : IShellExtensionQuery
{
    void Register(ShellExtensionRegistrar.InstallMode mode, string dllPath);
    void Unregister(ShellExtensionRegistrar.InstallMode mode);
    void CleanupOldRegistration();
}
