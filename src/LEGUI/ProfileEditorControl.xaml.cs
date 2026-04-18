#nullable disable

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using LECommonLibrary;

namespace LEGUI;

public partial class ProfileEditorControl : UserControl
{
    private readonly List<CultureInfo> _cultureInfos;
    private readonly List<TimeZoneInfo> _timezones;

    public static readonly DependencyProperty ShowDisplayOptionsProperty =
        DependencyProperty.Register(
            nameof(ShowDisplayOptions),
            typeof(bool),
            typeof(ProfileEditorControl),
            new PropertyMetadata(true, OnShowDisplayOptionsChanged));

    public bool ShowDisplayOptions
    {
        get => (bool)GetValue(ShowDisplayOptionsProperty);
        set => SetValue(ShowDisplayOptionsProperty, value);
    }

    public ProfileEditorControl()
    {
        InitializeComponent();

        _cultureInfos = CultureInfo.GetCultures(CultureTypes.AllCultures)
            .OrderBy(i => i.DisplayName).ToList();
        cbLocation.ItemsSource = _cultureInfos.Select(c => c.DisplayName);

        _timezones = TimeZoneInfo.GetSystemTimeZones().ToList();
        cbTimezone.ItemsSource = _timezones.Select(t => t.DisplayName);
    }

    public void LoadProfile(LEProfile source)
    {
        cbLocation.SelectedIndex = _cultureInfos.FindIndex(ci => ci.Name == source.Location);
        cbTimezone.SelectedIndex = _timezones.FindIndex(tz => tz.Id == source.Timezone);
        cbStartAsAdmin.IsChecked = source.RunAsAdmin;
        cbRedirectRegistry.IsChecked = source.RedirectRegistry;
        cbIsAdvancedRedirection.IsChecked = source.IsAdvancedRedirection;
        cbStartAsSuspend.IsChecked = source.RunWithSuspend;
        cbShowInMainMenu.IsChecked = source.ShowInMainMenu;
    }

    public LEProfile ReadProfile(LEProfile template)
    {
        var locationIdx = cbLocation.SelectedIndex;
        var timezoneIdx = cbTimezone.SelectedIndex;
        return new LEProfile(
            template.Name,
            template.Guid,
            ShowDisplayOptions ? cbShowInMainMenu.IsChecked == true : template.ShowInMainMenu,
            template.Parameter,
            locationIdx >= 0 ? _cultureInfos[locationIdx].Name : template.Location,
            timezoneIdx >= 0 ? _timezones[timezoneIdx].Id : template.Timezone,
            cbStartAsAdmin.IsChecked == true,
            cbRedirectRegistry.IsChecked == true,
            cbIsAdvancedRedirection.IsChecked == true,
            cbStartAsSuspend.IsChecked == true);
    }

    private static void OnShowDisplayOptionsChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (ProfileEditorControl)d;
        ctrl.displayGroup.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
    }
}
