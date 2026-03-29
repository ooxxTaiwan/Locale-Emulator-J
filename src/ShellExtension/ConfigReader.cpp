// src/ShellExtension/ConfigReader.cpp
#include "ConfigReader.h"
#include "StringUtils.h"
#include "pugixml/pugixml.hpp"
#include <shlwapi.h>
#include <fstream>
#include <sstream>
#include <cstring>
#include <mutex>

#pragma comment(lib, "shlwapi.lib")

// Forward declaration of the global HMODULE set in dllmain.cpp
extern HMODULE g_hModule;

std::wstring ConfigReader::GetModuleDirectory()
{
    static std::wstring s_cachedDir;
    static std::once_flag s_dirFlag;
    std::call_once(s_dirFlag, [&]() {
        wchar_t path[MAX_PATH] = {};
        GetModuleFileNameW(g_hModule, path, MAX_PATH);
        PathRemoveFileSpecW(path);
        s_cachedDir = std::wstring(path) + L"\\";
    });
    return s_cachedDir;
}

bool ConfigReader::ParseBool(const char* value, bool defaultValue)
{
    if (!value || value[0] == '\0')
        return defaultValue;

    if (_stricmp(value, "true") == 0)
        return true;
    if (_stricmp(value, "false") == 0)
        return false;

    return defaultValue;
}

// Thread-safe profile cache.
// Shell Extension runs in-process in Explorer.exe which may create
// ContextMenuHandler instances on different threads.
static std::mutex s_cacheMutex;
static std::vector<LEProfile> s_cachedProfiles;
static FILETIME s_cachedWriteTime = {};
static bool s_cached = false;

std::vector<LEProfile> ConfigReader::LoadProfiles()
{
    std::wstring configPath = GetModuleDirectory() + L"LEConfig.xml";

    // Check file modification time
    WIN32_FILE_ATTRIBUTE_DATA fileInfo = {};
    if (!GetFileAttributesExW(configPath.c_str(), GetFileExInfoStandard, &fileInfo))
    {
        // File doesn't exist or error — return empty, don't cache failure
        return {};
    }

    std::lock_guard<std::mutex> lock(s_cacheMutex);

    // Double-check after acquiring lock
    if (s_cached &&
        fileInfo.ftLastWriteTime.dwHighDateTime == s_cachedWriteTime.dwHighDateTime &&
        fileInfo.ftLastWriteTime.dwLowDateTime == s_cachedWriteTime.dwLowDateTime)
    {
        return s_cachedProfiles;
    }

    // Parse XML and always cache the result (including empty).
    // Empty is a valid state (user removed all profiles).
    // File-not-found is handled above (returns before lock, no cache update).
    s_cachedProfiles = LoadProfiles(configPath);
    s_cachedWriteTime = fileInfo.ftLastWriteTime;
    s_cached = true;

    return s_cachedProfiles;
}

std::vector<LEProfile> ConfigReader::LoadProfiles(const std::wstring& configPath)
{
    std::vector<LEProfile> profiles;

    pugi::xml_document doc;
    pugi::xml_parse_result result = doc.load_file(configPath.c_str());
    if (!result)
        return profiles;

    pugi::xml_node leConfig = doc.child("LEConfig");
    if (!leConfig)
        return profiles;

    pugi::xml_node profilesNode = leConfig.child("Profiles");
    if (!profilesNode)
        return profiles;

    for (pugi::xml_node profileNode : profilesNode.children("Profile"))
    {
        LEProfile profile;

        // Read attributes
        profile.name = StringUtils::Utf8ToWide(profileNode.attribute("Name").as_string());
        profile.guid = StringUtils::Utf8ToWide(profileNode.attribute("Guid").as_string());
        profile.showInMainMenu = ParseBool(
            profileNode.attribute("MainMenu").as_string(), false);

        // Read child elements
        profile.parameter = StringUtils::Utf8ToWide(
            profileNode.child_value("Parameter"));
        profile.location = StringUtils::Utf8ToWide(
            profileNode.child_value("Location"));
        profile.timezone = StringUtils::Utf8ToWide(
            profileNode.child_value("Timezone"));
        profile.runAsAdmin = ParseBool(
            profileNode.child_value("RunAsAdmin"), false);
        profile.redirectRegistry = ParseBool(
            profileNode.child_value("RedirectRegistry"), true);
        profile.isAdvancedRedirection = ParseBool(
            profileNode.child_value("IsAdvancedRedirection"), false);
        profile.runWithSuspend = ParseBool(
            profileNode.child_value("RunWithSuspend"), false);

        profiles.push_back(std::move(profile));
    }

    return profiles;
}

void ConfigReader::EnsureConfigExists()
{
    std::wstring configPath = GetModuleDirectory() + L"LEConfig.xml";

    if (GetFileAttributesW(configPath.c_str()) != INVALID_FILE_ATTRIBUTES)
        return; // File already exists

    WriteDefaultConfig(configPath);
}

void ConfigReader::WriteDefaultConfig(const std::wstring& path)
{
    // Generate two GUIDs for default profiles
    GUID guid1, guid2;
    CoCreateGuid(&guid1);
    CoCreateGuid(&guid2);

    wchar_t guidStr1[40] = {};
    wchar_t guidStr2[40] = {};
    StringFromGUID2(guid1, guidStr1, 40);
    StringFromGUID2(guid2, guidStr2, 40);

    // Strip braces from GUID strings: {xxxx} -> xxxx
    std::wstring g1(guidStr1 + 1, wcslen(guidStr1) - 2);
    std::wstring g2(guidStr2 + 1, wcslen(guidStr2) - 2);

    pugi::xml_document doc;
    auto decl = doc.append_child(pugi::node_declaration);
    decl.append_attribute("version") = "1.0";
    decl.append_attribute("encoding") = "utf-8";

    auto leConfig = doc.append_child("LEConfig");
    auto profilesNode = leConfig.append_child("Profiles");

    // Profile 1: Run in Japanese
    {
        auto p = profilesNode.append_child("Profile");
        p.append_attribute("Name") = "Run in Japanese";

        // Convert wide GUID to narrow string for pugixml (GUIDs are ASCII-safe)
        std::string g1Utf8;
        g1Utf8.reserve(g1.size());
        for (wchar_t ch : g1)
            g1Utf8.push_back(static_cast<char>(ch));
        p.append_attribute("Guid") = g1Utf8.c_str();
        p.append_attribute("MainMenu") = "False";

        p.append_child("Parameter").text().set("");
        p.append_child("Location").text().set("ja-JP");
        p.append_child("Timezone").text().set("Tokyo Standard Time");
        p.append_child("RunAsAdmin").text().set("False");
        p.append_child("RedirectRegistry").text().set("True");
        p.append_child("IsAdvancedRedirection").text().set("False");
        p.append_child("RunWithSuspend").text().set("False");
    }

    // Profile 2: Run in Japanese (Admin)
    {
        auto p = profilesNode.append_child("Profile");
        p.append_attribute("Name") = "Run in Japanese (Admin)";

        std::string g2Utf8;
        g2Utf8.reserve(g2.size());
        for (wchar_t ch : g2)
            g2Utf8.push_back(static_cast<char>(ch));
        p.append_attribute("Guid") = g2Utf8.c_str();
        p.append_attribute("MainMenu") = "False";

        p.append_child("Parameter").text().set("");
        p.append_child("Location").text().set("ja-JP");
        p.append_child("Timezone").text().set("Tokyo Standard Time");
        p.append_child("RunAsAdmin").text().set("True");
        p.append_child("RedirectRegistry").text().set("True");
        p.append_child("IsAdvancedRedirection").text().set("False");
        p.append_child("RunWithSuspend").text().set("False");
    }

    doc.save_file(path.c_str());

    // Invalidate cache so next LoadProfiles() re-reads
    s_cached = false;
}
