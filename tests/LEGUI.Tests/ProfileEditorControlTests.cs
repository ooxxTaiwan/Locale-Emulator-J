using LECommonLibrary;
using Xunit;

namespace LEGUI.Tests;

public class ProfileEditorControlTests
{
    private static LEProfile SampleProfile() => new LEProfile(
        "TestProfile", "{00000000-0000-0000-0000-000000000001}",
        showInMainMenu: true, parameter: "",
        location: "ja-JP", timezone: "Tokyo Standard Time",
        runAsAdmin: false, redirectRegistry: true,
        isAdvancedRedirection: false, runWithSuspend: false);

    [WpfFact]
    public void LoadThenRead_RoundTripsAllFields()
    {
        var ctrl = new ProfileEditorControl();
        var original = SampleProfile();

        ctrl.LoadProfile(original);
        var result = ctrl.ReadProfile(original);

        Assert.Equal(original.Location, result.Location);
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
}
