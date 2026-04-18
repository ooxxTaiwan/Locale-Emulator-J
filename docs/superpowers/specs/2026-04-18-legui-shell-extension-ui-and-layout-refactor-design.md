# 設計文件：LEGUI Shell Extension 安裝 UI 與版面重構

> **Issue**: ooxxTaiwan/Locale-Emulator-J#19
> **日期**: 2026-04-18
> **狀態**: 設計確認

---

## 1. 目標與動機

在 LEGUI 中新增 Shell Extension 安裝/解除安裝 UI，取代目前必須手動執行 `regsvr32` 的流程；同時順勢整頓兩個主視窗（`GlobalConfig` / `AppConfig`）95% 重複的版面，抽出共用元件、修掉既有 UX 問題。

**動機**：

- `ShellExtensionRegistrar` 後端已完成（per-user / all-users 雙模式、`RegistryView.Registry64`、舊 CLSID 清理），但前端 UI 從缺，使用者必須手動打 `regsvr32`。
- `DefaultLanguage.xaml` 已加入 9 個相關 i18n key，但沒有任何 XAML 按鈕連結到後端。
- `GlobalConfig` 和 `AppConfig` 兩個視窗 95% 版面重複，XAML 和 code-behind 都複製了一份，違反 DRY；擴充新分頁（如 Shell Extension）時必須兩邊同步維護。
- 既有版面有多處排版/命名/互動問題（變數名與標籤不符、CheckBox 含義不明、空標題對話框、老派 `BlurEffect`、無 Save 成功提示等），使用者體驗欠佳。

**重要定位**：這個任務表面上是「加幾顆按鈕」，實際上是「將設定對話框從扁平單頁結構升級為分頁結構」。分頁結構是後續擴充（ADS 移除、LEUpdater 重設計、Shell Extension 進階選項等）的基礎，不是一次性 UI 修補。

---

## 2. 決策摘要

| 項目 | 決策 | 理由 |
|------|------|------|
| **UI scope** | Scope 2：兩視窗都改 TabControl + 抽出 `ProfileEditorControl` UserControl | Scope 1 會讓兩視窗版面分歧；Scope 3 合併視窗偏離 issue 主軸 |
| **分頁結構** | GlobalConfig：`Profile` / `Shell Extension` / `About`；AppConfig：`Profile` / `About`（不含 Shell Extension） | Shell Extension 是系統級設定，不屬於單檔編輯層級；About 兩視窗共用合理 |
| **Shell Ext 分頁佈局** | 依模式分組（`Current User` GroupBox + `All Users` GroupBox），舊版清理置於最下方 | 如實反映底層資料模型（HKCU / HKLM 兩個獨立登錄檔位置可並存） |
| **UAC 升權策略** | 子 process 提權：主 LEGUI 啟動 `LEGUI.exe --shell-ext <verb> <scope>` 並以 `Verb="runas"` 觸發 UAC | 不影響主 UI 狀態，使用者未儲存的 profile 編輯不會遺失 |
| **Profile 分頁重構** | 最小修改：保留 4 個 GroupBox、同步變數名與 Header、補 tooltip、修錯字、加寬視窗 | 結構最接近現況，使用者無陌生感；更大幅重組留待後續使用者反饋觸發 |
| **Miscellaneous → Display** | GlobalConfig 的 `Miscellaneous` GroupBox 更名為 `Display` | 剩下的 `ShowInMainMenu` 確實只與 UI 顯示相關，語意更精準 |
| **視窗尺寸策略** | `SizeToContent="Height"` + `MinWidth="420"` | 移除寫死的 `Height="377.16"` 老派做法；寬度 420 可完整顯示長 locale 名稱 |
| **About 分頁內容** | 產品名 + 版本、原作者致謝、GitHub 連結、LGPL-3.0 授權連結 | 基本資訊四項，不過度設計 |
| **既有 tech debt** | 一併修復：空標題對話框、`IsChecked` null check 現代化、`CREATE__SUSPENDED` 錯字、`BlurEffect` 改直接 modal、Save 成功狀態提示 | 動 UI 時順手解決，避免短期內二次觸碰 |

---

## 3. 架構設計

### 3.1 元件拆分

```
src/LEGUI/
├── GlobalConfig.xaml / .cs             # 主視窗 A：全域 profile 管理
│   └── TabControl: [Profile] [Shell Extension] [About]
├── AppConfig.xaml / .cs                # 主視窗 B：單檔 .le.config 編輯
│   └── TabControl: [Profile] [About]
├── ProfileEditorControl.xaml / .cs     # 【新增】共用 UserControl
│   └── 4 個 GroupBox：Locale / Timezone / Advanced / Display
├── ShellExtensionPanel.xaml / .cs      # 【新增】UserControl
│   └── Current User 區 + All Users 區 + 舊版清理區
├── AboutPanel.xaml / .cs               # 【新增】UserControl
├── ShellExtensionRegistrar.cs          # 【既有】後端不動
├── ShellExtensionConstants.cs          # 【既有】不動
└── App.xaml.cs                         # 加入 --shell-ext CLI 處理
```

### 3.2 ProfileEditorControl API

```csharp
public partial class ProfileEditorControl : UserControl
{
    // 綁定當前編輯的 profile
    public LEProfile Profile { get; set; }

    // AppConfig 設 false（單檔模式無 ShowInMainMenu 概念）
    public static readonly DependencyProperty ShowDisplayOptionsProperty =
        DependencyProperty.Register(nameof(ShowDisplayOptions), typeof(bool),
            typeof(ProfileEditorControl), new PropertyMetadata(true));

    public bool ShowDisplayOptions { get; set; }
}
```

- 使用 `DependencyProperty` 讓 `ShowDisplayOptions` 可在 XAML 內直接設定並支援 binding。
- `Profile` 屬性的同步模型：
  - **Setter（載入）**：外部設定 `Profile = someLeProfile` 時，UserControl 將欄位值填入內部的 ComboBox / CheckBox 控制項
  - **取值（儲存）**：UserControl 對外暴露 `LEProfile ReadCurrentValues()` 方法，從內部控制項讀出當前使用者編輯的值並回傳新 `LEProfile`
  - **GlobalConfig 使用方式**：`cbGlobalProfiles_SelectionChanged` 時設定 `profileEditor.Profile = _profiles[selectedIndex]`；`bSaveGlobalSetting_Click` 時 `_profiles[selectedIndex] = profileEditor.ReadCurrentValues()`
  - 不引入 MVVM framework、不使用 `INotifyPropertyChanged`，維持既有 code-behind 模式

### 3.3 Shell Extension 分頁內部佈局

```
┌──────────────────────────────────────────────┐
│ ┌ Current User ─────────────────────────────┐│
│ │  Status: Installed / Not Installed         ││
│ │  [Install]       [Uninstall]               ││
│ └────────────────────────────────────────────┘│
│ ┌ All Users 🛡 ──────────────────────────────┐│
│ │  Status: Installed / Not Installed         ││
│ │  [Install 🛡]    [Uninstall 🛡]            ││
│ └────────────────────────────────────────────┘│
│                                                │
│ ⚠ Old version residue detected                 │
│ [Clean up old registration 🛡]                 │
│ （條件性顯示：HasOldRegistration() == true）    │
└──────────────────────────────────────────────┘
```

**按鈕狀態規則**：

- `Install (mode)`：`IsInstalled(mode) == true` 時 disabled
- `Uninstall (mode)`：`IsInstalled(mode) == false` 時 disabled
- 🛡 圖示表示「按下會觸發 UAC」；若當前 process 已是 admin，`runas` verb 不會再彈 UAC
- 舊版清理區塊以 `Visibility="Collapsed"` 預設隱藏，`OnLoaded` 時呼叫 `HasOldRegistration()` 決定是否顯示

---

## 4. UAC 升權流程

### 4.1 CLI 參數設計

在 `App.xaml.cs` 的 `App_OnStartup` 處理新增支援：

```
LEGUI.exe --shell-ext <verb> <scope>
  verb  = install | uninstall | cleanup-old
  scope = current-user | all-users
```

範例：

```
LEGUI.exe --shell-ext install all-users      # 寫 HKLM（需 admin）
LEGUI.exe --shell-ext uninstall current-user # 刪 HKCU
LEGUI.exe --shell-ext cleanup-old            # 清 HKCU + HKLM 舊 CLSID（需 admin）
```

### 4.2 執行流程

使用者在 UI 按「Install (All Users) 🛡」時：

1. 主 LEGUI 判定當前 token 是否為 admin（`SystemHelper.IsAdministrator()`）。
2. 若已是 admin，直接呼叫 `ShellExtensionRegistrar.Register(AllUsers, dllPath)`，完成後刷新 UI 狀態。
3. 若非 admin，啟動子 process，並以非同步方式等待完成以不阻塞 UI thread：
   ```csharp
   var psi = new ProcessStartInfo
   {
       FileName = Environment.ProcessPath,
       Arguments = "--shell-ext install all-users",
       Verb = "runas",           // 觸發 UAC
       UseShellExecute = true
   };
   using var p = Process.Start(psi);
   button.IsEnabled = false;     // 防止重複點擊
   await p.WaitForExitAsync();   // 非同步等待，不阻塞 UI
   button.IsEnabled = true;
   RefreshStatus(p.ExitCode);
   ```
4. 子 process 啟動後走 CLI 路徑：不顯示 UI，執行對應 `ShellExtensionRegistrar` 方法，`ExitCode` 0 = 成功、非 0 = 失敗。
5. 主 process 讀取 `ExitCode`，刷新 `IsInstalled()` 狀態並顯示成功/失敗訊息（i18n key `InstallSuccess` / 錯誤訊息）。

**Async 注意事項**：UI 事件 handler 改為 `async void`（WPF 慣例），`WaitForExitAsync()` 保證 UI 執行緒在 UAC 對話框彈出期間仍能回應（雖然 UAC 對話框本身會遮蔽整個桌面，這是系統行為）。

### 4.3 取消與錯誤處理

- 使用者在 UAC 對話框點「否」：`Process.Start` 拋 `Win32Exception` (code 1223)，主 process 捕獲並視為使用者取消，UI 不顯示錯誤。
- 其他失敗（如 DLL 路徑錯誤、登錄檔鎖定）：子 process 以非 0 ExitCode 退出，主 process 顯示通用錯誤訊息。

---

## 5. Profile 分頁重構細項

### 5.1 版面修正對照表

| 項目 | Before | After |
|------|--------|-------|
| 視窗尺寸 | `Height="377.16" Width="310"` | `SizeToContent="Height" MinWidth="420"` |
| `DebugOptions` GroupBox 變數名 | Header 寫 "Advanced Options"，變數叫 `DebugOptions` | 變數改名 `advancedOptionsGroup`，Header 維持 "Advanced Options" |
| CheckBox tooltip | 無 | 每個 CheckBox 加 `ToolTip`（英文；其他語言翻譯屬 Issue 21） |
| `CREATE__SUSPENDED` 錯字 | i18n key 值寫 `CREATE__SUSPENDED`（雙底線） | 改 `CREATE_SUSPENDED` |
| Miscellaneous GroupBox | 只包含 `ShowInMainMenu` 一項 | 改名 `Display`，語意更精準；新增 i18n key `Display` |

### 5.2 Tooltip 文案（英文版，放入 `DefaultLanguage.xaml`）

| CheckBox | Tooltip i18n key | 內容 |
|----------|------------------|------|
| Run as administrator | `AsAdminTip` | Launch the target process with elevated privileges. Required for some games. |
| Fake language-related keys in Registry | `RedirectRegistryTip` | Intercept Registry reads for locale-related keys, returning the emulated locale instead of the system's. |
| Fake system UI language | `IsAdvancedRedirectionTip` | Also redirect Registry keys that control system UI language, affecting some apps that query via `MUI_LANGUAGE_NAME`. |
| Create process with CREATE_SUSPENDED | `WithCREATESUSPENDEDTip` | Create the target process in suspended state (advanced debugging option). |
| Show in Shell Extension top-level menu | `ShowInMainMenuTip` | Display this profile directly in the right-click menu root instead of under a submenu. |

### 5.3 其他 tech debt 修正

| 項目 | Before | After |
|------|--------|-------|
| 空標題對話框 | `MessageBox.Show(I18n.GetString("ConfirmDel"), "", ...)` | 第二參數改傳 `"Locale Emulator"` |
| null check 樣板 | `IsChecked != null && (bool)IsChecked` | `IsChecked == true` |
| `SaveAs` 的 `BlurEffect` | 手動套用 `BlurEffect` + 取消 | 移除，依賴 `ShowDialog()` 的 modal 行為 |
| Save 成功提示 | 無任何視覺回饋 | 在視窗底部加一 `StatusBar`，儲存成功時顯示 "Saved"，3 秒後淡出（或改顏色） |

---

## 6. About 分頁內容

```
┌──────────────────────────────────────────────┐
│  Locale Emulator (Chu's fork)                │
│  Version 3.0.0-dev                           │
│                                              │
│  Original project: https://github.com/       │
│    xupefei/Locale-Emulator                   │
│  Core: https://github.com/xupefei/           │
│    Locale-Emulator-Core (archived 2022-04)   │
│  This fork: https://github.com/ooxxTaiwan/   │
│    Locale-Emulator-J                         │
│                                              │
│  License: LGPL-3.0                           │
│  Core portion: LGPL-3.0 / GPL-3.0            │
└──────────────────────────────────────────────┘
```

- 版本號從 `Assembly.GetExecutingAssembly().GetName().Version` 讀取。
- 連結為 `Hyperlink`（WPF 原生），點擊以系統預設瀏覽器開啟。
- 內容不 i18n 到其他 22 種語言，僅英文（屬 Issue 21 範圍）。

---

## 7. 範圍邊界

### 7.1 本次做（in scope）

- [x] GlobalConfig 和 AppConfig 改 TabControl 結構
- [x] 抽出 `ProfileEditorControl` UserControl
- [x] 新增 Shell Extension 分頁（含 Install / Uninstall / 舊版清理）
- [x] 新增 About 分頁
- [x] CLI 參數 `--shell-ext` 支援（子 process 提權路徑）
- [x] Profile 分頁版面修正（變數名同步、tooltip、錯字、尺寸）
- [x] 其他 tech debt（空標題對話框、null check、BlurEffect、Save 成功提示）
- [x] 英文 i18n key 加入 `DefaultLanguage.xaml`
- [x] LEGUI.Tests 覆蓋新元件邏輯

### 7.2 本次不做（out of scope）

- **其他 21 種語言翻譯**：屬 Issue 21，本次僅加英文 key
- **ADS (Zone Identifier) 移除**：屬 Issue 20
- **LEUpdater 重新設計**：屬 Issue 11
- **合併 GlobalConfig 與 AppConfig 為單一視窗**：屬重大重構，另案
- **XAML 自訂主題/樣式系統**：維持現有樸素風格
- **MVVM framework 導入**：維持現有 code-behind 模式，避免本次引入大量樣板

---

## 8. 測試策略

### 8.1 單元測試（`tests/LEGUI.Tests/`）

- `ProfileEditorControlTests`：載入 / 儲存 LEProfile 的欄位雙向同步（Location、Timezone、各 CheckBox）、`ShowDisplayOptions = false` 時 Display GroupBox 隱藏
- `ShellExtensionPanelTests`：按鈕啟用/停用狀態對應 `IsInstalled()` 結果、舊版清理區塊條件顯示邏輯
- `CliArgumentParserTests`：`--shell-ext install all-users` 等組合正確解析成對應動作
- 既有 `ShellExtensionRegistrarTests` 不受影響

### 8.2 手動驗證（Release build 實測）

- 開啟 LEGUI，切至 Shell Extension 分頁，安裝（Current User），確認 Explorer 右鍵出現 LE 選單
- 解除安裝，確認右鍵選單消失
- 安裝（All Users），確認 UAC 彈出、子 process 寫 HKLM 成功、主 UI 狀態刷新
- 舊版 CLSID 手動寫入 HKCU，開啟 LEGUI 確認清理提示出現，執行清理後提示消失
- 拖放 `game.exe` 至 LEGUI 測試 AppConfig 模式，確認 Shell Extension 分頁**不存在**、About 分頁存在

---

## 9. 風險與回滾

| 風險 | 影響 | 緩解 |
|------|------|------|
| 子 process 啟動自身 LEGUI.exe，CLI 路徑萬一顯示 UI 會造成假死 | 嚴重 | 子 process 路徑嚴格走 `Application.Current.Shutdown()`，不 `InitializeComponent` 任何 Window |
| `ProfileEditorControl` 的 `Profile` 屬性單向同步若處理不當會遺失編輯 | 中 | 單元測試覆蓋雙向同步；保留既有 `bSaveGlobalSetting_Click` 的完整欄位逐項寫回邏輯 |
| `SizeToContent` 可能在某些 DPI 設定下產生視窗跳動 | 低 | 若實測出現問題，降級為固定尺寸（`MinWidth + MinHeight`） |
| 既有使用者習慣 310×377 視窗尺寸，改 420 後不適應 | 低 | 視窗尺寸變化屬可接受的改善，不提供向下相容選項 |

**回滾策略**：本改動集中在 LEGUI 專案，不動 LEProc / Core / ShellExtension，若出現嚴重問題可單獨 revert LEGUI 的 commits，其他元件不受影響。

---

## 10. 設計階段提案與討論記錄

為保留決策透明度，本節記錄本次 brainstorming 中提出的替代方案與捨棄原因。

### 10.1 UI scope

- **Scope 1**（僅 GlobalConfig 改）被捨棄：會讓兩視窗版面分歧，未來維護成本更高
- **Scope 3**（合併兩視窗）被捨棄：偏離 issue 19 主軸，屬大型 refactoring，應另案

### 10.2 Tab 結構

- **方案 A**（不對稱：僅 GlobalConfig 有 Tab）被捨棄：兩視窗視覺差異大，感覺像兩個不同 app
- **方案 C**（完全對稱，AppConfig 也含 Shell Extension）被捨棄：語意錯位，使用者會誤以為是為單一 exe 安裝擴充

### 10.3 Shell Extension 分頁佈局

- **方案 A**（扁平 4 按鈕）被捨棄：狀態和動作未就近擺放，需跨區域視線移動
- **方案 C**（單一動作 + radio button）被捨棄：掩蓋「兩模式可並存」的事實，若 CU 已裝 AU 未裝無法如實表達

### 10.4 UAC 升權策略

- **方案 A**（重啟整個 LEGUI）被捨棄：Profile 分頁未儲存編輯會遺失，重啟後使用者還要再點一次按鈕
- **方案 C**（不特殊處理，讓失敗）被捨棄：違反 Windows 慣例（🛡 應主動觸發 UAC），UX 最差

### 10.5 Profile 分頁重構

- **方案 B**（併 Display 到 Advanced）被捨棄：UI 顯示和執行權限概念不同，強行合併語意糊
- **方案 C**（語意重組四分組）被捨棄：偏離既有習慣較多，本次以「最小修改」為原則，進階重組留待使用者回饋觸發

---

## 11. 實作順序建議

預估分為 6 個 logical 步驟，每步驟應有獨立的測試覆蓋並各自可 commit：

1. 新增 `ProfileEditorControl` UserControl（含測試），暫不接入主視窗
2. 重構 `GlobalConfig.xaml` 改用 TabControl，Profile 分頁放入 ProfileEditorControl
3. 重構 `AppConfig.xaml` 同步改 TabControl
4. 新增 `ShellExtensionPanel` + Shell Extension 分頁（UI 層先不處理 UAC）
5. 新增 CLI 參數 `--shell-ext` 處理 + 子 process 提權邏輯
6. 新增 About 分頁 + 清掃既有 tech debt（MessageBox 標題、null check、BlurEffect、Save 提示）

詳細實作計畫（TDD 循環、子任務拆分、驗收標準）由後續的 writing-plans 階段產出。

---

## 12. 相關文件

- Issue: ooxxTaiwan/Locale-Emulator-J#19
- 相關 issue：#20 (ADS 移除)、#21 (i18n 翻譯)
- 後端實作（已完成）：`src/LEGUI/ShellExtensionRegistrar.cs`
- 先前設計文件：`docs/superpowers/specs/2026-03-28-dotnet10-migration-design.md`
