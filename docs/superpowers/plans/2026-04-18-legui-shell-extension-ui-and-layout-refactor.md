# LEGUI Shell Extension UI and Layout Refactor - Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 LEGUI 加入 Shell Extension 安裝/解除安裝 UI，同時將 `GlobalConfig` / `AppConfig` 兩個主視窗改用 TabControl、抽出共用 `ProfileEditorControl`、修復既有版面問題。

**Architecture:** 兩個主視窗皆採 `TabControl`（Profile + About 共用，Shell Extension 僅 GlobalConfig 有）。共用 `ProfileEditorControl` UserControl 消除 95% 重複版面。Shell Extension 安裝 All Users 時以子 process（`LEGUI.exe --shell-ext ...`）提權，不影響主 UI 狀態。

**Tech Stack:** C# 14, .NET 10 (net10.0-windows), WPF, xUnit + NSubstitute + **Xunit.StaFact**（新增，WPF UserControl 測試需 STA thread）。

**Spec reference:** `docs/superpowers/specs/2026-04-18-legui-shell-extension-ui-and-layout-refactor-design.md`

**Issue:** ooxxTaiwan/Locale-Emulator-J#19

---

## Prerequisites

### P1: 建立 worktree

- [ ] **Step 1: 建立獨立 worktree 並切到 feature branch**

```bash
cd E:/Code/Locale-Emulator
git worktree add -b feature/issue-19-shell-ext-ui ../Locale-Emulator-issue-19 master
cd ../Locale-Emulator-issue-19
```

- [ ] **Step 2: 驗證 spec 已存在於 worktree**

```bash
ls docs/superpowers/specs/2026-04-18-legui-shell-extension-ui-and-layout-refactor-design.md
ls docs/superpowers/plans/2026-04-18-legui-shell-extension-ui-and-layout-refactor.md
```

Expected: 兩個檔案都存在。

### P2: 新增 Xunit.StaFact 套件

**Files:**
- Modify: `tests/LEGUI.Tests/LEGUI.Tests.csproj`

- [ ] **Step 1: 在 `LEGUI.Tests.csproj` 的 `<ItemGroup>` 加入**

```xml
<PackageReference Include="Xunit.StaFact" Version="1.*" />
```

- [ ] **Step 2: Restore 套件**

```bash
dotnet restore tests/LEGUI.Tests/LEGUI.Tests.csproj
```

Expected: restore 成功，無錯誤。

- [ ] **Step 3: 提交**

```bash
git add tests/LEGUI.Tests/LEGUI.Tests.csproj
git commit -m "test: add Xunit.StaFact for WPF UserControl tests"
```

---

## Phase 1: ProfileEditorControl UserControl

### Task 1.1: 建立 ProfileEditorControl 骨架與 LoadProfile/ReadProfile 測試

**Files:**
- Create: `src/LEGUI/ProfileEditorControl.xaml`
- Create: `src/LEGUI/ProfileEditorControl.xaml.cs`
- Create: `tests/LEGUI.Tests/ProfileEditorControlTests.cs`

- [ ] **Step 1: 撰寫失敗的測試（load / read 往返）**

`tests/LEGUI.Tests/ProfileEditorControlTests.cs`:

```csharp
using LECommonLibrary;
using Xunit;

namespace LEGUI.Tests;

public class ProfileEditorControlTests
{
    private static LEProfile SampleProfile() => new LEProfile(
        "TestProfile", "{00000000-0000-0000-0000-000000000001}",
        showInMainMenu: true, parameter: "",
        location: "ja-JP", timezone: "Tokyo Standard Time",
        runAsAdmin: false, redirectRegistry: true,
        isAdvancedRedirection: false, runWithSuspend: false);

    [WpfFact]
    public void LoadThenRead_RoundTripsAllFields()
    {
        var ctrl = new ProfileEditorControl();
        var original = SampleProfile();

        ctrl.LoadProfile(original);
        var result = ctrl.ReadProfile(original);

        Assert.Equal(original.Location, result.Location);
        Assert.Equal(original.Timezone, result.Timezone);
        Assert.Equal(original.RunAsAdmin, result.RunAsAdmin);
        Assert.Equal(original.RedirectRegistry, result.RedirectRegistry);
        Assert.Equal(original.IsAdvancedRedirection, result.IsAdvancedRedirection);
        Assert.Equal(original.RunWithSuspend, result.RunWithSuspend);
        Assert.Equal(original.ShowInMainMenu, result.ShowInMainMenu);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.Guid, result.Guid);
        Assert.Equal(original.Parameter, result.Parameter);
    }
}
```

- [ ] **Step 2: 執行測試確認失敗**

```bash
dotnet test tests/LEGUI.Tests/ --filter ProfileEditorControlTests
```

Expected: FAIL（`ProfileEditorControl` 類別不存在）

- [ ] **Step 3: 建立最小實作（含 XAML）**

`src/LEGUI/ProfileEditorControl.xaml`:

```xml
<UserControl x:Class="LEGUI.ProfileEditorControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid TextElement.FontFamily="{DynamicResource UIFont}">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>
        <GroupBox Grid.Row="0" Header="{DynamicResource LocationSettings}" Margin="4,2">
            <ComboBox x:Name="cbLocation" Margin="4" />
        </GroupBox>
        <GroupBox Grid.Row="1" Header="{DynamicResource TimezoneSettings}" Margin="4,2">
            <ComboBox x:Name="cbTimezone" Margin="4" />
        </GroupBox>
        <GroupBox Grid.Row="2" Header="{DynamicResource DebugOptions}" Margin="4,2">
            <StackPanel Margin="4">
                <CheckBox x:Name="cbStartAsAdmin" Content="{DynamicResource AsAdmin}" Margin="0,2" />
                <CheckBox x:Name="cbRedirectRegistry" Content="{DynamicResource RedirectRegistry}" Margin="0,2" />
                <CheckBox x:Name="cbIsAdvancedRedirection" Content="{DynamicResource IsAdvancedRedirection}" Margin="0,2" />
                <CheckBox x:Name="cbStartAsSuspend" Content="{DynamicResource WithCREATESUSPENDED}" Margin="0,2" />
            </StackPanel>
        </GroupBox>
        <GroupBox x:Name="displayGroup" Grid.Row="3" Header="{DynamicResource Display}" Margin="4,2">
            <CheckBox x:Name="cbShowInMainMenu" Content="{DynamicResource ShowInMainMenu}" Margin="4" />
        </GroupBox>
    </Grid>
</UserControl>
```

`src/LEGUI/ProfileEditorControl.xaml.cs`:

```csharp
#nullable disable

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using LECommonLibrary;

namespace LEGUI;

public partial class ProfileEditorControl : UserControl
{
    private readonly List<CultureInfo> _cultureInfos;
    private readonly List<TimeZoneInfo> _timezones;

    public static readonly DependencyProperty ShowDisplayOptionsProperty =
        DependencyProperty.Register(
            nameof(ShowDisplayOptions),
            typeof(bool),
            typeof(ProfileEditorControl),
            new PropertyMetadata(true, OnShowDisplayOptionsChanged));

    public bool ShowDisplayOptions
    {
        get => (bool)GetValue(ShowDisplayOptionsProperty);
        set => SetValue(ShowDisplayOptionsProperty, value);
    }

    public ProfileEditorControl()
    {
        InitializeComponent();

        _cultureInfos = CultureInfo.GetCultures(CultureTypes.AllCultures)
            .OrderBy(i => i.DisplayName).ToList();
        cbLocation.ItemsSource = _cultureInfos.Select(c => c.DisplayName);

        _timezones = TimeZoneInfo.GetSystemTimeZones().ToList();
        cbTimezone.ItemsSource = _timezones.Select(t => t.DisplayName);
    }

    public void LoadProfile(LEProfile source)
    {
        cbLocation.SelectedIndex = _cultureInfos.FindIndex(ci => ci.Name == source.Location);
        cbTimezone.SelectedIndex = _timezones.FindIndex(tz => tz.Id == source.Timezone);
        cbStartAsAdmin.IsChecked = source.RunAsAdmin;
        cbRedirectRegistry.IsChecked = source.RedirectRegistry;
        cbIsAdvancedRedirection.IsChecked = source.IsAdvancedRedirection;
        cbStartAsSuspend.IsChecked = source.RunWithSuspend;
        cbShowInMainMenu.IsChecked = source.ShowInMainMenu;
    }

    public LEProfile ReadProfile(LEProfile template)
    {
        var locationIdx = cbLocation.SelectedIndex;
        var timezoneIdx = cbTimezone.SelectedIndex;
        return new LEProfile(
            template.Name,
            template.Guid,
            ShowDisplayOptions ? cbShowInMainMenu.IsChecked == true : template.ShowInMainMenu,
            template.Parameter,
            locationIdx >= 0 ? _cultureInfos[locationIdx].Name : template.Location,
            timezoneIdx >= 0 ? _timezones[timezoneIdx].Id : template.Timezone,
            cbStartAsAdmin.IsChecked == true,
            cbRedirectRegistry.IsChecked == true,
            cbIsAdvancedRedirection.IsChecked == true,
            cbStartAsSuspend.IsChecked == true);
    }

    private static void OnShowDisplayOptionsChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (ProfileEditorControl)d;
        ctrl.displayGroup.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
    }
}
```

- [ ] **Step 4: 在 `DefaultLanguage.xaml` 加入 `Display` i18n key**

Modify `src/LEGUI/Lang/DefaultLanguage.xaml`，加入：
```xml
<system:String x:Key="Display">Display</system:String>
```

- [ ] **Step 5: 執行測試確認通過**

```bash
dotnet test tests/LEGUI.Tests/ --filter ProfileEditorControlTests
```

Expected: PASS（1 個測試通過）

- [ ] **Step 6: 提交**

```bash
git add src/LEGUI/ProfileEditorControl.xaml src/LEGUI/ProfileEditorControl.xaml.cs src/LEGUI/Lang/DefaultLanguage.xaml tests/LEGUI.Tests/ProfileEditorControlTests.cs
git commit -m "feat(LEGUI): add ProfileEditorControl UserControl with Load/Read round-trip"
```

---

### Task 1.2: ShowDisplayOptions 隱藏測試

**Files:**
- Modify: `tests/LEGUI.Tests/ProfileEditorControlTests.cs`

- [ ] **Step 1: 加入測試**

```csharp
[WpfFact]
public void ShowDisplayOptionsFalse_HidesDisplayGroup()
{
    var ctrl = new ProfileEditorControl();
    ctrl.ShowDisplayOptions = false;

    Assert.Equal(System.Windows.Visibility.Collapsed, ctrl.displayGroup.Visibility);
}

[WpfFact]
public void ShowDisplayOptionsFalse_ReadProfilePreservesShowInMainMenuFromTemplate()
{
    var ctrl = new ProfileEditorControl();
    ctrl.ShowDisplayOptions = false;
    var template = SampleProfile(); // ShowInMainMenu=true
    ctrl.LoadProfile(template);

    // UI 的 cbShowInMainMenu 若被 UI 改了也不應影響 ReadProfile
    ctrl.cbShowInMainMenu.IsChecked = false;
    var result = ctrl.ReadProfile(template);

    Assert.True(result.ShowInMainMenu); // 沿用 template
}
```

注意：`displayGroup` 和 `cbShowInMainMenu` 需要 `internal` 可存取（LEGUI.csproj 已有 `InternalsVisibleTo`）。將 XAML 的 `x:Name` 生成的欄位預設為 `internal`（WPF 預設即 `internal`，但需確認編譯結果）。

若測試編譯失敗因為欄位是 `private`，在 `ProfileEditorControl.xaml.cs` 明確宣告：

```csharp
internal GroupBox displayGroup => (GroupBox)FindName("displayGroup");
internal CheckBox cbShowInMainMenu => (CheckBox)FindName("cbShowInMainMenu");
```

- [ ] **Step 2: 執行測試確認通過**

```bash
dotnet test tests/LEGUI.Tests/ --filter ProfileEditorControlTests
```

Expected: 3 個測試通過

- [ ] **Step 3: 提交**

```bash
git add tests/LEGUI.Tests/ProfileEditorControlTests.cs src/LEGUI/ProfileEditorControl.xaml.cs
git commit -m "test(LEGUI): cover ShowDisplayOptions visibility and template preservation"
```

---

### Task 1.3: 加入 Tooltips 與修錯字

**Files:**
- Modify: `src/LEGUI/Lang/DefaultLanguage.xaml`
- Modify: `src/LEGUI/ProfileEditorControl.xaml`

- [ ] **Step 1: 加入 tooltip i18n keys 與修正 `CREATE__SUSPENDED` 錯字**

Modify `src/LEGUI/Lang/DefaultLanguage.xaml`，將既有 `WithCREATESUSPENDED` 的**值**從 `Create process with CREATE__SUSPENDED` 改為 `Create process with CREATE_SUSPENDED`（單底線），並新增 tooltip key：

```xml
<system:String x:Key="WithCREATESUSPENDED">Create process with CREATE_SUSPENDED</system:String>
<system:String x:Key="AsAdminTip">Launch the target process with elevated privileges. Required for some games.</system:String>
<system:String x:Key="RedirectRegistryTip">Intercept Registry reads for locale-related keys, returning the emulated locale instead of the system's.</system:String>
<system:String x:Key="IsAdvancedRedirectionTip">Also redirect Registry keys that control system UI language, affecting some apps that query via MUI_LANGUAGE_NAME.</system:String>
<system:String x:Key="WithCREATESUSPENDEDTip">Create the target process in suspended state (advanced debugging option).</system:String>
<system:String x:Key="ShowInMainMenuTip">Display this profile directly in the right-click menu root instead of under a submenu.</system:String>
```

- [ ] **Step 2: 在 `ProfileEditorControl.xaml` 的 CheckBox 加 `ToolTip`**

修改 `ProfileEditorControl.xaml`，每個 CheckBox 加對應 tooltip：

```xml
<CheckBox x:Name="cbStartAsAdmin" Content="{DynamicResource AsAdmin}"
          ToolTip="{DynamicResource AsAdminTip}" Margin="0,2" />
<CheckBox x:Name="cbRedirectRegistry" Content="{DynamicResource RedirectRegistry}"
          ToolTip="{DynamicResource RedirectRegistryTip}" Margin="0,2" />
<CheckBox x:Name="cbIsAdvancedRedirection" Content="{DynamicResource IsAdvancedRedirection}"
          ToolTip="{DynamicResource IsAdvancedRedirectionTip}" Margin="0,2" />
<CheckBox x:Name="cbStartAsSuspend" Content="{DynamicResource WithCREATESUSPENDED}"
          ToolTip="{DynamicResource WithCREATESUSPENDEDTip}" Margin="0,2" />
```

以及 displayGroup 內的 cbShowInMainMenu：
```xml
<CheckBox x:Name="cbShowInMainMenu" Content="{DynamicResource ShowInMainMenu}"
          ToolTip="{DynamicResource ShowInMainMenuTip}" Margin="4" />
```

- [ ] **Step 3: 建置驗證**

```bash
dotnet build src/LEGUI/
```

Expected: 成功，0 錯誤。

- [ ] **Step 4: 提交**

```bash
git add src/LEGUI/Lang/DefaultLanguage.xaml src/LEGUI/ProfileEditorControl.xaml
git commit -m "feat(LEGUI): add tooltips to advanced options checkboxes, fix CREATE_SUSPENDED typo"
```

---

## Phase 2: GlobalConfig TabControl 重構

### Task 2.1: 重構 GlobalConfig.xaml 採用 TabControl

**Files:**
- Modify: `src/LEGUI/GlobalConfig.xaml`
- Modify: `src/LEGUI/GlobalConfig.xaml.cs`
- Modify: `src/LEGUI/Lang/DefaultLanguage.xaml`

- [ ] **Step 1: 加入新的 i18n keys**

Modify `src/LEGUI/Lang/DefaultLanguage.xaml`，加入：
```xml
<system:String x:Key="TabProfile">Profile</system:String>
<system:String x:Key="TabShellExtension">Shell Extension</system:String>
<system:String x:Key="TabAbout">About</system:String>
<system:String x:Key="SavedStatus">Saved</system:String>
```

- [ ] **Step 2: 以 TabControl 改寫 `GlobalConfig.xaml`**

覆寫全檔為：

```xml
<Window x:Class="LEGUI.GlobalConfig"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:legui="clr-namespace:LEGUI"
        Title="LEGUI GLOBAL" SizeToContent="Height" MinWidth="420" Width="420"
        WindowStartupLocation="CenterScreen" ResizeMode="CanMinimize">
    <DockPanel TextElement.FontFamily="{DynamicResource UIFont}">
        <StatusBar x:Name="statusBar" DockPanel.Dock="Bottom" Height="22">
            <StatusBarItem><TextBlock x:Name="statusText" /></StatusBarItem>
        </StatusBar>
        <TabControl Margin="4" MinHeight="380">
            <TabItem Header="{DynamicResource TabProfile}">
                <DockPanel Margin="4">
                    <Grid DockPanel.Dock="Top" Height="60" Background="#FFF1F1F1" Margin="0,0,0,6">
                        <Grid.RowDefinitions>
                            <RowDefinition Height="*" />
                            <RowDefinition Height="*" />
                        </Grid.RowDefinitions>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="*" />
                        </Grid.ColumnDefinitions>
                        <ComboBox x:Name="cbGlobalProfiles" Grid.Row="0" Grid.ColumnSpan="3"
                                  VerticalContentAlignment="Center" Margin="3,3,3,4"
                                  SelectionChanged="cbGlobalProfiles_SelectionChanged" />
                        <Button x:Name="bSaveGlobalSetting" Grid.Row="1" Grid.Column="0"
                                Content="{DynamicResource Save}" Margin="3,0,3,5"
                                Click="bSaveGlobalSetting_Click" />
                        <Button x:Name="bSaveGlobalSettingAs" Grid.Row="1" Grid.Column="1"
                                Content="{DynamicResource SaveAs}" Margin="3,0,3,5"
                                Click="bSaveGlobalSettingAs_Click" />
                        <Button x:Name="bDeleteGlobalSetting" Grid.Row="1" Grid.Column="2"
                                Content="{DynamicResource Delete}" Margin="3,0,3,5"
                                Click="bDeleteGlobalSetting_Click" />
                    </Grid>
                    <legui:ProfileEditorControl x:Name="profileEditor" />
                </DockPanel>
            </TabItem>
            <TabItem Header="{DynamicResource TabShellExtension}">
                <!-- 暫時為空，Task 4 會填入 ShellExtensionPanel -->
                <Grid />
            </TabItem>
            <TabItem Header="{DynamicResource TabAbout}">
                <!-- 暫時為空，Task 6 會填入 AboutPanel -->
                <Grid />
            </TabItem>
        </TabControl>
    </DockPanel>
</Window>
```

- [ ] **Step 3: 改寫 `GlobalConfig.xaml.cs` 使用 ProfileEditorControl**

```csharp
#nullable disable

using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LECommonLibrary;

namespace LEGUI;

public partial class GlobalConfig
{
    private readonly List<LEProfile> _profiles;
    private readonly DispatcherTimer _statusClearTimer;

    public GlobalConfig()
    {
        InitializeComponent();

        _profiles = LEConfig.GetProfiles().ToList();
        cbGlobalProfiles.ItemsSource = _profiles.Select(p => p.Name);

        _statusClearTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _statusClearTimer.Tick += (_, _) =>
        {
            statusText.Text = string.Empty;
            _statusClearTimer.Stop();
        };
    }

    private void ShowSavedStatus()
    {
        statusText.Text = I18n.GetString("SavedStatus");
        _statusClearTimer.Stop();
        _statusClearTimer.Start();
    }

    private void bSaveGlobalSetting_Click(object sender, RoutedEventArgs e)
    {
        if (cbGlobalProfiles.Items.Count == 0)
            return;

        var idx = cbGlobalProfiles.SelectedIndex;
        _profiles[idx] = profileEditor.ReadProfile(_profiles[idx]);
        LEConfig.SaveGlobalConfigFile(_profiles.ToArray());
        ShowSavedStatus();
    }

    private void cbGlobalProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bDeleteGlobalSetting.IsEnabled = cbGlobalProfiles.Items.Count != 0;
        bSaveGlobalSetting.IsEnabled = cbGlobalProfiles.Items.Count != 0;

        if (cbGlobalProfiles.SelectedIndex == -1)
            return;

        profileEditor.LoadProfile(_profiles[cbGlobalProfiles.SelectedIndex]);
    }

    private void bSaveGlobalSettingAs_Click(object sender, RoutedEventArgs e)
    {
        var ib = new InputBox
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Instruction = I18n.GetString("SaveAsInstruction"),
            OkText = I18n.GetString("Save"),
            CancelText = I18n.GetString("Cancel")
        };

        if (ib.ShowDialog() == true && !string.IsNullOrEmpty(ib.Text))
        {
            SaveProfileAs(ib.Text);
            cbGlobalProfiles.SelectedIndex = _profiles.Count - 1;
            ShowSavedStatus();
        }
    }

    private void SaveProfileAs(string name)
    {
        // 以當前 UI 值建立 profile，Name 用新名稱、Guid 新生
        var template = cbGlobalProfiles.SelectedIndex >= 0
            ? _profiles[cbGlobalProfiles.SelectedIndex]
            : new LEProfile(true);
        template.Name = name;
        template.Guid = Guid.NewGuid().ToString();
        var created = profileEditor.ReadProfile(template);

        _profiles.Add(created);
        LEConfig.SaveGlobalConfigFile(_profiles.ToArray());

        cbGlobalProfiles.ItemsSource = _profiles.Select(p => p.Name);
    }

    private void bDeleteGlobalSetting_Click(object sender, RoutedEventArgs e)
    {
        if (cbGlobalProfiles.SelectedIndex == -1)
            return;

        if (MessageBoxResult.No == MessageBox.Show(
            I18n.GetString("ConfirmDel"),
            "Locale Emulator",
            MessageBoxButton.YesNo))
            return;

        _profiles.RemoveAt(cbGlobalProfiles.SelectedIndex);
        LEConfig.SaveGlobalConfigFile(_profiles.ToArray());
        cbGlobalProfiles.ItemsSource = _profiles.Select(p => p.Name);
        cbGlobalProfiles.SelectedIndex = 0;
    }
}
```

重點變更：
- 移除 `CultureInfo`/`TimeZoneInfo` 清單（移至 ProfileEditorControl 內部）
- 移除 BlurEffect（`ShowDialog()` modal 已足夠）
- `MessageBox.Show` 第二參數從 `""` 改為 `"Locale Emulator"`
- `IsChecked != null && (bool)IsChecked` 全部已在 ReadProfile 內改為 `== true`
- 新增 `ShowSavedStatus()` 透過 DispatcherTimer 3 秒後清空

- [ ] **Step 4: 建置驗證**

```bash
dotnet build src/LEGUI/
```

Expected: 成功。

- [ ] **Step 5: 提交**

```bash
git add src/LEGUI/GlobalConfig.xaml src/LEGUI/GlobalConfig.xaml.cs src/LEGUI/Lang/DefaultLanguage.xaml
git commit -m "refactor(LEGUI): GlobalConfig adopts TabControl and shared ProfileEditorControl"
```

---

## Phase 3: AppConfig TabControl 重構

### Task 3.1: 重構 AppConfig.xaml 採用 TabControl（ShowDisplayOptions=False）

**Files:**
- Modify: `src/LEGUI/AppConfig.xaml`
- Modify: `src/LEGUI/AppConfig.xaml.cs`

**說明**：既有 `bSaveAppSetting_Click` / `bShortcut_Click` 都呼叫 `RunAndShutdown()`（儲存後立即啟動目標程式並關閉 LEGUI），所以 AppConfig 不需要 StatusBar「Saved」回饋（App 會立刻關閉）。這與 GlobalConfig 行為不同。

- [ ] **Step 1: 以 TabControl 改寫 `AppConfig.xaml`（Profile + About，無 StatusBar）**

```xml
<Window x:Class="LEGUI.AppConfig"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:legui="clr-namespace:LEGUI"
        Title="LEGUI - " SizeToContent="Height" MinWidth="420" Width="420"
        WindowStartupLocation="CenterScreen" ResizeMode="CanMinimize">
    <Grid TextElement.FontFamily="{DynamicResource UIFont}">
        <TabControl Margin="4" MinHeight="340">
            <TabItem Header="{DynamicResource TabProfile}">
                <DockPanel Margin="4">
                    <Grid DockPanel.Dock="Top" Height="60" Background="#FFF1F1F1" Margin="0,0,0,6">
                        <Grid.RowDefinitions>
                            <RowDefinition Height="*" />
                            <RowDefinition Height="*" />
                        </Grid.RowDefinitions>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="*" />
                        </Grid.ColumnDefinitions>
                        <legui:MaskedTextBox x:Name="tbAppParameter" Grid.Row="0" Grid.ColumnSpan="3"
                                             MaskText="{StaticResource EnterArgument}"
                                             VerticalContentAlignment="Center" Margin="3,3,3,4" />
                        <Button x:Name="bSaveAppSetting" Grid.Row="1" Grid.Column="0"
                                Content="{DynamicResource Save}" Margin="3,0,3,5"
                                Click="bSaveAppSetting_Click" />
                        <Button x:Name="bShortcut" Grid.Row="1" Grid.Column="1"
                                Content="{DynamicResource Shortcut}" Margin="3,0,3,5"
                                Click="bShortcut_Click" />
                        <Button x:Name="bDeleteAppSetting" Grid.Row="1" Grid.Column="2"
                                Content="{DynamicResource Delete}" Margin="3,0,3,5"
                                Click="bDeleteAppSetting_Click" />
                    </Grid>
                    <legui:ProfileEditorControl x:Name="profileEditor" ShowDisplayOptions="False" />
                </DockPanel>
            </TabItem>
            <TabItem Header="{DynamicResource TabAbout}">
                <!-- 暫時為空，Task 6 會填入 AboutPanel -->
                <Grid />
            </TabItem>
        </TabControl>
    </Grid>
</Window>
```

- [ ] **Step 2: 改寫 `AppConfig.xaml.cs` 使用 ProfileEditorControl**

覆寫全檔：

```csharp
#nullable disable

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Windows;
using LECommonLibrary;

namespace LEGUI;

public partial class AppConfig
{
    public AppConfig()
    {
        InitializeComponent();

        Title += Path.GetFileName(App.StandaloneFilePath).Replace(".le.config", "");

        // 載入既有設定（或使用預設）
        var configs = LEConfig.GetProfiles(App.StandaloneFilePath);
        LEProfile initial;
        if (configs.Length > 0)
        {
            initial = configs[0];
            if (!string.IsNullOrEmpty(initial.Parameter))
            {
                tbAppParameter.FontStyle = FontStyles.Normal;
                tbAppParameter.Text = initial.Parameter;
            }
        }
        else
        {
            // 預設 ja-JP / Tokyo
            initial = new LEProfile(true);
        }

        profileEditor.LoadProfile(initial);
    }

    private void SaveSetting()
    {
        // 以當前 UI 值建立 profile
        var template = new LEProfile(
            Path.GetFileName(App.StandaloneFilePath),
            Guid.NewGuid().ToString(),
            false,              // AppConfig 不用 ShowInMainMenu（ShowDisplayOptions=False 會沿用此值）
            tbAppParameter.Text,
            "ja-JP",            // 會被 profileEditor.ReadProfile 覆寫
            "Tokyo Standard Time",
            false, false, false, false);

        var crt = profileEditor.ReadProfile(template);
        crt.Parameter = tbAppParameter.Text; // ReadProfile 沿用 template.Parameter，已 OK

        LEConfig.SaveApplicationConfigFile(App.StandaloneFilePath, crt);
    }

    private void CreateShortcut(string path)
    {
        try
        {
            var link = (IShellLink)new ShellLink();
            link.SetPath(Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "LEProc.exe"));
            link.SetArguments($"-run \"{path}\"");
            link.SetIconLocation(
                AssociationReader.GetAssociatedIcon(Path.GetExtension(path)).Replace("%1", path), 0);
            link.SetDescription($"Run {Path.GetFileName(path)} with Locale Emulator");
            link.SetWorkingDirectory(Path.GetDirectoryName(path));

            var file = (IPersistFile)link;
            file.Save(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    Path.GetFileNameWithoutExtension(path) + ".lnk"),
                false);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message + "\r\n\r\n" + ex.StackTrace, "Locale Emulator");
        }
    }

    private void RunAndShutdown()
    {
        Process.Start(
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "LEProc.exe"),
            $"-run \"{App.StandaloneFilePath.Replace(".le.config", "")}\"");
        Application.Current.Shutdown();
    }

    private void bSaveAppSetting_Click(object sender, RoutedEventArgs e)
    {
        SaveSetting();
        RunAndShutdown();
    }

    private void bShortcut_Click(object sender, RoutedEventArgs e)
    {
        SaveSetting();
        CreateShortcut(App.StandaloneFilePath.Replace(".le.config", ""));
        RunAndShutdown();
    }

    private void bDeleteAppSetting_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBoxResult.No == MessageBox.Show(
            I18n.GetString("ConfirmDel"),
            "Locale Emulator",
            MessageBoxButton.YesNo))
            return;

        if (File.Exists(App.StandaloneFilePath))
            File.Delete(App.StandaloneFilePath);

        Application.Current.Shutdown();
    }
}
```

重點變更：
- `CultureInfo` / `TimeZoneInfo` 清單完全移除（移至 ProfileEditorControl 內部）
- `SaveSetting` 改用 `profileEditor.ReadProfile(template)` 組 profile
- 空標題對話框修正為 `"Locale Emulator"`
- `IsChecked != null && (bool)IsChecked` 全部消失（由 ReadProfile 內部處理）
- **保留** `RunAndShutdown()` 既有行為（Save 後啟動目標程式）

- [ ] **Step 3: 建置驗證**

```bash
dotnet build src/LEGUI/
```

Expected: 成功。

- [ ] **Step 4: 提交**

```bash
git add src/LEGUI/AppConfig.xaml src/LEGUI/AppConfig.xaml.cs
git commit -m "refactor(LEGUI): AppConfig adopts TabControl and shared ProfileEditorControl"
```

---

## Phase 4: ShellExtensionPanel UserControl

### Task 4.1: 建立 ShellExtensionPanel 骨架（僅 UI + 狀態顯示）

**Files:**
- Create: `src/LEGUI/ShellExtensionPanel.xaml`
- Create: `src/LEGUI/ShellExtensionPanel.xaml.cs`
- Create: `tests/LEGUI.Tests/ShellExtensionPanelTests.cs`

- [ ] **Step 1: 先寫失敗測試（按鈕狀態對應 IsInstalled）**

```csharp
using NSubstitute;
using Xunit;

namespace LEGUI.Tests;

public class ShellExtensionPanelTests
{
    [WpfFact]
    public void RefreshStatus_WhenCurrentUserInstalled_DisablesInstallButton()
    {
        var panel = new ShellExtensionPanel();
        var registrar = Substitute.For<IShellExtensionQuery>();
        registrar.IsInstalled(ShellExtensionRegistrar.InstallMode.CurrentUser).Returns(true);
        registrar.IsInstalled(ShellExtensionRegistrar.InstallMode.AllUsers).Returns(false);
        registrar.HasOldRegistration().Returns(false);

        panel.SetQuery(registrar);
        panel.RefreshStatus();

        Assert.False(panel.bInstallCurrentUser.IsEnabled);
        Assert.True(panel.bUninstallCurrentUser.IsEnabled);
        Assert.True(panel.bInstallAllUsers.IsEnabled);
        Assert.False(panel.bUninstallAllUsers.IsEnabled);
    }

    [WpfFact]
    public void RefreshStatus_HasOldRegistration_ShowsCleanupSection()
    {
        var panel = new ShellExtensionPanel();
        var registrar = Substitute.For<IShellExtensionQuery>();
        registrar.HasOldRegistration().Returns(true);

        panel.SetQuery(registrar);
        panel.RefreshStatus();

        Assert.Equal(System.Windows.Visibility.Visible, panel.cleanupSection.Visibility);
    }
}
```

**抽介面**：為了測試，加入薄介面 `IShellExtensionQuery` 封裝 `ShellExtensionRegistrar` 的查詢方法：

```csharp
// src/LEGUI/IShellExtensionQuery.cs
public interface IShellExtensionQuery
{
    bool IsInstalled(ShellExtensionRegistrar.InstallMode mode);
    bool HasOldRegistration();
}
```

- [ ] **Step 2: 執行測試確認失敗**

```bash
dotnet test tests/LEGUI.Tests/ --filter ShellExtensionPanelTests
```

Expected: FAIL（ShellExtensionPanel 不存在）

- [ ] **Step 3: 建立 XAML**

`src/LEGUI/ShellExtensionPanel.xaml`:

```xml
<UserControl x:Class="LEGUI.ShellExtensionPanel"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <StackPanel Margin="8" TextElement.FontFamily="{DynamicResource UIFont}">
        <GroupBox Header="Current User" Margin="0,4">
            <StackPanel Margin="8">
                <TextBlock>
                    <Run Text="{DynamicResource InstallStatus}" />
                    <Run x:Name="tStatusCurrentUser" FontWeight="Bold" />
                </TextBlock>
                <StackPanel Orientation="Horizontal" Margin="0,6,0,0">
                    <Button x:Name="bInstallCurrentUser" Content="{DynamicResource InstallCurrentUser}"
                            MinWidth="120" Margin="0,0,8,0" Click="bInstallCurrentUser_Click" />
                    <Button x:Name="bUninstallCurrentUser" Content="{DynamicResource Uninstall}"
                            MinWidth="100" Click="bUninstallCurrentUser_Click" />
                </StackPanel>
            </StackPanel>
        </GroupBox>
        <GroupBox Header="All Users 🛡" Margin="0,4">
            <StackPanel Margin="8">
                <TextBlock>
                    <Run Text="{DynamicResource InstallStatus}" />
                    <Run x:Name="tStatusAllUsers" FontWeight="Bold" />
                </TextBlock>
                <StackPanel Orientation="Horizontal" Margin="0,6,0,0">
                    <Button x:Name="bInstallAllUsers" Content="{DynamicResource InstallAllUsers}"
                            MinWidth="120" Margin="0,0,8,0" Click="bInstallAllUsers_Click" />
                    <Button x:Name="bUninstallAllUsers" Content="{DynamicResource Uninstall}"
                            MinWidth="100" Click="bUninstallAllUsers_Click" />
                </StackPanel>
            </StackPanel>
        </GroupBox>
        <StackPanel x:Name="cleanupSection" Visibility="Collapsed" Margin="0,8">
            <TextBlock Text="⚠ Old version residue detected" Foreground="DarkRed" />
            <Button x:Name="bCleanupOld" Content="Clean up old registration" MinWidth="180"
                    HorizontalAlignment="Left" Margin="0,4,0,0" Click="bCleanupOld_Click" />
        </StackPanel>
    </StackPanel>
</UserControl>
```

- [ ] **Step 4: 建立 code-behind（最小實作至測試通過）**

`src/LEGUI/ShellExtensionPanel.xaml.cs`:

```csharp
#nullable disable

using System.Windows;
using System.Windows.Controls;

namespace LEGUI;

public partial class ShellExtensionPanel : UserControl
{
    private IShellExtensionQuery _query;

    public ShellExtensionPanel()
    {
        InitializeComponent();
    }

    public void SetQuery(IShellExtensionQuery query) => _query = query;

    public void RefreshStatus()
    {
        if (_query == null) return;

        var cuInstalled = _query.IsInstalled(ShellExtensionRegistrar.InstallMode.CurrentUser);
        var auInstalled = _query.IsInstalled(ShellExtensionRegistrar.InstallMode.AllUsers);

        tStatusCurrentUser.Text = " " + I18n.GetString(cuInstalled ? "Installed" : "NotInstalled");
        tStatusAllUsers.Text = " " + I18n.GetString(auInstalled ? "Installed" : "NotInstalled");

        bInstallCurrentUser.IsEnabled = !cuInstalled;
        bUninstallCurrentUser.IsEnabled = cuInstalled;
        bInstallAllUsers.IsEnabled = !auInstalled;
        bUninstallAllUsers.IsEnabled = auInstalled;

        cleanupSection.Visibility = _query.HasOldRegistration()
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    // 以下 click handler 先留空，Task 4.2+ 實作
    private void bInstallCurrentUser_Click(object sender, RoutedEventArgs e) { }
    private void bUninstallCurrentUser_Click(object sender, RoutedEventArgs e) { }
    private void bInstallAllUsers_Click(object sender, RoutedEventArgs e) { }
    private void bUninstallAllUsers_Click(object sender, RoutedEventArgs e) { }
    private void bCleanupOld_Click(object sender, RoutedEventArgs e) { }
}
```

實作 `IShellExtensionQuery` adapter（讓 ShellExtensionRegistrar 能直接當 IShellExtensionQuery 用）：

```csharp
// 在 ShellExtensionRegistrar.cs 加入 interface 實作
public sealed class ShellExtensionRegistrar : IShellExtensionQuery
{
    // ...既有內容... 既有的 IsInstalled(mode) 和 HasOldRegistration() 簽名已相容
}
```

- [ ] **Step 5: 執行測試確認通過**

```bash
dotnet test tests/LEGUI.Tests/ --filter ShellExtensionPanelTests
```

Expected: 2 個測試通過

- [ ] **Step 6: 提交**

```bash
git add src/LEGUI/ShellExtensionPanel.xaml src/LEGUI/ShellExtensionPanel.xaml.cs src/LEGUI/IShellExtensionQuery.cs src/LEGUI/ShellExtensionRegistrar.cs tests/LEGUI.Tests/ShellExtensionPanelTests.cs
git commit -m "feat(LEGUI): add ShellExtensionPanel with install status display"
```

---

### Task 4.2: 接上 Install/Uninstall 按鈕的 in-process 路徑（admin 情境）

**Files:**
- Modify: `src/LEGUI/ShellExtensionPanel.xaml.cs`
- Modify: `src/LEGUI/GlobalConfig.xaml`（把 Shell Extension tab 內容改為 ShellExtensionPanel）
- Modify: `src/LEGUI/GlobalConfig.xaml.cs`（建立 registrar、註入 panel）
- Modify: `tests/LEGUI.Tests/ShellExtensionPanelTests.cs`

- [ ] **Step 1: 測試：按下 Install，呼叫 registrar.Register**

擴充介面為完整 command：

```csharp
public interface IShellExtensionCommand : IShellExtensionQuery
{
    void Register(ShellExtensionRegistrar.InstallMode mode, string dllPath);
    void Unregister(ShellExtensionRegistrar.InstallMode mode);
    void CleanupOldRegistration();
}
```

測試：

```csharp
[WpfFact]
public void InstallCurrentUser_CallsRegisterWithCurrentUserMode()
{
    var panel = new ShellExtensionPanel();
    var command = Substitute.For<IShellExtensionCommand>();
    command.IsInstalled(Arg.Any<ShellExtensionRegistrar.InstallMode>()).Returns(false);
    panel.SetCommand(command, dllPath: @"C:\fake\ShellExtension.dll", isAdmin: true);

    panel.bInstallCurrentUser.RaiseEvent(
        new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));

    command.Received().Register(
        ShellExtensionRegistrar.InstallMode.CurrentUser,
        @"C:\fake\ShellExtension.dll");
}
```

- [ ] **Step 2: 擴充 ShellExtensionPanel 支援 admin in-process 路徑**

```csharp
public partial class ShellExtensionPanel : UserControl
{
    private IShellExtensionCommand _command;
    private string _dllPath;
    private bool _isAdmin;

    public void SetCommand(IShellExtensionCommand command, string dllPath, bool isAdmin)
    {
        _command = command;
        _dllPath = dllPath;
        _isAdmin = isAdmin;
        SetQuery(command);
    }

    private void bInstallCurrentUser_Click(object sender, RoutedEventArgs e)
        => HandleInstall(ShellExtensionRegistrar.InstallMode.CurrentUser);

    private void bInstallAllUsers_Click(object sender, RoutedEventArgs e)
        => HandleInstall(ShellExtensionRegistrar.InstallMode.AllUsers);

    private void bUninstallCurrentUser_Click(object sender, RoutedEventArgs e)
        => HandleUninstall(ShellExtensionRegistrar.InstallMode.CurrentUser);

    private void bUninstallAllUsers_Click(object sender, RoutedEventArgs e)
        => HandleUninstall(ShellExtensionRegistrar.InstallMode.AllUsers);

    private void bCleanupOld_Click(object sender, RoutedEventArgs e)
    {
        // All Users 路徑需要 admin — Task 4.3 會升級為 async 子 process
        _command.CleanupOldRegistration();
        RefreshStatus();
    }

    private void HandleInstall(ShellExtensionRegistrar.InstallMode mode)
    {
        // Task 4.3 會把 AllUsers 分支改為 async 子 process
        _command.Register(mode, _dllPath);
        RefreshStatus();
        MessageBox.Show(I18n.GetString("InstallSuccess"), "Locale Emulator");
    }

    private void HandleUninstall(ShellExtensionRegistrar.InstallMode mode)
    {
        _command.Unregister(mode);
        RefreshStatus();
        MessageBox.Show(I18n.GetString("UninstallSuccess"), "Locale Emulator");
    }
}
```

- [ ] **Step 3: 改 `GlobalConfig.xaml` 把 Shell Extension tab 填入 panel**

修改 `GlobalConfig.xaml` 的 `<TabItem Header="{DynamicResource TabShellExtension}">` 內容：

```xml
<TabItem Header="{DynamicResource TabShellExtension}">
    <legui:ShellExtensionPanel x:Name="shellExtPanel" />
</TabItem>
```

- [ ] **Step 4: 在 `GlobalConfig` 建構子初始化 panel**

```csharp
public GlobalConfig()
{
    InitializeComponent();
    // ... 既有 profile 初始化

    InitializeShellExtPanel();
}

private void InitializeShellExtPanel()
{
    var basePath = Path.GetDirectoryName(Path.GetDirectoryName(Environment.ProcessPath));
    if (string.IsNullOrEmpty(basePath))
        basePath = AppContext.BaseDirectory;

    var dllPath = ShellExtensionRegistrar.AutoDetectDllPath(basePath);
    var registrar = new ShellExtensionRegistrar(
        new RegistryOperations(),
        ShellExtensionConstants.NewClsid);

    shellExtPanel.SetCommand(registrar, dllPath, SystemHelper.IsAdministrator());
    shellExtPanel.RefreshStatus();
}
```

同時在 `ShellExtensionRegistrar.cs` 新增 `IShellExtensionCommand` 的實作（增加 `CleanupOldRegistration` 已存在、`Register`、`Unregister` 簽名相容 interface）。

- [ ] **Step 5: 執行測試**

```bash
dotnet test tests/LEGUI.Tests/
```

Expected: 全部通過

- [ ] **Step 6: 提交**

```bash
git add src/LEGUI/ShellExtensionPanel.xaml.cs src/LEGUI/IShellExtensionQuery.cs src/LEGUI/ShellExtensionRegistrar.cs src/LEGUI/GlobalConfig.xaml src/LEGUI/GlobalConfig.xaml.cs tests/LEGUI.Tests/ShellExtensionPanelTests.cs
git commit -m "feat(LEGUI): wire ShellExtensionPanel to GlobalConfig with in-process install path"
```

---

## Phase 5: CLI 參數 + Sub-process 提權

### Task 5.1: CLI argument parser

**Files:**
- Create: `src/LEGUI/ShellExtCliCommand.cs`
- Create: `tests/LEGUI.Tests/ShellExtCliCommandTests.cs`

- [ ] **Step 1: 寫失敗測試**

```csharp
using Xunit;

namespace LEGUI.Tests;

public class ShellExtCliCommandTests
{
    [Theory]
    [InlineData(new[] { "--shell-ext", "install", "current-user" },
                ShellExtCliCommand.Verb.Install,
                ShellExtensionRegistrar.InstallMode.CurrentUser)]
    [InlineData(new[] { "--shell-ext", "install", "all-users" },
                ShellExtCliCommand.Verb.Install,
                ShellExtensionRegistrar.InstallMode.AllUsers)]
    [InlineData(new[] { "--shell-ext", "uninstall", "current-user" },
                ShellExtCliCommand.Verb.Uninstall,
                ShellExtensionRegistrar.InstallMode.CurrentUser)]
    public void Parse_ValidArgs_ReturnsParsedCommand(
        string[] args, ShellExtCliCommand.Verb verb, ShellExtensionRegistrar.InstallMode mode)
    {
        var cmd = ShellExtCliCommand.Parse(args);
        Assert.NotNull(cmd);
        Assert.Equal(verb, cmd.ActionVerb);
        Assert.Equal(mode, cmd.Mode);
    }

    [Fact]
    public void Parse_CleanupOld_NoModeRequired()
    {
        var cmd = ShellExtCliCommand.Parse(new[] { "--shell-ext", "cleanup-old" });
        Assert.NotNull(cmd);
        Assert.Equal(ShellExtCliCommand.Verb.CleanupOld, cmd.ActionVerb);
    }

    [Fact]
    public void Parse_NonShellExtArgs_ReturnsNull()
    {
        Assert.Null(ShellExtCliCommand.Parse(new[] { "game.exe" }));
        Assert.Null(ShellExtCliCommand.Parse(Array.Empty<string>()));
    }

    [Fact]
    public void Parse_InvalidVerb_ReturnsNull()
    {
        Assert.Null(ShellExtCliCommand.Parse(new[] { "--shell-ext", "xyz", "current-user" }));
    }
}
```

- [ ] **Step 2: 執行確認失敗**

```bash
dotnet test tests/LEGUI.Tests/ --filter ShellExtCliCommandTests
```

Expected: FAIL

- [ ] **Step 3: 實作**

`src/LEGUI/ShellExtCliCommand.cs`:

```csharp
#nullable enable

namespace LEGUI;

public sealed class ShellExtCliCommand
{
    public enum Verb { Install, Uninstall, CleanupOld }

    public Verb ActionVerb { get; }
    public ShellExtensionRegistrar.InstallMode Mode { get; }

    private ShellExtCliCommand(Verb verb, ShellExtensionRegistrar.InstallMode mode)
    {
        ActionVerb = verb;
        Mode = mode;
    }

    public static ShellExtCliCommand? Parse(string[] args)
    {
        if (args.Length < 2 || args[0] != "--shell-ext") return null;

        return args[1] switch
        {
            "install" when args.Length == 3 && TryParseMode(args[2], out var im)
                => new ShellExtCliCommand(Verb.Install, im),
            "uninstall" when args.Length == 3 && TryParseMode(args[2], out var um)
                => new ShellExtCliCommand(Verb.Uninstall, um),
            "cleanup-old"
                => new ShellExtCliCommand(Verb.CleanupOld, ShellExtensionRegistrar.InstallMode.AllUsers),
            _ => null
        };
    }

    private static bool TryParseMode(string s, out ShellExtensionRegistrar.InstallMode mode)
    {
        mode = default;
        if (s == "current-user") { mode = ShellExtensionRegistrar.InstallMode.CurrentUser; return true; }
        if (s == "all-users")    { mode = ShellExtensionRegistrar.InstallMode.AllUsers; return true; }
        return false;
    }
}
```

- [ ] **Step 4: 測試通過**

```bash
dotnet test tests/LEGUI.Tests/ --filter ShellExtCliCommandTests
```

Expected: 5 個測試通過

- [ ] **Step 5: 提交**

```bash
git add src/LEGUI/ShellExtCliCommand.cs tests/LEGUI.Tests/ShellExtCliCommandTests.cs
git commit -m "feat(LEGUI): add ShellExtCliCommand parser for --shell-ext arguments"
```

---

### Task 5.2: App.xaml.cs 處理 --shell-ext 分支

**Files:**
- Modify: `src/LEGUI/App.xaml.cs`

- [ ] **Step 1: 在 `App_OnStartup` 最前端加入 CLI 分支**

```csharp
private void App_OnStartup(object sender, StartupEventArgs e)
{
    // 新增：優先處理 --shell-ext CLI，跳過所有 UI 啟動邏輯
    var cli = ShellExtCliCommand.Parse(e.Args);
    if (cli != null)
    {
        int exitCode = ExecuteShellExtCommand(cli);
        Current.Shutdown(exitCode);
        return;
    }

    // ... 既有邏輯
}

private int ExecuteShellExtCommand(ShellExtCliCommand cli)
{
    try
    {
        var basePath = Path.GetDirectoryName(Path.GetDirectoryName(Environment.ProcessPath))
                       ?? AppContext.BaseDirectory;
        var dllPath = ShellExtensionRegistrar.AutoDetectDllPath(basePath);
        var registrar = new ShellExtensionRegistrar(
            new RegistryOperations(),
            ShellExtensionConstants.NewClsid);

        switch (cli.ActionVerb)
        {
            case ShellExtCliCommand.Verb.Install:
                registrar.Register(cli.Mode, dllPath);
                break;
            case ShellExtCliCommand.Verb.Uninstall:
                registrar.Unregister(cli.Mode);
                break;
            case ShellExtCliCommand.Verb.CleanupOld:
                registrar.CleanupOldRegistration();
                break;
        }
        return 0;
    }
    catch (Exception ex)
    {
        // 子 process 不顯示 UI，只回傳錯誤代碼
        System.Diagnostics.Debug.WriteLine($"--shell-ext failed: {ex}");
        return 1;
    }
}
```

- [ ] **Step 2: 建置驗證**

```bash
dotnet build src/LEGUI/
```

Expected: 成功。

- [ ] **Step 3: 手動驗證 CLI（以 admin 身分執行）**

```bash
# 編譯 + 以 admin 執行（需要手動開 Windows Terminal 以管理員身分）
dotnet build -c Release src/LEGUI/
# 在管理員 shell 中：
src/LEGUI/bin/Release/net10.0-windows/LEGUI.exe --shell-ext install current-user
echo $LASTEXITCODE  # 應為 0
# 確認 HKCU\Software\Classes\CLSID\{A8B4F5C2-...} 存在
```

- [ ] **Step 4: 提交**

```bash
git add src/LEGUI/App.xaml.cs
git commit -m "feat(LEGUI): handle --shell-ext CLI branch in App startup"
```

---

### Task 5.3: Shell Extension 按鈕改走子 process 提權路徑

**Files:**
- Modify: `src/LEGUI/ShellExtensionPanel.xaml.cs`

- [ ] **Step 1: 改 `HandleInstall` / `HandleUninstall` / `bCleanupOld_Click` 為 async，非 admin 時啟動子 process**

```csharp
private async void HandleInstall(ShellExtensionRegistrar.InstallMode mode)
{
    if (mode == ShellExtensionRegistrar.InstallMode.AllUsers && !_isAdmin)
    {
        await RunElevatedAsync("install", "all-users");
    }
    else
    {
        _command.Register(mode, _dllPath);
    }
    RefreshStatus();
    MessageBox.Show(I18n.GetString("InstallSuccess"), "Locale Emulator");
}

private async void HandleUninstall(ShellExtensionRegistrar.InstallMode mode)
{
    if (mode == ShellExtensionRegistrar.InstallMode.AllUsers && !_isAdmin)
    {
        await RunElevatedAsync("uninstall", "all-users");
    }
    else
    {
        _command.Unregister(mode);
    }
    RefreshStatus();
    MessageBox.Show(I18n.GetString("UninstallSuccess"), "Locale Emulator");
}

private async void bCleanupOld_Click(object sender, RoutedEventArgs e)
{
    if (!_isAdmin)
    {
        await RunElevatedAsync("cleanup-old", null);
    }
    else
    {
        _command.CleanupOldRegistration();
    }
    RefreshStatus();
}

private async Task RunElevatedAsync(string verb, string? scope)
{
    SetAllButtonsEnabled(false);
    try
    {
        var args = scope != null ? $"--shell-ext {verb} {scope}" : $"--shell-ext {verb}";
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = Environment.ProcessPath!,
            Arguments = args,
            Verb = "runas",
            UseShellExecute = true
        };
        try
        {
            using var p = System.Diagnostics.Process.Start(psi);
            if (p == null) return;
            await p.WaitForExitAsync();
            if (p.ExitCode != 0)
            {
                MessageBox.Show(
                    $"Operation failed with exit code {p.ExitCode}.",
                    "Locale Emulator",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (System.ComponentModel.Win32Exception win32) when (win32.NativeErrorCode == 1223)
        {
            // 使用者取消 UAC，不顯示錯誤
        }
    }
    finally
    {
        SetAllButtonsEnabled(true);
    }
}

private void SetAllButtonsEnabled(bool enabled)
{
    bInstallCurrentUser.IsEnabled = enabled;
    bUninstallCurrentUser.IsEnabled = enabled;
    bInstallAllUsers.IsEnabled = enabled;
    bUninstallAllUsers.IsEnabled = enabled;
    bCleanupOld.IsEnabled = enabled;
}
```

- [ ] **Step 2: 建置驗證**

```bash
dotnet build src/LEGUI/
```

Expected: 成功。

- [ ] **Step 3: 手動測試（非 admin 身分執行 LEGUI）**

- 開啟 LEGUI（一般使用者權限）
- 切到 Shell Extension 分頁
- 點「Install (All Users) 🛡」→ 應彈 UAC 對話框
- 點「是」→ 子 process 執行，狀態刷新為 "Installed"
- 點 UAC 的「否」→ 無錯誤訊息，狀態不變

- [ ] **Step 4: 提交**

```bash
git add src/LEGUI/ShellExtensionPanel.xaml.cs
git commit -m "feat(LEGUI): elevate install/uninstall/cleanup via sub-process when not admin"
```

---

## Phase 6: About Tab + 最後清理

### Task 6.1: 建立 AboutPanel UserControl

**Files:**
- Create: `src/LEGUI/AboutPanel.xaml`
- Create: `src/LEGUI/AboutPanel.xaml.cs`

- [ ] **Step 1: 建立 XAML**

`src/LEGUI/AboutPanel.xaml`:

```xml
<UserControl x:Class="LEGUI.AboutPanel"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <StackPanel Margin="20" TextElement.FontFamily="{DynamicResource UIFont}">
        <TextBlock FontSize="16" FontWeight="Bold">
            Locale Emulator (ooxxTaiwan fork)
        </TextBlock>
        <TextBlock Margin="0,4,0,0">
            <Run Text="Version " /><Run x:Name="tVersion" />
        </TextBlock>
        <TextBlock Margin="0,16,0,0" FontWeight="Bold">Original project</TextBlock>
        <TextBlock>
            <Hyperlink NavigateUri="https://github.com/xupefei/Locale-Emulator"
                       RequestNavigate="Hyperlink_RequestNavigate">
                github.com/xupefei/Locale-Emulator
            </Hyperlink>
        </TextBlock>
        <TextBlock Margin="0,8,0,0" FontWeight="Bold">Core (archived 2022-04)</TextBlock>
        <TextBlock>
            <Hyperlink NavigateUri="https://github.com/xupefei/Locale-Emulator-Core"
                       RequestNavigate="Hyperlink_RequestNavigate">
                github.com/xupefei/Locale-Emulator-Core
            </Hyperlink>
        </TextBlock>
        <TextBlock Margin="0,8,0,0" FontWeight="Bold">This fork</TextBlock>
        <TextBlock>
            <Hyperlink NavigateUri="https://github.com/ooxxTaiwan/Locale-Emulator-J"
                       RequestNavigate="Hyperlink_RequestNavigate">
                github.com/ooxxTaiwan/Locale-Emulator-J
            </Hyperlink>
        </TextBlock>
        <TextBlock Margin="0,16,0,0">License: LGPL-3.0</TextBlock>
        <TextBlock>Core portion: LGPL-3.0 / GPL-3.0</TextBlock>
    </StackPanel>
</UserControl>
```

- [ ] **Step 2: 建立 code-behind**

`src/LEGUI/AboutPanel.xaml.cs`:

```csharp
#nullable disable

using System.Diagnostics;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace LEGUI;

public partial class AboutPanel : UserControl
{
    public AboutPanel()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        tVersion.Text = version?.ToString(3) ?? "unknown";
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
```

- [ ] **Step 3: 在 GlobalConfig 和 AppConfig 把 About TabItem 內容換成 `<legui:AboutPanel />`**

Modify `src/LEGUI/GlobalConfig.xaml`:

```xml
<TabItem Header="{DynamicResource TabAbout}">
    <legui:AboutPanel />
</TabItem>
```

Modify `src/LEGUI/AppConfig.xaml` 的 About tab 同樣。

- [ ] **Step 4: 建置驗證**

```bash
dotnet build src/LEGUI/
```

Expected: 成功。

- [ ] **Step 5: 提交**

```bash
git add src/LEGUI/AboutPanel.xaml src/LEGUI/AboutPanel.xaml.cs src/LEGUI/GlobalConfig.xaml src/LEGUI/AppConfig.xaml
git commit -m "feat(LEGUI): add AboutPanel with version, hyperlinks, and license info"
```

---

### Task 6.2: Smoke test 手動驗證清單

**Files:**
- 無程式碼修改；僅手動驗證

- [ ] **Step 1: 建置 Release**

```bash
dotnet build -c Release LocaleEmulator.sln
# 確保 ShellExtension x86+x64 都編譯成功（vs 需要）
```

- [ ] **Step 2: 驗證 GlobalConfig 各項**

- [ ] 雙擊 `LEGUI.exe`（一般使用者）→ 視窗開啟、寬度 420、`SizeToContent` 正常
- [ ] Profile 分頁：選擇 profile、修改欄位、按 Save → 狀態列顯示 "Saved" 3 秒後消失
- [ ] 錯字修正：CheckBox 顯示 "Create process with CREATE_SUSPENDED"（單底線）
- [ ] Tooltip：hover 每個 CheckBox 都出現說明文字
- [ ] Save As：輸入新名稱 → 加入 profile 清單、無 BlurEffect
- [ ] Delete：對話框標題為 "Locale Emulator"（非空）

- [ ] **Step 3: 驗證 Shell Extension 分頁**

- [ ] Current User 區塊狀態顯示正確（初次 "Not Installed"）
- [ ] Install (Current User) → 無 UAC、狀態變 "Installed"、Explorer 右鍵 .exe 出現選單
- [ ] All Users 區塊按 Install → UAC 彈出、點「是」→ 狀態變 "Installed"
- [ ] Uninstall (Current User) → 狀態變 "Not Installed"
- [ ] 點 UAC 的「否」→ 無錯誤訊息、狀態不變

- [ ] **Step 4: 驗證 AppConfig 分頁**

- [ ] 拖放 exe 到 `LEGUI.exe` → 開 AppConfig
- [ ] **確認沒有 Shell Extension 分頁**（只有 Profile + About）
- [ ] Profile 分頁**沒有 Display GroupBox**（ShowDisplayOptions=False）
- [ ] Save 運作正常

- [ ] **Step 5: 驗證 About 分頁**

- [ ] 版本號顯示 "3.0.0"
- [ ] 點超連結開啟系統預設瀏覽器

- [ ] **Step 6: 全部通過後在原 worktree 執行完整測試**

```bash
dotnet test LocaleEmulator.sln
```

Expected: 全綠

- [ ] **Step 7: 提交驗證 log**

```bash
git commit --allow-empty -m "test: complete manual smoke validation for issue #19"
```

---

## Final Checklist（PR 前）

- [ ] 所有 `dotnet build` + `dotnet test` 通過（無新 warning）
- [ ] Release 建置 x86 + x64 + managed 皆成功
- [ ] Smoke test 全部手動驗證通過
- [ ] Issue #19 原始 checkbox 對應的功能全部完成
- [ ] PR body 包含：設計討論（spec 的 Section 10）、scope 擴張說明、測試結果
- [ ] PR 連結 `Closes #19`
- [ ] 加 issue comment 說明 scope 擴張（DRY + UI 版面重構）

---

## 附註

- **Worktree 清理**：PR merged 後 `git worktree remove ../Locale-Emulator-issue-19 && git branch -d feature/issue-19-shell-ext-ui`
- **i18n 翻譯**：本 plan 只處理 `DefaultLanguage.xaml`，其他 21 個語言由 Issue #21 接手
- **SizeToContent 跳動**：如實測分頁切換有視窗跳動，將 TabControl 各分頁包 `<Grid MinHeight="440">` 統一基線
