// src/ShellExtension/dllmain.cpp
#include <windows.h>
#include <guiddef.h>
#include <shlobj.h>
#include <new>              // for std::nothrow
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
