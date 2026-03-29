// src/ShellExtension/ConfigReader.h
#pragma once

#include <string>
#include <vector>
#include <optional>
#include <windows.h>

struct LEProfile
{
    std::wstring name;
    std::wstring guid;
    bool showInMainMenu = false;
    std::wstring parameter;
    std::wstring location;
    std::wstring timezone;
    bool runAsAdmin = false;
    bool redirectRegistry = true;
    bool isAdvancedRedirection = false;
    bool runWithSuspend = false;
};

class ConfigReader
{
public:
    // Loads profiles from LEConfig.xml located next to this DLL.
    // Returns empty vector on any failure.
    static std::vector<LEProfile> LoadProfiles();

    // Loads profiles from a specific config file path.
    static std::vector<LEProfile> LoadProfiles(const std::wstring& configPath);

    // Gets the directory where this DLL is located.
    static std::wstring GetModuleDirectory();

    // Ensures a default LEConfig.xml exists; creates one if missing.
    static void EnsureConfigExists();

private:
    static bool ParseBool(const char* value, bool defaultValue);
    static void WriteDefaultConfig(const std::wstring& path);
};
