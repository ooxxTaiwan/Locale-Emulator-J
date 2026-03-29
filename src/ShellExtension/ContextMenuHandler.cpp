// src/ShellExtension/ContextMenuHandler.cpp
#include "ContextMenuHandler.h"
#include "ConfigReader.h"
#include "I18n.h"
#include "Resource.h"
#include <shlwapi.h>
#include <shellapi.h>
#include <strsafe.h>
#include <mutex>

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

    // Load bitmaps once per DLL lifetime (static cache)
    static std::once_flag s_bmpFlag;
    static HBITMAP s_bmpPurple, s_bmpGray, s_bmpBlue, s_bmpYellow;

    std::call_once(s_bmpFlag, [this]() {
        bool hiDpi = Is4KDisplay();
        s_bmpPurple = LoadMenuBitmap(IDB_PURPLE, IDB_PURPLE_200, hiDpi);
        s_bmpGray   = LoadMenuBitmap(IDB_GRAY, IDB_GRAY_200, hiDpi);
        s_bmpBlue   = LoadMenuBitmap(IDB_BLUE, IDB_BLUE_200, hiDpi);
        s_bmpYellow = LoadMenuBitmap(IDB_YELLOW, IDB_YELLOW_200, hiDpi);
    });

    m_bmpPurple = s_bmpPurple;
    m_bmpGray   = s_bmpGray;
    m_bmpBlue   = s_bmpBlue;
    m_bmpYellow = s_bmpYellow;

    // Build default menu items
    m_menuItems.push_back({I18n::GetString(L"Submenu"),
                           true, -1, m_bmpYellow, L""});
    m_menuItems.push_back({I18n::GetString(L"RunDefault"),
                           true, -1, m_bmpYellow, L"-run \"%APP%\""});
    m_menuItems.push_back({I18n::GetString(L"ManageApp"),
                           true, -1, m_bmpGray, L"-manage \"%APP%\""});
    m_menuItems.push_back({I18n::GetString(L"ManageAll"),
                           true, -1, m_bmpBlue, L"-global"});

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
    // Bitmaps are static (live for DLL lifetime), do not DeleteObject here.
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
                        static_cast<USHORT>(m_menuItems.size()));
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

HBITMAP ContextMenuHandler::LoadMenuBitmap(int normalId, int hiDpiId, bool hiDpi)
{
    int resId = hiDpi ? hiDpiId : normalId;
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

    // RAII wrapper for HANDLE — closes automatically on scope exit
    struct HandleGuard
    {
        HANDLE h;
        HandleGuard(HANDLE handle) : h(handle) {}
        ~HandleGuard() { if (h != INVALID_HANDLE_VALUE) CloseHandle(h); }
        HandleGuard(const HandleGuard&) = delete;
        HandleGuard& operator=(const HandleGuard&) = delete;
    };

    HandleGuard file(CreateFileW(path.c_str(), GENERIC_READ,
                                 FILE_SHARE_READ | FILE_SHARE_WRITE,
                                 nullptr, OPEN_EXISTING,
                                 FILE_ATTRIBUTE_NORMAL, nullptr));
    if (file.h == INVALID_HANDLE_VALUE)
        return PEType::Unknown;

    // Read DOS header to check MZ signature
    WORD dosSignature = 0;
    DWORD bytesRead = 0;

    if (!ReadFile(file.h, &dosSignature, sizeof(dosSignature), &bytesRead, nullptr)
        || bytesRead != sizeof(dosSignature)
        || dosSignature != 0x5A4D) // "MZ"
        return PEType::Unknown;

    // Seek to e_lfanew offset (0x3C)
    if (SetFilePointer(file.h, 0x3C, nullptr, FILE_BEGIN) == INVALID_SET_FILE_POINTER)
        return PEType::Unknown;

    DWORD peOffset = 0;
    if (!ReadFile(file.h, &peOffset, sizeof(peOffset), &bytesRead, nullptr)
        || bytesRead != sizeof(peOffset))
        return PEType::Unknown;

    // Seek past PE signature ("PE\0\0") to COFF header Machine field
    // PE signature is 4 bytes, Machine is the first 2 bytes of COFF header
    LONG machineOffset = static_cast<LONG>(peOffset) + 4;
    if (SetFilePointer(file.h, machineOffset, nullptr, FILE_BEGIN) == INVALID_SET_FILE_POINTER)
        return PEType::Unknown;

    WORD machine = 0;
    if (!ReadFile(file.h, &machine, sizeof(machine), &bytesRead, nullptr)
        || bytesRead != sizeof(machine))
        return PEType::Unknown;

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
