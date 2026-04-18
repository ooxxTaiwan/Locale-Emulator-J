#nullable disable

using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;

namespace LEGUI;

internal class I18n
{
    internal static readonly CultureInfo CurrentCultureInfo = CultureInfo.CurrentUICulture;

    private static ResourceDictionary cacheDictionary;

    internal static string GetString(string key)
    {
        try
        {
            var dict = LoadDictionary();
            var s = (string)dict[key];

            if (string.IsNullOrEmpty(s))
                return key;

            return s;
        }
        catch
        {
            return key;
        }
    }

    private static ResourceDictionary LoadDictionary()
    {
        if (cacheDictionary != null)
            return cacheDictionary;

        ResourceDictionary dictionary = null;
        try
        {
            var langDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Lang\");

            var firstLangPath = Path.Combine(langDir, CurrentCultureInfo.Name + ".xaml");
            var fallbackLangPath = Path.Combine(langDir,
                                                $@"{CurrentCultureInfo.TwoLetterISOLanguageName}.xaml");

            // Append (not Insert at 0): WPF MergedDictionaries lookup walks in
            // reverse index order, so the last-added dictionary takes priority.
            // We want the locale dictionary to win over DefaultLanguage.xaml.
            if (File.Exists(firstLangPath))
            {
                using var stream = new FileStream(firstLangPath, FileMode.Open);
                Application.Current.Resources.MergedDictionaries
                           .Add(XamlReader.Load(stream) as ResourceDictionary);
            }
            else if (File.Exists(fallbackLangPath))
            {
                using var stream = new FileStream(fallbackLangPath, FileMode.Open);
                Application.Current.Resources.MergedDictionaries
                           .Add(XamlReader.Load(stream) as ResourceDictionary);
            }
        }
        catch
        {
        }

        // Pick the highest-priority dictionary (last in MergedDictionaries — locale if
        // available, otherwise DefaultLanguage). This cached reference is used by
        // GetString for direct key lookup; DynamicResource lookups on UI elements
        // walk the whole MergedDictionaries chain on their own.
        var merged = Application.Current.Resources.MergedDictionaries;
        if (merged.Count == 0)
            throw new Exception("No language file.");
        dictionary = merged[merged.Count - 1];

        cacheDictionary = dictionary;

        return cacheDictionary;
    }

    internal static void LoadLanguage()
    {
        // Eagerly trigger the locale dictionary load so UI elements bound with
        // {DynamicResource ...} resolve correctly on first render. We no longer
        // replace MergedDictionaries wholesale — DefaultLanguage.xaml (declared in
        // App.xaml) must remain as fallback so keys present only in the default
        // dictionary still resolve when a locale file lacks them.
        LoadDictionary();
    }
}
