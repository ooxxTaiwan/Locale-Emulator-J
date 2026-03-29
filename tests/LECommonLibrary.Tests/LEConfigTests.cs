using System.Xml.Linq;
using Xunit;

namespace LECommonLibrary.Tests;

public class LEConfigTests : IDisposable
{
    private readonly string _tempDir;

    public LEConfigTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "LEConfigTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string CreateTempConfigFile(string xml)
    {
        var path = Path.Combine(_tempDir, "test.xml");
        File.WriteAllText(path, xml);
        return path;
    }

    [Fact]
    public void GetProfiles_ValidXml_ParsesAllProfiles()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <LEConfig>
              <Profiles>
                <Profile Name="Run in Japanese" Guid="guid-1" MainMenu="true">
                  <Parameter>-test</Parameter>
                  <Location>ja-JP</Location>
                  <Timezone>Tokyo Standard Time</Timezone>
                  <RunAsAdmin>false</RunAsAdmin>
                  <RedirectRegistry>true</RedirectRegistry>
                  <IsAdvancedRedirection>false</IsAdvancedRedirection>
                  <RunWithSuspend>false</RunWithSuspend>
                </Profile>
                <Profile Name="Run in Chinese" Guid="guid-2" MainMenu="false">
                  <Parameter></Parameter>
                  <Location>zh-TW</Location>
                  <Timezone>Taipei Standard Time</Timezone>
                  <RunAsAdmin>true</RunAsAdmin>
                  <RedirectRegistry>true</RedirectRegistry>
                  <IsAdvancedRedirection>true</IsAdvancedRedirection>
                  <RunWithSuspend>false</RunWithSuspend>
                </Profile>
              </Profiles>
            </LEConfig>
            """;

        var path = CreateTempConfigFile(xml);
        var profiles = LEConfig.GetProfiles(path);

        Assert.Equal(2, profiles.Length);

        Assert.Equal("Run in Japanese", profiles[0].Name);
        Assert.Equal("guid-1", profiles[0].Guid);
        Assert.True(profiles[0].ShowInMainMenu);
        Assert.Equal("-test", profiles[0].Parameter);
        Assert.Equal("ja-JP", profiles[0].Location);
        Assert.Equal("Tokyo Standard Time", profiles[0].Timezone);
        Assert.False(profiles[0].RunAsAdmin);
        Assert.True(profiles[0].RedirectRegistry);
        Assert.False(profiles[0].IsAdvancedRedirection);
        Assert.False(profiles[0].RunWithSuspend);

        Assert.Equal("Run in Chinese", profiles[1].Name);
        Assert.Equal("zh-TW", profiles[1].Location);
        Assert.True(profiles[1].RunAsAdmin);
        Assert.True(profiles[1].IsAdvancedRedirection);
    }

    [Fact]
    public void GetProfiles_MissingOptionalElements_UsesDefaults()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <LEConfig>
              <Profiles>
                <Profile Name="Minimal" Guid="guid-min" MainMenu="false">
                  <Parameter></Parameter>
                  <Location>ko-KR</Location>
                  <Timezone>Korea Standard Time</Timezone>
                </Profile>
              </Profiles>
            </LEConfig>
            """;

        var path = CreateTempConfigFile(xml);
        var profiles = LEConfig.GetProfiles(path);

        Assert.Single(profiles);
        Assert.False(profiles[0].RunAsAdmin);       // default "false"
        Assert.True(profiles[0].RedirectRegistry);   // default "true"
        Assert.False(profiles[0].IsAdvancedRedirection); // default "false"
        Assert.False(profiles[0].RunWithSuspend);    // default "false"
    }

    [Fact]
    public void GetProfiles_EmptyFile_ReturnsEmptyArray()
    {
        var path = CreateTempConfigFile("<LEConfig><Profiles /></LEConfig>");
        var profiles = LEConfig.GetProfiles(path);
        Assert.Empty(profiles);
    }

    [Fact]
    public void GetProfiles_InvalidXml_ReturnsEmptyArray()
    {
        var path = CreateTempConfigFile("not xml at all");
        var profiles = LEConfig.GetProfiles(path);
        Assert.Empty(profiles);
    }

    [Fact]
    public void GetProfiles_NonExistentFile_ReturnsEmptyArray()
    {
        var profiles = LEConfig.GetProfiles(Path.Combine(_tempDir, "nonexistent.xml"));
        Assert.Empty(profiles);
    }

    [Fact]
    public void SaveAndReload_RoundTrips()
    {
        var path = Path.Combine(_tempDir, "roundtrip.xml");
        var original = new LEProfile(
            "Test",
            "test-guid",
            true,
            "-arg1",
            "ja-JP",
            "Tokyo Standard Time",
            true,
            true,
            false,
            true);

        LEConfig.SaveApplicationConfigFile(path, original);
        var loaded = LEConfig.GetProfiles(path);

        Assert.Single(loaded);
        Assert.Equal("Test", loaded[0].Name);
        Assert.Equal("test-guid", loaded[0].Guid);
        Assert.True(loaded[0].ShowInMainMenu);
        Assert.Equal("-arg1", loaded[0].Parameter);
        Assert.Equal("ja-JP", loaded[0].Location);
        Assert.Equal("Tokyo Standard Time", loaded[0].Timezone);
        Assert.True(loaded[0].RunAsAdmin);
        Assert.True(loaded[0].RedirectRegistry);
        Assert.False(loaded[0].IsAdvancedRedirection);
        Assert.True(loaded[0].RunWithSuspend);
    }
}
