using Xunit;

namespace LECommonLibrary.Tests;

public class LEProfileTests
{
    [Fact]
    public void DefaultConstructor_SetsJaJPDefaults()
    {
        var profile = new LEProfile(true);

        Assert.Equal("ja-JP", profile.Name);
        Assert.Equal("ja-JP", profile.Location);
        Assert.Equal("Tokyo Standard Time", profile.Timezone);
        Assert.False(profile.ShowInMainMenu);
        Assert.False(profile.RunAsAdmin);
        Assert.True(profile.RedirectRegistry);
        Assert.False(profile.IsAdvancedRedirection);
        Assert.False(profile.RunWithSuspend);
        Assert.Equal(string.Empty, profile.Parameter);
        Assert.NotNull(profile.Guid);
        Assert.NotEqual(string.Empty, profile.Guid);
    }

    [Fact]
    public void FullConstructor_SetsAllProperties()
    {
        var profile = new LEProfile(
            name: "Test Profile",
            guid: "test-guid-123",
            showInMainMenu: true,
            parameter: "-somearg",
            location: "zh-TW",
            timezone: "Taipei Standard Time",
            runAsAdmin: true,
            redirectRegistry: false,
            isAdvancedRedirection: true,
            runWithSuspend: true);

        Assert.Equal("Test Profile", profile.Name);
        Assert.Equal("test-guid-123", profile.Guid);
        Assert.True(profile.ShowInMainMenu);
        Assert.Equal("-somearg", profile.Parameter);
        Assert.Equal("zh-TW", profile.Location);
        Assert.Equal("Taipei Standard Time", profile.Timezone);
        Assert.True(profile.RunAsAdmin);
        Assert.False(profile.RedirectRegistry);
        Assert.True(profile.IsAdvancedRedirection);
        Assert.True(profile.RunWithSuspend);
    }

    [Fact]
    public void DefaultParameterlessStruct_HasNullStringsAndFalseFlags()
    {
        var profile = new LEProfile();

        Assert.Null(profile.Name);
        Assert.Null(profile.Guid);
        Assert.Null(profile.Location);
        Assert.Null(profile.Timezone);
        Assert.Null(profile.Parameter);
        Assert.False(profile.ShowInMainMenu);
        Assert.False(profile.RunAsAdmin);
        Assert.False(profile.RedirectRegistry);
        Assert.False(profile.IsAdvancedRedirection);
        Assert.False(profile.RunWithSuspend);
    }
}
