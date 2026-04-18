#nullable disable

using System.Windows;
using System.Windows.Controls;

namespace LEGUI;

public partial class ShellExtensionPanel : UserControl
{
    private IShellExtensionQuery _query;
    private IShellExtensionCommand _command;
    private string _dllPath;
    private bool _isAdmin;

    /// <summary>
    /// Replaceable message shower — defaults to MessageBox.Show.
    /// Tests can swap this out to avoid blocking on a modal dialog.
    /// </summary>
    internal Action<string> ShowMessage = text =>
        MessageBox.Show(text, "Locale Emulator");

    public ShellExtensionPanel()
    {
        InitializeComponent();
    }

    public void SetQuery(IShellExtensionQuery query) => _query = query;

    /// <summary>Wire up install/uninstall actions. Task 4.2 is in-process admin path only;
    /// Task 5.3 will add UAC-elevated sub-process for AllUsers mode when !isAdmin.</summary>
    public void SetCommand(IShellExtensionCommand command, string dllPath, bool isAdmin)
    {
        _command = command;
        _dllPath = dllPath;
        _isAdmin = isAdmin;
        SetQuery(command);
    }

    public void RefreshStatus()
    {
        if (_query == null) return;

        var cuInstalled = _query.IsInstalled(ShellExtensionRegistrar.InstallMode.CurrentUser);
        var auInstalled = _query.IsInstalled(ShellExtensionRegistrar.InstallMode.AllUsers);

        tStatusCurrentUser.Text = " " + I18n.GetString(cuInstalled ? "Installed" : "NotInstalled");
        tStatusAllUsers.Text = " " + I18n.GetString(auInstalled ? "Installed" : "NotInstalled");

        bInstallCurrentUser.IsEnabled = !cuInstalled;
        bUninstallCurrentUser.IsEnabled = cuInstalled;
        bInstallAllUsers.IsEnabled = !auInstalled;
        bUninstallAllUsers.IsEnabled = auInstalled;

        cleanupSection.Visibility = _query.HasOldRegistration()
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void bInstallCurrentUser_Click(object sender, RoutedEventArgs e)
        => HandleInstall(ShellExtensionRegistrar.InstallMode.CurrentUser);

    private void bInstallAllUsers_Click(object sender, RoutedEventArgs e)
        => HandleInstall(ShellExtensionRegistrar.InstallMode.AllUsers);

    private void bUninstallCurrentUser_Click(object sender, RoutedEventArgs e)
        => HandleUninstall(ShellExtensionRegistrar.InstallMode.CurrentUser);

    private void bUninstallAllUsers_Click(object sender, RoutedEventArgs e)
        => HandleUninstall(ShellExtensionRegistrar.InstallMode.AllUsers);

    private void bCleanupOld_Click(object sender, RoutedEventArgs e)
    {
        // NOTE: AllUsers cleanup writes HKLM which requires admin. Task 5.3 will route
        // non-admin requests through a UAC-elevated sub-process.
        _command?.CleanupOldRegistration();
        RefreshStatus();
    }

    private void HandleInstall(ShellExtensionRegistrar.InstallMode mode)
    {
        // NOTE: Task 5.3 will route non-admin AllUsers requests through sub-process with UAC.
        _command?.Register(mode, _dllPath);
        RefreshStatus();
        if (_command != null)
            ShowMessage(I18n.GetString("InstallSuccess"));
    }

    private void HandleUninstall(ShellExtensionRegistrar.InstallMode mode)
    {
        _command?.Unregister(mode);
        RefreshStatus();
        if (_command != null)
            ShowMessage(I18n.GetString("UninstallSuccess"));
    }
}
