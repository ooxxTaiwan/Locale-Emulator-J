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

// ========================================================================
// Helpers
// ========================================================================

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
        }
    }

    if (failures > 0)
    {
        wprintf(L"\nRESULT: %d export check(s) FAILED\n", failures);
        return 1;
    }

    // Registration test (DllRegisterServer / DllUnregisterServer round-trip)
    // is intentionally NOT automated. It modifies HKCR and could interfere
    // with an existing Shell Extension installation. Use manual testing:
    //   regsvr32 ShellExtension.dll
    //   regsvr32 /u ShellExtension.dll

    wprintf(L"\nRESULT: all export checks passed\n");
    return 0;
}
