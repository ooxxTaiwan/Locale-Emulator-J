using System.IO;
using System.Xml.Linq;
using Xunit;

namespace LEGUI.Tests;

public class LocaleCompletenessTests
{
    private static readonly string LangDir =
        Path.Combine(
            Path.GetDirectoryName(typeof(LocaleCompletenessTests).Assembly.Location)!,
            "Lang");

    private static readonly XNamespace SystemNs =
        "clr-namespace:System;assembly=System.Runtime";
    private static readonly XNamespace XamlNs =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    private static readonly Lazy<HashSet<string>> DefaultKeys =
        new(() => LoadKeys("DefaultLanguage.xaml"));

    public static IEnumerable<object[]> LocaleFiles =>
        Directory.GetFiles(LangDir, "*.xaml")
                 .Where(f => Path.GetFileName(f) != "DefaultLanguage.xaml")
                 .Select(f => new object[] { Path.GetFileName(f) });

    [Theory]
    [MemberData(nameof(LocaleFiles))]
    public void Locale_HasAllKeysFromDefaultLanguage(string localeFileName)
    {
        var localeKeys = LoadKeys(localeFileName);
        var missing    = DefaultKeys.Value.Except(localeKeys).OrderBy(k => k).ToList();

        Assert.True(
            missing.Count == 0,
            $"{localeFileName} is missing {missing.Count} key(s) from DefaultLanguage.xaml: " +
            string.Join(", ", missing));
    }

    [Theory]
    [MemberData(nameof(LocaleFiles))]
    public void Locale_HasNoEmptyValues(string localeFileName)
    {
        var emptyKeys = LoadKeyValues(localeFileName)
                       .Where(kv => string.IsNullOrWhiteSpace(kv.Value))
                       .Select(kv => kv.Key)
                       .OrderBy(k => k)
                       .ToList();

        Assert.True(
            emptyKeys.Count == 0,
            $"{localeFileName} has empty/whitespace value(s) for: " +
            string.Join(", ", emptyKeys));
    }

    private static HashSet<string> LoadKeys(string fileName) =>
        LoadKeyValues(fileName).Select(kv => kv.Key).ToHashSet();

    private static IEnumerable<KeyValuePair<string, string>> LoadKeyValues(string fileName)
    {
        var doc = XDocument.Load(Path.Combine(LangDir, fileName));
        return doc.Descendants(SystemNs + "String")
                  .Select(e => new KeyValuePair<string, string>(
                      (string)e.Attribute(XamlNs + "Key")!,
                      e.Value));
    }
}
