// tests/Core.Tests/main.cpp
// Standalone struct layout contract test between C++ Core and C# LEProc.
// Verifies that LEB, RTL_TIME_ZONE_INFORMATION, and REGISTRY_REDIRECTION_ENTRY64
// struct sizes and field offsets match the C# P/Invoke definitions in LoaderWrapper.cs.
// No framework dependency -- exit code 0 = pass, 1 = fail.

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stddef.h>
#include <stdio.h>

// ========================================================================
// KNOWN LIMITATION: These are hand-copied struct definitions, not the actual
// Core headers. If someone changes the struct layout in src/Core/LocaleEmulator/
// LocaleEmulator.h, this test will NOT automatically detect the mismatch.
//
// Mitigations:
// 1. Core is archived upstream code, rarely changed
// 2. All Core changes must use [Core] commit prefix — reviewer should check this file
// 3. End-to-end smoke test (Invoke-SmokeTest.ps1) catches ABI mismatches at runtime
//
// We cannot #include LocaleEmulator.h directly because it pulls in ml.h (~1.4MB)
// which has heavy dependencies on WDK, custom ML framework, and inline assembly.
// ========================================================================

typedef struct {
    SHORT Year;
    SHORT Month;
    SHORT Day;
    SHORT Hour;
    SHORT Minute;
    SHORT Second;
    SHORT Milliseconds;
    SHORT Weekday;
} LE_TIME_FIELDS;

typedef struct {
    LONG            Bias;
    WCHAR           StandardName[32];
    LE_TIME_FIELDS  StandardStart;
    LONG            StandardBias;
    WCHAR           DaylightName[32];
    LE_TIME_FIELDS  DaylightStart;
    LONG            DaylightBias;
} LE_RTL_TIME_ZONE_INFORMATION;

// LEB as defined in LocaleEmulator.h (the "real" struct used at runtime).
// The C# side marshals only the fixed portion (up to Timezone inclusive).
// NumberOfRegistryRedirectionEntries and RegistryReplacement are appended
// manually by LERegistryRedirector.GetBinaryData() in C#.
typedef struct {
    ULONG                           AnsiCodePage;
    ULONG                           OemCodePage;
    ULONG                           LocaleID;
    ULONG                           DefaultCharset;
    ULONG                           HookUILanguageApi;
    WCHAR                           DefaultFaceName[LF_FACESIZE];
    LE_RTL_TIME_ZONE_INFORMATION    Timezone;
    ULONG64                         NumberOfRegistryRedirectionEntries;
    // REGISTRY_REDIRECTION_ENTRY64 RegistryReplacement[1]; // variable-length tail
} LE_LEB;

// The "fixed" portion of LEB that C# marshals as its LEB struct
// (without NumberOfRegistryRedirectionEntries or RegistryReplacement).
typedef struct {
    ULONG                           AnsiCodePage;
    ULONG                           OemCodePage;
    ULONG                           LocaleID;
    ULONG                           DefaultCharset;
    ULONG                           HookUILanguageApi;
    WCHAR                           DefaultFaceName[LF_FACESIZE];
    LE_RTL_TIME_ZONE_INFORMATION    Timezone;
} LE_LEB_CSHARP;

// UNICODE_STRING64 as defined via UNICODE_STRING3264 in ml.h
#pragma pack(push, 8)
typedef struct {
    USHORT  Length;
    USHORT  MaximumLength;
    union {
        PWSTR   Buffer;
        ULONG64 Dummy;
    };
} LE_UNICODE_STRING64;
#pragma pack(pop)

// REGISTRY_ENTRY64 from LocaleEmulator.h
typedef struct {
    ULONG64             Root;
    LE_UNICODE_STRING64 SubKey;
    LE_UNICODE_STRING64 ValueName;
    ULONG               DataType;
    PVOID64             Data;
    ULONG64             DataSize;
} LE_REGISTRY_ENTRY64;

typedef struct {
    LE_REGISTRY_ENTRY64 Original;
    LE_REGISTRY_ENTRY64 Redirected;
} LE_REGISTRY_REDIRECTION_ENTRY64;

// ========================================================================
// Test helpers
// ========================================================================

static int g_failures = 0;

static void check_size(const char* name, size_t actual, size_t expected)
{
    if (actual == expected)
    {
        printf("PASS: sizeof(%s) = %zu (expected %zu)\n", name, actual, expected);
    }
    else
    {
        printf("FAIL: sizeof(%s) = %zu (expected %zu)\n", name, actual, expected);
        ++g_failures;
    }
}

static void check_offset(const char* structName, const char* fieldName,
                          size_t actual, size_t expected)
{
    if (actual == expected)
    {
        printf("PASS: offsetof(%s, %s) = %zu (expected %zu)\n",
               structName, fieldName, actual, expected);
    }
    else
    {
        printf("FAIL: offsetof(%s, %s) = %zu (expected %zu)\n",
               structName, fieldName, actual, expected);
        ++g_failures;
    }
}

// ========================================================================
// Main
// ========================================================================

int wmain()
{
    printf("=== Core.Tests: Struct Layout Contract Verification ===\n\n");

    // ------------------------------------------------------------------
    // 1. TIME_FIELDS size (C# uses ushort x8 = 16 bytes)
    // ------------------------------------------------------------------
    printf("--- TIME_FIELDS ---\n");
    check_size("TIME_FIELDS", sizeof(LE_TIME_FIELDS), 16);

    // ------------------------------------------------------------------
    // 2. RTL_TIME_ZONE_INFORMATION size and field offsets
    //    C# layout: int Bias (4) + byte[64] StandardName + TIME_FIELDS (16)
    //               + int StandardBias (4) + byte[64] DaylightName
    //               + TIME_FIELDS (16) + int DaylightBias (4)
    //    = 4 + 64 + 16 + 4 + 64 + 16 + 4 = 172 bytes
    // ------------------------------------------------------------------
    printf("\n--- RTL_TIME_ZONE_INFORMATION ---\n");
    check_size("RTL_TIME_ZONE_INFORMATION", sizeof(LE_RTL_TIME_ZONE_INFORMATION), 172);
    check_offset("RTL_TIME_ZONE_INFORMATION", "Bias",
                  offsetof(LE_RTL_TIME_ZONE_INFORMATION, Bias), 0);
    check_offset("RTL_TIME_ZONE_INFORMATION", "StandardName",
                  offsetof(LE_RTL_TIME_ZONE_INFORMATION, StandardName), 4);
    check_offset("RTL_TIME_ZONE_INFORMATION", "StandardStart",
                  offsetof(LE_RTL_TIME_ZONE_INFORMATION, StandardStart), 68);
    check_offset("RTL_TIME_ZONE_INFORMATION", "StandardBias",
                  offsetof(LE_RTL_TIME_ZONE_INFORMATION, StandardBias), 84);
    check_offset("RTL_TIME_ZONE_INFORMATION", "DaylightName",
                  offsetof(LE_RTL_TIME_ZONE_INFORMATION, DaylightName), 88);
    check_offset("RTL_TIME_ZONE_INFORMATION", "DaylightStart",
                  offsetof(LE_RTL_TIME_ZONE_INFORMATION, DaylightStart), 152);
    check_offset("RTL_TIME_ZONE_INFORMATION", "DaylightBias",
                  offsetof(LE_RTL_TIME_ZONE_INFORMATION, DaylightBias), 168);

    // ------------------------------------------------------------------
    // 3. LEB (C# marshalled portion) size and field offsets
    //    C# LEB: uint(4) + uint(4) + uint(4) + uint(4) + uint(4)
    //            + byte[64] + RTL_TIME_ZONE_INFORMATION(172)
    //    = 20 + 64 + 172 = 256 bytes
    // ------------------------------------------------------------------
    printf("\n--- LEB (C# marshalled portion) ---\n");
    check_size("LEB (C# portion)", sizeof(LE_LEB_CSHARP), 256);
    check_offset("LEB", "AnsiCodePage",
                  offsetof(LE_LEB_CSHARP, AnsiCodePage), 0);
    check_offset("LEB", "OemCodePage",
                  offsetof(LE_LEB_CSHARP, OemCodePage), 4);
    check_offset("LEB", "LocaleID",
                  offsetof(LE_LEB_CSHARP, LocaleID), 8);
    check_offset("LEB", "DefaultCharset",
                  offsetof(LE_LEB_CSHARP, DefaultCharset), 12);
    check_offset("LEB", "HookUILanguageApi",
                  offsetof(LE_LEB_CSHARP, HookUILanguageApi), 16);
    check_offset("LEB", "DefaultFaceName",
                  offsetof(LE_LEB_CSHARP, DefaultFaceName), 20);
    check_offset("LEB", "Timezone",
                  offsetof(LE_LEB_CSHARP, Timezone), 84);

    // ------------------------------------------------------------------
    // 4. Full LEB (C++ side, includes NumberOfRegistryRedirectionEntries)
    //    After Timezone (offset 84 + 172 = 256), then ULONG64 (8 bytes).
    //    Total up to (not including) RegistryReplacement[1].
    // ------------------------------------------------------------------
    printf("\n--- LEB (full C++ struct) ---\n");
    check_offset("LEB_full", "NumberOfRegistryRedirectionEntries",
                  offsetof(LE_LEB, NumberOfRegistryRedirectionEntries), 256);

    // ------------------------------------------------------------------
    // 5. UNICODE_STRING64 size
    //    C#: ushort(2) + ushort(2) + long(8) = 12 bytes
    //    C++: USHORT(2) + USHORT(2) + union { PWSTR, ULONG64 }(8) = 12 bytes
    //    But pack(8) may add padding => should be 16 (4 bytes padding after MaximumLength)
    //    ... Actually: check what ml.h does. It's inside #pragma pack(push, 8).
    //    With natural alignment the union (8-byte aligned) starts at offset 8,
    //    so total is 16. C# LayoutKind.Sequential uses default pack = 8,
    //    and long (8 bytes) forces alignment to 8, so C# also gets 16? No --
    //    C# SequentialLayout with default packing: ushort(2) + ushort(2) +
    //    long(8) => the long starts at offset 4 (C# pack doesn't pad to 8
    //    for longs by default in Sequential -- it uses min(field size, pack)).
    //    Wait, C# default Pack=0 means use type's natural alignment.
    //    long (Int64) has alignment 8 => padding to offset 8 => size 16.
    //    BUT the C# struct is declared with just `internal long Buffer;`
    //    (no union), and C# Sequential default pack=0 means fields are
    //    aligned to their natural alignment. Int64 (8 bytes) aligns to 8.
    //    So C#: offset 0: ushort(2), offset 2: ushort(2), pad 4 bytes,
    //    offset 8: long(8) => total 16.
    //
    //    Let's just verify the C++ side and document what C# must match.
    // ------------------------------------------------------------------
    printf("\n--- UNICODE_STRING64 ---\n");
    check_size("UNICODE_STRING64", sizeof(LE_UNICODE_STRING64), 16);
    check_offset("UNICODE_STRING64", "Length",
                  offsetof(LE_UNICODE_STRING64, Length), 0);
    check_offset("UNICODE_STRING64", "MaximumLength",
                  offsetof(LE_UNICODE_STRING64, MaximumLength), 2);
    check_offset("UNICODE_STRING64", "Dummy",
                  offsetof(LE_UNICODE_STRING64, Dummy), 8);

    // ------------------------------------------------------------------
    // 6. REGISTRY_ENTRY64 size and field offsets
    //    C# REGISTRY_ENTRY64:
    //      ulong Root (8)
    //      UNICODE_STRING64 SubKey (16)
    //      UNICODE_STRING64 ValueName (16)
    //      uint DataType (4) + padding(4)
    //      long Data (8)
    //      ulong DataSize (8)
    //    Total expected: 8 + 16 + 16 + 4 + 4(pad) + 8 + 8 = 64
    //    ... but wait, C# `long Data` is Int64 (8 bytes), C++ `PVOID64 Data`
    //    is a void* __ptr64 (8 bytes). Let's verify C++ layout.
    // ------------------------------------------------------------------
    printf("\n--- REGISTRY_ENTRY64 ---\n");
    check_size("REGISTRY_ENTRY64", sizeof(LE_REGISTRY_ENTRY64), 64);
    check_offset("REGISTRY_ENTRY64", "Root",
                  offsetof(LE_REGISTRY_ENTRY64, Root), 0);
    check_offset("REGISTRY_ENTRY64", "SubKey",
                  offsetof(LE_REGISTRY_ENTRY64, SubKey), 8);
    check_offset("REGISTRY_ENTRY64", "ValueName",
                  offsetof(LE_REGISTRY_ENTRY64, ValueName), 24);
    check_offset("REGISTRY_ENTRY64", "DataType",
                  offsetof(LE_REGISTRY_ENTRY64, DataType), 40);
    check_offset("REGISTRY_ENTRY64", "Data",
                  offsetof(LE_REGISTRY_ENTRY64, Data), 48);
    check_offset("REGISTRY_ENTRY64", "DataSize",
                  offsetof(LE_REGISTRY_ENTRY64, DataSize), 56);

    // ------------------------------------------------------------------
    // 7. REGISTRY_REDIRECTION_ENTRY64 size
    //    = 2 * sizeof(REGISTRY_ENTRY64) = 128
    // ------------------------------------------------------------------
    printf("\n--- REGISTRY_REDIRECTION_ENTRY64 ---\n");
    check_size("REGISTRY_REDIRECTION_ENTRY64",
               sizeof(LE_REGISTRY_REDIRECTION_ENTRY64), 128);
    check_offset("REGISTRY_REDIRECTION_ENTRY64", "Original",
                  offsetof(LE_REGISTRY_REDIRECTION_ENTRY64, Original), 0);
    check_offset("REGISTRY_REDIRECTION_ENTRY64", "Redirected",
                  offsetof(LE_REGISTRY_REDIRECTION_ENTRY64, Redirected), 64);

    // ------------------------------------------------------------------
    // Summary
    // ------------------------------------------------------------------
    printf("\n=== Summary ===\n");
    if (g_failures > 0)
    {
        printf("RESULT: %d check(s) FAILED\n", g_failures);
        return 1;
    }

    printf("RESULT: all checks passed\n");
    return 0;
}
