using NSubstitute;
using Xunit;

namespace LEGUI.Tests;

public class ShellExtensionPanelTests
{
    [WpfFact]
    public void RefreshStatus_WhenCurrentUserInstalled_DisablesInstallButton()
    {
        var panel = new ShellExtensionPanel();
        var query = Substitute.For<IShellExtensionQuery>();
        query.IsInstalled(ShellExtensionRegistrar.InstallMode.CurrentUser).Returns(true);
        query.IsInstalled(ShellExtensionRegistrar.InstallMode.AllUsers).Returns(false);
        query.HasOldRegistration().Returns(false);

        panel.SetQuery(query);
        panel.RefreshStatus();

        Assert.False(panel.bInstallCurrentUser.IsEnabled);
        Assert.True(panel.bUninstallCurrentUser.IsEnabled);
        Assert.True(panel.bInstallAllUsers.IsEnabled);
        Assert.False(panel.bUninstallAllUsers.IsEnabled);
    }

    [WpfFact]
    public void RefreshStatus_HasOldRegistration_ShowsCleanupSection()
    {
        var panel = new ShellExtensionPanel();
        var query = Substitute.For<IShellExtensionQuery>();
        query.IsInstalled(Arg.Any<ShellExtensionRegistrar.InstallMode>()).Returns(false);
        query.HasOldRegistration().Returns(true);

        panel.SetQuery(query);
        panel.RefreshStatus();

        Assert.Equal(System.Windows.Visibility.Visible, panel.cleanupSection.Visibility);
    }
}
