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
    HBITMAP LoadMenuBitmap(int normalId, int hiDpiId, bool hiDpi);

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
