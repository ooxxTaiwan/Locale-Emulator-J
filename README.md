Locale Emulator
===============

[![license](https://img.shields.io/github/license/xupefei/Locale-Emulator.svg)](https://www.gnu.org/licenses/lgpl-3.0.en.html)
[![CI](https://github.com/ooxxTaiwan/Locale-Emulator-J/actions/workflows/ci.yml/badge.svg)](https://github.com/ooxxTaiwan/Locale-Emulator-J/actions/workflows/ci.yml)

Yet Another System Region and Language Simulator

## About

Locale Emulator lets you run Windows applications under a different locale without changing the OS system locale. It works by hooking Windows API calls in target processes via native DLL injection.

## Core Source Attribution

The native hooking core (`src/Core/`) originates from [Locale-Emulator-Core](https://github.com/xupefei/Locale-Emulator-Core):

- **Upstream commit**: `ae7160dc5deb97947396abcd784f9b98b6ee38b3` (2021-08-23)
- **Archive date**: 2022-04-15 (repository archived by original owner)
- **Integration**: Source code copied directly into this repository for continued maintenance

## System Requirements

- **Minimum OS**: Windows 10 version 1607 or later
- **Runtime**: .NET 10 (framework-dependent deployment)
- **Target applications**: x86 (32-bit) executables only (Core hooking engine limitation)

## Build

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- Visual Studio 2026 (v18.0+) with:
  - .NET desktop development workload
  - Desktop development with C++ workload (MSVC v145)
- Or standalone: .NET 10 SDK + MSVC Build Tools 2026

### Build from IDE

1. Open `LocaleEmulator.sln` in Visual Studio 2026
2. Build Solution (Ctrl+Shift+B)

### Build from CLI

```bash
# Build all projects (x86 + x64)
msbuild LocaleEmulator.sln /p:Configuration=Release /p:Platform=Win32
msbuild LocaleEmulator.sln /p:Configuration=Release /p:Platform=x64

# Run tests
dotnet test LocaleEmulator.sln
```

### Build Output

- `Build/Release/x86/` -- LEProc, LEGUI, Core DLLs, ShellExtension (x86)
- `Build/Release/x64/` -- ShellExtension (x64 only)

## Translate

If you want to help translating Locale Emulator, you can find all strings in:

- `DefaultLanguage.xaml` in `LEGUI/Lang/` folder (WPF UI)
- `DefaultLanguage.xml` in `src/ShellExtension/Lang/` folder (context menu)

After you translated the above files into your language, please inform us by creating a pull request.

## License

![LGPL](https://www.gnu.org/graphics/lgplv3-147x51.png)

- Main project: [LGPL-3.0](https://opensource.org/licenses/LGPL-3.0)
- Core (`src/Core/`): LGPL-3.0 (code) / GPL-3.0 (project)
- Shell Extension uses [pugixml](https://pugixml.org/) (MIT license)

[Flat icon set](commit/eae9fbc27f1a4c85986577202b61742c6287e10a) from [graphicex](https://graphicex.com/icon-and-logo/15983-flat-alphabet-in-9-colors-with-long-shadow-6913875.html).

If you want to make any modification on these source codes while keeping new codes not protected by LGPL-3.0, please contact us for a sublicense instead.
