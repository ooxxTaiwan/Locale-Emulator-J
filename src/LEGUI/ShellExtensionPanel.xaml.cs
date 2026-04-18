#nullable disable

using System.Windows;
using System.Windows.Controls;

namespace LEGUI;

public partial class ShellExtensionPanel : UserControl
{
    private IShellExtensionQuery _query;

    public ShellExtensionPanel()
    {
        InitializeComponent();
    }

    public void SetQuery(IShellExtensionQuery query) => _query = query;

    public void RefreshStatus()
    {
        if (_query == null) return;

        var cuInstalled = _query.IsInstalled(ShellExtensionRegistrar.InstallMode.CurrentUser);
        var auInstalled = _query.IsInstalled(ShellExtensionRegistrar.InstallMode.AllUsers);

        tStatusCurrentUser.Text = " " + GetStatusText(cuInstalled);
        tStatusAllUsers.Text = " " + GetStatusText(auInstalled);

        bInstallCurrentUser.IsEnabled = !cuInstalled;
        bUninstallCurrentUser.IsEnabled = cuInstalled;
        bInstallAllUsers.IsEnabled = !auInstalled;
        bUninstallAllUsers.IsEnabled = auInstalled;

        cleanupSection.Visibility = _query.HasOldRegistration()
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static string GetStatusText(bool installed)
    {
        try
        {
            return I18n.GetString(installed ? "Installed" : "NotInstalled");
        }
        catch
        {
            return installed ? "Installed" : "Not Installed";
        }
    }
}
