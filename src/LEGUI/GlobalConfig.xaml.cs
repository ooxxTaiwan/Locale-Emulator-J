#nullable disable

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LECommonLibrary;

namespace LEGUI;

public partial class GlobalConfig
{
    private readonly List<LEProfile> _profiles;
    private readonly DispatcherTimer _statusClearTimer;

    public GlobalConfig()
    {
        InitializeComponent();

        _profiles = LEConfig.GetProfiles().ToList();
        cbGlobalProfiles.ItemsSource = _profiles.Select(p => p.Name);
        if (_profiles.Count > 0)
            cbGlobalProfiles.SelectedIndex = 0;     // triggers LoadProfile via SelectionChanged
        else
            profileEditor.LoadProfile(new LEProfile(true));

        _statusClearTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _statusClearTimer.Tick += (_, _) =>
        {
            statusText.Text = string.Empty;
            _statusClearTimer.Stop();
        };

        InitializeShellExtPanel();
    }

    private void InitializeShellExtPanel()
    {
        var processPath = Environment.ProcessPath;
        string basePath = null;
        if (!string.IsNullOrEmpty(processPath))
            basePath = Path.GetDirectoryName(Path.GetDirectoryName(processPath));
        if (string.IsNullOrEmpty(basePath))
            basePath = AppContext.BaseDirectory;

        var dllPath = ShellExtensionRegistrar.AutoDetectDllPath(basePath);
        var registrar = new ShellExtensionRegistrar(
            new RegistryOperations(),
            ShellExtensionConstants.NewClsid);

        shellExtPanel.SetCommand(registrar, dllPath, SystemHelper.IsAdministrator());
        shellExtPanel.RefreshStatus();
    }

    private void ShowSavedStatus()
    {
        statusText.Text = I18n.GetString("SavedStatus");
        _statusClearTimer.Stop();
        _statusClearTimer.Start();
    }

    private void bSaveGlobalSetting_Click(object sender, RoutedEventArgs e)
    {
        if (cbGlobalProfiles.Items.Count == 0)
            return;

        var idx = cbGlobalProfiles.SelectedIndex;
        _profiles[idx] = profileEditor.ReadProfile(_profiles[idx]);
        LEConfig.SaveGlobalConfigFile(_profiles.ToArray());
        ShowSavedStatus();
    }

    private void cbGlobalProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bDeleteGlobalSetting.IsEnabled = cbGlobalProfiles.Items.Count != 0;
        bSaveGlobalSetting.IsEnabled = cbGlobalProfiles.Items.Count != 0;

        if (cbGlobalProfiles.SelectedIndex == -1)
            return;

        profileEditor.LoadProfile(_profiles[cbGlobalProfiles.SelectedIndex]);
    }

    private void bSaveGlobalSettingAs_Click(object sender, RoutedEventArgs e)
    {
        var ib = new InputBox
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Instruction = I18n.GetString("SaveAsInstruction"),
            OkText = I18n.GetString("Save"),
            CancelText = I18n.GetString("Cancel")
        };

        if (ib.ShowDialog() == true && !string.IsNullOrEmpty(ib.Text))
        {
            SaveProfileAs(ib.Text);
            cbGlobalProfiles.SelectedIndex = _profiles.Count - 1;
            ShowSavedStatus();
        }
    }

    private void SaveProfileAs(string name)
    {
        // Build template preserving outer-owned fields (Name/Guid/Parameter).
        // Name uses user-provided text; Guid is freshly generated.
        var baseProfile = cbGlobalProfiles.SelectedIndex >= 0
            ? _profiles[cbGlobalProfiles.SelectedIndex]
            : new LEProfile(true);
        baseProfile.Name = name;
        baseProfile.Guid = Guid.NewGuid().ToString();
        var created = profileEditor.ReadProfile(baseProfile);

        _profiles.Add(created);
        LEConfig.SaveGlobalConfigFile(_profiles.ToArray());
        cbGlobalProfiles.ItemsSource = _profiles.Select(p => p.Name);
    }

    private void bDeleteGlobalSetting_Click(object sender, RoutedEventArgs e)
    {
        if (cbGlobalProfiles.SelectedIndex == -1)
            return;

        if (MessageBoxResult.No == MessageBox.Show(
            I18n.GetString("ConfirmDel"),
            "Locale Emulator",
            MessageBoxButton.YesNo))
            return;

        _profiles.RemoveAt(cbGlobalProfiles.SelectedIndex);
        LEConfig.SaveGlobalConfigFile(_profiles.ToArray());
        cbGlobalProfiles.ItemsSource = _profiles.Select(p => p.Name);
        cbGlobalProfiles.SelectedIndex = 0;
    }
}
