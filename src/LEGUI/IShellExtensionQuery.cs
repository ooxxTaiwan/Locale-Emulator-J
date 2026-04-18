namespace LEGUI;

public interface IShellExtensionQuery
{
    bool IsInstalled(ShellExtensionRegistrar.InstallMode mode);
    bool HasOldRegistration();
}
