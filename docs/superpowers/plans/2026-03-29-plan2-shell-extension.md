# Plan 2: C++ Shell Extension — 原生 COM 右鍵選單

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 用原生 C++ 重寫 Shell Extension，取代現有的 .NET COM DLL，提供 x86+x64 雙版本

**Architecture:** 標準 COM DLL (IShellExtInit + IContextMenu)，使用 pugixml 解析 XML config，支援 22 語言 i18n

**Tech Stack:** C++17, MSVC v145, COM, pugixml, Win32 API

**Branch:** `feature/dotnet10-shell-ext`

**PR:** Part of ooxxTaiwan/Locale-Emulator-J#1

**Depends on:** Plan 1 merged

---

## 前置資訊

### 新 COM GUID

```
{A8B4F5C2-7E3D-4F1A-9C6B-2D8E0F5A3B71}
```

此 GUID 與舊版 `{C52B9871-E5E9-41FD-B84D-C5ACADBEC7AE}` 不同，避免新舊版本註冊衝突。

### DllRegisterServer 職責邊界

- `DllRegisterServer` **僅**處理 `HKCR\CLSID\{GUID}\InprocServer32`（標準 COM 自註冊慣例）
- `ContextMenuHandlers` 註冊由 LEGUI 的 `ShellExtensionRegistrar` 處理（Plan 3）
- `DllRegisterServer` 是備用方案，供 CLI 使用者手動呼叫 `regsvr32`

### 語言檔案清單（22 檔 + DefaultLanguage.xml）

```
DefaultLanguage.xml, ca.xml, cs.xml, de.xml, es.xml, fr.xml, ind.xml,
it.xml, ja.xml, ka.xml, ko.xml, lt.xml, nb.xml, nl.xml, pl.xml,
pt-BR.xml, ru.xml, th.xml, tr-TR.xml, zh-CN.xml, zh-HK.xml, zh-TW.xml
```

### 點陣圖資源（4 色 x 2 解析度 = 8 檔）

```
purple.bmp / purple@200.bmp  → 使用者自訂 Profile
gray.bmp   / gray@200.bmp    → 管理此應用程式
blue.bmp   / blue@200.bmp    → 管理所有設定
yellow.bmp / yellow@200.bmp  → 子選單標題 / 以預設設定執行
```

### 選單結構

```
[separator]
Locale Emulator (yellow, 子選單) ──┐
  以預設設定執行 (yellow)           │
  [separator]                      │
  使用者 Profile A (purple)        │ ← ShowInMainMenu=false 的 Profile
  使用者 Profile B (purple)        │
  [separator]                      │
  管理此應用程式 (gray)             │
  管理所有設定 (blue)               │
                                   │
使用者 Profile C (purple)  ←───────┘   ShowInMainMenu=true 的 Profile 出現在主選單
```

---

## Step 1：建立專案目錄結構

- [ ] 建立 `src/ShellExtension/` 目錄及子目錄

```bash
mkdir -p src/ShellExtension/pugixml
mkdir -p src/ShellExtension/Lang
mkdir -p src/ShellExtension/Resources
```

**驗證**：目錄結構存在。

---

## Step 2：Vendor pugixml

- [ ] 下載 pugixml 1.14（MIT 授權）的 `pugixml.hpp` 與 `pugixml.cpp` 至 `src/ShellExtension/pugixml/`

```bash
# 從 GitHub release 下載
curl -L -o src/ShellExtension/pugixml/pugixml.hpp \
  "https://raw.githubusercontent.com/zeux/pugixml/v1.14/src/pugixml.hpp"
curl -L -o src/ShellExtension/pugixml/pugixml.cpp \
  "https://raw.githubusercontent.com/zeux/pugixml/v1.14/src/pugixml.cpp"
curl -L -o src/ShellExtension/pugixml/pugiconfig.hpp \
  "https://raw.githubusercontent.com/zeux/pugixml/v1.14/src/pugiconfig.hpp"
```

- [ ] 建立 `src/ShellExtension/pugixml/LICENSE.md`

```markdown
# pugixml License

pugixml is Copyright (C) 2006-2024 Arseny Kapoulkine.

Licensed under the MIT License.
See https://github.com/zeux/pugixml/blob/master/LICENSE.md
```

**驗證**：三個 pugixml 檔案存在且非空。

---

## Step 3：複製語言檔案

- [ ] 從 `src/LEContextMenuHandler/Lang/` 複製所有 `.xml` 到 `src/ShellExtension/Lang/`

```bash
cp src/LEContextMenuHandler/Lang/*.xml src/ShellExtension/Lang/
```

**驗證**：`src/ShellExtension/Lang/` 包含 23 個 XML 檔案（含 DefaultLanguage.xml）。

---

## Step 4：複製點陣圖資源

- [ ] 從 `src/LEContextMenuHandler/Resources/` 複製所有 `.bmp` 到 `src/ShellExtension/Resources/`

```bash
cp src/LEContextMenuHandler/Resources/*.bmp src/ShellExtension/Resources/
```

**驗證**：`src/ShellExtension/Resources/` 包含 8 個 BMP 檔案。

---

## Step 5：建立 Resource.h

- [ ] 建立 `src/ShellExtension/Resource.h`

```cpp
// src/ShellExtension/Resource.h
#pragma once

// Bitmap resource IDs
#define IDB_PURPLE          101
#define IDB_PURPLE_200      102
#define IDB_GRAY            103
#define IDB_GRAY_200        104
#define IDB_BLUE            105
#define IDB_BLUE_200        106
#define IDB_YELLOW          107
#define IDB_YELLOW_200      108
```

**驗證**：檔案存在，8 個資源 ID 定義。

---

## Step 6：建立 Resource.rc

- [ ] 建立 `src/ShellExtension/Resource.rc`

```rc
// src/ShellExtension/Resource.rc
#include "Resource.h"

IDB_PURPLE      BITMAP  "Resources\\purple.bmp"
IDB_PURPLE_200  BITMAP  "Resources\\purple@200.bmp"
IDB_GRAY        BITMAP  "Resources\\gray.bmp"
IDB_GRAY_200    BITMAP  "Resources\\gray@200.bmp"
IDB_BLUE        BITMAP  "Resources\\blue.bmp"
IDB_BLUE_200    BITMAP  "Resources\\blue@200.bmp"
IDB_YELLOW      BITMAP  "Resources\\yellow.bmp"
IDB_YELLOW_200  BITMAP  "Resources\\yellow@200.bmp"
```

**驗證**：檔案存在，8 個 BITMAP 資源宣告。

---

## Step 7：建立 ShellExtension.def

- [ ] 建立 `src/ShellExtension/ShellExtension.def`

```def
; src/ShellExtension/ShellExtension.def
LIBRARY "ShellExtension"

EXPORTS
    DllGetClassObject   PRIVATE
    DllCanUnloadNow     PRIVATE
    DllRegisterServer   PRIVATE
    DllUnregisterServer PRIVATE
```

**驗證**：檔案存在，4 個匯出函式。

---

## Step 8：實作 ConfigReader

- [ ] 建立 `src/ShellExtension/ConfigReader.h`

```cpp
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
    static std::wstring Utf8ToWide(const std::string& utf8);
    static void WriteDefaultConfig(const std::wstring& path);
};
```

- [ ] 建立 `src/ShellExtension/ConfigReader.cpp`

```cpp
// src/ShellExtension/ConfigReader.cpp
#include "ConfigReader.h"
#include "pugixml/pugixml.hpp"
#include <shlwapi.h>
#include <fstream>
#include <sstream>
#include <cstring>

#pragma comment(lib, "shlwapi.lib")

// Forward declaration of the global HMODULE set in dllmain.cpp
extern HMODULE g_hModule;

std::wstring ConfigReader::GetModuleDirectory()
{
    wchar_t path[MAX_PATH] = {};
    GetModuleFileNameW(g_hModule, path, MAX_PATH);
    PathRemoveFileSpecW(path);
    return std::wstring(path) + L"\\";
}

std::wstring ConfigReader::Utf8ToWide(const std::string& utf8)
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

bool ConfigReader::ParseBool(const char* value, bool defaultValue)
{
    if (!value || value[0] == '\0')
        return defaultValue;

    // Case-insensitive comparison
    if (_stricmp(value, "true") == 0 || _stricmp(value, "True") == 0)
        return true;
    if (_stricmp(value, "false") == 0 || _stricmp(value, "False") == 0)
        return false;

    return defaultValue;
}

std::vector<LEProfile> ConfigReader::LoadProfiles()
{
    std::wstring configPath = GetModuleDirectory() + L"LEConfig.xml";
    return LoadProfiles(configPath);
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
        profile.name = Utf8ToWide(profileNode.attribute("Name").as_string());
        profile.guid = Utf8ToWide(profileNode.attribute("Guid").as_string());
        profile.showInMainMenu = ParseBool(
            profileNode.attribute("MainMenu").as_string(), false);

        // Read child elements
        profile.parameter = Utf8ToWide(
            profileNode.child_value("Parameter"));
        profile.location = Utf8ToWide(
            profileNode.child_value("Location"));
        profile.timezone = Utf8ToWide(
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

        // Convert wide GUID to UTF-8 for pugixml
        std::string g1Utf8(g1.begin(), g1.end()); // ASCII-safe for GUIDs
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

        std::string g2Utf8(g2.begin(), g2.end());
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
}
```

**驗證**：編譯無誤（在 Step 12 整體驗證）。

---

## Step 9：實作 I18n

- [ ] 建立 `src/ShellExtension/I18n.h`

```cpp
// src/ShellExtension/I18n.h
#pragma once

#include <string>
#include <unordered_map>

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
    static std::wstring Utf8ToWide(const std::string& utf8);

    static bool s_loaded;
    static std::unordered_map<std::wstring, std::wstring> s_dictionary;
};
```

- [ ] 建立 `src/ShellExtension/I18n.cpp`

```cpp
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
```

**驗證**：編譯無誤（在 Step 12 整體驗證）。

---

## Step 10：實作 ClassFactory

- [ ] 建立 `src/ShellExtension/ClassFactory.h`

```cpp
// src/ShellExtension/ClassFactory.h
#pragma once

#include <unknwn.h>
#include <windows.h>

class ClassFactory : public IClassFactory
{
public:
    // IUnknown
    IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv) override;
    IFACEMETHODIMP_(ULONG) AddRef() override;
    IFACEMETHODIMP_(ULONG) Release() override;

    // IClassFactory
    IFACEMETHODIMP CreateInstance(IUnknown* pUnkOuter, REFIID riid, void** ppv) override;
    IFACEMETHODIMP LockServer(BOOL fLock) override;

    ClassFactory();

private:
    ~ClassFactory();
    LONG m_refCount;
};
```

- [ ] 建立 `src/ShellExtension/ClassFactory.cpp`

```cpp
// src/ShellExtension/ClassFactory.cpp
#include "ClassFactory.h"
#include "ContextMenuHandler.h"
#include <new>

extern LONG g_dllRefCount;

ClassFactory::ClassFactory()
    : m_refCount(1)
{
    InterlockedIncrement(&g_dllRefCount);
}

ClassFactory::~ClassFactory()
{
    InterlockedDecrement(&g_dllRefCount);
}

// IUnknown

IFACEMETHODIMP ClassFactory::QueryInterface(REFIID riid, void** ppv)
{
    static const QITAB qit[] =
    {
        QITABENT(ClassFactory, IClassFactory),
        { nullptr, 0 },
    };
    return QISearch(this, qit, riid, ppv);
}

IFACEMETHODIMP_(ULONG) ClassFactory::AddRef()
{
    return InterlockedIncrement(&m_refCount);
}

IFACEMETHODIMP_(ULONG) ClassFactory::Release()
{
    ULONG refCount = InterlockedDecrement(&m_refCount);
    if (refCount == 0)
        delete this;
    return refCount;
}

// IClassFactory

IFACEMETHODIMP ClassFactory::CreateInstance(IUnknown* pUnkOuter, REFIID riid, void** ppv)
{
    if (pUnkOuter != nullptr)
        return CLASS_E_NOAGGREGATION;

    auto* handler = new (std::nothrow) ContextMenuHandler();
    if (!handler)
        return E_OUTOFMEMORY;

    HRESULT hr = handler->QueryInterface(riid, ppv);
    handler->Release(); // QI already AddRef'd if successful
    return hr;
}

IFACEMETHODIMP ClassFactory::LockServer(BOOL fLock)
{
    if (fLock)
        InterlockedIncrement(&g_dllRefCount);
    else
        InterlockedDecrement(&g_dllRefCount);
    return S_OK;
}
```

**驗證**：編譯無誤（在 Step 12 整體驗證）。

---

## Step 11：實作 ContextMenuHandler

這是最核心的檔案，實作 `IShellExtInit` + `IContextMenu`。

- [ ] 建立 `src/ShellExtension/ContextMenuHandler.h`

```cpp
// src/ShellExtension/ContextMenuHandler.h
#pragma once

#include <windows.h>
#include <shlobj.h>
#include <string>
#include <vector>

struct MenuItem
{
    std::wstring text;
    bool enabled = true;
    // nullopt = always submenu; true = main menu; false = submenu
    // We use int: -1 = submenu, 0 = false (submenu), 1 = true (main menu)
    int showInMainMenu = -1;
    HBITMAP bitmap = nullptr;
    std::wstring commands;
};

class ContextMenuHandler : public IShellExtInit, public IContextMenu
{
public:
    ContextMenuHandler();

    // IUnknown
    IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv) override;
    IFACEMETHODIMP_(ULONG) AddRef() override;
    IFACEMETHODIMP_(ULONG) Release() override;

    // IShellExtInit
    IFACEMETHODIMP Initialize(LPCITEMIDLIST pidlFolder,
                              IDataObject* pdtobj,
                              HKEY hkeyProgID) override;

    // IContextMenu
    IFACEMETHODIMP QueryContextMenu(HMENU hMenu, UINT indexMenu,
                                    UINT idCmdFirst, UINT idCmdLast,
                                    UINT uFlags) override;
    IFACEMETHODIMP InvokeCommand(LPCMINVOKECOMMANDINFO pici) override;
    IFACEMETHODIMP GetCommandString(UINT_PTR idCmd, UINT uFlags,
                                    UINT* pwReserved, LPSTR pszName,
                                    UINT cchMax) override;

private:
    ~ContextMenuHandler();

    // PE header reading for architecture detection
    enum class PEType { X32, X64, Unknown };
    static PEType GetPEType(const std::wstring& path);

    // 4K display detection
    static bool Is4KDisplay();

    // Load a bitmap from resource, choosing @200 variant for 4K
    HBITMAP LoadMenuBitmap(int normalId, int hiDpiId);

    // Insert a menu item
    HRESULT InsertMenuItemHelper(HMENU hMenu, UINT position,
                                 UINT id, const std::wstring& text,
                                 bool enabled, HBITMAP bitmap,
                                 HMENU subMenu);

    // Launch LEProc.exe with the given command
    void LaunchLEProc(const std::wstring& command);

    LONG m_refCount;
    std::wstring m_selectedFile;
    std::vector<MenuItem> m_menuItems;

    HBITMAP m_bmpPurple = nullptr;
    HBITMAP m_bmpGray = nullptr;
    HBITMAP m_bmpBlue = nullptr;
    HBITMAP m_bmpYellow = nullptr;
};
```

- [ ] 建立 `src/ShellExtension/ContextMenuHandler.cpp`

```cpp
// src/ShellExtension/ContextMenuHandler.cpp
#include "ContextMenuHandler.h"
#include "ConfigReader.h"
#include "I18n.h"
#include "Resource.h"
#include <shlwapi.h>
#include <shellapi.h>
#include <strsafe.h>

#pragma comment(lib, "shlwapi.lib")

extern HMODULE g_hModule;
extern LONG g_dllRefCount;

// ========================================================================
// Constructor / Destructor
// ========================================================================

ContextMenuHandler::ContextMenuHandler()
    : m_refCount(1)
{
    InterlockedIncrement(&g_dllRefCount);

    // Load bitmaps based on DPI
    m_bmpPurple = LoadMenuBitmap(IDB_PURPLE, IDB_PURPLE_200);
    m_bmpGray   = LoadMenuBitmap(IDB_GRAY, IDB_GRAY_200);
    m_bmpBlue   = LoadMenuBitmap(IDB_BLUE, IDB_BLUE_200);
    m_bmpYellow = LoadMenuBitmap(IDB_YELLOW, IDB_YELLOW_200);

    // Build default menu items
    m_menuItems.push_back({I18n::GetString(L"Submenu"),
                           true, -1, m_bmpYellow, L""});
    m_menuItems.push_back({I18n::GetString(L"RunDefault"),
                           true, -1, m_bmpYellow, L"-run \"%APP%\""});
    m_menuItems.push_back({I18n::GetString(L"ManageApp"),
                           true, -1, m_bmpGray, L"-manage \"%APP%\""});
    m_menuItems.push_back({I18n::GetString(L"ManageAll"),
                           true, -1, m_bmpBlue, L"-global"});

    // Ensure config exists
    ConfigReader::EnsureConfigExists();

    // Load user-defined profiles
    auto profiles = ConfigReader::LoadProfiles();
    for (const auto& profile : profiles)
    {
        MenuItem item;
        item.text = profile.name;
        item.enabled = true;
        item.showInMainMenu = profile.showInMainMenu ? 1 : 0;
        item.bitmap = m_bmpPurple;
        item.commands = L"-runas \"" + profile.guid + L"\" \"%APP%\"";
        m_menuItems.push_back(std::move(item));
    }
}

ContextMenuHandler::~ContextMenuHandler()
{
    if (m_bmpPurple) DeleteObject(m_bmpPurple);
    if (m_bmpGray)   DeleteObject(m_bmpGray);
    if (m_bmpBlue)   DeleteObject(m_bmpBlue);
    if (m_bmpYellow) DeleteObject(m_bmpYellow);

    InterlockedDecrement(&g_dllRefCount);
}

// ========================================================================
// IUnknown
// ========================================================================

IFACEMETHODIMP ContextMenuHandler::QueryInterface(REFIID riid, void** ppv)
{
    static const QITAB qit[] =
    {
        QITABENT(ContextMenuHandler, IShellExtInit),
        QITABENT(ContextMenuHandler, IContextMenu),
        { nullptr, 0 },
    };
    return QISearch(this, qit, riid, ppv);
}

IFACEMETHODIMP_(ULONG) ContextMenuHandler::AddRef()
{
    return InterlockedIncrement(&m_refCount);
}

IFACEMETHODIMP_(ULONG) ContextMenuHandler::Release()
{
    ULONG refCount = InterlockedDecrement(&m_refCount);
    if (refCount == 0)
        delete this;
    return refCount;
}

// ========================================================================
// IShellExtInit
// ========================================================================

IFACEMETHODIMP ContextMenuHandler::Initialize(LPCITEMIDLIST /*pidlFolder*/,
                                              IDataObject* pdtobj,
                                              HKEY /*hkeyProgID*/)
{
    if (!pdtobj)
        return E_INVALIDARG;

    FORMATETC fe = { CF_HDROP, nullptr, DVASPECT_CONTENT, -1, TYMED_HGLOBAL };
    STGMEDIUM stm = {};

    HRESULT hr = pdtobj->GetData(&fe, &stm);
    if (FAILED(hr))
        return hr;

    HDROP hDrop = static_cast<HDROP>(GlobalLock(stm.hGlobal));
    if (!hDrop)
    {
        ReleaseStgMedium(&stm);
        return E_FAIL;
    }

    // Only handle single file selection
    UINT nFiles = DragQueryFileW(hDrop, 0xFFFFFFFF, nullptr, 0);
    if (nFiles != 1)
    {
        GlobalUnlock(stm.hGlobal);
        ReleaseStgMedium(&stm);
        return E_FAIL;
    }

    // Get the selected file path
    wchar_t fileName[MAX_PATH] = {};
    if (DragQueryFileW(hDrop, 0, fileName, MAX_PATH) == 0)
    {
        GlobalUnlock(stm.hGlobal);
        ReleaseStgMedium(&stm);
        return E_FAIL;
    }

    m_selectedFile = fileName;

    GlobalUnlock(stm.hGlobal);
    ReleaseStgMedium(&stm);

    // Check PE type of the target file
    std::wstring ext = PathFindExtensionW(fileName);
    // Convert extension to lowercase
    for (auto& ch : ext)
        ch = towlower(ch);

    std::wstring targetPath;
    if (ext == L".exe")
    {
        targetPath = m_selectedFile;
    }
    else
    {
        // For non-exe files, we still show the menu (user might have .le.config)
        // The original C# code tried to find associated programs, but for simplicity
        // and because LEProc handles the actual launching, we accept all files.
        targetPath = m_selectedFile;
    }

    // Read PE header to detect architecture.
    // For x64 targets, we still show the menu but with a warning,
    // as specified in the design doc: "display menu but note x86-only support,
    // or still allow attempting to launch (LEProc will report errors if unable to hook)"
    PEType peType = GetPEType(targetPath);
    if (peType == PEType::X64)
    {
        // Add a warning item at position 1 (after submenu title)
        MenuItem warning;
        warning.text = L"[x86 only]";
        warning.enabled = false;
        warning.showInMainMenu = -1;
        warning.bitmap = nullptr;
        warning.commands = L"";
        // Insert after the 4 default items
        if (m_menuItems.size() > 4)
            m_menuItems.insert(m_menuItems.begin() + 4, std::move(warning));
        else
            m_menuItems.push_back(std::move(warning));
    }

    return S_OK;
}

// ========================================================================
// IContextMenu
// ========================================================================

HRESULT ContextMenuHandler::InsertMenuItemHelper(HMENU hMenu, UINT position,
                                                  UINT id, const std::wstring& text,
                                                  bool enabled, HBITMAP bitmap,
                                                  HMENU subMenu)
{
    MENUITEMINFOW mii = {};
    mii.cbSize = sizeof(MENUITEMINFOW);

    UINT mask = MIIM_STRING | MIIM_FTYPE | MIIM_ID | MIIM_STATE;
    if (bitmap)
        mask |= MIIM_BITMAP;
    if (subMenu)
        mask |= MIIM_SUBMENU;
    mii.fMask = mask;

    mii.wID = id;
    mii.fType = MFT_STRING;
    mii.dwTypeData = const_cast<LPWSTR>(text.c_str());
    mii.hSubMenu = subMenu;
    mii.fState = enabled ? MFS_ENABLED : MFS_DISABLED;
    mii.hbmpItem = bitmap;

    if (!InsertMenuItemW(hMenu, position, TRUE, &mii))
        return HRESULT_FROM_WIN32(GetLastError());

    return S_OK;
}

IFACEMETHODIMP ContextMenuHandler::QueryContextMenu(HMENU hMenu, UINT indexMenu,
                                                     UINT idCmdFirst, UINT /*idCmdLast*/,
                                                     UINT uFlags)
{
    // If CMF_DEFAULTONLY is set, do nothing
    if (uFlags & CMF_DEFAULTONLY)
        return MAKE_HRESULT(SEVERITY_SUCCESS, 0, 0);

    // Add a separator before our items
    MENUITEMINFOW sep = {};
    sep.cbSize = sizeof(MENUITEMINFOW);
    sep.fMask = MIIM_TYPE;
    sep.fType = MFT_SEPARATOR;
    InsertMenuItemW(hMenu, indexMenu, TRUE, &sep);

    // Create submenu
    HMENU hSubMenu = CreatePopupMenu();
    if (!hSubMenu)
        return HRESULT_FROM_WIN32(GetLastError());

    // Item 0: Submenu title (inserted into main menu)
    InsertMenuItemHelper(hMenu, indexMenu + 1,
                         idCmdFirst + 0,
                         m_menuItems[0].text, true,
                         m_menuItems[0].bitmap, hSubMenu);

    // Item 1: RunDefault (first item in submenu)
    InsertMenuItemHelper(hSubMenu, 0,
                         idCmdFirst + 1,
                         m_menuItems[1].text, true,
                         m_menuItems[1].bitmap, nullptr);

    // Separator in submenu
    sep = {};
    sep.cbSize = sizeof(MENUITEMINFOW);
    sep.fMask = MIIM_TYPE;
    sep.fType = MFT_SEPARATOR;
    InsertMenuItemW(hSubMenu, 1, TRUE, &sep);

    // Item 2: ManageApp
    InsertMenuItemHelper(hSubMenu, 2,
                         idCmdFirst + 2,
                         m_menuItems[2].text, true,
                         m_menuItems[2].bitmap, nullptr);

    // Item 3: ManageAll
    InsertMenuItemHelper(hSubMenu, 3,
                         idCmdFirst + 3,
                         m_menuItems[3].text, true,
                         m_menuItems[3].bitmap, nullptr);

    // User-defined profiles: iterate in reverse order (matching original C# behavior)
    // Profiles with showInMainMenu=true go into the main menu (after submenu item)
    // Profiles with showInMainMenu=false/unset go into the submenu (before ManageApp)
    UINT mainMenuInsertPos = indexMenu + 2; // After the submenu item
    UINT subMenuProfilePos = 0; // Will insert before separator+ManageApp

    for (int i = static_cast<int>(m_menuItems.size()) - 1; i > 3; i--)
    {
        const auto& item = m_menuItems[i];
        UINT cmdId = idCmdFirst + static_cast<UINT>(i);

        if (item.showInMainMenu == 1) // true = main menu
        {
            InsertMenuItemHelper(hMenu, mainMenuInsertPos,
                                 cmdId, item.text,
                                 item.enabled, item.bitmap, nullptr);
        }
        else // false or default = submenu
        {
            InsertMenuItemHelper(hSubMenu, subMenuProfilePos,
                                 cmdId, item.text,
                                 item.enabled, item.bitmap, nullptr);
        }
    }

    // Add separator in submenu between profiles and ManageApp/ManageAll
    // Count items that went into submenu (non-main-menu items beyond index 3)
    UINT submenuProfileCount = 0;
    for (size_t i = 4; i < m_menuItems.size(); i++)
    {
        if (m_menuItems[i].showInMainMenu != 1)
            submenuProfileCount++;
    }

    if (submenuProfileCount > 0)
    {
        sep = {};
        sep.cbSize = sizeof(MENUITEMINFOW);
        sep.fMask = MIIM_TYPE;
        sep.fType = MFT_SEPARATOR;
        InsertMenuItemW(hSubMenu, submenuProfileCount, TRUE, &sep);
    }

    // Return the count of items added (largest command ID offset + 1)
    return MAKE_HRESULT(SEVERITY_SUCCESS, 0,
                        static_cast<USHORT>(m_menuItems.size()) + 3);
}

IFACEMETHODIMP ContextMenuHandler::InvokeCommand(LPCMINVOKECOMMANDINFO pici)
{
    // If the verb is passed as a string, we don't support it
    if (HIWORD(pici->lpVerb) != 0)
        return E_FAIL;

    UINT cmdIndex = LOWORD(pici->lpVerb);
    if (cmdIndex >= m_menuItems.size())
        return E_FAIL;

    const auto& item = m_menuItems[cmdIndex];
    if (item.commands.empty())
        return S_OK; // Submenu title or disabled item, do nothing

    LaunchLEProc(item.commands);
    return S_OK;
}

IFACEMETHODIMP ContextMenuHandler::GetCommandString(UINT_PTR /*idCmd*/,
                                                     UINT /*uFlags*/,
                                                     UINT* /*pwReserved*/,
                                                     LPSTR /*pszName*/,
                                                     UINT /*cchMax*/)
{
    return E_NOTIMPL;
}

// ========================================================================
// Helper methods
// ========================================================================

HBITMAP ContextMenuHandler::LoadMenuBitmap(int normalId, int hiDpiId)
{
    int resId = Is4KDisplay() ? hiDpiId : normalId;
    return static_cast<HBITMAP>(
        LoadImageW(g_hModule, MAKEINTRESOURCEW(resId),
                   IMAGE_BITMAP, 0, 0, LR_DEFAULTCOLOR));
}

bool ContextMenuHandler::Is4KDisplay()
{
    HDC hdc = GetDC(nullptr);
    if (!hdc)
        return false;

    int logicDpi = GetDeviceCaps(hdc, VERTRES);      // 10 = VERTRES
    int logY     = GetDeviceCaps(hdc, LOGPIXELSY);    // 90 = LOGPIXELSY
    int realDpi  = GetDeviceCaps(hdc, DESKTOPVERTRES); // 117 = DESKTOPVERTRES

    ReleaseDC(nullptr, hdc);

    return (logicDpi > 0 && realDpi / logicDpi == 2) || (logY / 96 == 2);
}

ContextMenuHandler::PEType ContextMenuHandler::GetPEType(const std::wstring& path)
{
    if (path.empty())
        return PEType::Unknown;

    HANDLE hFile = CreateFileW(path.c_str(), GENERIC_READ,
                               FILE_SHARE_READ | FILE_SHARE_WRITE,
                               nullptr, OPEN_EXISTING,
                               FILE_ATTRIBUTE_NORMAL, nullptr);
    if (hFile == INVALID_HANDLE_VALUE)
        return PEType::Unknown;

    // Read DOS header to check MZ signature
    WORD dosSignature = 0;
    DWORD bytesRead = 0;

    if (!ReadFile(hFile, &dosSignature, sizeof(dosSignature), &bytesRead, nullptr)
        || bytesRead != sizeof(dosSignature)
        || dosSignature != 0x5A4D) // "MZ"
    {
        CloseHandle(hFile);
        return PEType::Unknown;
    }

    // Seek to e_lfanew offset (0x3C)
    if (SetFilePointer(hFile, 0x3C, nullptr, FILE_BEGIN) == INVALID_SET_FILE_POINTER)
    {
        CloseHandle(hFile);
        return PEType::Unknown;
    }

    DWORD peOffset = 0;
    if (!ReadFile(hFile, &peOffset, sizeof(peOffset), &bytesRead, nullptr)
        || bytesRead != sizeof(peOffset))
    {
        CloseHandle(hFile);
        return PEType::Unknown;
    }

    // Seek past PE signature ("PE\0\0") to COFF header Machine field
    // PE signature is 4 bytes, Machine is the first 2 bytes of COFF header
    LONG machineOffset = static_cast<LONG>(peOffset) + 4;
    if (SetFilePointer(hFile, machineOffset, nullptr, FILE_BEGIN) == INVALID_SET_FILE_POINTER)
    {
        CloseHandle(hFile);
        return PEType::Unknown;
    }

    WORD machine = 0;
    if (!ReadFile(hFile, &machine, sizeof(machine), &bytesRead, nullptr)
        || bytesRead != sizeof(machine))
    {
        CloseHandle(hFile);
        return PEType::Unknown;
    }

    CloseHandle(hFile);

    if (machine == 0x014C) // IMAGE_FILE_MACHINE_I386
        return PEType::X32;
    if (machine == 0x8664) // IMAGE_FILE_MACHINE_AMD64
        return PEType::X64;

    return PEType::Unknown;
}

void ContextMenuHandler::LaunchLEProc(const std::wstring& command)
{
    // Build the full path to LEProc.exe (same directory as this DLL)
    std::wstring leProcPath = ConfigReader::GetModuleDirectory() + L"LEProc.exe";

    // Replace %APP% with the actual selected file path
    std::wstring cmd = command;
    const std::wstring placeholder = L"%APP%";
    size_t pos = cmd.find(placeholder);
    while (pos != std::wstring::npos)
    {
        cmd.replace(pos, placeholder.length(), m_selectedFile);
        pos = cmd.find(placeholder, pos + m_selectedFile.length());
    }

    // Launch LEProc.exe
    ShellExecuteW(nullptr, nullptr, leProcPath.c_str(),
                  cmd.c_str(), nullptr, SW_SHOWNORMAL);
}
```

**驗證**：編譯無誤（在 Step 12 整體驗證）。

---

## Step 12：實作 dllmain.cpp

- [ ] 建立 `src/ShellExtension/dllmain.cpp`

```cpp
// src/ShellExtension/dllmain.cpp
#include <windows.h>
#include <guiddef.h>
#include <shlobj.h>
#include "ClassFactory.h"
#include "Resource.h"

// Global variables
HMODULE g_hModule = nullptr;
LONG g_dllRefCount = 0;

// New COM GUID: {A8B4F5C2-7E3D-4F1A-9C6B-2D8E0F5A3B71}
// clang-format off
static const CLSID CLSID_ShellExtension =
    {0xA8B4F5C2, 0x7E3D, 0x4F1A,
     {0x9C, 0x6B, 0x2D, 0x8E, 0x0F, 0x5A, 0x3B, 0x71}};
// clang-format on

BOOL APIENTRY DllMain(HMODULE hModule, DWORD reason, LPVOID /*reserved*/)
{
    switch (reason)
    {
    case DLL_PROCESS_ATTACH:
        g_hModule = hModule;
        DisableThreadLibraryCalls(hModule);
        break;
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

// ========================================================================
// COM Exports
// ========================================================================

STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, void** ppv)
{
    if (!IsEqualCLSID(rclsid, CLSID_ShellExtension))
        return CLASS_E_CLASSNOTAVAILABLE;

    auto* factory = new (std::nothrow) ClassFactory();
    if (!factory)
        return E_OUTOFMEMORY;

    HRESULT hr = factory->QueryInterface(riid, ppv);
    factory->Release();
    return hr;
}

STDAPI DllCanUnloadNow()
{
    return g_dllRefCount > 0 ? S_FALSE : S_OK;
}

// DllRegisterServer: Only registers CLSID/InprocServer32 (standard COM convention).
// ContextMenuHandlers registration is handled by LEGUI's ShellExtensionRegistrar.
STDAPI DllRegisterServer()
{
    HRESULT hr = S_OK;

    wchar_t modulePath[MAX_PATH] = {};
    GetModuleFileNameW(g_hModule, modulePath, MAX_PATH);

    // Build registry key path: CLSID\{A8B4F5C2-7E3D-4F1A-9C6B-2D8E0F5A3B71}\InprocServer32
    wchar_t clsidStr[40] = {};
    StringFromGUID2(CLSID_ShellExtension, clsidStr, 40);

    wchar_t keyPath[256] = {};
    _snwprintf_s(keyPath, _TRUNCATE,
                 L"CLSID\\%s\\InprocServer32", clsidStr);

    HKEY hKey = nullptr;
    LONG result = RegCreateKeyExW(HKEY_CLASSES_ROOT, keyPath, 0, nullptr,
                                   REG_OPTION_NON_VOLATILE, KEY_SET_VALUE,
                                   nullptr, &hKey, nullptr);
    if (result != ERROR_SUCCESS)
        return HRESULT_FROM_WIN32(result);

    // Set default value to DLL path
    result = RegSetValueExW(hKey, nullptr, 0, REG_SZ,
                            reinterpret_cast<const BYTE*>(modulePath),
                            static_cast<DWORD>((wcslen(modulePath) + 1) * sizeof(wchar_t)));
    if (result != ERROR_SUCCESS)
        hr = HRESULT_FROM_WIN32(result);

    // Set ThreadingModel
    if (SUCCEEDED(hr))
    {
        const wchar_t* threadModel = L"Apartment";
        result = RegSetValueExW(hKey, L"ThreadingModel", 0, REG_SZ,
                                reinterpret_cast<const BYTE*>(threadModel),
                                static_cast<DWORD>((wcslen(threadModel) + 1) * sizeof(wchar_t)));
        if (result != ERROR_SUCCESS)
            hr = HRESULT_FROM_WIN32(result);
    }

    RegCloseKey(hKey);

    // Also register CLSID key with a friendly name
    wchar_t clsidKeyPath[256] = {};
    _snwprintf_s(clsidKeyPath, _TRUNCATE, L"CLSID\\%s", clsidStr);

    result = RegCreateKeyExW(HKEY_CLASSES_ROOT, clsidKeyPath, 0, nullptr,
                              REG_OPTION_NON_VOLATILE, KEY_SET_VALUE,
                              nullptr, &hKey, nullptr);
    if (result == ERROR_SUCCESS)
    {
        const wchar_t* friendlyName = L"Locale Emulator Shell Extension";
        RegSetValueExW(hKey, nullptr, 0, REG_SZ,
                       reinterpret_cast<const BYTE*>(friendlyName),
                       static_cast<DWORD>((wcslen(friendlyName) + 1) * sizeof(wchar_t)));
        RegCloseKey(hKey);
    }

    return hr;
}

STDAPI DllUnregisterServer()
{
    wchar_t clsidStr[40] = {};
    StringFromGUID2(CLSID_ShellExtension, clsidStr, 40);

    // Delete InprocServer32 key
    wchar_t keyPath[256] = {};
    _snwprintf_s(keyPath, _TRUNCATE,
                 L"CLSID\\%s\\InprocServer32", clsidStr);
    RegDeleteKeyW(HKEY_CLASSES_ROOT, keyPath);

    // Delete CLSID key
    _snwprintf_s(keyPath, _TRUNCATE, L"CLSID\\%s", clsidStr);
    RegDeleteKeyW(HKEY_CLASSES_ROOT, keyPath);

    return S_OK;
}
```

**驗證**：編譯無誤（在 Step 13 整體驗證）。

---

## Step 13：建立 ShellExtension.vcxproj

- [ ] 建立 `src/ShellExtension/ShellExtension.vcxproj`

此 vcxproj 支援 `Debug|Win32`、`Debug|x64`、`Release|Win32`、`Release|x64` 四組組態。

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

  <ItemGroup Label="ProjectConfigurations">
    <ProjectConfiguration Include="Debug|Win32">
      <Configuration>Debug</Configuration>
      <Platform>Win32</Platform>
    </ProjectConfiguration>
    <ProjectConfiguration Include="Debug|x64">
      <Configuration>Debug</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
    <ProjectConfiguration Include="Release|Win32">
      <Configuration>Release</Configuration>
      <Platform>Win32</Platform>
    </ProjectConfiguration>
    <ProjectConfiguration Include="Release|x64">
      <Configuration>Release</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
  </ItemGroup>

  <PropertyGroup Label="Globals">
    <VCProjectVersion>17.0</VCProjectVersion>
    <ProjectGuid>{B1F2E3D4-5A6B-7C8D-9E0F-A1B2C3D4E5F6}</ProjectGuid>
    <RootNamespace>ShellExtension</RootNamespace>
    <WindowsTargetPlatformVersion>10.0</WindowsTargetPlatformVersion>
    <Keyword>Win32Proj</Keyword>
  </PropertyGroup>

  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.Default.props" />

  <!-- Configuration-specific property groups -->
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|Win32'" Label="Configuration">
    <ConfigurationType>DynamicLibrary</ConfigurationType>
    <UseDebugLibraries>true</UseDebugLibraries>
    <PlatformToolset>v145</PlatformToolset>
    <CharacterSet>Unicode</CharacterSet>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|x64'" Label="Configuration">
    <ConfigurationType>DynamicLibrary</ConfigurationType>
    <UseDebugLibraries>true</UseDebugLibraries>
    <PlatformToolset>v145</PlatformToolset>
    <CharacterSet>Unicode</CharacterSet>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|Win32'" Label="Configuration">
    <ConfigurationType>DynamicLibrary</ConfigurationType>
    <UseDebugLibraries>false</UseDebugLibraries>
    <PlatformToolset>v145</PlatformToolset>
    <CharacterSet>Unicode</CharacterSet>
    <WholeProgramOptimization>true</WholeProgramOptimization>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|x64'" Label="Configuration">
    <ConfigurationType>DynamicLibrary</ConfigurationType>
    <UseDebugLibraries>false</UseDebugLibraries>
    <PlatformToolset>v145</PlatformToolset>
    <CharacterSet>Unicode</CharacterSet>
    <WholeProgramOptimization>true</WholeProgramOptimization>
  </PropertyGroup>

  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" />
  <ImportGroup Label="ExtensionSettings" />
  <ImportGroup Label="Shared" />

  <ImportGroup Label="PropertySheets">
    <Import Project="$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props"
            Condition="exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')"
            Label="LocalAppDataPlatform" />
  </ImportGroup>

  <PropertyGroup Label="UserMacros" />

  <!-- Output directories -->
  <PropertyGroup Condition="'$(Platform)'=='Win32'">
    <OutDir>$(SolutionDir)Build\$(Configuration)\x86\</OutDir>
    <IntDir>$(SolutionDir)Build\Intermediate\ShellExtension\$(Configuration)\x86\</IntDir>
    <TargetName>ShellExtension</TargetName>
  </PropertyGroup>
  <PropertyGroup Condition="'$(Platform)'=='x64'">
    <OutDir>$(SolutionDir)Build\$(Configuration)\x64\</OutDir>
    <IntDir>$(SolutionDir)Build\Intermediate\ShellExtension\$(Configuration)\x64\</IntDir>
    <TargetName>ShellExtension</TargetName>
  </PropertyGroup>

  <!-- Compiler settings: All configurations -->
  <ItemDefinitionGroup>
    <ClCompile>
      <LanguageStandard>stdcpp17</LanguageStandard>
      <PreprocessorDefinitions>WIN32;_WINDOWS;_USRDLL;SHELLEXTENSION_EXPORTS;%(PreprocessorDefinitions)</PreprocessorDefinitions>
      <AdditionalIncludeDirectories>$(ProjectDir);%(AdditionalIncludeDirectories)</AdditionalIncludeDirectories>
      <ConformanceMode>true</ConformanceMode>
      <SDLCheck>true</SDLCheck>
      <WarningLevel>Level3</WarningLevel>
    </ClCompile>
    <Link>
      <SubSystem>Windows</SubSystem>
      <ModuleDefinitionFile>ShellExtension.def</ModuleDefinitionFile>
      <AdditionalDependencies>shlwapi.lib;shell32.lib;ole32.lib;uuid.lib;advapi32.lib;gdi32.lib;user32.lib;%(AdditionalDependencies)</AdditionalDependencies>
    </Link>
  </ItemDefinitionGroup>

  <!-- Debug-specific -->
  <ItemDefinitionGroup Condition="'$(Configuration)'=='Debug'">
    <ClCompile>
      <Optimization>Disabled</Optimization>
      <PreprocessorDefinitions>_DEBUG;%(PreprocessorDefinitions)</PreprocessorDefinitions>
      <RuntimeLibrary>MultiThreadedDebugDLL</RuntimeLibrary>
    </ClCompile>
    <Link>
      <GenerateDebugInformation>true</GenerateDebugInformation>
    </Link>
  </ItemDefinitionGroup>

  <!-- Release-specific -->
  <ItemDefinitionGroup Condition="'$(Configuration)'=='Release'">
    <ClCompile>
      <Optimization>MaxSpeed</Optimization>
      <PreprocessorDefinitions>NDEBUG;%(PreprocessorDefinitions)</PreprocessorDefinitions>
      <RuntimeLibrary>MultiThreadedDLL</RuntimeLibrary>
      <FunctionLevelLinking>true</FunctionLevelLinking>
      <IntrinsicFunctions>true</IntrinsicFunctions>
    </ClCompile>
    <Link>
      <GenerateDebugInformation>true</GenerateDebugInformation>
      <OptimizeReferences>true</OptimizeReferences>
      <EnableCOMDATFolding>true</EnableCOMDATFolding>
    </Link>
  </ItemDefinitionGroup>

  <!-- Source files -->
  <ItemGroup>
    <ClCompile Include="dllmain.cpp" />
    <ClCompile Include="ClassFactory.cpp" />
    <ClCompile Include="ContextMenuHandler.cpp" />
    <ClCompile Include="ConfigReader.cpp" />
    <ClCompile Include="I18n.cpp" />
    <ClCompile Include="pugixml\pugixml.cpp" />
  </ItemGroup>

  <!-- Header files -->
  <ItemGroup>
    <ClInclude Include="ClassFactory.h" />
    <ClInclude Include="ContextMenuHandler.h" />
    <ClInclude Include="ConfigReader.h" />
    <ClInclude Include="I18n.h" />
    <ClInclude Include="Resource.h" />
    <ClInclude Include="pugixml\pugixml.hpp" />
    <ClInclude Include="pugixml\pugiconfig.hpp" />
  </ItemGroup>

  <!-- Resource files -->
  <ItemGroup>
    <ResourceCompile Include="Resource.rc" />
  </ItemGroup>

  <!-- Module definition file -->
  <ItemGroup>
    <None Include="ShellExtension.def" />
  </ItemGroup>

  <!-- Language files: copy to output -->
  <ItemGroup>
    <None Include="Lang\*.xml">
      <DeploymentContent>true</DeploymentContent>
    </None>
  </ItemGroup>

  <!-- Post-build: copy Lang files to output directory -->
  <PropertyGroup>
    <PostBuildEvent>
      xcopy /Y /I "$(ProjectDir)Lang\*.xml" "$(OutDir)Lang\"
    </PostBuildEvent>
  </PropertyGroup>

  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />
  <ImportGroup Label="ExtensionTargets" />

</Project>
```

**驗證**：檔案存在且為有效 XML。

---

## Step 14：Commit — 建立 Shell Extension 專案結構

- [ ] 執行 commit

```bash
git add src/ShellExtension/
git commit -m "feat: add C++ COM Shell Extension project (x86+x64)

Create native C++ Shell Extension to replace the .NET COM DLL
(LEContextMenuHandler). Implements IShellExtInit + IContextMenu
with pugixml for XML config parsing and 24-language i18n support.

New COM GUID: {A8B4F5C2-7E3D-4F1A-9C6B-2D8E0F5A3B71}

Files:
- ShellExtension.vcxproj: x86+x64 dual-platform build
- dllmain.cpp: DLL entry + COM exports (DllGetClassObject,
  DllCanUnloadNow, DllRegisterServer, DllUnregisterServer)
- ClassFactory.cpp/h: IClassFactory implementation
- ContextMenuHandler.cpp/h: IShellExtInit + IContextMenu
- ConfigReader.cpp/h: LEConfig.xml parser using pugixml
- I18n.cpp/h: Multi-language support with GetUserDefaultUILanguage()
- Resource.h/rc: Bitmap resources for menu icons (4K support)
- ShellExtension.def: DLL export definitions
- pugixml/ (vendored, MIT license)
- Lang/*.xml (copied from src/LEContextMenuHandler)
- Resources/*.bmp (copied from src/LEContextMenuHandler)

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Step 15：建置驗證 — x86

- [ ] 使用 MSBuild 建置 Win32 (x86) Release 組態

```bash
msbuild src/ShellExtension/ShellExtension.vcxproj /p:Configuration=Release /p:Platform=Win32 /t:Build
```

**預期結果**：
- 建置成功，無錯誤
- `Build\Release\x86\ShellExtension.dll` 存在
- `Build\Release\x86\Lang\` 目錄包含所有語言 XML 檔案

**若建置失敗**：
- 檢查錯誤訊息，逐一修正
- 常見問題：連結器找不到函式庫 → 檢查 AdditionalDependencies
- 常見問題：標頭檔找不到 → 檢查 AdditionalIncludeDirectories
- 修正後重新建置

---

## Step 16：建置驗證 — x64

- [ ] 使用 MSBuild 建置 x64 Release 組態

```bash
msbuild src/ShellExtension/ShellExtension.vcxproj /p:Configuration=Release /p:Platform=x64 /t:Build
```

**預期結果**：
- 建置成功，無錯誤
- `Build\Release\x64\ShellExtension.dll` 存在
- `Build\Release\x64\Lang\` 目錄包含所有語言 XML 檔案

**驗證 DLL 架構**：

```bash
# 用 dumpbin 確認 x86 DLL 是 32 位元
dumpbin /headers Build/Release/x86/ShellExtension.dll | findstr machine

# 預期輸出: 14C machine (x86)

# 用 dumpbin 確認 x64 DLL 是 64 位元
dumpbin /headers Build/Release/x64/ShellExtension.dll | findstr machine

# 預期輸出: 8664 machine (x64)
```

**驗證匯出函式**：

```bash
dumpbin /exports Build/Release/x64/ShellExtension.dll
# 預期看到: DllGetClassObject, DllCanUnloadNow, DllRegisterServer, DllUnregisterServer
```

---

## Step 17：Commit — 修正建置問題（如有）

- [ ] 若 Step 15/16 有修正，commit 修正內容

```bash
git add src/ShellExtension/
git commit -m "fix: resolve Shell Extension build issues

<描述具體修正的問題>

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Step 18：手動測試 — 註冊與驗證右鍵選單

> **注意**：此步驟需要管理員權限。使用系統原生的 regsvr32。

- [ ] 以管理員身份開啟命令提示字元

**在 64 位元 Windows 上（最常見情況）**：

```bash
# 註冊 x64 版本（因為 Explorer.exe 是 x64）
%SystemRoot%\System32\regsvr32.exe "Build\Release\x64\ShellExtension.dll"
```

**驗證**：
1. 開啟檔案總管
2. 找到任意 `.exe` 檔案
3. 右鍵點擊 → 應該看到「Locale Emulator」子選單
4. 子選單應包含：
   - 以預設設定執行（或當地語言對應的文字）
   - 分隔線
   - 使用者定義的 Profile（來自 LEConfig.xml）
   - 分隔線
   - 管理此應用程式
   - 管理所有設定
5. 點擊選單項目 → 應嘗試啟動 LEProc.exe（因 LEProc 可能不存在於同目錄，可能會失敗，這是預期行為）

- [ ] 測試完成後解除註冊

```bash
%SystemRoot%\System32\regsvr32.exe /u "Build\Release\x64\ShellExtension.dll"
```

**注意**：完整的 ContextMenuHandlers 註冊需要 LEGUI 的 `ShellExtensionRegistrar`（Plan 3）。此處的 `regsvr32` 註冊是備用方案，僅註冊 CLSID/InprocServer32。要讓 Explorer 顯示右鍵選單，還需要手動建立：

```
HKCR\*\shellex\ContextMenuHandlers\{A8B4F5C2-7E3D-4F1A-9C6B-2D8E0F5A3B71}
  (Default) = "Locale Emulator Shell Extension"
```

可用 reg 命令手動建立：

```bash
# 建立 ContextMenuHandler 登錄項（手動測試用）
reg add "HKCR\*\shellex\ContextMenuHandlers\{A8B4F5C2-7E3D-4F1A-9C6B-2D8E0F5A3B71}" /ve /d "Locale Emulator Shell Extension" /f

# 測試完畢後清除
reg delete "HKCR\*\shellex\ContextMenuHandlers\{A8B4F5C2-7E3D-4F1A-9C6B-2D8E0F5A3B71}" /f
%SystemRoot%\System32\regsvr32.exe /u "Build\Release\x64\ShellExtension.dll"
```

---

## Step 19：最終 Commit

- [ ] 確認所有檔案都已追蹤，建立最終 commit

```bash
git status
git add -A
git commit -m "chore: Shell Extension build verified for x86 and x64

Both Win32 and x64 configurations build successfully.
DLL exports verified: DllGetClassObject, DllCanUnloadNow,
DllRegisterServer, DllUnregisterServer.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## 完成檢查清單

| 項目 | 狀態 |
|------|------|
| `ShellExtension.vcxproj` 支援 Win32 + x64 | |
| pugixml vendored（MIT 授權） | |
| `ConfigReader` 可解析 LEConfig.xml | |
| `I18n` 可載入 Lang/*.xml，fallback 至 DefaultLanguage.xml | |
| `ClassFactory` 實作 IClassFactory | |
| `ContextMenuHandler` 實作 IShellExtInit + IContextMenu | |
| `dllmain.cpp` 匯出 4 個 COM 函式 | |
| `ShellExtension.def` 定義匯出 | |
| `Resource.h` + `Resource.rc` 含 8 個點陣圖資源 | |
| Lang/*.xml 已複製（23 檔） | |
| PE 標頭讀取判斷 x86/x64 | |
| 4K 顯示器偵測載入 @200 變體 | |
| x86 建置成功 | |
| x64 建置成功 | |
| `regsvr32` 註冊測試通過 | |
| 新 COM GUID `{A8B4F5C2-7E3D-4F1A-9C6B-2D8E0F5A3B71}` | |
| DllRegisterServer 僅處理 CLSID/InprocServer32 | |
| ContextMenuHandlers 留給 Plan 3 (LEGUI ShellExtensionRegistrar) | |

---

## 檔案清單

```
src/ShellExtension/
├── ShellExtension.vcxproj          # MSBuild 專案檔 (Win32 + x64)
├── ShellExtension.def              # DLL 匯出定義
├── dllmain.cpp                     # DLL 進入點 + COM 匯出函式
├── ClassFactory.h                  # IClassFactory 標頭
├── ClassFactory.cpp                # IClassFactory 實作
├── ContextMenuHandler.h            # IShellExtInit + IContextMenu 標頭
├── ContextMenuHandler.cpp          # IShellExtInit + IContextMenu 實作
├── ConfigReader.h                  # LEConfig.xml 解析器標頭
├── ConfigReader.cpp                # LEConfig.xml 解析器實作
├── I18n.h                          # 多語系載入標頭
├── I18n.cpp                        # 多語系載入實作
├── Resource.h                      # 資源 ID 定義
├── Resource.rc                     # 資源編譯檔
├── pugixml/                        # pugixml (vendored, MIT)
│   ├── pugixml.hpp
│   ├── pugixml.cpp
│   ├── pugiconfig.hpp
│   └── LICENSE.md
├── Lang/                           # 語言檔案（23 檔）
│   ├── DefaultLanguage.xml
│   ├── ca.xml
│   ├── cs.xml
│   ├── de.xml
│   ├── es.xml
│   ├── fr.xml
│   ├── ind.xml
│   ├── it.xml
│   ├── ja.xml
│   ├── ka.xml
│   ├── ko.xml
│   ├── lt.xml
│   ├── nb.xml
│   ├── nl.xml
│   ├── pl.xml
│   ├── pt-BR.xml
│   ├── ru.xml
│   ├── th.xml
│   ├── tr-TR.xml
│   ├── zh-CN.xml
│   ├── zh-HK.xml
│   └── zh-TW.xml
└── Resources/                      # 點陣圖資源（8 檔）
    ├── purple.bmp
    ├── purple@200.bmp
    ├── gray.bmp
    ├── gray@200.bmp
    ├── blue.bmp
    ├── blue@200.bmp
    ├── yellow.bmp
    └── yellow@200.bmp
```
