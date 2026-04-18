# Issue #21 i18n 補全 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 補全 LEGUI 21 locale 翻譯（24 既有 gap key + 15 從寫死字串抽出的新 key）、改良 4 個既有 key 命名、修正 ShellExtension 22 檔翻譯品質、加完整性測試。

**Architecture:** 6 個 commit 線性執行。Phase A 動 LEGUI 原始碼（A1 rename 4 key、A2 抽 15 寫死字串）；Phase B 我做 lead locale zh-TW；Phase C 平行 dispatch 20 agents 翻譯其餘 locale；Phase D 我修 ShellExt 品質；Phase E 加完整性測試；Phase F 最終驗證。

**Tech Stack:** C# .NET 10 / WPF XAML / xUnit / NSubstitute / PowerShell（mass rename）

**Spec:** `docs/superpowers/specs/2026-04-19-issue-21-i18n-translations-design.md`

**Worktree:** `E:\Code\Locale-Emulator\.claude\worktrees\issue-21-i18n-translations\`

**Branch:** `feature/issue-21-i18n-translations`

---

## File Structure

### 修改

| 檔案 | 由哪個 task 動 | 用途 |
|------|--------------|------|
| `src/LEGUI/Lang/DefaultLanguage.xaml` | Task 1, 2 | 4 key rename + 15 新 key + WithCreateSuspended value 改良 |
| `src/LEGUI/Lang/{ca,cs,de,es,fr,ind,it,ja,ka,ko,lt,nb,nl,pl,pt-BR,ru,th,tr-TR,zh-CN,zh-HK,zh-TW}.xaml` | Task 1, 3, 4 | 4 key rename + 39 key 翻譯補齊 + typo 修正 |
| `src/LEGUI/AppConfig.xaml` | Task 2 | Title 改用 DynamicResource |
| `src/LEGUI/AppConfig.xaml.cs` | Task 1, 2 | ConfirmDel rename + 抽 SetDescription / 2 個 MessageBox title |
| `src/LEGUI/GlobalConfig.xaml` | Task 2 | Title 改用 DynamicResource |
| `src/LEGUI/GlobalConfig.xaml.cs` | Task 1, 2 | ConfirmDel rename + 抽 1 個 MessageBox title |
| `src/LEGUI/AboutPanel.xaml` | Task 2 | 7 個 TextBlock 改用 DynamicResource |
| `src/LEGUI/InputBox.xaml` | Task 2 | bOk/bCancel Content 改用 DynamicResource |
| `src/LEGUI/App.xaml.cs` | Task 2 | 4 個 MessageBox 改用 i18n + 補上 unhandled exception title |
| `src/LEGUI/ShellExtensionPanel.xaml.cs` | Task 2 | 1 個 MessageBox title 改用 i18n |
| `src/LEGUI/ProfileEditorControl.xaml` | Task 1 | rename DebugOptions/WithCREATESUSPENDED/WithCREATESUSPENDEDTip 的 DynamicResource ref |
| `src/ShellExtension/Lang/{8 specific}.xml` | Task 5 | Submenu 統一為 "Locale Emulator" |
| `src/ShellExtension/Lang/{fr,ja,zh-TW,zh-HK,zh-CN,th}.xml` | Task 5 | 明顯錯誤修正 |
| `src/ShellExtension/Lang/{pt-BR,ind}.xml` | Task 5 | capitalization 一致性 |
| `tests/LEGUI.Tests/LEGUI.Tests.csproj` | Task 6 | 加 `<None Include="..\..\src\LEGUI\Lang\*.xaml" CopyToOutputDirectory="PreserveNewest" />` |

### 新增

| 檔案 | 用途 |
|------|------|
| `tests/LEGUI.Tests/LocaleCompletenessTests.cs` | xUnit `[Theory]` 測試 21 locale 完整性 + 非空值 |

---

## Task 1: 4 個既有 key rename（commit 1）

**目標**：將 4 個既有 key 改名（`ConfirmDel→ConfirmDelete`、`DebugOptions→AdvancedOptions`、`WithCREATESUSPENDED→WithCreateSuspended`、`WithCREATESUSPENDEDTip→WithCreateSuspendedTip`）。

**Files:**
- Modify: `src/LEGUI/Lang/DefaultLanguage.xaml:12, 17, 21, 37`
- Modify: `src/LEGUI/AppConfig.xaml.cs:113`
- Modify: `src/LEGUI/GlobalConfig.xaml.cs:119`
- Modify: `src/LEGUI/ProfileEditorControl.xaml:17, 22`
- Modify: `src/LEGUI/Lang/*.xaml`（21 locale 檔，mass rename）

### Step 1: 改 DefaultLanguage.xaml 的 4 個 key 名

讀 `src/LEGUI/Lang/DefaultLanguage.xaml`，逐個改：

- 第 12 行：`x:Key="ConfirmDel"` → `x:Key="ConfirmDelete"`
- 第 17 行：`x:Key="DebugOptions"` → `x:Key="AdvancedOptions"`
- 第 21 行：`x:Key="WithCREATESUSPENDED"` → `x:Key="WithCreateSuspended"`
- 第 37 行：`x:Key="WithCREATESUSPENDEDTip"` → `x:Key="WithCreateSuspendedTip"`

值不動。

### Step 2: 改 AppConfig.xaml.cs:113 的 i18n 字串引用

```csharp
// 第 113 行
I18n.GetString("ConfirmDel"),
```
改為：
```csharp
I18n.GetString("ConfirmDelete"),
```

### Step 3: 改 GlobalConfig.xaml.cs:119

同 Step 2，第 119 行 `I18n.GetString("ConfirmDel")` → `I18n.GetString("ConfirmDelete")`。

### Step 4: 改 ProfileEditorControl.xaml 的 3 個 DynamicResource 引用

第 17 行（GroupBox Header）：
```xml
<GroupBox Grid.Row="2" Header="{DynamicResource DebugOptions}" Margin="4,2">
```
改為：
```xml
<GroupBox Grid.Row="2" Header="{DynamicResource AdvancedOptions}" Margin="4,2">
```

第 22 行（CheckBox Content + ToolTip）：
```xml
<CheckBox x:Name="cbStartAsSuspend" Content="{DynamicResource WithCREATESUSPENDED}" ToolTip="{DynamicResource WithCREATESUSPENDEDTip}" Margin="0,2" />
```
改為：
```xml
<CheckBox x:Name="cbStartAsSuspend" Content="{DynamicResource WithCreateSuspended}" ToolTip="{DynamicResource WithCreateSuspendedTip}" Margin="0,2" />
```

### Step 5: 21 locale 檔 mass rename（PowerShell 腳本）

建立並執行（pwsh）：

```powershell
$patterns = @(
    @{ Old = 'x:Key="WithCREATESUSPENDEDTip"'; New = 'x:Key="WithCreateSuspendedTip"' }
    @{ Old = 'x:Key="WithCREATESUSPENDED"';    New = 'x:Key="WithCreateSuspended"' }
    @{ Old = 'x:Key="DebugOptions"';            New = 'x:Key="AdvancedOptions"' }
    @{ Old = 'x:Key="ConfirmDel"';              New = 'x:Key="ConfirmDelete"' }
)
Get-ChildItem -Path src/LEGUI/Lang -Filter *.xaml | ForEach-Object {
    $content = Get-Content -Path $_.FullName -Raw
    foreach ($p in $patterns) {
        $content = $content -replace [regex]::Escape($p.Old), $p.New
    }
    Set-Content -Path $_.FullName -Value $content -NoNewline
}
```

**注意**：`WithCREATESUSPENDEDTip` 必須先處理（順序優先），否則 `WithCREATESUSPENDED` 會先 match 到部分字串。腳本已正確排序。

DefaultLanguage 也包含在路徑下（會被腳本掃到），但 Step 1 已手動改，腳本對它是 no-op（key 已是新名）。

### Step 6: Build 驗證

執行：
```bash
dotnet build src/LEGUI/ --nologo
```
預期：build 成功，0 errors。warnings 可能有，但 i18n key 名變更不會引發 compile error（runtime lookup）。

### Step 7: Run LEGUI tests

執行：
```bash
dotnet test tests/LEGUI.Tests/ --nologo
```
預期：26 既有 test 全綠（rename 不影響邏輯）。

### Step 8: Commit

```bash
git add src/LEGUI/Lang/*.xaml src/LEGUI/AppConfig.xaml.cs src/LEGUI/GlobalConfig.xaml.cs src/LEGUI/ProfileEditorControl.xaml
git commit -m "$(cat <<'EOF'
refactor(LEGUI-i18n): rename 4 keys for naming consistency

- ConfirmDel → ConfirmDelete (避免無意義縮寫)
- DebugOptions → AdvancedOptions (key 名與英文 value "Advanced Options" 對齊)
- WithCREATESUSPENDED → WithCreateSuspended (Win32 API 常數應在 value 不在 key)
- WithCREATESUSPENDEDTip → WithCreateSuspendedTip (同上)

22 個 locale 檔 + 4 個 source code 引用同步更新。
翻譯值不變；新 key 對應的舊翻譯仍正常 lookup。

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: 抽 15 個寫死字串 → DefaultLanguage 新 key + source code refactor（commit 2）

**目標**：把 15 個寫死的 user-facing 字串抽到 i18n 系統，連帶改良 `WithCreateSuspended` 的 label 文字。

**Files:**
- Modify: `src/LEGUI/Lang/DefaultLanguage.xaml`（+15 key、改 WithCreateSuspended value）
- Modify: `src/LEGUI/App.xaml.cs:19, 68-74, 76-81, 98-101`
- Modify: `src/LEGUI/AboutPanel.xaml:5-33`
- Modify: `src/LEGUI/AppConfig.xaml:5`
- Modify: `src/LEGUI/AppConfig.xaml.cs:73, 85, 114`
- Modify: `src/LEGUI/GlobalConfig.xaml:5`
- Modify: `src/LEGUI/GlobalConfig.xaml.cs:120`
- Modify: `src/LEGUI/InputBox.xaml:10, 11`
- Modify: `src/LEGUI/ShellExtensionPanel.xaml.cs:21`

### Step 1: 在 DefaultLanguage.xaml 加 15 個新 key

在 `src/LEGUI/Lang/DefaultLanguage.xaml` 的 `</ResourceDictionary>` 之前，加入以下 15 行（按字母順序排在 `ShellExtOperationFailed` 之後即可）：

```xml
    <system:String x:Key="AppName">Locale Emulator</system:String>
    <system:String x:Key="Ok">OK</system:String>
    <system:String x:Key="AboutTitle">Locale Emulator (ooxxTaiwan fork)</system:String>
    <system:String x:Key="AboutVersion">Version </system:String>
    <system:String x:Key="AboutOriginalProject">Original project</system:String>
    <system:String x:Key="AboutCore">Core (archived 2022-04)</system:String>
    <system:String x:Key="AboutThisFork">This fork</system:String>
    <system:String x:Key="AboutLicense">License: LGPL-3.0</system:String>
    <system:String x:Key="AboutCoreLicense">Core portion: LGPL-3.0 / GPL-3.0</system:String>
    <system:String x:Key="AppConfigTitle">LEGUI - </system:String>
    <system:String x:Key="GlobalConfigTitle">LEGUI GLOBAL</system:String>
    <system:String x:Key="ErrorHomeDirNotWritable">Home directory is not writable.&#x0A;Please move LE to another location and try again.&#x0A;Home directory: {0}</system:String>
    <system:String x:Key="ErrorDirNotWritable">The directory is not writable.&#x0A;Please use global profile instead.&#x0A;Current Directory: {0}</system:String>
    <system:String x:Key="ErrorAdminRequired">LEGUI requires administrator privilege to write to the current directory.</system:String>
    <system:String x:Key="ShortcutDescription">Run {0} with Locale Emulator</system:String>
```

**注意**：
- `AboutVersion` 含尾空白
- `AppConfigTitle` 含尾空白
- 多行訊息用 `&#x0A;`（XAML 內換行 entity）
- `{0}` 是 `string.Format` placeholder

### Step 2: 改良 WithCreateSuspended 的英文 value

在 `src/LEGUI/Lang/DefaultLanguage.xaml` 第 21 行（Task 1 已 rename）：
```xml
<system:String x:Key="WithCreateSuspended">Create process with CREATE_SUSPENDED</system:String>
```
改為：
```xml
<system:String x:Key="WithCreateSuspended">Start process suspended (for debugging)</system:String>
```

Tooltip（`WithCreateSuspendedTip`）不動。

### Step 3: 改 InputBox.xaml 的 OK / Cancel 按鈕

`src/LEGUI/InputBox.xaml` 第 10-11 行：
```xml
<Button x:Name="bOk" Content="OK" Margin="68,98,140.6,21.2" Click="bOk_Click" />
<Button x:Name="bCancel" Content="Cancel" Margin="138,98,71.6,21.2" Click="bCancel_Click" />
```
改為：
```xml
<Button x:Name="bOk" Content="{DynamicResource Ok}" Margin="68,98,140.6,21.2" Click="bOk_Click" />
<Button x:Name="bCancel" Content="{DynamicResource Cancel}" Margin="138,98,71.6,21.2" Click="bCancel_Click" />
```

### Step 4: 改 AboutPanel.xaml 的 7 個寫死 TextBlock

`src/LEGUI/AboutPanel.xaml` 完整改寫（保留結構，內容改 DynamicResource）：

```xml
<UserControl x:Class="LEGUI.AboutPanel"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <StackPanel Margin="20" TextElement.FontFamily="{DynamicResource UIFont}">
        <TextBlock FontSize="16" FontWeight="Bold" Text="{DynamicResource AboutTitle}" />
        <TextBlock Margin="0,4,0,0">
            <Run Text="{DynamicResource AboutVersion}" /><Run x:Name="tVersion" />
        </TextBlock>
        <TextBlock Margin="0,16,0,0" FontWeight="Bold" Text="{DynamicResource AboutOriginalProject}" />
        <TextBlock>
            <Hyperlink NavigateUri="https://github.com/xupefei/Locale-Emulator"
                       RequestNavigate="Hyperlink_RequestNavigate">
                github.com/xupefei/Locale-Emulator
            </Hyperlink>
        </TextBlock>
        <TextBlock Margin="0,8,0,0" FontWeight="Bold" Text="{DynamicResource AboutCore}" />
        <TextBlock>
            <Hyperlink NavigateUri="https://github.com/xupefei/Locale-Emulator-Core"
                       RequestNavigate="Hyperlink_RequestNavigate">
                github.com/xupefei/Locale-Emulator-Core
            </Hyperlink>
        </TextBlock>
        <TextBlock Margin="0,8,0,0" FontWeight="Bold" Text="{DynamicResource AboutThisFork}" />
        <TextBlock>
            <Hyperlink NavigateUri="https://github.com/ooxxTaiwan/Locale-Emulator-J"
                       RequestNavigate="Hyperlink_RequestNavigate">
                github.com/ooxxTaiwan/Locale-Emulator-J
            </Hyperlink>
        </TextBlock>
        <TextBlock Margin="0,16,0,0" Text="{DynamicResource AboutLicense}" />
        <TextBlock Text="{DynamicResource AboutCoreLicense}" />
    </StackPanel>
</UserControl>
```

**注意**：URL 與 Hyperlink 內的 GitHub 路徑保留為硬編碼（非 i18n 對象，所有 locale 顯示相同 URL）。

### Step 5: 改 AppConfig.xaml + .xaml.cs 的 Title 與其他

`src/LEGUI/AppConfig.xaml` 第 5 行：
```xml
Title="LEGUI - " Width="420" Height="440"
```
改為：
```xml
Title="{DynamicResource AppConfigTitle}" Width="420" Height="440"
```

`src/LEGUI/AppConfig.xaml.cs`：
- 第 18 行原本：
  ```csharp
  Title += Path.GetFileName(App.StandaloneFilePath).Replace(".le.config", "");
  ```
  改為（用 `I18n.GetString` 顯式組裝，避免 XAML DynamicResource binding 被 += 覆蓋的隱性語意）：
  ```csharp
  Title = I18n.GetString("AppConfigTitle") + Path.GetFileName(App.StandaloneFilePath).Replace(".le.config", "");
  ```

- 第 73 行 `link.SetDescription($"Run {Path.GetFileName(path)} with Locale Emulator");` 改為：
  ```csharp
  link.SetDescription(string.Format(I18n.GetString("ShortcutDescription"), Path.GetFileName(path)));
  ```

- 第 85 行 `MessageBox.Show(ex.Message + "\r\n\r\n" + ex.StackTrace, "Locale Emulator");` 改為：
  ```csharp
  MessageBox.Show(ex.Message + "\r\n\r\n" + ex.StackTrace, I18n.GetString("AppName"));
  ```

- 第 114 行 `"Locale Emulator"` MessageBox title 改為 `I18n.GetString("AppName")`。

### Step 6: 改 GlobalConfig.xaml + .xaml.cs

`src/LEGUI/GlobalConfig.xaml` 第 5 行：
```xml
Title="LEGUI GLOBAL" Width="420" Height="470"
```
改為：
```xml
Title="{DynamicResource GlobalConfigTitle}" Width="420" Height="470"
```

`src/LEGUI/GlobalConfig.xaml.cs` 第 120 行：
```csharp
"Locale Emulator",
```
改為：
```csharp
I18n.GetString("AppName"),
```

### Step 7: 改 App.xaml.cs 的 4 個 MessageBox

`src/LEGUI/App.xaml.cs` 第 19 行（unhandled exception handler）：
```csharp
(sender, args) => MessageBox.Show(((Exception) args.ExceptionObject).Message);
```
改為：
```csharp
(sender, args) => MessageBox.Show(((Exception) args.ExceptionObject).Message, I18n.GetString("AppName"));
```

第 68-74 行（home dir not writable）：
```csharp
MessageBox.Show(
    "Home directory is not writable. \r\n"
    + "Please move LE to another location and try again.\r\n"
    + $"Home directory: {Path.GetDirectoryName(LEConfig.GlobalConfigPath)}",
    "Locale Emulator",
    MessageBoxButton.OK,
    MessageBoxImage.Error);
```
改為：
```csharp
MessageBox.Show(
    string.Format(I18n.GetString("ErrorHomeDirNotWritable"),
                  Path.GetDirectoryName(LEConfig.GlobalConfigPath)),
    I18n.GetString("AppName"),
    MessageBoxButton.OK,
    MessageBoxImage.Error);
```

第 76-81 行（dir not writable）：
```csharp
MessageBox.Show(
    "The directory is not writable.\r\n" + "Please use global profile instead.\r\n"
    + $"Current Directory: {Path.GetDirectoryName(StandaloneFilePath)}",
    "Locale Emulator",
    MessageBoxButton.OK,
    MessageBoxImage.Error);
```
改為：
```csharp
MessageBox.Show(
    string.Format(I18n.GetString("ErrorDirNotWritable"),
                  Path.GetDirectoryName(StandaloneFilePath)),
    I18n.GetString("AppName"),
    MessageBoxButton.OK,
    MessageBoxImage.Error);
```

第 98-101 行（admin required）：
```csharp
MessageBox.Show("LEGUI requires administrator privilege to write to the current directory.",
                "Locale Emulator",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
```
改為：
```csharp
MessageBox.Show(I18n.GetString("ErrorAdminRequired"),
                I18n.GetString("AppName"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
```

### Step 8: 改 ShellExtensionPanel.xaml.cs:21

`src/LEGUI/ShellExtensionPanel.xaml.cs` 第 21 行：
```csharp
MessageBox.Show(text, "Locale Emulator");
```
改為：
```csharp
MessageBox.Show(text, I18n.GetString("AppName"));
```

### Step 9: Build 驗證

執行：
```bash
dotnet build src/LEGUI/ --nologo
```
預期：build 成功，0 errors。

### Step 10: Run LEGUI tests

執行：
```bash
dotnet test tests/LEGUI.Tests/ --nologo
```
預期：26 個既有 test 全綠（i18n 改動不影響邏輯）。

### Step 11: Commit

```bash
git add src/LEGUI/Lang/DefaultLanguage.xaml src/LEGUI/App.xaml.cs src/LEGUI/AboutPanel.xaml src/LEGUI/AppConfig.xaml src/LEGUI/AppConfig.xaml.cs src/LEGUI/GlobalConfig.xaml src/LEGUI/GlobalConfig.xaml.cs src/LEGUI/InputBox.xaml src/LEGUI/ShellExtensionPanel.xaml.cs
git commit -m "$(cat <<'EOF'
feat(LEGUI-i18n): extract hardcoded user-facing strings to i18n keys

抽取 15 個寫死字串到 DefaultLanguage.xaml：
- AppName（6 個 MessageBox title 統一）
- Ok（InputBox button；Cancel 復用既有 key）
- AboutTitle / AboutVersion / AboutOriginalProject / AboutCore /
  AboutThisFork / AboutLicense / AboutCoreLicense（AboutPanel 7 個 TextBlock）
- AppConfigTitle / GlobalConfigTitle（兩個視窗 Title）
- ErrorHomeDirNotWritable / ErrorDirNotWritable / ErrorAdminRequired
  （App startup 的 3 個錯誤 MessageBox，含 {0} placeholder）
- ShortcutDescription（桌面捷徑描述，含 {0} placeholder）

連帶改良 WithCreateSuspended label 文字：
"Create process with CREATE_SUSPENDED" → "Start process suspended (for debugging)"
（Win32 API 常數對非開發者無意義，移到 tooltip 說明）

連帶補上 unhandled exception MessageBox 的 AppName title。

21 locale 檔此 commit 後缺 39 個 key（24 既有 gap + 15 新增），
WithCreateSuspended 值需依新意重譯 — 由 commit 3, 4 補齊。

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: zh-TW lead locale 翻譯（commit 3）

**目標**：完整翻譯 zh-TW 39 個新 key + 修 typo + 套用 4 rename 後的新 key 名稱（rename 已在 Task 1 完成 key 名，這裡是補翻譯值）。**zh-TW 完成後使用者會 review，再進 Task 4 平行翻譯。**

**Files:**
- Modify: `src/LEGUI/Lang/zh-TW.xaml`

### Step 1: 讀目前的 zh-TW.xaml

讀 `src/LEGUI/Lang/zh-TW.xaml`。當前狀態（Task 1 已 rename，Task 2 未影響此檔）：

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:system="clr-namespace:System;assembly=System.Runtime">
    <FontFamily x:Key="UIFont">微軟正黑體</FontFamily>
    <system:String x:Key="EnterArgument">在此輸入執行參數</system:String>
    <system:String x:Key="Save">儲存</system:String>
    <system:String x:Key="Shortcut">在桌面放置捷徑</system:String>
    <system:String x:Key="Cancel">取消</system:String>
    <system:String x:Key="SaveAs">另存為...</system:String>
    <system:String x:Key="SaveAsInstruction">設定名稱：</system:String>
    <system:String x:Key="Delete">刪除</system:String>
    <system:String x:Key="ConfirmDelete">確定刪除此設定嗎？</system:String>
    <system:String x:Key="Location">位置</system:String>
    <system:String x:Key="LocationSettings">位置設定</system:String>
    <system:String x:Key="TimezoneSettings">時區設定</system:String>
    <system:String x:Key="Timezone">時區</system:String>
    <system:String x:Key="AdvancedOptions">進階選項</system:String>
    <system:String x:Key="AsAdmin">以管理員身分執行</system:String>
    <system:String x:Key="RedirectRegistry">偽造語言註冊表中語言相關選項</system:String>
    <system:String x:Key="IsAdvancedRedirection">偽造系統 UI 語言</system:String>
    <system:String x:Key="WithCreateSuspended">用 CREATE__SUSPENDED 標誌建立進程</system:String>
    <system:String x:Key="Miscellaneous">其它</system:String>
    <system:String x:Key="ShowInMainMenu">在主選單中顯示此設定</system:String>
</ResourceDictionary>
```

### Step 2: 改 WithCreateSuspended 值（typo 修 + 依新英文重譯）

第 21 行 `用 CREATE__SUSPENDED 標誌建立進程` 改為：
```xml
<system:String x:Key="WithCreateSuspended">啟動程序並暫停（除錯用）</system:String>
```

理由：英文已從 `Create process with CREATE_SUSPENDED` 改為 `Start process suspended (for debugging)`，不再含 API 常數於 label。

### Step 3: 補全 39 個新 key 的 zh-TW 翻譯

在 `</ResourceDictionary>` 之前加入：

```xml
    <!-- PR 29 新增的 24 個 key -->
    <system:String x:Key="InstallSuccess">Shell 擴充功能安裝成功。請重新啟動檔案總管或登出以套用變更。</system:String>
    <system:String x:Key="UninstallSuccess">Shell 擴充功能已解除安裝。請重新啟動檔案總管或登出以套用變更。</system:String>
    <system:String x:Key="InstallShellExtTitle">Shell 擴充功能</system:String>
    <system:String x:Key="InstallCurrentUser">安裝（目前使用者）</system:String>
    <system:String x:Key="InstallAllUsers">安裝（所有使用者）</system:String>
    <system:String x:Key="Uninstall">解除安裝</system:String>
    <system:String x:Key="InstallStatus">狀態：</system:String>
    <system:String x:Key="Installed">已安裝</system:String>
    <system:String x:Key="NotInstalled">未安裝</system:String>
    <system:String x:Key="Display">顯示</system:String>
    <system:String x:Key="AsAdminTip">以提升的權限啟動目標程序。某些遊戲需要此選項。</system:String>
    <system:String x:Key="RedirectRegistryTip">攔截目標程序對 locale 相關 Registry key 的讀取，回傳模擬的 locale 而非系統實際值。</system:String>
    <system:String x:Key="IsAdvancedRedirectionTip">同時偽造控制系統 UI 語言的 Registry key，影響某些透過 MUI_LANGUAGE_NAME 查詢的應用程式。</system:String>
    <system:String x:Key="WithCreateSuspendedTip">以暫停狀態建立目標程序（進階除錯選項）。</system:String>
    <system:String x:Key="ShowInMainMenuTip">直接在右鍵選單根層顯示此設定，而非置於子選單之下。</system:String>
    <system:String x:Key="TabProfile">設定</system:String>
    <system:String x:Key="TabShellExtension">Shell 擴充功能</system:String>
    <system:String x:Key="TabAbout">關於</system:String>
    <system:String x:Key="SavedStatus">已儲存</system:String>
    <system:String x:Key="ShellExtCurrentUserHeader">目前使用者</system:String>
    <system:String x:Key="ShellExtAllUsersHeader">所有使用者 &#x1F6E1;</system:String>
    <system:String x:Key="ShellExtOldResidueDetected">&#x26A0; 偵測到舊版殘留</system:String>
    <system:String x:Key="ShellExtCleanupOld">清理舊版註冊</system:String>
    <system:String x:Key="ShellExtOperationFailed">Shell 擴充功能操作失敗：{0}</system:String>

    <!-- Task 2 新增的 15 個 key -->
    <system:String x:Key="AppName">Locale Emulator</system:String>
    <system:String x:Key="Ok">確定</system:String>
    <system:String x:Key="AboutTitle">Locale Emulator（ooxxTaiwan fork）</system:String>
    <system:String x:Key="AboutVersion">版本 </system:String>
    <system:String x:Key="AboutOriginalProject">原專案</system:String>
    <system:String x:Key="AboutCore">Core（已封存於 2022-04）</system:String>
    <system:String x:Key="AboutThisFork">本 fork</system:String>
    <system:String x:Key="AboutLicense">授權：LGPL-3.0</system:String>
    <system:String x:Key="AboutCoreLicense">Core 部分：LGPL-3.0 / GPL-3.0</system:String>
    <system:String x:Key="AppConfigTitle">LEGUI - </system:String>
    <system:String x:Key="GlobalConfigTitle">LEGUI 全域設定</system:String>
    <system:String x:Key="ErrorHomeDirNotWritable">家目錄無寫入權限。&#x0A;請將 LE 移至其他位置後重試。&#x0A;家目錄：{0}</system:String>
    <system:String x:Key="ErrorDirNotWritable">目錄無寫入權限。&#x0A;請改用全域設定。&#x0A;目前目錄：{0}</system:String>
    <system:String x:Key="ErrorAdminRequired">LEGUI 需要系統管理員權限才能寫入目前目錄。</system:String>
    <system:String x:Key="ShortcutDescription">用 Locale Emulator 執行 {0}</system:String>
```

**注意**：
- `Profile` 沿用 zh-TW 既有用法「設定」（如既有 `ShowInMainMenu = "在主選單中顯示此設定"`）
- `&#x0A;` 為 XAML 內換行 entity
- `&#x1F6E1;`、`&#x26A0;` emoji 保留 PR 29 既有 pattern
- `{0}` 為 `string.Format` placeholder
- `AboutTitle` 用全形括號（zh-TW 慣例）
- `AppConfigTitle` 保留尾空白與英文「LEGUI -」（產品名 + dash 為通用 pattern）

### Step 4: Build 驗證

執行：
```bash
dotnet build src/LEGUI/ --nologo
```
預期：build 成功，0 errors。

### Step 5: 視覺檢查 zh-TW 翻譯

人工讀過 zh-TW.xaml 一次，確認：
- 無多餘空白
- emoji entity 正確
- placeholder `{0}` 位置語意通順
- 詞彙一致（設定 / 安裝 / 解除安裝）

### Step 6: Commit

```bash
git add src/LEGUI/Lang/zh-TW.xaml
git commit -m "$(cat <<'EOF'
feat(LEGUI-i18n): translate new keys to zh-TW + fix CREATE__SUSPENDED typo

補全 zh-TW 缺失的 39 個 key（24 PR 29 gap + 15 從寫死字串抽出）。
WithCreateSuspended 值依新英文意重譯：「啟動程序並暫停（除錯用）」。

詞彙：
- Profile = 設定（沿用既有用法）
- Install / Uninstall = 安裝 / 解除安裝
- Shell Extension = Shell 擴充功能
- Current User / All Users = 目前使用者 / 所有使用者
- OK = 確定（zh-TW Windows 慣例）
- Restart Explorer = 重新啟動檔案總管

zh-TW 為 lead locale；其餘 20 個 locale 由 commit 4 平行翻譯。

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

### Step 7: 等待使用者 review

完成 commit 後，**告訴使用者**：
> "zh-TW lead locale 完成（commit `<hash>`）。請 review `src/LEGUI/Lang/zh-TW.xaml` 的詞彙與翻譯。確認 OK 後我再進 Task 4 平行翻譯其餘 20 locale。"

⚠ **Gate**：使用者明確 OK 才能進 Task 4。

---

## Task 4: 平行翻譯 20 locale（commit 4）

**目標**：用 20 個平行 agent 翻譯其餘 20 locale，每 agent 負責 1 個 locale 檔。

**Files:**
- Modify: `src/LEGUI/Lang/{ca,cs,de,es,fr,ind,it,ja,ka,ko,lt,nb,nl,pl,pt-BR,ru,th,tr-TR,zh-CN,zh-HK}.xaml`

### Step 1: 準備 agent prompt 模板

對每個 locale `<X>`，用以下模板（變數以 `{{...}}` 標示）：

```
你是 i18n 翻譯 agent，負責翻譯 LEGUI 的 {{LOCALE}} (<語言名稱>) locale 檔。

## 任務

修改 `src/LEGUI/Lang/{{LOCALE}}.xaml`：

1. 讀取自己的既有檔，繼承所有現有翻譯
2. 補全所有 `src/LEGUI/Lang/DefaultLanguage.xaml` 中存在但本檔缺的 key（共 39 個新 key）
3. 修改 `WithCreateSuspended` 既有 key 的值：依新英文 "Start process suspended (for debugging)" 重譯（不要把舊的 CREATE__SUSPENDED 換成 CREATE_SUSPENDED 就完事，整句要重寫）
4. 嚴格遵守本 locale 的 glossary（見 spec Section 6.1 的 #### {{LOCALE}} 區塊）
5. **順手檢查既有翻譯有無明顯錯誤**（例如沒翻譯成本 locale 仍是英文、明顯 typo、明顯 mistranslation），有就修。**只修明顯錯誤，不修純 stylistic 偏好。**
6. 寫回 `src/LEGUI/Lang/{{LOCALE}}.xaml`（**只寫自己的這檔，不碰其他 locale**）

## 必讀檔案

- `docs/superpowers/specs/2026-04-19-issue-21-i18n-translations-design.md`（spec，特別 Section 6.1 的 {{LOCALE}} glossary 區塊）
- `src/LEGUI/Lang/DefaultLanguage.xaml`（英文 source of truth）
- `src/LEGUI/Lang/{{LOCALE}}.xaml`（既有翻譯，繼承）
- `src/LEGUI/Lang/zh-TW.xaml`（範本，看新 key 在哪個位置如何排版、emoji entity 如何處理、含 placeholder 的 key 如何寫）

## 輸出

- 完整的 `src/LEGUI/Lang/{{LOCALE}}.xaml` 檔案內容（用 Write tool）
- 簡短報告（< 100 字）：列出所修改的 key 數量、有無修既有翻譯的明顯錯誤

## 注意事項

- emoji entity（如 `&#x1F6E1;`、`&#x26A0;`）保留
- placeholder `{0}` 不要翻譯，位置依語法調整
- 換行用 `&#x0A;`（XAML entity）
- 產品名 `Locale Emulator` 不翻譯（依 glossary 對應的 G1-G10 處理）
- UIFont 維持本檔既有設定
- 寫死字串抽出來的英文（如 `License: LGPL-3.0`）保留不翻動的 token
```

### Step 2: 平行 dispatch 20 個 agents（單一訊息多個 Agent tool call）

**重要**：必須在**同一訊息**內呼叫 20 個 Agent tool，才能真正平行。

20 個 locale 列表（按字母順序）：
- ca, cs, de, es, fr, ind, it, ja, ka, ko, lt, nb, nl, pl, pt-BR, ru, th, tr-TR, zh-CN, zh-HK

每個 agent：
- description: `翻譯 LEGUI <locale>.xaml`
- subagent_type: `general-purpose`
- prompt: 上述模板填入該 locale 變數

### Step 3: 收集 agent 結果並驗證

每個 agent 完成後檢查：
1. 該 locale .xaml 檔有被寫入（`git status` 顯示 modified）
2. 檔案語法為合法 XML（XAML）— 用 `XmlDocument` 解析或 `dotnet build` 驗證
3. 檔案 key 數 = DefaultLanguage 的 key 數

### Step 4: Build 驗證

執行：
```bash
dotnet build src/LEGUI/ --nologo
```
預期：build 成功，0 errors。

### Step 5: Run LEGUI tests

執行：
```bash
dotnet test tests/LEGUI.Tests/ --nologo
```
預期：26 個既有 test 全綠（i18n 補全不影響邏輯）。

### Step 6: Commit

```bash
git add src/LEGUI/Lang/ca.xaml src/LEGUI/Lang/cs.xaml src/LEGUI/Lang/de.xaml src/LEGUI/Lang/es.xaml src/LEGUI/Lang/fr.xaml src/LEGUI/Lang/ind.xaml src/LEGUI/Lang/it.xaml src/LEGUI/Lang/ja.xaml src/LEGUI/Lang/ka.xaml src/LEGUI/Lang/ko.xaml src/LEGUI/Lang/lt.xaml src/LEGUI/Lang/nb.xaml src/LEGUI/Lang/nl.xaml src/LEGUI/Lang/pl.xaml src/LEGUI/Lang/pt-BR.xaml src/LEGUI/Lang/ru.xaml src/LEGUI/Lang/th.xaml src/LEGUI/Lang/tr-TR.xaml src/LEGUI/Lang/zh-CN.xaml src/LEGUI/Lang/zh-HK.xaml
git commit -m "$(cat <<'EOF'
feat(LEGUI-i18n): translate new keys to remaining 20 locales + fix CREATE__SUSPENDED typo

補全 ca, cs, de, es, fr, ind, it, ja, ka, ko, lt, nb, nl, pl, pt-BR, ru, th,
tr-TR, zh-CN, zh-HK 缺失的 39 個 key（24 PR 29 gap + 15 從寫死字串抽出）。

WithCreateSuspended 值依新英文 "Start process suspended (for debugging)"
重譯，連帶修掉 CREATE__SUSPENDED 雙底線 typo。

依 spec Section 6.1 per-locale glossary 鎖定詞彙：
- Install / Uninstall / Shell Extension / Current User / All Users 跨鍵一致
- Profile 沿用各 locale 既有用法（設定 / 配置 / プロファイル / ...）
- AppName 所有 locale 都填 "Locale Emulator"（產品名）

部分 agent 順手修了既有翻譯的明顯錯誤（如 pt-BR 的 DebugOptions 仍是英文）。

由 20 個平行 agent dispatch 完成；翻譯品質 zh-TW/zh-CN/ja/en
經人工 review，其餘 best-effort，社群可後續 PR 修訂。

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: ShellExtension 翻譯品質修正（commit 5）

**目標**：執行 spec Section 7.4 的 D1 + D2 + D3 三類修正（共 18 個變更）。

**Files:**
- Modify: `src/ShellExtension/Lang/{fr,it,ja,ka,ko,lt,nl,ru}.xml`（D1 Submenu 統一）
- Modify: `src/ShellExtension/Lang/{fr,ja,zh-TW,zh-HK,zh-CN,th}.xml`（D2 明顯錯誤）
- Modify: `src/ShellExtension/Lang/{pt-BR,ind}.xml`（D3 capitalization）

### Step 1: D1 — Submenu 統一為 "Locale Emulator"（8 檔）

逐檔修改 `<Submenu>...</Submenu>`：

| 檔案 | 原值 | 改為 |
|------|------|------|
| `src/ShellExtension/Lang/fr.xml` | `<Submenu>Émulateur local</Submenu>` | `<Submenu>Locale Emulator</Submenu>` |
| `src/ShellExtension/Lang/it.xml` | `<Submenu>Emulatore Locale</Submenu>` | `<Submenu>Locale Emulator</Submenu>` |
| `src/ShellExtension/Lang/ja.xml` | `<Submenu>ロケールエミュレータ</Submenu>` | `<Submenu>Locale Emulator</Submenu>` |
| `src/ShellExtension/Lang/ka.xml` | `<Submenu>ლოკალის ემულატორი</Submenu>` | `<Submenu>Locale Emulator</Submenu>` |
| `src/ShellExtension/Lang/ko.xml` | `<Submenu>로케일 에뮬레이터</Submenu>` | `<Submenu>Locale Emulator</Submenu>` |
| `src/ShellExtension/Lang/lt.xml` | `<Submenu>Lokalės emuliatorius</Submenu>` | `<Submenu>Locale Emulator</Submenu>` |
| `src/ShellExtension/Lang/nl.xml` | `<Submenu>Landinstellingen emulator</Submenu>` | `<Submenu>Locale Emulator</Submenu>` |
| `src/ShellExtension/Lang/ru.xml` | `<Submenu>Эмулятор локали</Submenu>` | `<Submenu>Locale Emulator</Submenu>` |

### Step 2: D2 — 明顯錯誤修正（7 變更）

#### fr.xml — RunDefault typo + ManageAll 補完整

```xml
<RunDefault>Éxecuter avec le profil de cette application</RunDefault>
```
改為：
```xml
<RunDefault>Exécuter avec le profil de cette application</RunDefault>
```

```xml
<ManageAll>Gestion du profil global</ManageAll>
```
改為：
```xml
<ManageAll>Modifier la liste des profils globaux</ManageAll>
```

#### ja.xml — ManageAll 用詞修正

```xml
<ManageAll>汎用性プロファイルリストを編集</ManageAll>
```
改為：
```xml
<ManageAll>グローバルプロファイル一覧を編集</ManageAll>
```

#### zh-TW.xml + zh-HK.xml — ManageAll 用詞精確化

兩檔同樣修正：
```xml
<ManageAll>管理通用設定清單</ManageAll>
```
改為：
```xml
<ManageAll>管理全域設定清單</ManageAll>
```

#### zh-CN.xml — ManageAll 用詞精確化

```xml
<ManageAll>管理通用配置列表</ManageAll>
```
改為：
```xml
<ManageAll>管理全局配置列表</ManageAll>
```

#### th.xml — ManageAll 翻譯精確化

```xml
<ManageAll>แก้ไขโปรไฟล์โดยรวม</ManageAll>
```
改為：
```xml
<ManageAll>แก้ไขรายการโปรไฟล์ทั่วไป</ManageAll>
```

### Step 3: D3 — pt-BR + ind capitalization 一致性

#### pt-BR.xml — 3 個 key 統一 sentence case

```xml
<RunDefault>Executar com Perfil de Aplicativo</RunDefault>
<ManageApp>Modificar Perfil de Aplicativo</ManageApp>
<ManageAll>Editar lista Global de Perfis</ManageAll>
```
改為：
```xml
<RunDefault>Executar com perfil de aplicativo</RunDefault>
<ManageApp>Modificar perfil de aplicativo</ManageApp>
<ManageAll>Editar lista global de perfis</ManageAll>
```

#### ind.xml — 2 個 key 統一 sentence case（ManageAll 已 OK）

```xml
<RunDefault>Jalankan dengan Profil Aplikasi</RunDefault>
<ManageApp>Ubah Profil Aplikasi</ManageApp>
```
改為：
```xml
<RunDefault>Jalankan dengan profil aplikasi</RunDefault>
<ManageApp>Ubah profil aplikasi</ManageApp>
```

### Step 4: 驗證 XML 語法

對所有改動的 ShellExt XML 檔，確認 XML 仍可解析：

```powershell
$files = @('fr', 'it', 'ja', 'ka', 'ko', 'lt', 'nl', 'ru', 'zh-TW', 'zh-HK', 'zh-CN', 'th', 'pt-BR', 'ind') | ForEach-Object { "src/ShellExtension/Lang/$_.xml" }
foreach ($f in $files) {
    try {
        [xml](Get-Content -Path $f -Raw) | Out-Null
        Write-Host "OK: $f"
    } catch {
        Write-Error "Invalid XML: $f -- $_"
    }
}
```
預期：每檔輸出 `OK: ...`，無 error。

### Step 5: Commit

```bash
git add src/ShellExtension/Lang/fr.xml src/ShellExtension/Lang/it.xml src/ShellExtension/Lang/ja.xml src/ShellExtension/Lang/ka.xml src/ShellExtension/Lang/ko.xml src/ShellExtension/Lang/lt.xml src/ShellExtension/Lang/nl.xml src/ShellExtension/Lang/ru.xml src/ShellExtension/Lang/zh-TW.xml src/ShellExtension/Lang/zh-HK.xml src/ShellExtension/Lang/zh-CN.xml src/ShellExtension/Lang/th.xml src/ShellExtension/Lang/pt-BR.xml src/ShellExtension/Lang/ind.xml
git commit -m "$(cat <<'EOF'
fix(ShellExt-i18n): standardize Submenu and fix translation quality across 22 locales

D1: 統一 Submenu 為產品名 "Locale Emulator"（8 檔）
- fr / it / ja / ka / ko / lt / nl / ru：原本翻成各自語言，與其他 13 檔不一致

D2: 明顯錯誤修正
- fr RunDefault: "Éxecuter" → "Exécuter"（重音位置 typo）
- fr ManageAll: 補完「list」與複數（"Gestion du profil global" → "Modifier la liste des profils globaux"）
- ja ManageAll: "汎用性"（versatility）→「グローバル」；「リスト」→「一覧」
- zh-TW / zh-HK ManageAll: "通用" → "全域"（Microsoft Windows zh-TW/HK 慣用詞）
- zh-CN ManageAll: "通用" → "全局"（Microsoft Windows zh-CN 慣用詞）
- th ManageAll: 補上「list」與「global」精確語意

D3: pt-BR + ind 內部 capitalization 一致性（統一 sentence case）

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: 完整性測試（commit 6）

**目標**：加 xUnit `[Theory]` 測試 21 locale 完整性 + 非空值。

**Files:**
- Create: `tests/LEGUI.Tests/LocaleCompletenessTests.cs`
- Modify: `tests/LEGUI.Tests/LEGUI.Tests.csproj`

### Step 1: 修改 LEGUI.Tests.csproj 加入 Lang 拷貝

讀目前 `tests/LEGUI.Tests/LEGUI.Tests.csproj`，在 `</Project>` 之前加入新 `<ItemGroup>`：

```xml
  <ItemGroup>
    <None Include="..\..\src\LEGUI\Lang\*.xaml"
          CopyToOutputDirectory="PreserveNewest"
          Link="Lang\%(Filename)%(Extension)" />
  </ItemGroup>
```

### Step 2: 建立 LocaleCompletenessTests.cs

新增檔案 `tests/LEGUI.Tests/LocaleCompletenessTests.cs`：

```csharp
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Xunit;

namespace LEGUI.Tests;

public class LocaleCompletenessTests
{
    private static readonly string LangDir =
        Path.Combine(
            Path.GetDirectoryName(typeof(LocaleCompletenessTests).Assembly.Location)!,
            "Lang");

    private static readonly XNamespace SystemNs =
        "clr-namespace:System;assembly=System.Runtime";
    private static readonly XNamespace XamlNs =
        "http://schemas.microsoft.com/winfx/2006/xaml";

    public static IEnumerable<object[]> LocaleFiles =>
        Directory.GetFiles(LangDir, "*.xaml")
                 .Where(f => Path.GetFileName(f) != "DefaultLanguage.xaml")
                 .Select(f => new object[] { Path.GetFileName(f) });

    [Theory]
    [MemberData(nameof(LocaleFiles))]
    public void Locale_HasAllKeysFromDefaultLanguage(string localeFileName)
    {
        var defaultKeys = LoadKeys("DefaultLanguage.xaml");
        var localeKeys  = LoadKeys(localeFileName);
        var missing     = defaultKeys.Except(localeKeys).OrderBy(k => k).ToList();

        Assert.True(
            missing.Count == 0,
            $"{localeFileName} is missing {missing.Count} key(s) from DefaultLanguage.xaml: " +
            string.Join(", ", missing));
    }

    [Theory]
    [MemberData(nameof(LocaleFiles))]
    public void Locale_HasNoEmptyValues(string localeFileName)
    {
        var emptyKeys = LoadKeyValues(localeFileName)
                       .Where(kv => string.IsNullOrWhiteSpace(kv.Value))
                       .Select(kv => kv.Key)
                       .OrderBy(k => k)
                       .ToList();

        Assert.True(
            emptyKeys.Count == 0,
            $"{localeFileName} has empty/whitespace value(s) for: " +
            string.Join(", ", emptyKeys));
    }

    private static HashSet<string> LoadKeys(string fileName) =>
        LoadKeyValues(fileName).Select(kv => kv.Key).ToHashSet();

    private static IEnumerable<KeyValuePair<string, string>> LoadKeyValues(string fileName)
    {
        var doc = XDocument.Load(Path.Combine(LangDir, fileName));
        return doc.Descendants(SystemNs + "String")
                  .Select(e => new KeyValuePair<string, string>(
                      (string)e.Attribute(XamlNs + "Key")!,
                      e.Value));
    }
}
```

### Step 3: Run 新增測試 — 預期全綠

執行：
```bash
dotnet test tests/LEGUI.Tests/ --filter "FullyQualifiedName~LocaleCompletenessTests" --nologo -v normal
```
預期：42 個 test pass（21 locale × 2 test）。如紅燈，根據錯誤訊息回去 Task 3 / 4 補對應 locale 的 key。

### Step 4: Run 全套 LEGUI tests

執行：
```bash
dotnet test tests/LEGUI.Tests/ --nologo
```
預期：26 既有 + 42 新 = 68 全綠。

### Step 5: Commit

```bash
git add tests/LEGUI.Tests/LocaleCompletenessTests.cs tests/LEGUI.Tests/LEGUI.Tests.csproj
git commit -m "$(cat <<'EOF'
test(LEGUI): add i18n key completeness and non-empty value tests

新增 LocaleCompletenessTests.cs 透過 xUnit [Theory] + MemberData
為 21 個 locale 各跑 2 個獨立測試：

1. Locale_HasAllKeysFromDefaultLanguage：確保每 locale 含 DefaultLanguage 全部 key
2. Locale_HasNoEmptyValues：確保每 locale 無空 / whitespace 值

Theory 設計：紅燈時可一眼看出哪個 locale 漏哪個 key，
不必猜整段對齊問題。

LEGUI.Tests.csproj 加入 <None Include="..\..\src\LEGUI\Lang\*.xaml"
CopyToOutputDirectory="PreserveNewest" Link="Lang\..." />
將 Lang 拷至測試輸出目錄供測試讀取。

防漂移：未來新增 locale 自動納管（Directory.GetFiles 動態掃）。
PR 29 漏 24 key 的情況再也不會發生。

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 7: 最終驗證（無 commit）

**目標**：完整 build + test + 視覺 smoke。

### Step 1: Clean build LEGUI

執行：
```bash
dotnet build src/LEGUI/ --no-incremental --nologo
```
預期：0 errors。warnings 只能維持既有的（不應因本 PR 新增）。

### Step 2: Run 全套 .NET tests

執行：
```bash
dotnet test tests/LECommonLibrary.Tests/ --nologo
dotnet test tests/LEGUI.Tests/ --nologo
dotnet test tests/LEProc.Tests/ --arch x86 --nologo
```
預期：所有 test 全綠。LEGUI.Tests 應有 68 test（26 既有 + 42 新）。

### Step 3: 通知使用者進視覺 smoke

完成 Step 1-2 後告訴使用者：

> "全部 commit 完成（commit 1-6）。請進 Windows Sandbox 跑視覺 smoke：
> 1. 把 Build/Release/ 拷進 Sandbox
> 2. Sandbox 內以系統 locale = ja-JP 啟動 LEGUI，screenshot 主視窗 / Shell Extension 分頁 / About 分頁
> 3. 重複 zh-TW、zh-CN
> 4. 確認無英文殘留、無亂碼、tooltip 顯示正確
>
> 同時請 review 翻譯：zh-TW.xaml、zh-CN.xaml、ja.xaml、DefaultLanguage.xaml diff。"

### Step 4: 視使用者反饋決定後續

- 若 smoke 全綠且翻譯 review 通過 → 進入「建立 PR」流程（push branch + 開 PR with Issue #21 連結 + 提及 Issue #30 解釋 LEProc 範圍邊界）
- 若有問題 → 回對應 Task（如翻譯品質回 Task 3 / 4）修正

---

## Self-Review Checklist

執行 plan 前的最後 sanity check（這份是給 plan writer / executor 看的）：

- [ ] **Spec coverage**：
  - 24 PR 29 gap key → Task 3, 4 的翻譯涵蓋 ✓
  - 15 寫死字串抽出 → Task 2 ✓
  - 4 key rename → Task 1 ✓
  - WithCreateSuspended label 改良 → Task 2 Step 2 ✓
  - CREATE__SUSPENDED typo 修 → Task 3 Step 2 + Task 4（隱含於翻譯重寫）✓
  - ShellExt 18 變更 → Task 5 ✓
  - 完整性測試 → Task 6 ✓
- [ ] **Type / API consistency**：
  - `I18n.GetString(...)` 用法（Task 2 / 3 / 4 一致）✓
  - `Application.Current.FindResource(...)` vs `I18n.GetString(...)` — 統一用 `I18n.GetString` 風格（依 LEGUI 既有 helper 慣例）
  - `string.Format(I18n.GetString(...), arg)` 用於 placeholder ✓
- [ ] **Phase ordering**：A → B → ⚠Gate → C → D → E → F ✓
- [ ] **Commit 數**：6 commits ✓
- [ ] **檔案路徑** 都用 worktree 內相對路徑 ✓

---

## 關聯資源

- **Issue #21**：本 PR 解決的 issue（i18n 補全）
- **Issue #30**：LEProc → WPF + i18n 研究（本 PR **不**處理，由 #30 追蹤）
- **Spec**：`docs/superpowers/specs/2026-04-19-issue-21-i18n-translations-design.md`
