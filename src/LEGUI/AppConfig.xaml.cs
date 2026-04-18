#nullable disable

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Windows;
using LECommonLibrary;

namespace LEGUI;

public partial class AppConfig
{
    public AppConfig()
    {
        InitializeComponent();

        Title += Path.GetFileName(App.StandaloneFilePath).Replace(".le.config", "");

        // Load existing config or fall back to default.
        var configs = LEConfig.GetProfiles(App.StandaloneFilePath);
        LEProfile initial;
        if (configs.Length > 0)
        {
            initial = configs[0];
            if (!string.IsNullOrEmpty(initial.Parameter))
            {
                tbAppParameter.FontStyle = FontStyles.Normal;
                tbAppParameter.Text = initial.Parameter;
            }
        }
        else
        {
            initial = new LEProfile(true);
        }

        profileEditor.LoadProfile(initial);
    }

    private void SaveSetting()
    {
        // ReadProfile overwrites every editable field from UI, so only Name / Guid /
        // Parameter / (and ShowInMainMenu, which stays false thanks to
        // ShowDisplayOptions=false on the editor) are actually consumed from this
        // template. The remaining fields take LEProfile's defaults as harmless
        // placeholders in case LoadProfile was never called and an index is -1.
        var defaults = new LEProfile(true);
        var template = new LEProfile(
            Path.GetFileName(App.StandaloneFilePath),
            Guid.NewGuid().ToString(),
            defaults.ShowInMainMenu,
            tbAppParameter.Text,
            defaults.Location, defaults.Timezone,
            defaults.RunAsAdmin, defaults.RedirectRegistry,
            defaults.IsAdvancedRedirection, defaults.RunWithSuspend);

        var crt = profileEditor.ReadProfile(template);
        LEConfig.SaveApplicationConfigFile(App.StandaloneFilePath, crt);
    }

    private void CreateShortcut(string path)
    {
        try
        {
            var link = (IShellLink)new ShellLink();

            link.SetPath(Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "LEProc.exe"));
            link.SetArguments($"-run \"{path}\"");
            link.SetIconLocation(
                AssociationReader.GetAssociatedIcon(Path.GetExtension(path)).Replace("%1", path), 0);
            link.SetDescription($"Run {Path.GetFileName(path)} with Locale Emulator");
            link.SetWorkingDirectory(Path.GetDirectoryName(path));

            var file = (IPersistFile)link;
            file.Save(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    Path.GetFileNameWithoutExtension(path) + ".lnk"),
                false);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message + "\r\n\r\n" + ex.StackTrace, "Locale Emulator");
        }
    }

    private void RunAndShutdown()
    {
        Process.Start(
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "LEProc.exe"),
            $"-run \"{App.StandaloneFilePath.Replace(".le.config", "")}\"");
        Application.Current.Shutdown();
    }

    private void bSaveAppSetting_Click(object sender, RoutedEventArgs e)
    {
        SaveSetting();
        RunAndShutdown();
    }

    private void bShortcut_Click(object sender, RoutedEventArgs e)
    {
        SaveSetting();
        CreateShortcut(App.StandaloneFilePath.Replace(".le.config", ""));
        RunAndShutdown();
    }

    private void bDeleteAppSetting_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBoxResult.No == MessageBox.Show(
            I18n.GetString("ConfirmDel"),
            "Locale Emulator",
            MessageBoxButton.YesNo))
            return;

        if (File.Exists(App.StandaloneFilePath))
            File.Delete(App.StandaloneFilePath);

        Application.Current.Shutdown();
    }
}
