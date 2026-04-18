using NSubstitute;
using Xunit;

namespace LEGUI.Tests;

public class ShellExtensionPanelTests
{
    [WpfFact]
    public void RefreshStatus_WhenCurrentUserInstalled_DisablesInstallButton()
    {
        var panel = new ShellExtensionPanel();
        var command = Substitute.For<IShellExtensionCommand>();
        command.IsInstalled(ShellExtensionRegistrar.InstallMode.CurrentUser).Returns(true);
        command.IsInstalled(ShellExtensionRegistrar.InstallMode.AllUsers).Returns(false);
        command.HasOldRegistration().Returns(false);

        panel.SetCommand(command, dllPath: string.Empty, isAdmin: true);
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
        var command = Substitute.For<IShellExtensionCommand>();
        command.IsInstalled(Arg.Any<ShellExtensionRegistrar.InstallMode>()).Returns(false);
        command.HasOldRegistration().Returns(true);

        panel.SetCommand(command, dllPath: string.Empty, isAdmin: true);
        panel.RefreshStatus();

        Assert.Equal(System.Windows.Visibility.Visible, panel.cleanupSection.Visibility);
    }

    [WpfFact]
    public void InstallCurrentUser_CallsRegisterWithCurrentUserMode()
    {
        var panel = new ShellExtensionPanel();
        panel.ShowMessage = _ => { }; // suppress dialog in tests
        var command = Substitute.For<IShellExtensionCommand>();
        command.IsInstalled(Arg.Any<ShellExtensionRegistrar.InstallMode>()).Returns(false);
        command.HasOldRegistration().Returns(false);

        panel.SetCommand(command, dllPath: @"C:\fake\ShellExtension.dll", isAdmin: true);

        panel.bInstallCurrentUser.RaiseEvent(
            new System.Windows.RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));

        command.Received().Register(
            ShellExtensionRegistrar.InstallMode.CurrentUser,
            @"C:\fake\ShellExtension.dll");
    }
}
