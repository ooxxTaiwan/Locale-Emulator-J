// tests/ShellExtension.Tests/main.cpp
// Standalone COM export and registration test for ShellExtension.dll.
// No framework dependency -- exit code 0 = pass, 1 = fail.

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdio.h>
#include <stdlib.h>

// ========================================================================
// RAII helpers
// ========================================================================

struct ModuleGuard
{
    HMODULE h;
    explicit ModuleGuard(HMODULE m) : h(m) {}
    ~ModuleGuard() { if (h) FreeLibrary(h); }
    ModuleGuard(const ModuleGuard&) = delete;
    ModuleGuard& operator=(const ModuleGuard&) = delete;
};

struct RegKeyGuard
{
    HKEY h;
    explicit RegKeyGuard(HKEY k = nullptr) : h(k) {}
    ~RegKeyGuard() { if (h) RegCloseKey(h); }
    RegKeyGuard(const RegKeyGuard&) = delete;
    RegKeyGuard& operator=(const RegKeyGuard&) = delete;
};

// ========================================================================
// Helpers
// ========================================================================

static bool IsElevated()
{
    HANDLE token = nullptr;
    if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &token))
        return false;

    TOKEN_ELEVATION elevation = {};
    DWORD size = sizeof(elevation);
    BOOL ok = GetTokenInformation(token, TokenElevation, &elevation,
                                  sizeof(elevation), &size);
    CloseHandle(token);
    return ok && elevation.TokenIsElevated != 0;
}

static bool FileExists(const wchar_t* path)
{
    DWORD attr = GetFileAttributesW(path);
    return attr != INVALID_FILE_ATTRIBUTES &&
           !(attr & FILE_ATTRIBUTE_DIRECTORY);
}

// ========================================================================
// Main
// ========================================================================

int wmain()
{
    int failures = 0;

    // ------------------------------------------------------------------
    // 1. Locate ShellExtension.dll
    // ------------------------------------------------------------------
    wchar_t exeDir[MAX_PATH] = {};
    GetModuleFileNameW(nullptr, exeDir, MAX_PATH);
    // Trim to directory
    wchar_t* lastSlash = wcsrchr(exeDir, L'\\');
    if (lastSlash) *(lastSlash + 1) = L'\0';

    wchar_t dllPath[MAX_PATH] = {};

    // Try 1: next to this exe
    _snwprintf_s(dllPath, _TRUNCATE, L"%sShellExtension.dll", exeDir);
    if (!FileExists(dllPath))
    {
        // Try 2: relative from test exe to source build output
        _snwprintf_s(dllPath, _TRUNCATE,
                     L"%s..\\..\\src\\ShellExtension\\Build\\Release\\x64\\ShellExtension.dll",
                     exeDir);
    }
    if (!FileExists(dllPath))
    {
        wprintf(L"FAIL: Cannot find ShellExtension.dll\n");
        wprintf(L"  Searched: %sShellExtension.dll\n", exeDir);
        wprintf(L"  Searched: %s\n", dllPath);
        return 1;
    }

    wprintf(L"INFO: Loading %s\n", dllPath);

    HMODULE hDll = LoadLibraryW(dllPath);
    if (!hDll)
    {
        wprintf(L"FAIL: LoadLibraryW failed (error %lu)\n", GetLastError());
        return 1;
    }
    ModuleGuard dllGuard(hDll);

    wprintf(L"PASS: LoadLibraryW succeeded\n");

    // ------------------------------------------------------------------
    // 2. Verify 4 COM exports exist
    // ------------------------------------------------------------------
    const char* exports[] = {
        "DllRegisterServer",
        "DllUnregisterServer",
        "DllGetClassObject",
        "DllCanUnloadNow",
    };

    using DllRegUnregFn = HRESULT(STDAPICALLTYPE*)();

    DllRegUnregFn pfnRegister = nullptr;
    DllRegUnregFn pfnUnregister = nullptr;

    for (int i = 0; i < 4; ++i)
    {
        FARPROC proc = GetProcAddress(hDll, exports[i]);
        if (!proc)
        {
            wprintf(L"FAIL: Export '%hs' not found\n", exports[i]);
            ++failures;
        }
        else
        {
            wprintf(L"PASS: Export '%hs' found\n", exports[i]);
            if (i == 0) pfnRegister   = reinterpret_cast<DllRegUnregFn>(proc);
            if (i == 1) pfnUnregister = reinterpret_cast<DllRegUnregFn>(proc);
        }
    }

    if (failures > 0)
    {
        wprintf(L"\nRESULT: %d export check(s) FAILED\n", failures);
        return 1;
    }

    // ------------------------------------------------------------------
    // 3. Registration round-trip (requires elevation)
    // ------------------------------------------------------------------
    if (!IsElevated())
    {
        wprintf(L"\nSKIP: registration test requires elevation\n");
        wprintf(L"\nRESULT: all export checks passed (registration skipped)\n");
        return 0;
    }

    // 3a. Call DllRegisterServer
    HRESULT hr = pfnRegister();
    if (FAILED(hr))
    {
        wprintf(L"FAIL: DllRegisterServer returned 0x%08lX\n",
                static_cast<unsigned long>(hr));
        return 1;
    }
    wprintf(L"PASS: DllRegisterServer succeeded\n");

    // 3b. Verify registry key exists
    const wchar_t* keyPath =
        L"CLSID\\{A8B4F5C2-7E3D-4F1A-9C6B-2D8E0F5A3B71}\\InprocServer32";

    {
        HKEY hKey = nullptr;
        LONG rc = RegOpenKeyExW(HKEY_CLASSES_ROOT, keyPath, 0, KEY_READ, &hKey);
        RegKeyGuard keyGuard(hKey);

        if (rc != ERROR_SUCCESS)
        {
            wprintf(L"FAIL: Registry key not found after DllRegisterServer\n");
            wprintf(L"  Key: HKCR\\%s\n", keyPath);
            // Best-effort cleanup
            pfnUnregister();
            return 1;
        }
        wprintf(L"PASS: Registry key exists after DllRegisterServer\n");
    }

    // 3c. Call DllUnregisterServer
    hr = pfnUnregister();
    if (FAILED(hr))
    {
        wprintf(L"FAIL: DllUnregisterServer returned 0x%08lX\n",
                static_cast<unsigned long>(hr));
        return 1;
    }
    wprintf(L"PASS: DllUnregisterServer succeeded\n");

    // 3d. Verify registry key is gone
    {
        HKEY hKey = nullptr;
        LONG rc = RegOpenKeyExW(HKEY_CLASSES_ROOT, keyPath, 0, KEY_READ, &hKey);
        RegKeyGuard keyGuard(hKey);

        if (rc == ERROR_SUCCESS)
        {
            wprintf(L"FAIL: Registry key still exists after DllUnregisterServer\n");
            wprintf(L"  Key: HKCR\\%s\n", keyPath);
            ++failures;
        }
        else
        {
            wprintf(L"PASS: Registry key removed after DllUnregisterServer\n");
        }
    }

    // ------------------------------------------------------------------
    // Summary
    // ------------------------------------------------------------------
    if (failures > 0)
    {
        wprintf(L"\nRESULT: %d test(s) FAILED\n", failures);
        return 1;
    }

    wprintf(L"\nRESULT: all tests passed\n");
    return 0;
}
