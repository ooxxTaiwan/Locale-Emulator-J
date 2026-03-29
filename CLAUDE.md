# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Locale Emulator (LE) is a Windows utility that emulates system locale/region settings, letting users run applications under a different locale without changing the OS setting. It hooks Windows API calls in target processes via native DLL injection.

The project consists of three .NET 10 managed projects and two C++ native components, plus a C++ COM Shell Extension. The native Core (originally from [Locale-Emulator-Core](https://github.com/xupefei/Locale-Emulator-Core), commit `ae7160d`, archived 2022-04-15) is integrated directly into this repository under `src/Core/`.

## Build

- **IDE**: Visual Studio 2026 (v18.0+) with .NET 10 SDK and MSVC Build Tools v145
- **Framework**: .NET 10 (`net10.0-windows`) for managed projects; C++ (MSVC v145) for native projects
- **Solution**: `LocaleEmulator.sln` (unified C# + C++ mixed solution)

### CLI Build

```bash
# Full build (all projects, both platforms)
msbuild LocaleEmulator.sln /p:Configuration=Release /p:Platform=Win32
msbuild LocaleEmulator.sln /p:Configuration=Release /p:Platform=x64

# .NET projects only
dotnet build src/LECommonLibrary/
dotnet build src/LEProc/
dotnet build src/LEGUI/

# Run .NET tests
dotnet test tests/LECommonLibrary.Tests/
dotnet test tests/LEGUI.Tests/
dotnet test tests/LEProc.Tests/ --arch x86    # LEProc is x86, needs x86 test host
```

### Build Output

```
Build/Release/
  x86/
    LEProc.exe              # .NET 10 (x86)
    LECommonLibrary.dll     # .NET 10 (AnyCPU)
    LEGUI.exe               # .NET 10 (AnyCPU)
    LoaderDll.dll           # Core native (x86)
    LocaleEmulator.dll      # Core native (x86)
    ShellExtension.dll      # Shell Extension (x86)
    Lang/                   # Language files
  x64/
    ShellExtension.dll      # Shell Extension (x64)
```

### Build Configurations (C++ projects)

| C++ Project | Debug\|Win32 | Release\|Win32 | Debug\|x64 | Release\|x64 |
|-------------|:---:|:---:|:---:|:---:|
| Core/LoaderDll | - | ✓ | - | - |
| Core/LocaleEmulator | - | ✓ | - | - |
| Core/Loader | - | ✓ | - | - |
| ShellExtension | ✓ | ✓ | ✓ | ✓ |
| LocaleTestApp | ✓ | ✓ | - | - |
| Core.Tests | ✓ | ✓ | - | - |
| ShellExtension.Tests | - | - | ✓ | ✓ |

Note: Core only has Release. CI uses `dotnet build` for .NET projects and `msbuild` for C++ vcxproj individually.

### Build Prerequisites

- .NET 10 SDK
- .NET 10 Windows Desktop Runtime x86 (for LEProc.Tests `--arch x86`)
- MSVC Build Tools 2026 (v145) -- via VS 2026 or standalone Build Tools
- All assemblies are strong-name signed with `key.snk`
- Shared build properties defined in `Directory.Build.props`

## Architecture

### Project Relationship Diagram

```
User right-clicks .exe
    |
    v
ShellExtension (C++, x64)          <-- COM Shell Extension, loaded by Explorer.exe
    | Reads LEConfig.xml
    | Invokes LEProc.exe -runas {guid} {path}
    v
LEProc (.NET 10, x86)              <-- Core launcher
    | References LECommonLibrary
    | P/Invoke calls LoaderDll.dll
    v
Core/LoaderDll (C++, x86)          <-- Creates target process + injects LocaleEmulator.dll
    | Writes LEB to shared memory
    | Injects DLL
    v
Core/LocaleEmulator (C++, x86)     <-- Injected into target process, hooks Windows API
    | Reads LEB
    | Rewrites PEB/NLS tables
    | Installs syscall hook + EAT hook
    v
Target Process                      <-- Believes it runs under target locale
```

```
LEGUI (.NET 10, AnyCPU)             <-- Profile management UI + Shell Extension installer
    | References LECommonLibrary
    | ShellExtensionRegistrar: registers/unregisters Shell Extension via Registry API
    | Invokes LEProc.exe (manage mode)
    v
LECommonLibrary (.NET 10, AnyCPU)   <-- Shared code: LEConfig, LEProfile, SystemHelper
```

### Project Responsibilities

| Project | Language | Platform | Responsibility |
|---------|----------|----------|----------------|
| LECommonLibrary | C# | AnyCPU | XML config read/write, Profile data model, OS utility functions |
| LEProc | C# | x86 | CLI entry point, CultureInfo lookup, LEB struct assembly, P/Invoke calls to Core |
| LEGUI | C# (WPF) | AnyCPU | Profile management UI, Shell Extension install/uninstall (`ShellExtensionRegistrar`) |
| Core/LoaderDll | C++ | x86 | Exports `LeCreateProcess`: creates target process and injects `LocaleEmulator.dll` |
| Core/LocaleEmulator | C++ | x86 | Injected DLL: hooks syscall/EAT, rewrites PEB NLS tables, intercepts locale-related APIs |
| ShellExtension | C++ | x86+x64 | COM Shell Extension: right-click menu, reads config, launches LEProc |

## Key Data Flow

1. User right-clicks exe -> Shell Extension (C++ COM DLL) reads `LEConfig.xml` and invokes `LEProc.exe -runas {guid} {path}`
2. LEProc loads profile from XML config, resolves codepage/locale/timezone from .NET `CultureInfo`
3. `LoaderWrapper` marshals a `LEB` struct + registry redirect entries into unmanaged memory
4. Native `LeCreateProcess` (from `LoaderDll.dll`) creates the target process with `LocaleEmulator.dll` injected
5. `LocaleEmulator.dll` hooks syscalls and EAT entries, rewrites PEB NLS tables so the target process sees the emulated locale

## Configuration Format

XML files (`LEConfig.xml` and `*.le.config`) share the same schema:

```xml
<LEConfig>
  <Profiles>
    <Profile Name="..." Guid="..." MainMenu="true|false">
      <Parameter>...</Parameter>
      <Location>ja-JP</Location>
      <Timezone>Tokyo Standard Time</Timezone>
      <RunAsAdmin>false</RunAsAdmin>
      <RedirectRegistry>true</RedirectRegistry>
      <IsAdvancedRedirection>false</IsAdvancedRedirection>
      <RunWithSuspend>false</RunWithSuspend>
    </Profile>
  </Profiles>
</LEConfig>
```

## Testing

| Layer | Framework | Command |
|-------|-----------|---------|
| .NET unit tests (LECommonLibrary, LEGUI) | xUnit + NSubstitute | `dotnet test` |
| .NET unit tests (LEProc) | xUnit + NSubstitute | `dotnet test --arch x86` (requires Windows Desktop Runtime x86) |
| C++ unit tests | Standalone (no framework) | Run `*Tests*.exe` binaries from `Build/Release/x86/` and `Build/Release/x64/` |
| Smoke tests | PowerShell + LocaleTestApp | `.\tests\SmokeTest\Invoke-SmokeTest.ps1` |

### Test Projects

- `tests/LECommonLibrary.Tests/` -- XML config parsing, profile model, system helpers
- `tests/LEProc.Tests/` -- codepage mapping, CultureInfo queries, CLI argument parsing, registry redirect
- `tests/LEGUI.Tests/` -- Shell Extension registration logic, i18n loading, ViewModel logic
- `tests/Core.Tests/` -- C++/C# struct layout contract verification: LEB, RTL_TIME_ZONE_INFORMATION, REGISTRY_REDIRECTION_ENTRY64 sizes and field offsets (standalone C++ exe, x86)
- `tests/ShellExtension.Tests/` -- COM export verification, DllRegisterServer/Unregister round-trip (x64)

### Smoke Test

Verifies end-to-end locale hooking by launching a Win32 test app through LEProc with different locale profiles (ja-JP, zh-TW, zh-CN, ko-KR, en-US) and checking `GetACP()`, `GetOEMCP()`, `GetUserDefaultLCID()` return values.

## Conventions

- **License**: LGPL-3.0 (Core portion: LGPL-3.0/GPL-3.0)
- **LEProc must remain x86**: Core's syscall hooking engine (`HookSysCall_x86`) only has x86 implementation. It pattern-matches `KiFastSystemCall` opcodes, which are x86-only.
- **Shell Extension requires x86 + x64**: x64 for 64-bit Explorer.exe, x86 for 32-bit Explorer on 32-bit Windows.
- **`[Core]` commit prefix**: All modifications to files under `src/Core/` must use the `[Core]` prefix in commit messages, to distinguish from upstream code.
- **NLS mode pending verification**: `System.Globalization.UseNls` setting needs characterization test validation before enabling. Core itself directly manipulates NLS tables and is unaffected by this .NET setting.
- **Minimum Windows version**: Windows 10 1607+ (.NET 10 requirement)
- **Language files**: LEGUI i18n via XAML resource dictionaries in `src/LEGUI/Lang/` (22 languages); Shell Extension i18n via XML in `src/ShellExtension/Lang/` (22 languages)
