#nullable disable

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using LECommonLibrary;

namespace LEGUI;

public partial class ProfileEditorControl : UserControl
{
    // Static cache: both lists are OS-wide and stable for the process lifetime.
    // CultureInfo.GetCultures(AllCultures) returns 800+ entries and is noticeably
    // expensive; rebuilding it per control instance (two windows × reopens) was
    // wasteful.
    private static readonly List<CultureInfo> s_cultureInfos =
        CultureInfo.GetCultures(CultureTypes.AllCultures).OrderBy(i => i.DisplayName).ToList();
    private static readonly List<TimeZoneInfo> s_timezones =
        TimeZoneInfo.GetSystemTimeZones().ToList();

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
        cbRegion.ItemsSource = s_cultureInfos.Select(c => c.DisplayName);
        cbTimezone.ItemsSource = s_timezones.Select(t => t.DisplayName);
    }

    public void LoadProfile(LEProfile source)
    {
        cbRegion.SelectedIndex = s_cultureInfos.FindIndex(ci => ci.Name == source.Region);
        cbTimezone.SelectedIndex = s_timezones.FindIndex(tz => tz.Id == source.Timezone);
        cbStartAsAdmin.IsChecked = source.RunAsAdmin;
        cbRedirectRegistry.IsChecked = source.RedirectRegistry;
        cbIsAdvancedRedirection.IsChecked = source.IsAdvancedRedirection;
        cbStartAsSuspend.IsChecked = source.RunWithSuspend;
        cbShowInMainMenu.IsChecked = source.ShowInMainMenu;
    }

    public LEProfile ReadProfile(LEProfile template)
    {
        var regionIdx = cbRegion.SelectedIndex;
        var timezoneIdx = cbTimezone.SelectedIndex;
        return new LEProfile(
            template.Name,
            template.Guid,
            ShowDisplayOptions ? cbShowInMainMenu.IsChecked == true : template.ShowInMainMenu,
            template.Parameter,
            regionIdx >= 0 ? s_cultureInfos[regionIdx].Name : template.Region,
            timezoneIdx >= 0 ? s_timezones[timezoneIdx].Id : template.Timezone,
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
