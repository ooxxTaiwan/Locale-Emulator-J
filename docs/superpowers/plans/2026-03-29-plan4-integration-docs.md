# Plan 4: Integration & Documentation --- 整合測試與文件

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 建立 smoke test 基礎設施、CI/CD pipeline、完整文件更新

**Architecture:** GitHub Actions CI、PowerShell smoke test、Markdown 文件

**Tech Stack:** GitHub Actions, PowerShell, Markdown

**Branch:** `feature/dotnet10-integration`

**PR:** Closes ooxxTaiwan/Locale-Emulator-J#1

**Depends on:** Plans 1, 2, 3 all merged

---

## Part A: Smoke Test Infrastructure

### Task A1: 建立 x86 Win32 Console 測試應用程式

**目的**：建立一個極簡的 x86 原生 Win32 console app，用於 smoke test 驗證 LEProc 的 locale hook 是否生效。

- [ ] 建立目錄 `tests/SmokeTest/LocaleTestApp/`
- [ ] 建立 `LocaleTestApp.vcxproj`（Win32 Console, x86 only, 靜態連結 CRT）
- [ ] 建立 `main.cpp`
- [ ] 驗證可透過 `msbuild` 建置為 `LocaleTestApp.exe`

**`tests/SmokeTest/LocaleTestApp/main.cpp` 完整內容**：

```cpp
// LocaleTestApp - Locale Emulator Smoke Test Target
// 印出目前程序的 locale 相關 Windows API 回傳值，供 smoke test 腳本比對。
// 輸出格式為 KEY=VALUE，每行一個，便於 PowerShell 解析。

#include <windows.h>
#include <stdio.h>

int main()
{
    // Active Code Page (ACP)
    UINT acp = GetACP();
    printf("ACP=%u\n", acp);

    // OEM Code Page
    UINT oemcp = GetOEMCP();
    printf("OEMCP=%u\n", oemcp);

    // User Default Locale ID
    LCID lcid = GetUserDefaultLCID();
    printf("LCID=0x%04X\n", lcid);

    // User Default UI Language (informational)
    LANGID langid = GetUserDefaultUILanguage();
    printf("UILANG=0x%04X\n", langid);

    // Thread Locale
    LCID threadLocale = GetThreadLocale();
    printf("THREADLOCALE=0x%04X\n", threadLocale);

    return 0;
}
```

**`tests/SmokeTest/LocaleTestApp/LocaleTestApp.vcxproj` 完整內容**：

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <ItemGroup Label="ProjectConfigurations">
    <ProjectConfiguration Include="Debug|Win32">
      <Configuration>Debug</Configuration>
      <Platform>Win32</Platform>
    </ProjectConfiguration>
    <ProjectConfiguration Include="Release|Win32">
      <Configuration>Release</Configuration>
      <Platform>Win32</Platform>
    </ProjectConfiguration>
  </ItemGroup>

  <PropertyGroup Label="Globals">
    <ProjectGuid>{A1B2C3D4-5678-9ABC-DEF0-SMOKE0000001}</ProjectGuid>
    <RootNamespace>LocaleTestApp</RootNamespace>
    <WindowsTargetPlatformVersion>10.0</WindowsTargetPlatformVersion>
  </PropertyGroup>

  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.Default.props" />

  <PropertyGroup Label="Configuration" Condition="'$(Configuration)|$(Platform)'=='Debug|Win32'">
    <ConfigurationType>Application</ConfigurationType>
    <PlatformToolset>v145</PlatformToolset>
    <CharacterSet>Unicode</CharacterSet>
    <UseDebugLibraries>true</UseDebugLibraries>
  </PropertyGroup>
  <PropertyGroup Label="Configuration" Condition="'$(Configuration)|$(Platform)'=='Release|Win32'">
    <ConfigurationType>Application</ConfigurationType>
    <PlatformToolset>v145</PlatformToolset>
    <CharacterSet>Unicode</CharacterSet>
    <UseDebugLibraries>false</UseDebugLibraries>
  </PropertyGroup>

  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" />

  <PropertyGroup>
    <OutDir>$(SolutionDir)Build\$(Configuration)\x86\</OutDir>
    <IntDir>$(Configuration)\</IntDir>
  </PropertyGroup>

  <ItemDefinitionGroup Condition="'$(Configuration)|$(Platform)'=='Release|Win32'">
    <ClCompile>
      <RuntimeLibrary>MultiThreaded</RuntimeLibrary>
      <Optimization>MaxSpeed</Optimization>
    </ClCompile>
    <Link>
      <SubSystem>Console</SubSystem>
    </Link>
  </ItemDefinitionGroup>
  <ItemDefinitionGroup Condition="'$(Configuration)|$(Platform)'=='Debug|Win32'">
    <ClCompile>
      <RuntimeLibrary>MultiThreadedDebug</RuntimeLibrary>
      <Optimization>Disabled</Optimization>
    </ClCompile>
    <Link>
      <SubSystem>Console</SubSystem>
    </Link>
  </ItemDefinitionGroup>

  <ItemGroup>
    <ClCompile Include="main.cpp" />
  </ItemGroup>

  <Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />
</Project>
```

**驗證方式**：

```powershell
msbuild tests/SmokeTest/LocaleTestApp/LocaleTestApp.vcxproj /p:Configuration=Release /p:Platform=Win32
# 執行確認輸出格式正確
.\Build\Release\x86\LocaleTestApp.exe
# 預期輸出類似：
# ACP=950
# OEMCP=950
# LCID=0x0404
# UILANG=0x0404
# THREADLOCALE=0x0404
```

---

### Task A2: 建立 Smoke Test PowerShell 腳本

**目的**：自動化 smoke test，針對不同 locale profile 啟動 LEProc + LocaleTestApp，比對輸出值。

- [ ] 建立 `tests/SmokeTest/Invoke-SmokeTest.ps1`
- [ ] 實作 locale profile 參數矩陣
- [ ] 實作執行與輸出捕獲邏輯
- [ ] 實作比對與結果報告

**`tests/SmokeTest/Invoke-SmokeTest.ps1` 完整內容**：

```powershell
<#
.SYNOPSIS
    Locale Emulator Smoke Test - 驗證 LEProc 對不同 locale 的 hook 效果。

.DESCRIPTION
    使用 LEProc 搭配不同 locale profile 啟動 LocaleTestApp.exe，
    擷取其 GetACP()/GetOEMCP()/GetUserDefaultLCID() 輸出並比對預期值。

.PARAMETER BuildDir
    建置輸出目錄路徑（預設為 repo 根目錄下 Build\Release\x86）

.PARAMETER Verbose
    顯示詳細輸出

.EXAMPLE
    .\Invoke-SmokeTest.ps1
    .\Invoke-SmokeTest.ps1 -BuildDir "C:\MyBuild\Release\x86"
#>

[CmdletBinding()]
param(
    [string]$BuildDir = (Join-Path $PSScriptRoot "..\..\Build\Release\x86")
)

$ErrorActionPreference = "Stop"

# ============================================================
# Smoke Test Matrix
# ============================================================
# 每個 locale profile 定義：
#   - Name:    人類可讀名稱
#   - Location: locale 名稱（傳給 LEProc -run）
#   - ACP:     預期 Active Code Page
#   - OEMCP:   預期 OEM Code Page
#   - LCID:    預期 Locale ID（十六進位字串，不含 0x 前綴）
# ============================================================

$TestMatrix = @(
    @{
        Name     = "Japanese (ja-JP)"
        Location = "ja-JP"
        ACP      = 932
        OEMCP    = 932
        LCID     = "0411"
    },
    @{
        Name     = "Traditional Chinese (zh-TW)"
        Location = "zh-TW"
        ACP      = 950
        OEMCP    = 950
        LCID     = "0404"
    },
    @{
        Name     = "Simplified Chinese (zh-CN)"
        Location = "zh-CN"
        ACP      = 936
        OEMCP    = 936
        LCID     = "0804"
    },
    @{
        Name     = "Korean (ko-KR)"
        Location = "ko-KR"
        ACP      = 949
        OEMCP    = 949
        LCID     = "0412"
    },
    @{
        Name     = "English (en-US)"
        Location = "en-US"
        ACP      = 1252
        OEMCP    = 437
        LCID     = "0409"
    }
)

# ============================================================
# Helper Functions
# ============================================================

function Write-TestHeader {
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host " Locale Emulator Smoke Test" -ForegroundColor Cyan
    Write-Host " $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host ""
}

function Write-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Detail = ""
    )
    if ($Passed) {
        Write-Host "  [PASS] $TestName" -ForegroundColor Green
    } else {
        Write-Host "  [FAIL] $TestName" -ForegroundColor Red
        if ($Detail) {
            Write-Host "         $Detail" -ForegroundColor Yellow
        }
    }
}

function Parse-LocaleTestOutput {
    param([string[]]$OutputLines)

    $result = @{}
    foreach ($line in $OutputLines) {
        if ($line -match '^(\w+)=(.+)$') {
            $result[$Matches[1]] = $Matches[2].Trim()
        }
    }
    return $result
}

# ============================================================
# Main
# ============================================================

Write-TestHeader

# 確認必要檔案存在
$leProcPath = Join-Path $BuildDir "LEProc.exe"
$testAppPath = Join-Path $BuildDir "LocaleTestApp.exe"
$loaderDllPath = Join-Path $BuildDir "LoaderDll.dll"
$localeEmulatorDllPath = Join-Path $BuildDir "LocaleEmulator.dll"

$prerequisites = @(
    @{ Path = $leProcPath;             Name = "LEProc.exe" },
    @{ Path = $testAppPath;            Name = "LocaleTestApp.exe" },
    @{ Path = $loaderDllPath;          Name = "LoaderDll.dll" },
    @{ Path = $localeEmulatorDllPath;  Name = "LocaleEmulator.dll" }
)

Write-Host "Checking prerequisites..." -ForegroundColor White
$allPresent = $true
foreach ($prereq in $prerequisites) {
    if (Test-Path $prereq.Path) {
        Write-Host "  [OK] $($prereq.Name)" -ForegroundColor Green
    } else {
        Write-Host "  [MISSING] $($prereq.Name) - $($prereq.Path)" -ForegroundColor Red
        $allPresent = $false
    }
}

if (-not $allPresent) {
    Write-Host ""
    Write-Host "ERROR: Missing prerequisite files. Cannot proceed." -ForegroundColor Red
    Write-Host "Ensure all projects are built and Core DLLs are in place." -ForegroundColor Yellow
    exit 1
}

# 先不透過 LEProc 直接跑一次，記錄 baseline
Write-Host ""
Write-Host "Running baseline (no locale emulation)..." -ForegroundColor White
try {
    $baselineOutput = & $testAppPath 2>&1
    $baseline = Parse-LocaleTestOutput $baselineOutput
    Write-Host "  System ACP=$($baseline.ACP), OEMCP=$($baseline.OEMCP), LCID=$($baseline.LCID)" -ForegroundColor Gray
} catch {
    Write-Host "  [ERROR] Failed to run baseline: $_" -ForegroundColor Red
}

# 執行各 locale 測試
$totalTests = 0
$passedTests = 0
$failedTests = 0

Write-Host ""
Write-Host "Running locale smoke tests..." -ForegroundColor White

foreach ($test in $TestMatrix) {
    Write-Host ""
    Write-Host "--- $($test.Name) ---" -ForegroundColor Cyan

    $totalTests++

    try {
        # 透過 LEProc -run 啟動 LocaleTestApp
        # LEProc 會根據 Location 參數設定 locale 並建立程序
        # 注意：實際參數格式可能因遷移後 LEProc CLI 變化而需調整
        $tempConfigPath = $testAppPath + ".le.config"

        # 產生臨時 .le.config 檔案（必須放在目標程式旁邊，LEProc -run 會讀取 <target-path>.le.config）
        $configXml = @"
<?xml version="1.0" encoding="utf-8"?>
<LEConfig>
  <Profiles>
    <Profile Name="SmokeTest_$($test.Location)" Guid="{$(New-Guid)}" MainMenu="false">
      <Location>$($test.Location)</Location>
      <Timezone>Tokyo Standard Time</Timezone>
      <RunAsAdmin>false</RunAsAdmin>
      <RedirectRegistry>false</RedirectRegistry>
      <IsAdvancedRedirection>false</IsAdvancedRedirection>
      <RunWithSuspend>false</RunWithSuspend>
    </Profile>
  </Profiles>
</LEConfig>
"@
        $configXml | Out-File -FilePath $tempConfigPath -Encoding UTF8

        # 使用 LEProc -run 執行目標程式
        # LEProc -run 會讀取 <target-path>.le.config（即目標程式路徑 + ".le.config"）
        $output = & $leProcPath -run $testAppPath 2>&1 | Out-String
        $outputLines = $output -split "`n"
        $parsed = Parse-LocaleTestOutput $outputLines

        # 比對結果
        $testPassed = $true
        $details = @()

        # 比對 ACP
        $actualACP = [int]($parsed.ACP)
        if ($actualACP -ne $test.ACP) {
            $testPassed = $false
            $details += "ACP: expected=$($test.ACP), actual=$actualACP"
        } else {
            Write-Verbose "  ACP=$actualACP (expected $($test.ACP)) OK"
        }

        # 比對 OEMCP
        $actualOEMCP = [int]($parsed.OEMCP)
        if ($actualOEMCP -ne $test.OEMCP) {
            $testPassed = $false
            $details += "OEMCP: expected=$($test.OEMCP), actual=$actualOEMCP"
        } else {
            Write-Verbose "  OEMCP=$actualOEMCP (expected $($test.OEMCP)) OK"
        }

        # 比對 LCID（十六進位比對）
        $actualLCID = $parsed.LCID -replace '^0x', ''
        $expectedLCID = $test.LCID
        if ($actualLCID -ne $expectedLCID) {
            $testPassed = $false
            $details += "LCID: expected=0x$expectedLCID, actual=0x$actualLCID"
        } else {
            Write-Verbose "  LCID=0x$actualLCID (expected 0x$expectedLCID) OK"
        }

        if ($testPassed) {
            $passedTests++
            Write-TestResult -TestName $test.Name -Passed $true
        } else {
            $failedTests++
            Write-TestResult -TestName $test.Name -Passed $false -Detail ($details -join "; ")
        }

        # 清理臨時 config
        if (Test-Path $tempConfigPath) {
            Remove-Item $tempConfigPath -Force
        }

    } catch {
        $failedTests++
        Write-TestResult -TestName $test.Name -Passed $false -Detail "Exception: $_"
    }
}

# ============================================================
# Summary
# ============================================================
Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " Summary" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Total:  $totalTests" -ForegroundColor White
Write-Host "  Passed: $passedTests" -ForegroundColor Green
Write-Host "  Failed: $failedTests" -ForegroundColor $(if ($failedTests -gt 0) { "Red" } else { "Green" })
Write-Host ""

if ($failedTests -gt 0) {
    Write-Host "SMOKE TEST FAILED" -ForegroundColor Red
    exit 1
} else {
    Write-Host "ALL SMOKE TESTS PASSED" -ForegroundColor Green
    exit 0
}
```

**驗證方式**：

```powershell
# 從 repo 根目錄執行
.\tests\SmokeTest\Invoke-SmokeTest.ps1 -Verbose
```

---

### Task A3: 建立 Smoke Test Matrix 文件

- [ ] 在 `tests/SmokeTest/README.md` 中記錄 smoke test 矩陣與執行方式

**`tests/SmokeTest/README.md` 完整內容**：

```markdown
# Locale Emulator Smoke Test

## 概述

Smoke test 驗證 LEProc + Core DLL 的 locale hook 是否正確生效。
使用一個極簡的 Win32 console app (`LocaleTestApp.exe`) 作為目標程式，
透過 LEProc 以不同 locale profile 啟動，擷取 Windows API 回傳值並比對。

## 測試矩陣

| Locale   | ACP  | OEMCP | LCID   | 說明             |
|----------|------|-------|--------|------------------|
| ja-JP    | 932  | 932   | 0x0411 | 日文（最常用）   |
| zh-TW    | 950  | 950   | 0x0404 | 繁體中文         |
| zh-CN    | 936  | 936   | 0x0804 | 簡體中文         |
| ko-KR    | 949  | 949   | 0x0412 | 韓文             |
| en-US    | 1252 | 437   | 0x0409 | 英文（基準比對） |

## 驗證 API

| API                        | 用途                     |
|----------------------------|--------------------------|
| `GetACP()`                 | Active Code Page         |
| `GetOEMCP()`              | OEM Code Page            |
| `GetUserDefaultLCID()`    | User Default Locale ID   |
| `GetUserDefaultUILanguage()` | UI Language (資訊參考) |
| `GetThreadLocale()`       | Thread Locale (資訊參考) |

## 執行方式

### 先決條件

1. 已完成完整建置（x86 Release）
2. `Build\Release\x86\` 包含：`LEProc.exe`, `LocaleTestApp.exe`, `LoaderDll.dll`, `LocaleEmulator.dll`

### 執行

```powershell
.\tests\SmokeTest\Invoke-SmokeTest.ps1
```

### 自訂建置目錄

```powershell
.\tests\SmokeTest\Invoke-SmokeTest.ps1 -BuildDir "D:\MyBuild\Release\x86"
```

## 已知限制

- 僅測試 x86 目標程式（Core hook 引擎限制）
- `GetUserDefaultUILanguage()` 不受 LE hook 影響，回傳系統 UI 語言
- 需要管理員權限才能在某些環境下正常執行 LEProc
```

---

## Part B: CI/CD (GitHub Actions)

### Task B1: 建立 GitHub Actions CI Workflow

**目的**：建立自動化 CI pipeline，每次 push/PR 觸發建置與測試。

- [ ] 建立 `.github/workflows/` 目錄
- [ ] 建立 `.github/workflows/ci.yml`
- [ ] 推送至 GitHub 並驗證 workflow 執行成功

**`.github/workflows/ci.yml` 完整內容**：

```yaml
# ============================================================
# Locale Emulator - CI Workflow
# ============================================================
# 觸發條件：push 至 master、所有 Pull Request
# 執行內容：建置全部專案（x86 + x64）、執行 .NET 測試、執行 C++ 測試
# Runner：windows-2025-vs2026（Windows Server 2025 + VS 2026 Build Tools）
# ============================================================

name: CI

# 觸發條件
on:
  push:
    branches: [ master ]        # push 至 master 時觸發
  pull_request:
    branches: [ master ]        # 對 master 發 PR 時觸發

# 環境變數（全域）
env:
  DOTNET_NOLOGO: true           # 不顯示 .NET 歡迎訊息
  DOTNET_CLI_TELEMETRY_OPTOUT: true  # 關閉遙測

jobs:
  build-and-test:
    name: Build & Test
    runs-on: windows-2025-vs2026     # Windows Server 2025 + VS 2026 Build Tools（public preview，日後可能併入 windows-latest）

    steps:
      # --------------------------------------------------------
      # Step 1: 取得原始碼
      # --------------------------------------------------------
      - name: Checkout repository
        uses: actions/checkout@v4

      # --------------------------------------------------------
      # Step 2: 安裝 .NET 10 SDK
      # --------------------------------------------------------
      # GitHub runner 預裝的 .NET 版本可能不含 .NET 10，
      # 使用 actions/setup-dotnet 確保版本正確。
      - name: Setup .NET 10 SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      # --------------------------------------------------------
      # Step 3: 設定 MSBuild
      # --------------------------------------------------------
      # 需要 MSBuild 來建置 C++ 專案（.vcxproj）。
      # microsoft/setup-msbuild 會尋找 VS Build Tools 並加入 PATH。
      - name: Setup MSBuild
        uses: microsoft/setup-msbuild@v2

      # --------------------------------------------------------
      # Step 4: 還原 .NET 相依性
      # --------------------------------------------------------
      - name: Restore .NET dependencies
        run: dotnet restore LocaleEmulator.sln

      # --------------------------------------------------------
      # Step 5: 建置 Debug|Win32（含測試專案）
      # --------------------------------------------------------
      # Debug 組態建置所有專案（含測試專案）。
      # 根據 spec solution matrix，測試專案只在 Debug 建置。
      - name: Build Solution (Debug|Win32)
        run: msbuild LocaleEmulator.sln /p:Configuration=Debug /p:Platform=Win32 /m /v:minimal

      # --------------------------------------------------------
      # Step 6: 執行 .NET 測試（xUnit）
      # --------------------------------------------------------
      # dotnet test 會自動探索並執行所有 xUnit 測試專案。
      # --no-build 避免重複建置（已在 Step 5 以 Debug 建置過）。
      - name: Run .NET tests
        run: dotnet test LocaleEmulator.sln --no-build --configuration Debug --logger "console;verbosity=detailed"

      # --------------------------------------------------------
      # Step 7: 執行 C++ 測試（Google Test）
      # --------------------------------------------------------
      # Google Test 產出獨立的 .exe，在 Debug 建置輸出中尋找。
      - name: Run C++ tests (Google Test)
        run: |
          $testExes = Get-ChildItem -Path "Build\Debug\x86" -Filter "*Tests*.exe" -Recurse
          $failed = $false
          foreach ($exe in $testExes) {
            Write-Host "Running: $($exe.FullName)" -ForegroundColor Cyan
            & $exe.FullName --gtest_output=xml:$($exe.DirectoryName)\$($exe.BaseName)_results.xml
            if ($LASTEXITCODE -ne 0) {
              Write-Host "FAILED: $($exe.Name)" -ForegroundColor Red
              $failed = $true
            }
          }
          if ($failed) { exit 1 }
        shell: pwsh

      # --------------------------------------------------------
      # Step 8: 建置 Release|Win32（產品建置，不含測試）
      # --------------------------------------------------------
      - name: Build Solution (Release|Win32)
        run: msbuild LocaleEmulator.sln /p:Configuration=Release /p:Platform=Win32 /m /v:minimal

      # --------------------------------------------------------
      # Step 9: 建置 Release|x64（僅 ShellExtension）
      # --------------------------------------------------------
      - name: Build Solution (Release|x64)
        run: msbuild LocaleEmulator.sln /p:Configuration=Release /p:Platform=x64 /m /v:minimal

      # --------------------------------------------------------
      # Step 8.5: 執行 Smoke Tests
      # --------------------------------------------------------
      - name: Smoke Tests
        run: .\tests\SmokeTest\Invoke-SmokeTest.ps1
        shell: pwsh
        if: success()

      # --------------------------------------------------------
      # Step 9: 上傳建置產物（可選）
      # --------------------------------------------------------
      # 將建置輸出上傳為 artifact，方便下載測試。
      - name: Upload build artifacts
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: build-output
          path: |
            Build/Release/x86/
            Build/Release/x64/
          retention-days: 7
```

---

### Task B2: 驗證 CI 通過

- [ ] 推送 branch 至 GitHub
- [ ] 在 GitHub web 介面確認 CI workflow 觸發
- [ ] 檢視 workflow 執行結果，所有 step 綠色通過
- [ ] 如有失敗，根據錯誤訊息修正並重新推送

**驗證指令**：

```bash
# 推送至遠端
git push -u origin feature/dotnet10-integration

# 用 gh CLI 查看 workflow 狀態
gh run list --workflow=ci.yml --limit 1
gh run view <run-id>
```

**常見問題排除**：

| 問題 | 原因 | 解決方式 |
|------|------|----------|
| `dotnet: command not found` | .NET SDK 未安裝 | 確認 `actions/setup-dotnet` 版本與 `dotnet-version` 設定 |
| `MSBuild not found` | VS Build Tools 未安裝 | 確認 `microsoft/setup-msbuild@v2` 與 runner 相容 |
| C++ 建置失敗 | 平台工具組版本不符 | 檢查 runner 預裝的 MSVC 版本，必要時調整 `.vcxproj` 的 `PlatformToolset` |
| 測試探索不到 | 測試專案未建置 | 移除 `--no-build` 或確認 Debug 組態已建置 |
| Google Test exe 找不到 | 輸出路徑不符 | 調整 Step 8 的搜尋路徑 |

---

## Part C: GitHub Actions Tutorial

### Task C1: 撰寫 GitHub Actions CI 完整教學

**目的**：為第一次接觸 GitHub Actions 的使用者，以本專案為範例，撰寫詳盡的教學文件。

- [ ] 建立 `docs/guides/github-actions-ci.md`

**`docs/guides/github-actions-ci.md` 完整內容**：

````markdown
# GitHub Actions CI 教學 --- 以 Locale Emulator 為例

本文件是寫給第一次使用 GitHub Actions 的開發者的完整教學。
以 Locale Emulator 專案的 CI workflow 為實際範例，逐步解說每個概念。

---

## 目錄

1. [什麼是 GitHub Actions？](#1-什麼是-github-actions)
2. [核心概念](#2-核心概念)
3. [我們的 CI Workflow 逐行解說](#3-我們的-ci-workflow-逐行解說)
4. [觸發條件詳解](#4-觸發條件詳解)
5. [Runner 環境說明](#5-runner-環境說明)
6. [如何在 GitHub 網頁查看結果](#6-如何在-github-網頁查看結果)
7. [常見失敗排除指南](#7-常見失敗排除指南)
8. [本專案 CI/CD 操作指南](#8-本專案-cicd-操作指南)
9. [未來可能性](#9-未來可能性)

---

## 1. 什麼是 GitHub Actions？

GitHub Actions 是 GitHub 內建的 CI/CD（持續整合/持續部署）平台。
簡單來說，它讓你在程式碼推送（push）或發 Pull Request 時，
**自動**在雲端機器上執行一系列任務：建置、測試、部署等。

**為什麼需要它？**

- **自動化建置**：不需要手動打開 Visual Studio 按 Build
- **自動化測試**：每次改動都自動跑測試，確保沒有破壞既有功能
- **品質把關**：PR 必須通過 CI 才能合併（可設定 branch protection）
- **透明度**：所有人都能在 GitHub 網頁上看到建置結果

**費用**：公開 repo 完全免費；私有 repo 每月有 2,000 分鐘免費額度。

---

## 2. 核心概念

GitHub Actions 有四個核心概念，由外到內層層包含：

```
Workflow (工作流程)
  └── Job (工作)
        └── Step (步驟)
              └── Action (動作)
```

### 2.1 Workflow（工作流程）

- 一個 `.yml` 檔案 = 一個 Workflow
- 放在 `.github/workflows/` 目錄下
- 定義「什麼時候觸發」和「要做什麼」
- 一個 repo 可以有多個 Workflow

**在我們的專案中**：`.github/workflows/ci.yml` 就是一個 Workflow。

### 2.2 Job（工作）

- Workflow 中的一個執行單位
- 每個 Job 跑在獨立的 Runner（虛擬機器）上
- 多個 Job 預設平行執行，也可以設定先後順序
- 如果一個 Job 失敗，不影響其他 Job（除非設定了依賴）

**在我們的專案中**：只有一個 Job `build-and-test`，因為建置和測試有順序依賴。

### 2.3 Step（步驟）

- Job 中的一個執行步驟
- 按順序依次執行
- 可以是「使用現有 Action」或「執行自訂指令」
- 每個 Step 都有名稱（`name:`），方便辨識

**在我們的專案中**：有 9 個 Step，從 checkout 到上傳 artifact。

### 2.4 Action（動作）

- 可重用的步驟單元，由社群或 GitHub 官方提供
- 用 `uses:` 關鍵字引用
- 格式：`owner/repo@version`

**在我們的專案中使用的 Action**：

| Action | 用途 |
|--------|------|
| `actions/checkout@v4` | 把 repo 程式碼下載到 Runner |
| `actions/setup-dotnet@v4` | 安裝指定版本的 .NET SDK |
| `microsoft/setup-msbuild@v2` | 設定 MSBuild 路徑 |
| `actions/upload-artifact@v4` | 上傳建置產物 |

### 2.5 Runner（執行器）

- 執行 Job 的機器
- GitHub 提供免費的 Hosted Runner（雲端虛擬機器）
- 可選 `ubuntu-latest`、`windows-2025-vs2026`、`macos-latest`
- 也可以自架 Self-hosted Runner

**在我們的專案中**：使用 `windows-2025-vs2026`，因為要建置 Windows 專案（C#、C++、WPF），需要 VS 2026 Build Tools。

---

## 3. 我們的 CI Workflow 逐行解說

以下是 `.github/workflows/ci.yml` 的完整逐行解說。

### 3.1 Workflow 名稱

```yaml
name: CI
```

這會顯示在 GitHub 的 Actions 頁面上。取個簡短、好辨識的名字。

### 3.2 觸發條件

```yaml
on:
  push:
    branches: [ master ]
  pull_request:
    branches: [ master ]
```

- `push` + `branches: [ master ]`：有人 push 到 `master` 分支時觸發
- `pull_request` + `branches: [ master ]`：有人對 `master` 發 PR 時觸發（包含 PR 更新）

### 3.3 全域環境變數

```yaml
env:
  DOTNET_NOLOGO: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true
```

- `DOTNET_NOLOGO`：不顯示 .NET CLI 的歡迎訊息，讓 log 更乾淨
- `DOTNET_CLI_TELEMETRY_OPTOUT`：關閉 .NET CLI 遙測回報

### 3.4 Job 定義

```yaml
jobs:
  build-and-test:
    name: Build & Test
    runs-on: windows-2025-vs2026
```

- `build-and-test`：Job 的 ID（用於引用）
- `name`：顯示在 GitHub UI 上的名稱
- `runs-on`：指定 Runner 環境

### 3.5 Step 1: Checkout

```yaml
- name: Checkout repository
  uses: actions/checkout@v4
```

把 repo 的程式碼下載到 Runner 的工作目錄。沒有這步，Runner 上什麼都沒有。

### 3.6 Step 2: Setup .NET 10

```yaml
- name: Setup .NET 10 SDK
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '10.0.x'
```

- `actions/setup-dotnet`：GitHub 官方的 .NET 安裝 Action
- `dotnet-version: '10.0.x'`：安裝 .NET 10 的最新 patch 版本（例如 10.0.100）
- `with:`：傳入 Action 的參數

### 3.7 Step 3: Setup MSBuild

```yaml
- name: Setup MSBuild
  uses: microsoft/setup-msbuild@v2
```

Microsoft 官方提供的 Action，尋找 Runner 上已安裝的 Visual Studio Build Tools，
並把 `msbuild.exe` 加入 `PATH` 環境變數，讓後續 step 可以直接用 `msbuild` 指令。

### 3.8 Step 4: Restore

```yaml
- name: Restore .NET dependencies
  run: dotnet restore LocaleEmulator.sln
```

還原 NuGet 套件。`run:` 表示直接執行命令列指令（不是使用 Action）。

### 3.9 Step 5: Build x86

```yaml
- name: Build Solution (x86)
  run: msbuild LocaleEmulator.sln /p:Configuration=Release /p:Platform=Win32 /m /v:minimal
```

- `/p:Configuration=Release`：使用 Release 組態
- `/p:Platform=Win32`：建置 Win32 平台（C++ 專案編譯為 x86，.NET 專案映射為 AnyCPU，靠 csproj 的 PlatformTarget 控制實際位元數）
- `/m`：啟用多核心平行建置
- `/v:minimal`：最少輸出量（減少 log 雜訊）

### 3.10 Step 6: Build x64

```yaml
- name: Build Solution (x64)
  run: msbuild LocaleEmulator.sln /p:Configuration=Release /p:Platform=x64 /m /v:minimal
```

x64 組態只會建置 ShellExtension（其他專案在 Solution 組態中標記為「不建置」）。

### 3.11 Step 7: .NET Tests

```yaml
- name: Run .NET tests
  run: dotnet test LocaleEmulator.sln --no-build --configuration Release --logger "console;verbosity=detailed"
```

- `dotnet test`：自動探索並執行所有 xUnit 測試專案
- `--no-build`：不重複建置（已在 Step 5 建置過）
- `--logger`：輸出詳細測試結果到 console

### 3.12 Step 8: C++ Tests

```yaml
- name: Run C++ tests (Google Test)
  run: |
    $testExes = Get-ChildItem -Path "Build\Release\x86" -Filter "*Tests*.exe" -Recurse
    ...
  shell: pwsh
```

- `run: |`：多行指令
- `shell: pwsh`：使用 PowerShell Core 執行
- Google Test 的 exe 直接跑就好，不需要特別的 test runner

### 3.13 Step 9: Upload Artifacts

```yaml
- name: Upload build artifacts
  uses: actions/upload-artifact@v4
  if: always()
  with:
    name: build-output
    path: |
      Build/Release/x86/
      Build/Release/x64/
    retention-days: 7
```

- `if: always()`：即使前面的 step 失敗，也執行這步（方便下載失敗時的建置產物來除錯）
- `retention-days: 7`：artifact 保留 7 天後自動刪除
- 上傳後可以在 GitHub Actions 的 run 頁面下載 zip

---

## 4. 觸發條件詳解

### 4.1 我們的設定

```yaml
on:
  push:
    branches: [ master ]
  pull_request:
    branches: [ master ]
```

這表示：

| 事件 | 觸發條件 | 範例 |
|------|----------|------|
| `push` | 有人直接 push 到 `master` | `git push origin master` |
| `pull_request` | 有人對 `master` 發 PR，或更新 PR | 建立 PR、push 新 commit 到 PR branch |

### 4.2 常見觸發條件

```yaml
# 所有 push（不限分支）
on: push

# 特定分支
on:
  push:
    branches: [ master, develop ]

# 特定路徑變更才觸發
on:
  push:
    paths:
      - 'src/**'
      - 'tests/**'
    paths-ignore:
      - '**.md'

# 手動觸發（在 GitHub 網頁上按按鈕）
on: workflow_dispatch

# 排程（每天 UTC 0:00）
on:
  schedule:
    - cron: '0 0 * * *'

# Tag 觸發（用於 Release workflow）
on:
  push:
    tags:
      - 'v*'
```

### 4.3 PR 的特殊行為

PR 觸發時，GitHub Actions 會自動在「PR 合併後的狀態」上跑 CI。
也就是說，它測試的是「如果你現在按 Merge，結果會是什麼」。
這是最安全的做法，確保合併後不會壞掉。

---

## 5. Runner 環境說明

### 5.1 什麼是 Runner？

Runner 是執行 CI 的虛擬機器。GitHub 提供免費的 Hosted Runner：

| Runner | OS | 用途 |
|--------|-----|------|
| `ubuntu-latest` | Ubuntu 24.04 | Linux 專案、Node.js、Python 等 |
| `windows-2025-vs2026` | Windows Server 2025 | .NET、C++/MSVC、WPF 等 Windows 專案 |
| `macos-latest` | macOS 15 | iOS/macOS 開發、Swift |

### 5.2 Windows Runner 預裝軟體

`windows-2025-vs2026` Runner 預裝了大量工具。與我們相關的：

- **Visual Studio Build Tools**（含 MSVC 編譯器和 MSBuild）
- **.NET SDK**（通常是最新的 LTS 版本，可能需要額外安裝 .NET 10）
- **Git**
- **PowerShell Core (pwsh)**
- **CMake**
- **NuGet**

完整清單：https://github.com/actions/runner-images

### 5.3 Runner 的生命週期

```
1. CI 觸發
2. GitHub 分配一台全新的 Windows VM
3. VM 上已預裝常用工具
4. 執行 Workflow 中的所有 Step
5. Workflow 結束後，VM 被銷毀（不保留任何狀態）
```

**重點**：每次 CI 都是全新的環境。不需要擔心上次 CI 的殘留檔案。
但也意味著每次都要重新安裝額外工具（如 .NET 10 SDK）。

---

## 6. 如何在 GitHub 網頁查看結果

### 6.1 從 Repository 首頁進入

1. 打開 GitHub repo 頁面
2. 點選上方的 **Actions** 分頁
3. 左側顯示所有 Workflow（我們只有「CI」）
4. 右側顯示每次執行的記錄

### 6.2 查看 Workflow Run

每一行代表一次 CI 執行：

```
  CI                                    #15     master    2m 30s    ✅
  CI                                    #14     PR #5     1m 45s    ❌
```

- 綠色勾 ✅ = 成功
- 紅色叉 ❌ = 失敗
- 黃色圈 ○ = 執行中

點進去可以看到每個 Job 的狀態。

### 6.3 查看 Job 詳情

點選 Job（例如「Build & Test」），展開後可以看到：

- 每個 Step 的名稱和狀態（勾/叉）
- 點選 Step 可以展開完整的 console 輸出
- 如果 Step 失敗，會自動展開失敗的那步

### 6.4 在 PR 上查看

發 PR 時，CI 結果會直接顯示在 PR 頁面底部：

```
  ✅ CI / Build & Test (pull_request) — All checks have passed
```

或

```
  ❌ CI / Build & Test (pull_request) — 1 failure
```

點「Details」可以跳到該 run 的詳情頁面。

### 6.5 下載 Artifact

如果 Workflow 有上傳 artifact（我們的 Step 9）：

1. 進入 Workflow Run 詳情頁面
2. 頁面底部有「Artifacts」區塊
3. 點選 artifact 名稱即可下載 zip

---

## 7. 常見失敗排除指南

### 7.1 .NET SDK 版本問題

**症狀**：

```
error : The current .NET SDK does not support targeting net10.0-windows.
```

**原因**：Runner 預裝的 .NET SDK 版本太舊。

**解決**：確認 `setup-dotnet` 的 `dotnet-version` 設定正確：

```yaml
- uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '10.0.x'    # 確認這裡寫 10.0.x
```

### 7.2 MSBuild 找不到

**症狀**：

```
'msbuild' is not recognized as an internal or external command
```

**原因**：`setup-msbuild` 沒有正確設定，或 Runner 上沒有 VS Build Tools。

**解決**：確認 `microsoft/setup-msbuild@v2` step 在 `msbuild` 指令之前。

### 7.3 C++ 平台工具組版本不符

**症狀**：

```
error MSB8020: The build tools for v145 cannot be found.
```

**原因**：Runner 上的 VS Build Tools 版本與 `.vcxproj` 指定的 `PlatformToolset` 不符。

**解決**：

1. 查看 Runner 預裝的 VS 版本
2. 修改 `.vcxproj` 的 `<PlatformToolset>` 改為 Runner 上有的版本
3. 或在 Workflow 中安裝對應版本的 Build Tools

### 7.4 測試失敗

**症狀**：

```
Failed   TestName
Error Message: Assert.Equal() Failure
```

**解決**：

1. 展開失敗的 Step 查看詳細錯誤訊息
2. 記下失敗的測試名稱和錯誤值
3. 在本機重現：`dotnet test --filter "FullyQualifiedName~TestName"`
4. 修正程式碼後 push，CI 會自動重新執行

### 7.5 Workflow 完全不觸發

**可能原因**：

1. `.yml` 檔案不在 `.github/workflows/` 目錄下
2. YAML 語法錯誤（縮排敏感）
3. 觸發條件不符合（branch 名稱、路徑篩選等）
4. Workflow 被停用（Actions 頁面 → 選擇 Workflow → Enable）

**除錯方式**：

```bash
# 驗證 YAML 語法
python -c "import yaml; yaml.safe_load(open('.github/workflows/ci.yml'))"

# 或使用 actionlint（GitHub 官方推薦的 lint 工具）
actionlint .github/workflows/ci.yml
```

### 7.6 Runner 磁碟空間不足

**症狀**：建置途中出現 I/O 錯誤。

**解決**：Windows Runner 通常有 14 GB 可用空間。如果不夠：

```yaml
- name: Free disk space
  run: |
    Remove-Item "C:\Program Files\dotnet\sdk\*" -Recurse -Force -ErrorAction SilentlyContinue
    # 移除不需要的預裝軟體
  shell: pwsh
```

---

## 8. 本專案 CI/CD 操作指南

### 8.1 日常開發流程

```
1. 建立 feature branch：git checkout -b feature/my-feature
2. 開發 + 本機測試：dotnet test
3. Push to remote：git push -u origin feature/my-feature
4. 在 GitHub 發 PR → CI 自動觸發
5. 等 CI 通過 → Code Review → Merge
```

### 8.2 CI 失敗怎麼辦

1. **不要慌**。CI 失敗是正常的，它就是設計來抓問題的。
2. 點進 Actions 頁面，找到失敗的 run
3. 展開紅色的 step，閱讀錯誤訊息
4. 在本機修正問題
5. Push 新 commit，CI 會自動重跑

### 8.3 手動重新執行

有時候 CI 失敗不是程式碼的問題（例如 GitHub 暫時故障、網路逾時）。
可以手動重跑：

1. Actions 頁面 → 點選失敗的 run
2. 右上角按「Re-run all jobs」
3. 等待重新執行

### 8.4 查看歷史紀錄

Actions 頁面會保留所有 CI 執行紀錄。可以：

- 篩選分支：左側 Filter 選擇特定 branch
- 篩選狀態：只看失敗的 run
- 搜尋：用 commit message 或 PR 標題搜尋

---

## 9. 未來可能性

以下功能不在目前 CI 範圍內，但未來可以擴充：

### 9.1 Release Workflow

打 tag 自動建置並發布到 GitHub Releases：

```yaml
on:
  push:
    tags:
      - 'v*'

jobs:
  release:
    runs-on: windows-2025-vs2026
    steps:
      - uses: actions/checkout@v4
      # ... build steps ...
      - name: Create Release
        uses: softprops/action-gh-release@v2
        with:
          files: Build/Release/**/*
```

### 9.2 Branch Protection

設定 master branch 規則：

1. Repository Settings → Branches → Add rule
2. Branch name pattern: `master`
3. 勾選「Require status checks to pass before merging」
4. 選擇「CI / Build & Test」
5. 這樣 PR 必須 CI 通過才能 Merge

### 9.3 Code Coverage

加入測試覆蓋率報告：

```yaml
- name: Run tests with coverage
  run: dotnet test --collect:"XPlat Code Coverage"

- name: Upload coverage
  uses: codecov/codecov-action@v4
```

### 9.4 Cache

快取 NuGet 套件以加速 CI：

```yaml
- uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
```

### 9.5 Matrix Build

同時在多個 OS 或 .NET 版本上測試：

```yaml
strategy:
  matrix:
    os: [windows-2025-vs2026, windows-2022]
    dotnet: ['10.0.x']
runs-on: ${{ matrix.os }}
```
````

---

## Part D: CLAUDE.md Rewrite

### Task D1: 完全改寫 CLAUDE.md

**目的**：反映 .NET 10 遷移後的新架構，包含所有專案關係、建置方式、測試策略。

- [ ] 備份現有 `CLAUDE.md`（git history 已保留，不需另存）
- [ ] 以下方完整內容覆寫 `CLAUDE.md`

**`CLAUDE.md` 完整內容**：

````markdown
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

# Run tests
dotnet test LocaleEmulator.sln
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

### Build Prerequisites

- .NET 10 SDK
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
| .NET unit tests | xUnit + NSubstitute | `dotnet test` |
| C++ unit tests | Google Test | Run `*Tests*.exe` binaries from `Build/Release/x86/` |
| Smoke tests | PowerShell + LocaleTestApp | `.\tests\SmokeTest\Invoke-SmokeTest.ps1` |

### Test Projects

- `tests/LECommonLibrary.Tests/` -- XML config parsing, profile model, system helpers
- `tests/LEProc.Tests/` -- codepage mapping, CultureInfo queries, CLI argument parsing, registry redirect
- `tests/LEGUI.Tests/` -- Shell Extension registration logic, i18n loading, ViewModel logic
- `tests/Core.Tests/` -- LEB/LEPEB struct layout, registry redirect entry serialization

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
````

---

## Part E: README.md Update

### Task E1: 更新 README.md

**目的**：反映新的建置方式、Core 整合說明、系統需求。

- [ ] 以下方完整內容覆寫 `README.md`

**`README.md` 完整內容**：

````markdown
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
````

---

## Part F: Final PR

### Task F1: 確認所有前置條件

- [ ] Plans 1, 2, 3 全部已合併至 `master`
- [ ] `feature/dotnet10-integration` branch 已從合併後的 `master` 建立
- [ ] 所有 Part A-E 的檔案已 commit

### Task F2: 建立整合 commit

- [ ] `git add` 所有新增/修改的檔案
- [ ] 提交 commit

```bash
git add .github/workflows/ci.yml
git add tests/SmokeTest/
git add docs/guides/github-actions-ci.md
git add CLAUDE.md
git add README.md

git commit -m "feat: add CI/CD, smoke tests, and documentation update

- Add GitHub Actions CI workflow (build x86/x64, .NET tests, Google Test)
- Add smoke test infrastructure (LocaleTestApp + PowerShell test script)
- Add GitHub Actions CI tutorial for first-time users
- Rewrite CLAUDE.md for .NET 10 + C++ architecture
- Update README.md with Core attribution, build prereqs, system requirements

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

### Task F3: 推送並建立 PR

- [ ] 推送 branch 至 GitHub
- [ ] 建立 PR，標題包含 `Closes #1`

```bash
# 推送
git push -u origin feature/dotnet10-integration

# 建立 PR
gh pr create \
  --title "feat: .NET 10 migration - integration, CI/CD & docs" \
  --body "$(cat <<'EOF'
## Summary

- Add GitHub Actions CI workflow for automated build & test
- Add smoke test infrastructure (LocaleTestApp.exe + PowerShell script)
- Add comprehensive GitHub Actions tutorial (docs/guides/github-actions-ci.md)
- Rewrite CLAUDE.md for new .NET 10 + C++ architecture
- Update README.md with Core attribution, build prerequisites, system requirements

## Integration Plan (Plan 4)

This PR completes the final phase of the .NET 10 migration:

- **Part A**: Smoke test infrastructure (x86 test app + PowerShell test script + test matrix)
- **Part B**: GitHub Actions CI (build x86/x64, .NET tests, Google Test, artifact upload)
- **Part C**: GitHub Actions tutorial for first-time users
- **Part D**: CLAUDE.md complete rewrite
- **Part E**: README.md update

## Test plan

- [ ] CI workflow triggers on push and passes all steps
- [ ] Smoke test script runs locally and reports correct results
- [ ] CLAUDE.md accurately describes new architecture
- [ ] README.md build instructions work from clean clone
- [ ] GitHub Actions tutorial is accurate and complete

Closes #1

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

### Task F4: 驗證 PR

- [ ] PR 頁面顯示 CI 已觸發
- [ ] CI 所有 step 通過（綠色）
- [ ] PR 正確關聯 Issue #1
- [ ] 確認 PR 描述完整

---

## Appendix: 檔案清單

本 Plan 產出的所有檔案：

| 檔案路徑 | 用途 | Part |
|----------|------|------|
| `tests/SmokeTest/LocaleTestApp/main.cpp` | Smoke test 目標程式 | A1 |
| `tests/SmokeTest/LocaleTestApp/LocaleTestApp.vcxproj` | 目標程式專案檔 | A1 |
| `tests/SmokeTest/Invoke-SmokeTest.ps1` | Smoke test 自動化腳本 | A2 |
| `tests/SmokeTest/README.md` | Smoke test 文件 | A3 |
| `.github/workflows/ci.yml` | GitHub Actions CI workflow | B1 |
| `docs/guides/github-actions-ci.md` | GitHub Actions 完整教學 | C1 |
| `CLAUDE.md` | 專案指引（全面改寫） | D1 |
| `README.md` | 專案 README（更新） | E1 |

## Appendix: 時間估計

| Task | 預估時間 |
|------|----------|
| A1: LocaleTestApp | 3 分鐘 |
| A2: Smoke Test Script | 5 分鐘 |
| A3: Smoke Test README | 2 分鐘 |
| B1: CI Workflow | 3 分鐘 |
| B2: 驗證 CI | 5 分鐘（含等待 CI 執行） |
| C1: GitHub Actions Tutorial | 5 分鐘（內容已完整提供） |
| D1: CLAUDE.md Rewrite | 3 分鐘（內容已完整提供） |
| E1: README.md Update | 2 分鐘（內容已完整提供） |
| F1-F4: PR 流程 | 5 分鐘 |
| **Total** | **~33 分鐘** |
