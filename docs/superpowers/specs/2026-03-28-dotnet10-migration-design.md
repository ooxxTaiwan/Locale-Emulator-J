# 設計文件：Locale Emulator 升級至 .NET 10

> **Issue**: ooxxTaiwan/Locale-Emulator-J#1
> **日期**: 2026-03-28
> **狀態**: 設計確認

---

## 1. 目標與動機

將 Locale Emulator 從 .NET Framework 4.0 (Client Profile) 升級至 .NET 10 (LTS)。

**動機**：
- .NET Framework 4.0 已非推薦使用的 Windows 開發平台
- 效能與現代化：利用 .NET 10 的效能改進與新 API
- 開發體驗：使用 SDK-style csproj、dotnet CLI、C# 14、VS 2026
- 部署便利性：未來可轉為 self-contained 部署

**重要定位**：這個專案不只是「把 UI/CLI 升級到 .NET 10」，而是同時接手一個已停止維護的原生 hooking core（[Locale-Emulator-Core](https://github.com/xupefei/Locale-Emulator-Core)，最後 commit `ae7160d` 於 2021-08-23，repo 於 2022-04-15 被 owner 封存）。Core 的 syscall hooking 機制依賴特定 Windows 內部實作細節（opcode 模式比對），未來 Windows 更新可能破壞其相容性。

---

## 2. 決策摘要

| 項目 | 決策 | 理由 |
|------|------|------|
| 目標框架 | .NET 10 (LTS, `net10.0-windows`) | LTS 版本，支援至 2028/11 |
| IDE / 工具鏈 | VS 2026 (v18.0+) / .NET 10 SDK + MSVC Build Tools v145 | .NET 10 需要 VS 2026；支援純 CLI 開發 |
| C# 版本 | C# 14 | .NET 10 搭配版本 |
| Shell Extension | C++ 原生重寫（x86 + x64 雙版本），一次到位 | .NET 不適合 in-process COM Shell Extension |
| 部署方式 | Framework-dependent（未來考慮 self-contained） | 先求簡單，減少發布體積 |
| Core 整合 | 直接複製原始碼至 repo | 避免 submodule/subtree 複雜度 |
| 架構支援 | x86（Core hooking 限制）；Shell Extension 為 x86 + x64 | Core 的 syscall hooking 僅有 x86 實作 |
| LEInstaller | 功能併入 LEGUI（保留 per-user 與 all-users 雙模式） | 減少獨立專案維護，不犧牲現有安裝能力 |
| LEUpdater | 完全移除 | 非必要功能，未來另案重新設計 |
| UI 框架 | 維持 WPF | 遷移成本最低，.NET 10 完全支援 |
| ARM64 | 延後另案 | 範圍控制，Hook 引擎需從頭實作 |
| 全球化後端 | NLS 模式（`UseNls = true`）— **待驗證** | 需以 characterization test 比對 .NET Framework 4.0 與 .NET 10 的差異後決定 |
| 最低 Windows 版本 | Windows 10 1607+ | .NET 10 要求；不再支援 Win7/8 |
| 測試 | TDD（xUnit + Google Test + NSubstitute） | 確保遷移穩固 |
| 執行方案 | 方案 A：Bottom-Up（由內而外），含 Phase 0 基線建立 | 先重現 Core 可建置基線，再逐層向上 |

---

## 3. Solution 結構

```
Locale-Emulator-J/
├── LocaleEmulator.sln              # 統一 Solution（C# + C++ 混合）
│
├── src/
│   ├── LECommonLibrary/            # .NET 10 類別庫
│   ├── LEProc/                     # .NET 10 啟動器 (x86)
│   ├── LEGUI/                      # .NET 10 WPF 應用程式
│   │
│   ├── Core/                       # 原生 C++ (來自 Locale-Emulator-Core, commit ae7160d)
│   │   ├── LoaderDll/              # LeCreateProcess 匯出 DLL
│   │   ├── LocaleEmulator/         # 注入目標程式的 Hook DLL
│   │   ├── Loader/                 # 獨立命令列載入器（保留，可用於偵錯與測試）
│   │   ├── _Compilers/             # 修改版 VS2015 工具鏈（建置依賴，spike 驗證前保留）
│   │   ├── _Libs/                  # 未公開 API 靜態程式庫（解壓後 commit）
│   │   ├── _WDK/                   # WDK 標頭與程式庫
│   │   └── _Headers/               # 自訂標頭
│   │
│   └── ShellExtension/            # 新建 C++ COM Shell Extension
│
├── tests/
│   ├── LECommonLibrary.Tests/      # xUnit 測試
│   ├── LEProc.Tests/               # xUnit 測試
│   ├── LEGUI.Tests/                # xUnit 測試 (ViewModel 邏輯)
│   └── Core.Tests/                 # Google Test (C++)
│       ├── LoaderDll.Tests/        # LeCreateProcess 相關邏輯
│       └── LocaleEmulator.Tests/   # Hook 引擎基礎邏輯
│
├── docs/
│   ├── superpowers/specs/          # 設計文件
│   └── guides/
│       └── github-actions-ci.md    # GitHub Actions CI 教學
│
├── .github/
│   └── workflows/
│       └── ci.yml                  # CI workflow
│
├── Build/                          # 建置輸出
│   ├── Debug/
│   │   ├── x86/
│   │   └── x64/
│   └── Release/
│       ├── x86/
│       └── x64/
│
├── key.snk                         # 統一簽署金鑰
├── Directory.Build.props           # 共用 MSBuild 屬性
├── .gitignore                      # 更新後的 gitignore
├── CLAUDE.md
└── README.md
```

### 建置輸出結構

```
Build/Release/
├── x86/
│   ├── LEProc.exe                 # .NET (x86)
│   ├── LECommonLibrary.dll        # .NET (AnyCPU)
│   ├── LEGUI.exe                  # .NET (AnyCPU)
│   ├── LoaderDll.dll              # Core 原生 (x86)
│   ├── LocaleEmulator.dll         # Core 原生 (x86)
│   ├── ShellExtension.dll         # Shell Extension (x86)
│   └── Lang/
│       ├── DefaultLanguage.xml    # Shell Extension 語言檔
│       ├── DefaultLanguage.xaml   # LEGUI 語言檔
│       └── ...
└── x64/
    └── ShellExtension.dll         # Shell Extension (x64)
```

---

## 4. Core 整合

### Phase 0：建立可建置基線

在進行任何 .NET 10 遷移之前，先把 upstream Core 原樣納入 repo 並重現可建置狀態：

1. 複製 upstream `ae7160dc5deb97947396abcd784f9b98b6ee38b3` 至 `src/Core/`
2. 解壓所有 `.7z` 封存檔（`_Compilers`、`_Libs`、`_WDK`）
3. 使用 `_Compilers` 中的修改版工具鏈，在 VS 2026 環境下重現建置
4. 驗證產出的 `LoaderDll.dll` / `LocaleEmulator.dll` / `Loader.exe` 可正常運作
5. 此基線建立成功後，才進入後續步驟

### 來源與溯源（Provenance）

- 來源 repo：`https://github.com/xupefei/Locale-Emulator-Core`
- 釘定 commit：`ae7160dc5deb97947396abcd784f9b98b6ee38b3`（2021-08-23，最後一次更新）
- 授權：LGPL-3.0（程式碼）/ GPL-3.0（專案整體）
- **原始 `.7z` 封存檔保留**並記錄 SHA-256 hash（用於溯源驗證），解壓後的檔案另外 commit 供直接建置
- Repo 膨脹評估：`_Libs`、`_WDK` 解壓後合計約數十 MB，可接受。大型二進位檔視需要以 `.gitattributes` 標記 Git LFS
- **Patch 追蹤**：所有對 Core 的修改以獨立 commit 記錄，commit message 標註 `[Core]` 前綴，便於追溯與區分

### 建置系統調整

| 項目 | 現狀 | 目標 |
|------|------|------|
| 平台工具組 | `v140_xp` / `v141` | **spike**：嘗試 `v145`（VS 2026），成功前維持 `_Compilers` 工具鏈 |
| XP 相容 | 啟用（`_xp` 後綴） | 移除（最低 Windows 10） |
| 連結器 | 修改過的 VS2015 `link.exe`（`_Compilers`） | **spike**：嘗試標準連結器，失敗則保留修改版 |
| 組態 | 僅 `Release\|Win32` | `Debug\|Win32` + `Release\|Win32`（預留 x64 組態定義） |
| 輸出路徑 | 各專案獨立 | 統一至 `Build/{Config}/x86/` |

**`_Compilers` 處理策略**：upstream 三個 `.vcxproj` 都把 `..\_Compilers` 放在 `ExecutablePath` 第一位，這是明確的建置依賴而非可選項。修改版 `link.exe` 的具體差異需比對 `link.bak`（原版）與 `link.exe`（修改版）才能判斷能否用標準工具鏈取代。在 spike 驗證成功之前，`_Compilers` 必須保留。

### 依賴處理

| 依賴 | 處理方式 |
|------|----------|
| `_Compilers/_Compilers.7z` | 解壓並保留，作為建置依賴。Spike 驗證標準 VS 2026 連結器後才考慮移除 |
| `_Libs/_Libs.7z` | 解壓，直接 commit `.lib` 檔案；保留原始 `.7z` 及其 SHA-256 hash |
| `_WDK/` | 解壓並保留（未公開核心結構定義） |
| `_Headers/` | 保留原樣（覆蓋編譯器內建標頭） |

---

## 5. C++ Shell Extension 設計

### 職責

取代現有 LEContextMenuHandler（.NET COM DLL），實作為原生 C++ COM Shell Extension：

1. 使用者右鍵點擊 `.exe` → 顯示 Locale Emulator 子選單
2. 讀取 `LEConfig.xml` 取得全域 Profile 列表
3. 提供選單項目：
   - 「以預設設定執行」→ `LEProc.exe -run {path}`
   - 「管理此應用程式」→ `LEProc.exe -manage {path}`
   - 「管理所有設定」→ `LEProc.exe -global`
   - 每個 Profile → `LEProc.exe -runas {guid} {path}`
4. 支援 4K 顯示器偵測

**x86/WOW64 邊界**：Core 的 Hook 機制僅支援 x86 目標程式（含 WOW64 場景下的 32 位元程式）。Shell Extension 應讀取目標 exe 的 PE 標頭判斷架構：
- **x86 目標**：正常顯示所有選單項目
- **x64 目標**：顯示選單但標注「僅支援 32 位元應用程式」，或仍允許嘗試啟動（LEProc 會在無法 Hook 時回報錯誤）

### COM 介面

```
IShellExtInit    → 初始化，取得被右鍵點擊的檔案路徑
IContextMenu     → 建構選單、處理選單點擊事件
IClassFactory    → COM 物件工廠

匯出函式：
  DllGetClassObject / DllCanUnloadNow / DllRegisterServer / DllUnregisterServer
```

### 架構（x86 + x64）

Shell Extension **必須**提供 x64 版本，因為：
- 64 位元 Windows 上 Explorer.exe 是 64 位元程序
- 64 位元程序無法載入 32 位元 DLL（OS 硬性限制）
- 如果只有 x86 版本，右鍵選單在 64 位元 Windows 上完全不會出現

Shell Extension 不依賴 Core（只讀 XML + 啟動 LEProc.exe），x64 編譯毫無障礙。

### COM 註冊模型

Shell Extension 的右鍵選單要實際出現，需要**兩組**登錄檔條目：

**1. COM 伺服器註冊**（告訴 Windows DLL 在哪裡）：
```
HKCR\CLSID\{GUID}\InprocServer32
  (Default) = <DLL 完整路徑>
  ThreadingModel = Apartment
```

**2. Context Menu Handler 註冊**（告訴 Explorer 對所有檔案類型載入此 CLSID）：
```
HKCR\*\shellex\ContextMenuHandlers\{GUID}
  (Default) = <Friendly Name>
```

**架構與登錄檔對應**：
- 64 位元 Windows：Explorer 是 x64，讀取原生 64 位元登錄檔視圖。註冊 x64 DLL 至 `HKCR\CLSID\{GUID}\InprocServer32`
- 32 位元 Windows：不存在 `Wow6432Node`，直接註冊 x86 DLL 至 `HKCR\CLSID\{GUID}\InprocServer32`

注：`Wow6432Node` 是 64 位元 Windows 上的 WOW64 登錄檔重導向節點，供 32 位元程序存取；在 32 位元 Windows 上不存在。

### per-user 與 all-users 安裝

現有 LEInstaller 支援兩種安裝模式（`Form1.cs` 第 223-276 行），新設計必須保留此能力：

| 模式 | 登錄檔根 | 是否需要管理員 | 影響範圍 |
|------|----------|---------------|----------|
| **all-users** | `HKLM\Software\Classes\...` | 是 | 所有使用者 |
| **current-user** | `HKCU\Software\Classes\...` | 否 | 僅當前使用者 |

現有實作使用 `RegOverridePredefKey` 將 `HKCR` 重導向至 `HKCU\Software\Classes`，達到 per-user COM 註冊。新的 C++ Shell Extension 的 `DllRegisterServer` 預設寫入 HKCR（machine-wide）；per-user 註冊由 LEGUI 直接用 Registry API 寫入 `HKCU\Software\Classes\...` 下的對應 key。

### XML 解析

使用 **pugixml**（單一標頭檔，MIT 授權，API 簡潔，與 LGPL-3.0 相容）。

### 多語系

- 沿用現有 XML 格式與檔案結構（22 語言）
- 用 pugixml 讀取語言檔
- `GetUserDefaultUILanguage()` 偵測系統語言
- Fallback 至 `DefaultLanguage.xml`（英文）

### COM GUID

使用**新 GUID**（不沿用舊的 `C52B9871-...`），避免新舊版本 COM 註冊衝突。

### 專案結構

```
src/ShellExtension/
├── ShellExtension.vcxproj
├── ShellExtension.def          # DLL 匯出定義
├── dllmain.cpp                 # DLL 進入點 + COM 匯出函式
├── ClassFactory.cpp/.h         # IClassFactory 實作
├── ContextMenuHandler.cpp/.h   # IShellExtInit + IContextMenu 實作
├── ConfigReader.cpp/.h         # LEConfig.xml 解析
├── I18n.cpp/.h                 # 多語系載入
├── Resource.h / Resource.rc    # 資源定義與圖示
├── pugixml/                    # pugixml（vendored）
│   ├── pugixml.hpp
│   └── pugixml.cpp
└── Lang/                       # 語言檔案（22 語言）
```

### 註冊與安裝（整合至 LEGUI）

**權威規格**：所有 Shell Extension 註冊/解除安裝邏輯統一由 LEGUI 的 `ShellExtensionRegistrar` 類別處理。LEGUI **不呼叫** `DllRegisterServer`，不使用 `LoadLibrary`，不使用 `regsvr32`。兩種安裝模式走**同一個 helper、同一份 key 清單**，僅根 hive 不同。

**`ShellExtensionRegistrar` 規格**：

```
輸入：
  mode        = AllUsers | CurrentUser
  dllPath     = ShellExtension.dll 的完整路徑（由架構自動偵測決定）
  clsid       = 新 COM GUID

寫入的登錄檔（兩種模式共用同一份 key 清單）：

  {root}\Software\Classes\CLSID\{clsid}\InprocServer32
    (Default)        = {dllPath}
    ThreadingModel   = Apartment

  {root}\Software\Classes\*\shellex\ContextMenuHandlers\{clsid}
    (Default)        = LocaleEmulator Shell Extension

其中 {root} 依模式決定：
  AllUsers    → HKLM（需要管理員權限）
  CurrentUser → HKCU（不需要管理員權限）

Registry view（關鍵）：
  64 位元 OS → 強制使用 RegistryView.Registry64
               （確保寫入原生 64 位元視圖，Explorer.exe 讀取此視圖）
  32 位元 OS → 使用 RegistryView.Default
```

**架構自動偵測**：
- `Environment.Is64BitOperatingSystem`
- 64 位元 OS：`dllPath` 指向 `Build/x64/ShellExtension.dll`
- 32 位元 OS：`dllPath` 指向 `Build/x86/ShellExtension.dll`

**權限提升**：
- all-users 模式需要管理員權限寫入 HKLM
- LEGUI 使用 `SystemHelper.RunWithElevatedProcess` 以提升權限重新啟動自身

**`DllRegisterServer` / `DllUnregisterServer`（C++ DLL 側）**：
- 僅處理 `HKCR\CLSID\{GUID}\InprocServer32`（標準 COM 自註冊慣例）
- 僅供 CLI 使用者手動呼叫：`%SystemRoot%\System32\regsvr32.exe ShellExtension.dll`（64 位元版本）
- 不寫入 `ContextMenuHandlers`（CLI 使用者需自行處理，或使用 LEGUI）
- 這是備用方案，不是主要安裝路徑

**舊版清理**：`ShellExtensionRegistrar` 同時負責偵測舊 COM GUID（`C52B9871-...`）在 HKLM 和 HKCU 中的殘留，提供清理選項

---

## 6. .NET 專案遷移

### Directory.Build.props（共用屬性）

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <LangVersion>14</LangVersion>
    <SignAssembly>true</SignAssembly>
    <AssemblyOriginatorKeyFile>$(MSBuildThisFileDirectory)key.snk</AssemblyOriginatorKeyFile>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- UseNls 待驗證：需以 characterization test 比對 .NET Framework 4.0 與 .NET 10
       在 ICU/NLS 兩種模式下對所有支援 locale 的 ANSICodePage/OEMCodePage/LCID 差異後決定。
       UseNls=true 只影響 .NET 端的 CultureInfo 查詢結果，Core 本身直接操作 NLS 表不受此設定影響。 -->
  <!--
  <ItemGroup>
    <RuntimeHostConfigurationOption Include="System.Globalization.UseNls" Value="true" />
  </ItemGroup>
  -->
</Project>
```

### LECommonLibrary

| 項目 | 現狀 | 遷移後 |
|------|------|--------|
| 專案格式 | 舊式 `.csproj` | SDK-style |
| 輸出類型 | Class Library | Class Library |
| 平台 | AnyCPU | AnyCPU |
| Post-Build | copy 至 LEInstaller\Resources\ | 移除 |

**程式碼調整**：
- 移除 `AssemblyInfo.cs`（屬性移至 csproj）
- `LEConfig.cs`：`XDocument` API 完全相容
- `AssociationReader.cs`：`Registry.GetValue()` 在 `net10.0-windows` 下直接可用
- `GlobalHelper.cs`：移除 `GetLEVersion()` / `GetLastUpdate()` / `SetLastUpdate()`，版本改用 csproj `<Version>` + `Assembly.GetName().Version`
- P/Invoke 可選擇性改用 `[LibraryImport]`

### LEProc

| 項目 | 現狀 | 遷移後 |
|------|------|--------|
| 專案格式 | 舊式 `.csproj` | SDK-style |
| 輸出類型 | WinExe | WinExe |
| 平台 | x86 | x86（`<PlatformTarget>x86</PlatformTarget>`） |
| Unsafe | 啟用 | 維持（`<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`） |

**程式碼調整**：
- `Program.cs`：**移除** 第 24-32 行 LEUpdater 啟動程式碼
- `Program.cs`：版本顯示改用 `Assembly.GetName().Version`
- `LoaderWrapper.cs`：`Marshal` 操作、`[DllImport("LoaderDll.dll")]` 完全相容
- `app.manifest`：更新 supportedOS，移除 Win7/8
- `app.config`：移除（`runtimeconfig.json` 由 SDK 自動產生）

### LEGUI

| 項目 | 現狀 | 遷移後 |
|------|------|--------|
| 專案格式 | 舊式 `.csproj` | SDK-style（`<UseWPF>true</UseWPF>`） |
| 輸出類型 | WinExe | WinExe |
| 平台 | AnyCPU | AnyCPU |
| i18n | XAML 資源字典（22 語言） | 沿用 |

**新增功能**（從 LEInstaller 整合）：
- Shell Extension 安裝/解除安裝 UI（支援 per-user 與 all-users 雙模式）
- 安裝方式：統一由 `ShellExtensionRegistrar` 類別以 Registry API 寫入（64 位元 OS 強制 `RegistryView.Registry64`），兩種模式同一份 key 清單、僅根 hive 不同
- ADS（Zone Identifier）移除功能
- 舊版 COM GUID 清理偵測（HKLM + HKCU）

**程式碼調整**：
- 語言檔案用 `<None CopyToOutputDirectory="PreserveNewest">` 取代 post-build copy
- Shell Extension 安裝邏輯與 i18n 邏輯抽為獨立類別（可測試性）

### Nullable 遷移策略

- 遷移時逐檔修正明確的 null 問題
- 暫時無法處理的檔案使用 `#nullable disable`
- 測試專案從一開始完整啟用 nullable

### 移除的專案

| 專案 | 處理 |
|------|------|
| LEUpdater | 刪除整個目錄 |
| LEInstaller | 刪除整個目錄 |
| LEContextMenuHandler | 刪除整個目錄 |

### `LEVersion.xml` 處理

移除 `LEVersion.xml`，版本號改由 csproj `<Version>` 屬性管理，程式中用 `Assembly.GetName().Version` 讀取。

---

## 7. 測試策略

### 框架

| 層級 | 框架 | 理由 |
|------|------|------|
| .NET | xUnit | .NET 最主流，`dotnet test` 原生支援 |
| C++ | Google Test | VS 2026 Test Explorer 原生支援 |
| Mocking | NSubstitute | 語法簡潔，無爭議 |

### 各專案測試範圍

#### LECommonLibrary.Tests (xUnit)

| 測試對象 | 測試重點 |
|----------|----------|
| `LEConfig` | XML 讀寫：Profile 屬性解析、預設值、空檔案、格式錯誤 |
| `LEProfile` | 資料模型屬性存取、預設值 |
| `SystemHelper` | `Is64BitOS()`、`EnsureAbsolutePath()`、`CheckPermission()` 等純邏輯 |
| `AssociationReader` | 副檔名關聯查詢 |
| `PEFileReader` | PE 檔案格式解析（用測試用 PE 檔案） |

#### LEProc.Tests (xUnit)

| 測試對象 | 測試重點 |
|----------|----------|
| codepage 對應 | `GetCharsetFromANSICodepage()` 各 codepage 回傳正確 charset |
| CultureInfo 查詢 | NLS 模式下 `ANSICodePage`、`OEMCodePage`、`LCID` 正確性 |
| 命令列解析 | `-run`、`-runas`、`-manage`、`-global` 參數解析 |
| `RegistryEntriesLoader` | 不同 locale 產生的 Registry 重導向表正確性 |
| `RegistryEntry` | `GetValue` delegate 回傳正確值 |
| `LERegistryRedirector` | 條目新增、序列化為 byte array |
| `LoaderWrapper` 結構體 | `LEB` struct 大小、欄位偏移正確性 |

#### LEGUI.Tests (xUnit)

| 測試對象 | 測試重點 |
|----------|----------|
| Shell Extension 安裝邏輯 | 登錄檔路徑與值正確性 |
| i18n 語言載入 | 語言檔案讀取、fallback 邏輯 |
| Profile 編輯邏輯 | ViewModel 的儲存/刪除/新增邏輯 |

#### Core.Tests — 單元測試 (Google Test)

少量單元測試，覆蓋可隔離的邏輯：

| 測試對象 | 測試重點 |
|----------|----------|
| `LEB` 結構體 | 欄位大小、記憶體佈局、對齊 |
| `LEPEB` 結構體 | 共享記憶體區段資料結構 |
| Registry 重導向表 | `REGISTRY_REDIRECTION_ENTRY64` 序列化/反序列化 |
| NLS 檔案路徑 | codepage 對應正確的 NLS 檔案路徑 |
| Section 物件命名 | 命名規則正確性 |

**注意**：Core 的真正風險（遠端程序注入、PEB/NLS 重寫、syscall patching）無法用單元測試覆蓋。單元測試只是基本保障，不是信心來源。

#### Core — 整合 Smoke Test 矩陣（核心驗證手段）

Core 的信心來源是整合 smoke test，而非單元測試：

| 測試維度 | 測試項目 |
|----------|----------|
| 目標架構 | x86 EXE、x86 EXE on WOW64 |
| Locale Profile | ja-JP、zh-TW、zh-CN、ko-KR、en-US |
| OS 版本 | Windows 10（最新）、Windows 11（最新） |
| 驗證方式 | 啟動目標程式後檢查 `GetACP()`、`GetOEMCP()`、`GetUserDefaultLCID()` 回傳值 |
| 目標程式類型 | 簡單 Win32 console app、WinForms app、已知的真實應用程式 |

Smoke test 以腳本或測試 harness 自動化，記錄每個組合的 pass/fail 狀態。

#### Characterization Test — UseNls 驗證

在決定是否啟用 `System.Globalization.UseNls = true` 之前：

1. 在 .NET Framework 4.0 環境下，對所有支援的 locale 取得 `ANSICodePage`、`OEMCodePage`、`LCID`，記錄為基準值
2. 在 .NET 10 ICU 模式（預設）下，取得相同值，比對差異
3. 在 .NET 10 NLS 模式下，取得相同值，比對差異
4. 根據差異決定是否全域啟用 NLS 模式，或只在有差異的 locale 做特殊處理

### TDD 執行流程（Bottom-Up）

```
Phase 0: Core 基線     → 原樣納入 repo → 使用 _Compilers 工具鏈重現建置 → smoke test 驗證
Step 1:  Core 調整      → 先寫 Core 單元測試 → 調整建置系統 → 測試通過 → spike 標準工具鏈
Step 2:  Shell Extension → ConfigReader 可測試；COM 介面手動驗證
Step 3:  LECommonLibrary → 先寫 Tests → 遷移 → 測試通過
Step 4:  LEProc          → 先寫 Tests（含 characterization test）→ 遷移 → 測試通過
Step 5:  LEGUI           → 先寫 Tests → 遷移 + 整合 LEInstaller → 測試通過
Step 6:  清理            → 全部測試 + smoke test 確認無回歸
```

---

## 8. 建置系統

### Solution 組態矩陣

| 專案 | Debug\|x86 | Debug\|x64 | Release\|x86 | Release\|x64 |
|------|-----------|-----------|-------------|-------------|
| LECommonLibrary | AnyCPU ✓ | AnyCPU ✓ | AnyCPU ✓ | AnyCPU ✓ |
| LEProc | x86 ✓ | 不建置 | x86 ✓ | 不建置 |
| LEGUI | AnyCPU ✓ | AnyCPU ✓ | AnyCPU ✓ | AnyCPU ✓ |
| Core/LoaderDll | Win32 ✓ | 不建置 | Win32 ✓ | 不建置 |
| Core/LocaleEmulator | Win32 ✓ | 不建置 | Win32 ✓ | 不建置 |
| ShellExtension | Win32 ✓ | x64 ✓ | Win32 ✓ | x64 ✓ |
| LECommonLibrary.Tests | AnyCPU ✓ | AnyCPU ✓ | 不建置 | 不建置 |
| LEProc.Tests | x86 ✓ | 不建置 | 不建置 | 不建置 |
| LEGUI.Tests | AnyCPU ✓ | AnyCPU ✓ | 不建置 | 不建置 |
| Core.Tests | Win32 ✓ | 不建置 | 不建置 | 不建置 |

### Post-Build 事件

| 現有 | 遷移後 |
|------|--------|
| LECommonLibrary → LEInstaller\Resources\ | 移除 |
| LEContextMenuHandler → Lang\*.xml + DLL | 移除 |
| LEGUI → Lang\*.xaml | 改用 `<None CopyToOutputDirectory="PreserveNewest">` |

### CLI 建置方式

```bash
# 完整建置（solution platform 為 Win32/x64，非 x86）
msbuild LocaleEmulator.sln /p:Configuration=Release /p:Platform=Win32
msbuild LocaleEmulator.sln /p:Configuration=Release /p:Platform=x64

# 僅 .NET 專案
dotnet build src/LECommonLibrary/
dotnet build src/LEProc/
dotnet build src/LEGUI/

# 測試
dotnet test LocaleEmulator.sln
# Google Test 二進位檔直接執行
```

### 建置先決條件

- .NET 10 SDK
- MSVC Build Tools 2026（v145）— 可透過 VS 2026 安裝或獨立安裝 Build Tools

---

## 9. 清理項目

### 刪除項目

| 項目 | 動作 |
|------|------|
| `LEUpdater/` | 刪除整個目錄 |
| `LEInstaller/` | 刪除整個目錄 |
| `LEContextMenuHandler/` | 刪除整個目錄 |
| 各專案 `Properties/AssemblyInfo.cs` | 移除（屬性移至 csproj） |
| 各專案獨立的 `key.snk` | 移除（統一使用根目錄 `key.snk`） |
| `LEProc/LEVersion.xml` | 移除（改用 csproj `<Version>`） |
| `LEProc/Program.cs` 第 24-32 行 | 刪除 LEUpdater 啟動程式碼 |

### 更新項目

| 項目 | 動作 |
|------|------|
| `app.manifest`（LEProc） | 更新 supportedOS：移除 Win7/8，確認 Win10/11 |
| `Scripts/sign-package.ps1` | 更新路徑：`Build\Release\*` → `Build\Release\x86\*` + `Build\Release\x64\*` |
| `COPYING` / `COPYING.LESSER` | 保留，確認與 Core（LGPL-3.0 / GPL-3.0）授權一致 |
| `GlobalHelper.CheckCoreDLLs()` | 保留（檢查 `LoaderDll.dll` 和 `LocaleEmulator.dll` 是否存在），不需改動 |
| 舊 COM GUID 清理 | LEGUI 中偵測並清理 `C52B9871-E5E9-41FD-B84D-C5ACADBEC7AE`（HKLM + HKCU） |

### `.gitignore` 更新

現有 `.gitignore` 是 VS 2013 時代的基本規則，需要大幅擴展：

```
# 新增項目
Build/
**/bin/
**/obj/
*.user
*.suo
.vs/

# C++ 建置中間產物
*.ilk
*.exp
*.iobj
*.ipdb
x64/
Win32/

# .NET 10
*.runtimeconfig.json
*.deps.json
```

### `.gitattributes` 更新

若 Core 的 `_Libs`、`_WDK` 解壓後有大型二進位檔，加入 Git LFS 追蹤：

```
# 視需要加入
src/Core/_Libs/**/*.lib filter=lfs diff=lfs merge=lfs -text
```

### `README.md` 更新

- Core 來源說明（upstream repo URL、commit hash、演變原因）
- 建置先決條件（.NET 10 SDK、MSVC Build Tools 2026 v145）
- CLI 建置指令（`msbuild` 和 `dotnet build`）
- 最低系統需求（Windows 10 1607+）
- 授權說明（LGPL-3.0，Core 部分為 LGPL-3.0 / GPL-3.0）

### `CLAUDE.md` 更新

現有 CLAUDE.md 的內容幾乎全部過時，需要全面改寫。具體更新項目：

| 區段 | 現有內容 | 更新為 |
|------|----------|--------|
| **Project Overview** | 六個 C# 專案 | 三個 .NET 10 專案 + 兩個 C++ 專案 + Core 整合，含 Shell Extension 重寫背景 |
| **Build** | VS 2015/2017、.NET Framework 4.0、手動複製 Core DLL | VS 2026、.NET 10 SDK + MSVC v145、`msbuild` 一次建置全部、`dotnet build` 亦可 |
| **Build - Output** | `.\Build\Release\` | `.\Build\Release\x86\` + `.\Build\Release\x64\`（僅 ShellExtension） |
| **Build - Core** | 「built separately and must be copied」 | 「整合在 `src/Core/`，隨 Solution 一同建置」 |
| **Build - Test** | 「No test framework exists; testing is manual」 | xUnit（.NET）+ Google Test（C++）、`dotnet test`、smoke test 矩陣 |
| **Architecture** | 六個專案說明含 LEUpdater、LEInstaller、LEContextMenuHandler | 移除三個已刪除專案，新增 Core、ShellExtension 說明，含專案間關係與權責 |
| **Key Data Flow** | LEContextMenuHandler → LEProc | ShellExtension（C++）→ LEProc，Core 整合在 Solution 中 |
| **Conventions** | LEProc must remain x86、Post-build events 說明 | x86 限制原因詳述（Core hooking）、Shell Extension x86+x64、NLS 模式待驗證、`[Core]` commit 前綴慣例 |

**CLAUDE.md Architecture 區段應包含的專案間關係圖**：

```
使用者右鍵 .exe
    │
    ▼
ShellExtension (C++, x64)          ← COM Shell Extension，載入於 Explorer.exe
    │ 讀取 LEConfig.xml
    │ 呼叫 LEProc.exe -runas {guid} {path}
    ▼
LEProc (.NET 10, x86)              ← 核心啟動器
    │ 引用 LECommonLibrary
    │ P/Invoke 呼叫 LoaderDll.dll
    ▼
Core/LoaderDll (C++, x86)          ← 建立目標程序 + 注入 LocaleEmulator.dll
    │ 寫入 LEB 至共享記憶體
    │ 注入 DLL
    ▼
Core/LocaleEmulator (C++, x86)     ← 被注入到目標程序，Hook Windows API
    │ 讀取 LEB
    │ 重寫 PEB/NLS 表
    │ 安裝 syscall hook + EAT hook
    ▼
目標程式                            ← 認為自己跑在目標 locale 下
```

```
LEGUI (.NET 10, AnyCPU)             ← Profile 管理 UI + Shell Extension 安裝
    │ 引用 LECommonLibrary
    │ ShellExtensionRegistrar：以 Registry API 註冊/解除 Shell Extension
    │ 呼叫 LEProc.exe（管理模式）
    ▼
LECommonLibrary (.NET 10, AnyCPU)   ← 共用程式碼：LEConfig、LEProfile、SystemHelper
```

**各專案權責**：

| 專案 | 語言 | 架構 | 權責 |
|------|------|------|------|
| LECommonLibrary | C# | AnyCPU | XML config 讀寫、Profile 資料模型、OS 工具函式 |
| LEProc | C# | x86 | CLI 入口、CultureInfo 查詢、LEB 結構體組裝、P/Invoke 呼叫 Core |
| LEGUI | C# (WPF) | AnyCPU | Profile 管理 UI、Shell Extension 安裝/解除安裝（`ShellExtensionRegistrar`） |
| Core/LoaderDll | C++ | x86 | 匯出 `LeCreateProcess`：建立目標程序並注入 `LocaleEmulator.dll` |
| Core/LocaleEmulator | C++ | x86 | 被注入 DLL：Hook syscall/EAT，重寫 PEB NLS 表，攔截 locale 相關 API |
| ShellExtension | C++ | x86+x64 | COM Shell Extension：右鍵選單、讀取 config、啟動 LEProc |

---

## 10. CI/CD

### GitHub Actions CI Workflow

檔案：`.github/workflows/ci.yml`

觸發條件：push 至 `master`、所有 PR

Job 內容：
1. Checkout 程式碼
2. 安裝 .NET 10 SDK（`actions/setup-dotnet`）
3. 設定 MSBuild（`microsoft/setup-msbuild`）
4. 建置 Solution（x86 組態）
5. 建置 Solution（x64 組態，僅 ShellExtension）
6. 執行 .NET 測試（`dotnet test`）
7. 執行 C++ Google Test

Runner：`windows-latest`（需驗證是否包含 VS 2026 Build Tools）

### 文件交付

`docs/guides/github-actions-ci.md`：
1. GitHub Actions 概念說明（workflow、job、step、runner）
2. 本專案 CI workflow 逐行解說
3. 觸發條件設定
4. Runner 環境說明
5. 各 step 用途解說
6. 查看執行結果教學
7. 常見問題排除
8. 本專案 CI/CD 操作指南
9. 未來可能性（Release workflow、artifact 發布、branch protection）

### 未來可能性（不在本次範圍）

- Release workflow：打 tag 自動建置並發布至 GitHub Releases
- Branch protection：PR 必須通過 CI 才能合併
- Code coverage 報告

---

## 11. 遷移風險總結

| 風險 | 等級 | 緩解方式 |
|------|------|----------|
| **Windows 更新破壞 Core Hook 機制** | **最高** | Core 的 `HookSysCall_x86` 比對 `KiFastSystemCall` 的 5 種已知 opcode 模式，不匹配則回傳 `STATUS_HOOK_PORT_UNSUPPORTED_SYSTEM`。upstream 最後一次修正是 2021-08-23（Fix for Windows 11 build 10.22000.132），repo 於 2022-04-15 封存。Windows 11 24H2 的相容性**需以 Phase 0 smoke test 實測驗證**，不可假設。緩解：整合 smoke test 矩陣持續監控；長期需理解 HookPort 並維護 opcode 模式表 |
| **Core 工具鏈可重現性** | **高** | 三個 `.vcxproj` 都把 `_Compilers` 放在 `ExecutablePath` 第一位，修改版 `link.exe` 是明確建置依賴。緩解：Phase 0 先用 `_Compilers` 重現基線；標準工具鏈作為 spike 驗證 |
| CultureInfo ICU/NLS 差異影響 codepage 查詢 | 中 | characterization test 比對後決定 |
| Shell Extension COM 註冊/載入問題 | 中 | 提供 regsvr32 CLI 備案 |
| WPF XAML 資源字典相容性 | 低 | .NET 10 WPF 高度相容 |
| P/Invoke 行為差異 | 低 | Marshal API 完全相容 |

---

## 12. 已驗證的相容性

以下項目經驗證**不構成遷移阻礙**：

| 項目 | 狀態 |
|------|------|
| Registry API (`Microsoft.Win32.Registry`) | `net10.0-windows` 直接可用 |
| Marshal / P/Invoke (`[DllImport]`) | 完全相容 |
| BinaryFormatter（`.resx` 中繼資料） | 僅 Designer 元資料，非實際使用 |
| `AppDomain.CurrentDomain` | `UnhandledException` 事件相容 |
| WPF XAML 資源字典 | .NET 10 完全支援 |
| 強名稱簽署（`key.snk`） | 可繼續使用，runtime 不強制驗證 |
| `XDocument` XML API | 完全相容 |
