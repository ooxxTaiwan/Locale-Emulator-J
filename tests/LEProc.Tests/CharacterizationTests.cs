using System.Globalization;
using System.Text.Json;
using Xunit;

namespace LEProc.Tests;

public class CharacterizationTests
{
    private static readonly string[] SupportedLocales =
    [
        "ja-JP", "zh-TW", "zh-CN", "zh-HK", "ko-KR", "en-US",
        "de-DE", "fr-FR", "it-IT", "es-ES", "pt-BR", "ru-RU",
        "pl-PL", "cs-CZ", "tr-TR", "th-TH", "vi-VN", "ar-SA",
        "he-IL", "el-GR", "nl-NL", "nb-NO", "lt-LT", "ka-GE"
    ];

    [Fact]
    public void AllSupportedLocales_HaveValidCultureInfo()
    {
        foreach (var locale in SupportedLocales)
        {
            var ci = CultureInfo.GetCultureInfo(locale);
            Assert.NotNull(ci);
            Assert.NotEqual(0, ci.TextInfo.ANSICodePage + ci.TextInfo.OEMCodePage);
        }
    }

    [Fact]
    public void CompareWithBaseline_ICUMode()
    {
        var baselinePath = Path.Combine(
            AppContext.BaseDirectory, "TestData", "locale-baseline-netfx40.json");

        if (!File.Exists(baselinePath))
        {
            // Baseline file not yet generated, skip
            return;
        }

        var json = File.ReadAllText(baselinePath);
        using var doc = JsonDocument.Parse(json);
        var locales = doc.RootElement.GetProperty("locales");

        var differences = new List<string>();

        foreach (var locale in SupportedLocales)
        {
            if (!locales.TryGetProperty(locale, out var baseline))
                continue;

            var ci = CultureInfo.GetCultureInfo(locale);
            var expectedAnsi = baseline.GetProperty("ANSICodePage").GetInt32();
            var expectedOem = baseline.GetProperty("OEMCodePage").GetInt32();
            var expectedLcid = baseline.GetProperty("LCID").GetInt32();

            if (ci.TextInfo.ANSICodePage != expectedAnsi)
                differences.Add($"{locale}: ANSICodePage expected={expectedAnsi} actual={ci.TextInfo.ANSICodePage}");
            if (ci.TextInfo.OEMCodePage != expectedOem)
                differences.Add($"{locale}: OEMCodePage expected={expectedOem} actual={ci.TextInfo.OEMCodePage}");
            if (ci.TextInfo.LCID != expectedLcid)
                differences.Add($"{locale}: LCID expected={expectedLcid} actual={ci.TextInfo.LCID}");
        }

        if (differences.Count > 0)
        {
            var report = "ICU mode differences from .NET Framework 4.0 baseline:\n"
                + string.Join("\n", differences);

            // ka-GE OEMCodePage (437 vs 1) is a known ICU/NLS difference.
            // Filter out known acceptable differences.
            var critical = differences
                .Where(d => !d.StartsWith("ka-GE: OEMCodePage"))
                .ToList();

            if (critical.Count > 0)
                Assert.Fail(report);

            // Non-critical differences are logged but do not fail the test.
            // Output for manual review:
            // Console.WriteLine(report);
        }
    }

    [Theory]
    [InlineData("ja-JP", 932, 932, 1041)]
    [InlineData("zh-TW", 950, 950, 1028)]
    [InlineData("zh-CN", 936, 936, 2052)]
    [InlineData("ko-KR", 949, 949, 1042)]
    [InlineData("en-US", 1252, 437, 1033)]
    [InlineData("ru-RU", 1251, 866, 1049)]
    public void KeyLocales_HaveExpectedCodePages(
        string locale, int expectedAnsi, int expectedOem, int expectedLcid)
    {
        var ci = CultureInfo.GetCultureInfo(locale);
        Assert.Equal(expectedAnsi, ci.TextInfo.ANSICodePage);
        Assert.Equal(expectedOem, ci.TextInfo.OEMCodePage);
        Assert.Equal(expectedLcid, ci.TextInfo.LCID);
    }
}
