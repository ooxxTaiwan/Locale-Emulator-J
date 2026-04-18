using LECommonLibrary;
using Xunit;

namespace LEGUI.Tests;

public class ProfileEditorControlTests
{
    // All booleans deliberately set to values that differ from an un-toggled CheckBox default (false),
    // so a hypothetical no-op LoadProfile would fail this round-trip test instead of false-passing.
    private static LEProfile SampleProfile() => new LEProfile(
        "TestProfile", "{00000000-0000-0000-0000-000000000001}",
        showInMainMenu: true, parameter: "arg1",
        region: "ja-JP", timezone: "Tokyo Standard Time",
        runAsAdmin: true, redirectRegistry: true,
        isAdvancedRedirection: true, runWithSuspend: true);

    [WpfFact]
    public void LoadThenRead_RoundTripsAllFields()
    {
        var ctrl = new ProfileEditorControl();
        var original = SampleProfile();

        ctrl.LoadProfile(original);
        var result = ctrl.ReadProfile(original);

        Assert.Equal(original.Region, result.Region);
        Assert.Equal(original.Timezone, result.Timezone);
        Assert.Equal(original.RunAsAdmin, result.RunAsAdmin);
        Assert.Equal(original.RedirectRegistry, result.RedirectRegistry);
        Assert.Equal(original.IsAdvancedRedirection, result.IsAdvancedRedirection);
        Assert.Equal(original.RunWithSuspend, result.RunWithSuspend);
        Assert.Equal(original.ShowInMainMenu, result.ShowInMainMenu);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.Guid, result.Guid);
        Assert.Equal(original.Parameter, result.Parameter);
    }

    [WpfFact]
    public void ShowDisplayOptionsFalse_HidesDisplayGroup()
    {
        var ctrl = new ProfileEditorControl();
        ctrl.ShowDisplayOptions = false;

        Assert.Equal(System.Windows.Visibility.Collapsed, ctrl.displayGroup.Visibility);
    }

    [WpfFact]
    public void ShowDisplayOptionsFalse_ReadProfilePreservesShowInMainMenuFromTemplate()
    {
        var ctrl = new ProfileEditorControl();
        ctrl.ShowDisplayOptions = false;
        var template = SampleProfile(); // ShowInMainMenu=true
        ctrl.LoadProfile(template);

        // Even if user changes cbShowInMainMenu via UI, ReadProfile should preserve template value.
        ctrl.cbShowInMainMenu.IsChecked = false;
        var result = ctrl.ReadProfile(template);

        Assert.True(result.ShowInMainMenu); // preserved from template
    }
}
