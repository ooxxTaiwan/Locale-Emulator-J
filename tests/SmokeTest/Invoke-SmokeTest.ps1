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
        # 重要：LEProc 透過 LeCreateProcess 建立新程序，目標 app 的 stdout
        # 不會回傳到 LEProc。因此 LocaleTestApp 將結果寫入檔案。
        $resultFile = Join-Path (Split-Path $testAppPath) "locale_result.txt"
        if (Test-Path $resultFile) { Remove-Item $resultFile -Force }

        & $leProcPath -run $testAppPath 2>&1 | Out-Null

        # 等待目標程式完成並寫入結果檔
        $waitCount = 0
        while (-not (Test-Path $resultFile) -and $waitCount -lt 30) {
            Start-Sleep -Milliseconds 500
            $waitCount++
        }

        if (-not (Test-Path $resultFile)) {
            throw "LocaleTestApp 未產出 locale_result.txt（等待 15 秒後逾時）"
        }

        $outputLines = Get-Content $resultFile
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

        # 清理臨時檔案
        if (Test-Path $tempConfigPath) { Remove-Item $tempConfigPath -Force }
        if (Test-Path $resultFile) { Remove-Item $resultFile -Force }

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
