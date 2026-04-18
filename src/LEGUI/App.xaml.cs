#nullable disable

using System.IO;
using System.Windows;
using LECommonLibrary;

namespace LEGUI;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    internal static string StandaloneFilePath = string.Empty;

    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException +=
            (sender, args) => MessageBox.Show(((Exception) args.ExceptionObject).Message, I18n.AppName);

        base.OnStartup(e);
    }

    private void App_OnStartup(object sender, StartupEventArgs e)
    {
        // If running as a --shell-ext sub-process (e.g. spawned from ShellExtensionPanel
        // to elevate for an HKLM write), bypass all UI startup and execute the command.
        var cli = ShellExtCliCommand.Parse(e.Args);
        if (cli != null)
        {
            int exitCode = ExecuteShellExtCommand(cli);
            Current.Shutdown(exitCode);
            return;
        }

        // Reject malformed --shell-ext invocations instead of falling through to file-path
        // mode (which would treat "--shell-ext" as a dropped file and open AppConfig).
        if (e.Args.Length > 0 && e.Args[0] == ShellExtCliCommand.Prefix)
        {
            System.Diagnostics.Debug.WriteLine($"Malformed {ShellExtCliCommand.Prefix} invocation: {string.Join(' ', e.Args)}");
            Current.Shutdown(2);
            return;
        }

        if (e.Args.Length != 0)
        {
            StandaloneFilePath = SystemHelper.EnsureAbsolutePath(e.Args[0]);

            // This happens when user is trying to drop a exe onto LEGUI.
            if (!StandaloneFilePath.EndsWith(".le.config", true, null))
                StandaloneFilePath += ".le.config";
        }

        var isGlobalProfile = string.IsNullOrEmpty(StandaloneFilePath);

        LEConfig.CheckGlobalConfigFile(true);

        // Load locale dictionary before permission check so the error MessageBoxes
        // below resolve in the user's language rather than always showing English.
        I18n.LoadLanguage();

        // We check StandaloneFilePath before loading UI, because this wil be faster.
        if (
            !SystemHelper.CheckPermission(isGlobalProfile
                                              ? Path.GetDirectoryName(LEConfig.GlobalConfigPath)
                                              : Path.GetDirectoryName(StandaloneFilePath)))
        {
            if (SystemHelper.IsAdministrator())
            {
                // We can do nothing now.
                if (isGlobalProfile)
                    MessageBox.Show(
                                    I18n.Format("ErrorInstallDirNotWritable",
                                                Path.GetDirectoryName(LEConfig.GlobalConfigPath)),
                                    I18n.AppName,
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                else
                    MessageBox.Show(
                                    I18n.Format("ErrorDirNotWritable",
                                                Path.GetDirectoryName(StandaloneFilePath)),
                                    I18n.AppName,
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);

                Current.Shutdown();
            }
            else
            {
                // If we are not administrator, we can ask for administrator permission.
                try
                {
                    SystemHelper.RunWithElevatedProcess(
                                                        Path.Combine(
                                                                     Path.GetDirectoryName(LEConfig.GlobalConfigPath),
                                                                     "LEGUI.exe"),
                                                        e.Args);
                }
                catch (Exception)
                {
                    MessageBox.Show(I18n.GetString("ErrorAdminRequired"),
                                    I18n.AppName,
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
                finally
                {
                    Current.Shutdown();
                }
            }
        }

        Current.StartupUri = isGlobalProfile
                                 ? new Uri("GlobalConfig.xaml", UriKind.RelativeOrAbsolute)
                                 : new Uri("AppConfig.xaml", UriKind.RelativeOrAbsolute);
    }

    private int ExecuteShellExtCommand(ShellExtCliCommand cli)
    {
        try
        {
            var dllPath = ShellExtensionRegistrar.AutoDetectDllPath(
                ShellExtensionRegistrar.GetBuildOutputRoot());
            var registrar = new ShellExtensionRegistrar(
                new RegistryOperations(),
                ShellExtensionConstants.NewClsid);

            switch (cli.ActionVerb)
            {
                case ShellExtCliCommand.Verb.Install:
                    registrar.Register(cli.Mode, dllPath);
                    break;
                case ShellExtCliCommand.Verb.Uninstall:
                    registrar.Unregister(cli.Mode);
                    break;
                case ShellExtCliCommand.Verb.CleanupOld:
                    registrar.CleanupOldRegistration();
                    break;
            }
            return 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"--shell-ext failed: {ex}");
            return 1;
        }
    }
}
