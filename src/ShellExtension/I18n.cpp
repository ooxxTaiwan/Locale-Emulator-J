// src/ShellExtension/I18n.cpp
#include "I18n.h"
#include "ConfigReader.h" // for GetModuleDirectory()
#include "pugixml/pugixml.hpp"
#include <windows.h>
#include <shlwapi.h>

#pragma comment(lib, "shlwapi.lib")

bool I18n::s_loaded = false;
std::unordered_map<std::wstring, std::wstring> I18n::s_dictionary;

std::wstring I18n::Utf8ToWide(const std::string& utf8)
{
    if (utf8.empty())
        return {};

    int len = MultiByteToWideChar(CP_UTF8, 0, utf8.c_str(),
                                  static_cast<int>(utf8.size()), nullptr, 0);
    if (len <= 0)
        return {};

    std::wstring wide(len, L'\0');
    MultiByteToWideChar(CP_UTF8, 0, utf8.c_str(),
                        static_cast<int>(utf8.size()), &wide[0], len);
    return wide;
}

std::wstring I18n::GetLangDirectory()
{
    return ConfigReader::GetModuleDirectory() + L"Lang\\";
}

std::wstring I18n::DetectLanguageCode()
{
    // Use GetUserDefaultUILanguage() as specified in the design doc.
    LANGID langId = GetUserDefaultUILanguage();

    wchar_t localeName[LOCALE_NAME_MAX_LENGTH] = {};
    if (LCIDToLocaleName(MAKELCID(langId, SORT_DEFAULT),
                         localeName, LOCALE_NAME_MAX_LENGTH, 0) == 0)
    {
        return L"en"; // fallback
    }

    return std::wstring(localeName);
}

void I18n::LoadDictionary()
{
    if (s_loaded)
        return;

    s_loaded = true;
    s_dictionary.clear();

    std::wstring langDir = GetLangDirectory();
    std::wstring langCode = DetectLanguageCode();

    // Try full locale name first (e.g., "zh-TW.xml")
    std::wstring primaryPath = langDir + langCode + L".xml";

    // Try two-letter fallback (e.g., "zh.xml")
    std::wstring fallbackCode = langCode;
    auto dashPos = fallbackCode.find(L'-');
    if (dashPos != std::wstring::npos)
        fallbackCode = fallbackCode.substr(0, dashPos);
    std::wstring fallbackPath = langDir + fallbackCode + L".xml";

    // Default language path
    std::wstring defaultPath = langDir + L"DefaultLanguage.xml";

    // Try loading in order: primary -> fallback -> default
    pugi::xml_document doc;
    pugi::xml_parse_result result;

    if (GetFileAttributesW(primaryPath.c_str()) != INVALID_FILE_ATTRIBUTES)
    {
        result = doc.load_file(primaryPath.c_str());
    }

    if (!result && GetFileAttributesW(fallbackPath.c_str()) != INVALID_FILE_ATTRIBUTES)
    {
        result = doc.load_file(fallbackPath.c_str());
    }

    if (!result)
    {
        result = doc.load_file(defaultPath.c_str());
    }

    if (!result)
        return; // All loading attempts failed

    // Parse <Strings> element: each child element's tag name is the key,
    // its text content is the value.
    pugi::xml_node strings = doc.child("Strings");
    if (!strings)
        return;

    for (pugi::xml_node child : strings.children())
    {
        std::wstring key = Utf8ToWide(child.name());
        std::wstring value = Utf8ToWide(child.child_value());
        if (!key.empty())
            s_dictionary[key] = value;
    }
}

std::wstring I18n::GetString(const std::wstring& key)
{
    LoadDictionary();

    auto it = s_dictionary.find(key);
    if (it != s_dictionary.end() && !it->second.empty())
        return it->second;

    return key; // Return key as fallback
}

void I18n::Reset()
{
    s_loaded = false;
    s_dictionary.clear();
}
