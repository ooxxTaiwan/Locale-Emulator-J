#nullable disable

using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace LEGUI;

public partial class ShellExtensionPanel : UserControl
{
    private IShellExtensionCommand _command;
    private string _dllPath;
    private bool _isAdmin;

    /// <summary>
    /// Replaceable message shower — defaults to MessageBox.Show.
    /// Tests can swap this out to avoid blocking on a modal dialog.
    /// </summary>
    internal Action<string> ShowMessage = text =>
        MessageBox.Show(text, I18n.GetString("AppName"));

    public ShellExtensionPanel()
    {
        InitializeComponent();
    }

    /// <summary>Wire up install/uninstall actions. AllUsers mode when !isAdmin is routed
    /// through a UAC-elevated sub-process via RunElevatedAsync.</summary>
    public void SetCommand(IShellExtensionCommand command, string dllPath, bool isAdmin)
    {
        _command = command;
        _dllPath = dllPath;
        _isAdmin = isAdmin;
    }

    public void RefreshStatus()
    {
        if (_command == null) return;

        var cuInstalled = _command.IsInstalled(ShellExtensionRegistrar.InstallMode.CurrentUser);
        var auInstalled = _command.IsInstalled(ShellExtensionRegistrar.InstallMode.AllUsers);

        tStatusCurrentUser.Text = " " + I18n.GetString(cuInstalled ? "Installed" : "NotInstalled");
        tStatusAllUsers.Text = " " + I18n.GetString(auInstalled ? "Installed" : "NotInstalled");

        bInstallCurrentUser.IsEnabled = !cuInstalled;
        bUninstallCurrentUser.IsEnabled = cuInstalled;
        bInstallAllUsers.IsEnabled = !auInstalled;
        bUninstallAllUsers.IsEnabled = auInstalled;

        cleanupSection.Visibility = _command.HasOldRegistration()
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private async void bInstallCurrentUser_Click(object sender, RoutedEventArgs e)
        => await HandleInstallAsync(ShellExtensionRegistrar.InstallMode.CurrentUser);

    private async void bInstallAllUsers_Click(object sender, RoutedEventArgs e)
        => await HandleInstallAsync(ShellExtensionRegistrar.InstallMode.AllUsers);

    private async void bUninstallCurrentUser_Click(object sender, RoutedEventArgs e)
        => await HandleUninstallAsync(ShellExtensionRegistrar.InstallMode.CurrentUser);

    private async void bUninstallAllUsers_Click(object sender, RoutedEventArgs e)
        => await HandleUninstallAsync(ShellExtensionRegistrar.InstallMode.AllUsers);

    private async void bCleanupOld_Click(object sender, RoutedEventArgs e)
    {
        if (_isAdmin)
        {
            TryInProcess(() => _command?.CleanupOldRegistration());
        }
        else
        {
            await RunElevatedAsync(ShellExtCliCommand.VerbCleanupOld, null);
        }
        RefreshStatus();
    }

    private async Task HandleInstallAsync(ShellExtensionRegistrar.InstallMode mode)
    {
        if (mode == ShellExtensionRegistrar.InstallMode.AllUsers && !_isAdmin)
        {
            if (!await RunElevatedAsync(ShellExtCliCommand.VerbInstall, ShellExtCliCommand.ScopeAllUsers))
            {
                RefreshStatus();
                return;
            }
        }
        else
        {
            if (!TryInProcess(() => _command?.Register(mode, _dllPath)))
            {
                RefreshStatus();
                return;
            }
        }
        RefreshStatus();
        if (_command != null)
            ShowMessage(I18n.GetString("InstallSuccess"));
    }

    private async Task HandleUninstallAsync(ShellExtensionRegistrar.InstallMode mode)
    {
        if (mode == ShellExtensionRegistrar.InstallMode.AllUsers && !_isAdmin)
        {
            if (!await RunElevatedAsync(ShellExtCliCommand.VerbUninstall, ShellExtCliCommand.ScopeAllUsers))
            {
                RefreshStatus();
                return;
            }
        }
        else
        {
            if (!TryInProcess(() => _command?.Unregister(mode)))
            {
                RefreshStatus();
                return;
            }
        }
        RefreshStatus();
        if (_command != null)
            ShowMessage(I18n.GetString("UninstallSuccess"));
    }

    /// <summary>Run an in-process registry operation, showing a localized error and returning false on failure.</summary>
    private bool TryInProcess(Action op)
    {
        try
        {
            op();
            return true;
        }
        catch (Exception ex)
        {
            ShowMessage(string.Format(I18n.GetString("ShellExtOperationFailed"), ex.Message));
            return false;
        }
    }

    /// <summary>
    /// Spawn LEGUI.exe with --shell-ext CLI and "runas" verb to trigger UAC.
    /// Returns true on success (exit code 0), false on cancellation or failure.
    /// </summary>
    private async Task<bool> RunElevatedAsync(string verb, string scope)
    {
        SetAllButtonsEnabled(false);
        try
        {
            var args = scope != null
                ? $"{ShellExtCliCommand.Prefix} {verb} {scope}"
                : $"{ShellExtCliCommand.Prefix} {verb}";
            var psi = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath,
                Arguments = args,
                Verb = "runas",
                UseShellExecute = true
            };

            using var p = Process.Start(psi);
            if (p == null) return false;

            await p.WaitForExitAsync();

            if (p.ExitCode != 0)
            {
                ShowMessage(string.Format(I18n.GetString("ShellExtOperationFailed"), $"exit code {p.ExitCode}"));
                return false;
            }
            return true;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // User cancelled UAC prompt — silently return, do not show error.
            return false;
        }
        finally
        {
            SetAllButtonsEnabled(true);
        }
    }

    private void SetAllButtonsEnabled(bool enabled)
    {
        bInstallCurrentUser.IsEnabled = enabled;
        bUninstallCurrentUser.IsEnabled = enabled;
        bInstallAllUsers.IsEnabled = enabled;
        bUninstallAllUsers.IsEnabled = enabled;
        bCleanupOld.IsEnabled = enabled;
    }
}
