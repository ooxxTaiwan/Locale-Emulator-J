# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Locale Emulator (LE) is a Windows utility that emulates system locale/region settings, letting users run applications under a different locale without changing the OS setting. It hooks Windows API calls in target processes via native DLL injection.

## Build

- **IDE**: Visual Studio 2015/2017+ (solution: `LocaleEmulator.sln`)
- **Framework**: .NET Framework 4.0 (Client Profile)
- **Build**: Open solution in VS and build, or use `msbuild LocaleEmulator.sln /p:Configuration=Release`
- **Output**: `.\Build\Release\` (or `.\Build\Debug\`)
- **Native core DLLs** (`LoaderDll.dll`, `LocaleEmulator.dll`) are built separately from [Locale-Emulator-Core](https://github.com/xupefei/Locale-Emulator-Core) and must be copied into the build output folder
- All assemblies are strong-name signed with `key.snk`
- No NuGet packages; pure .NET Framework dependencies
- No test framework exists; testing is manual

## Architecture

Six C# projects, all targeting .NET Framework 4.0:

### LECommonLibrary (Class Library)
Shared code consumed by all other projects. Key types:
- `LEConfig` — reads/writes XML config files (`LEConfig.xml` for global, `*.le.config` for per-app)
- `LEProfile` — data model for a locale profile (location, timezone, codepage, flags)
- `SystemHelper` — OS utilities (64-bit detection, UAC elevation, DPI)

### LEProc (Console/WinExe, x86 only)
Core launcher that creates processes under emulated locale. This is the most critical component.
- `Program.cs` — CLI entry point; parses `-run`, `-runas`, `-manage`, `-global` arguments
- `LoaderWrapper.cs` — P/Invoke wrapper around native `LoaderDll.dll`; marshals `LEB` struct (codepage, locale ID, charset, timezone) and registry redirect entries to the native loader via `LeCreateProcess`
- Profile precedence: app-specific `.le.config` > first global profile > hardcoded ja-JP default
- `LERegistryRedirector` / `RegistryEntriesLoader` — build registry redirect table passed to the native DLL

### LEGUI (WPF Application)
Profile management UI with two main windows:
- `GlobalConfig.xaml` — manage global profiles stored in `LEConfig.xml`
- `AppConfig.xaml` — manage per-application profiles (`.le.config` files)
- i18n via XAML resource dictionaries in `LEGUI/Lang/` (23+ languages)

### LEContextMenuHandler (Class Library, COM)
Windows shell extension providing right-click context menu on executables. Implements `IShellExtInit` + `IContextMenu` via COM interop. COM GUID: `C52B9871-E5E9-41FD-B84D-C5ACADBEC7AE`. Language files in `LEContextMenuHandler/Lang/*.xml`.

### LEInstaller (WinForms)
Registers/unregisters the shell extension COM component. Handles ADS removal and DLL version checks. Supports per-user and all-users installation (latter requires admin).

### LEUpdater (WinForms)
Scheduled update checker; compares local `LEVersion.xml` against remote version.

## Key Data Flow

1. User right-clicks exe -> shell extension (LEContextMenuHandler) invokes `LEProc.exe -runas {guid} {path}`
2. LEProc loads profile from XML config, resolves codepage/locale/timezone from .NET `CultureInfo`
3. `LoaderWrapper` marshals a `LEB` struct + registry redirect entries into unmanaged memory
4. Native `LeCreateProcess` (from `LoaderDll.dll`) creates the target process with locale hooks injected

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

## Conventions

- License: LGPL-3.0 (LEContextMenuHandler includes Microsoft Public License code)
- LEProc must remain x86 — it needs to load 32-bit `LoaderDll.dll`
- Post-build events copy outputs between projects (e.g., LECommonLibrary DLL -> LEInstaller resources, language files -> output dirs)
