using System.Globalization;
using Xunit;

namespace LEProc.Tests;

public class RegistryEntriesLoaderTests
{
    [Fact]
    public void GetRegistryEntries_NonAdvanced_ReturnsFourEntries()
    {
        var entries = RegistryEntriesLoader.GetRegistryEntries(false);
        Assert.Equal(4, entries.Length);
    }

    [Fact]
    public void GetRegistryEntries_Advanced_ReturnsEightEntries()
    {
        var entries = RegistryEntriesLoader.GetRegistryEntries(true);
        Assert.Equal(8, entries.Length);
    }

    [Fact]
    public void GetRegistryEntries_NonAdvanced_AllAreHKLM()
    {
        var entries = RegistryEntriesLoader.GetRegistryEntries(false);
        Assert.All(entries, e => Assert.Equal("HKEY_LOCAL_MACHINE", e.Root));
    }

    [Fact]
    public void GetRegistryEntries_Advanced_IncludesHKCU()
    {
        var entries = RegistryEntriesLoader.GetRegistryEntries(true);
        Assert.Contains(entries, e => e.Root == "HKEY_CURRENT_USER");
    }

    [Fact]
    public void GetRegistryEntries_JaJP_ReturnsCorrectACP()
    {
        var entries = RegistryEntriesLoader.GetRegistryEntries(false);
        var acpEntry = entries.First(e => e.Name == "ACP");

        var culture = CultureInfo.GetCultureInfo("ja-JP");
        var value = acpEntry.GetValue(culture);

        Assert.Equal("932", value);
    }

    [Fact]
    public void GetRegistryEntries_JaJP_ReturnsCorrectOEMCP()
    {
        var entries = RegistryEntriesLoader.GetRegistryEntries(false);
        var oemEntry = entries.First(e => e.Name == "OEMCP");

        var culture = CultureInfo.GetCultureInfo("ja-JP");
        var value = oemEntry.GetValue(culture);

        Assert.Equal("932", value);
    }

    [Fact]
    public void GetRegistryEntries_ZhTW_ReturnsCorrectCodePages()
    {
        var entries = RegistryEntriesLoader.GetRegistryEntries(false);
        var culture = CultureInfo.GetCultureInfo("zh-TW");

        var acp = entries.First(e => e.Name == "ACP").GetValue(culture);
        var oemcp = entries.First(e => e.Name == "OEMCP").GetValue(culture);

        Assert.Equal("950", acp);
        Assert.Equal("950", oemcp);
    }

    [Fact]
    public void GetRegistryEntries_Advanced_LocaleNameHasNullTerminator()
    {
        var entries = RegistryEntriesLoader.GetRegistryEntries(true);
        var localeNameEntry = entries.First(e => e.Name == "LocaleName");

        var culture = CultureInfo.GetCultureInfo("ja-JP");
        var value = localeNameEntry.GetValue(culture);

        Assert.EndsWith("\x00", value);
        Assert.Contains("ja-JP", value);
    }
}
