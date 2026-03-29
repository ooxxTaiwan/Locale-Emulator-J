// src/ShellExtension/I18n.h
#pragma once

#include <string>
#include <unordered_map>
#include <mutex>

class I18n
{
public:
    // Gets a localized string by key. Returns the key itself on failure.
    static std::wstring GetString(const std::wstring& key);

    // Forces reload of the language dictionary (useful if DLL directory changes).
    static void Reset();

private:
    static void LoadDictionary();
    static std::wstring GetLangDirectory();
    static std::wstring DetectLanguageCode();

    static std::mutex s_mutex;
    static bool s_loaded;
    static std::unordered_map<std::wstring, std::wstring> s_dictionary;
};
