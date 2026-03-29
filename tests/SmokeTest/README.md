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
