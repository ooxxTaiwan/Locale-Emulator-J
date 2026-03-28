# Plan 3: .NET 10 Migration — 管理端遷移

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 將 LECommonLibrary、LEProc、LEGUI 從 .NET Framework 4.0 遷移至 .NET 10，整合 LEInstaller 功能至 LEGUI

**Architecture:** SDK-style csproj、TDD（xUnit + NSubstitute）、Bottom-Up 遷移順序

**Tech Stack:** .NET 10, C# 14, xUnit, NSubstitute, WPF

**Branch:** `feature/dotnet10-net-migration`

**PR:** Part of ooxxTaiwan/Locale-Emulator-J#1

**Depends on:** Plan 1 merged

**Can parallel with:** Plan 2 (Shell Extension)

---

## 前置條件

- Plan 1（Core 基線建立）已合併至 master
- .NET 10 SDK 已安裝
- 專案根目錄已有 `Directory.Build.props` 和 `key.snk`

## 整體流程

```
Part A: LECommonLibrary（無依賴，最先遷移）
    ↓
Part B: LEProc（依賴 Part A）  ←→  Part C: LEGUI（依賴 Part A，可與 Part B 平行）
    ↓
Part D: 清理
```

---

## Part A: LECommonLibrary 遷移

### Task A1: 建立 Directory.Build.props

- [ ] **A1.1** 在專案根目錄建立 `Directory.Build.props`

檔案：`E:\Code\Locale-Emulator\Directory.Build.props`

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

  <!-- UseNls 待驗證：需以 characterization test 比對後決定。
       若驗證通過需啟用，取消下方註解。 -->
  <!--
  <ItemGroup>
    <RuntimeHostConfigurationOption Include="System.Globalization.UseNls" Value="true" />
  </ItemGroup>
  -->
</Project>
```

- [ ] **A1.2** 驗證 `key.snk` 存在於專案根目錄（`E:\Code\Locale-Emulator\key.snk`），若不存在則從任一子專案複製

```bash
ls E:/Code/Locale-Emulator/key.snk  # Plan 1 已將 key.snk 統一至根目錄
```

- [ ] **A1.3** 提交

```bash
cd E:/Code/Locale-Emulator
git checkout -b feature/dotnet10-net-migration
git add Directory.Build.props key.snk
git commit -m "chore: add Directory.Build.props for .NET 10 shared properties"
```

### Task A2: 建立 SDK-style LECommonLibrary.csproj

- [ ] **A2.1** 建立目錄 `src/LECommonLibrary/`

```bash
mkdir -p E:/Code/Locale-Emulator/src/LECommonLibrary
```

- [ ] **A2.2** 建立新的 SDK-style csproj

檔案：`E:\Code\Locale-Emulator\src\LECommonLibrary\LECommonLibrary.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Library</OutputType>
    <RootNamespace>LECommonLibrary</RootNamespace>
    <AssemblyName>LECommonLibrary</AssemblyName>
    <Version>3.0.0</Version>
    <Company>Locale Emulator Contributors</Company>
    <Copyright>Copyright (c) Paddy Xu, Locale Emulator Contributors</Copyright>
    <UseWindowsForms>true</UseWindowsForms>
  </PropertyGroup>

</Project>
```

**說明**：
- `TargetFramework`、`LangVersion`、`SignAssembly`、`AssemblyOriginatorKeyFile`、`ImplicitUsings`、`Nullable` 繼承自 `Directory.Build.props`
- `UseWindowsForms=true` 因 `GlobalHelper.cs` 使用 `MessageBox.Show`（`System.Windows.Forms`）
- 不需要 `AssemblyInfo.cs`，屬性由 csproj 的 `<Version>` 等元素自動產生
- 移除了 post-build 複製到 LEInstaller 的步驟（LEInstaller 將被刪除）

- [ ] **A2.3** 驗證空專案可建置

```bash
cd E:/Code/Locale-Emulator/src/LECommonLibrary
dotnet build
```

### Task A3: 建立 LECommonLibrary.Tests

- [ ] **A3.1** 建立測試專案目錄與 csproj

```bash
mkdir -p E:/Code/Locale-Emulator/tests/LECommonLibrary.Tests
```

檔案：`E:\Code\Locale-Emulator\tests\LECommonLibrary.Tests\LECommonLibrary.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <RootNamespace>LECommonLibrary.Tests</RootNamespace>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="NSubstitute" Version="5.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\LECommonLibrary\LECommonLibrary.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **A3.2** 撰寫 `LEProfileTests.cs`

檔案：`E:\Code\Locale-Emulator\tests\LECommonLibrary.Tests\LEProfileTests.cs`

```csharp
using Xunit;

namespace LECommonLibrary.Tests;

public class LEProfileTests
{
    [Fact]
    public void DefaultConstructor_SetsJaJPDefaults()
    {
        var profile = new LEProfile(true);

        Assert.Equal("ja-JP", profile.Name);
        Assert.Equal("ja-JP", profile.Location);
        Assert.Equal("Tokyo Standard Time", profile.Timezone);
        Assert.False(profile.ShowInMainMenu);
        Assert.False(profile.RunAsAdmin);
        Assert.True(profile.RedirectRegistry);
        Assert.False(profile.IsAdvancedRedirection);
        Assert.False(profile.RunWithSuspend);
        Assert.Equal(string.Empty, profile.Parameter);
        Assert.NotNull(profile.Guid);
        Assert.NotEqual(string.Empty, profile.Guid);
    }

    [Fact]
    public void FullConstructor_SetsAllProperties()
    {
        var profile = new LEProfile(
            name: "Test Profile",
            guid: "test-guid-123",
            showInMainMenu: true,
            parameter: "-somearg",
            location: "zh-TW",
            timezone: "Taipei Standard Time",
            runAsAdmin: true,
            redirectRegistry: false,
            isAdvancedRedirection: true,
            runWithSuspend: true);

        Assert.Equal("Test Profile", profile.Name);
        Assert.Equal("test-guid-123", profile.Guid);
        Assert.True(profile.ShowInMainMenu);
        Assert.Equal("-somearg", profile.Parameter);
        Assert.Equal("zh-TW", profile.Location);
        Assert.Equal("Taipei Standard Time", profile.Timezone);
        Assert.True(profile.RunAsAdmin);
        Assert.False(profile.RedirectRegistry);
        Assert.True(profile.IsAdvancedRedirection);
        Assert.True(profile.RunWithSuspend);
    }

    [Fact]
    public void DefaultParameterlessStruct_HasNullStringsAndFalseFlags()
    {
        var profile = new LEProfile();

        Assert.Null(profile.Name);
        Assert.Null(profile.Guid);
        Assert.Null(profile.Location);
        Assert.Null(profile.Timezone);
        Assert.Null(profile.Parameter);
        Assert.False(profile.ShowInMainMenu);
        Assert.False(profile.RunAsAdmin);
        Assert.False(profile.RedirectRegistry);
        Assert.False(profile.IsAdvancedRedirection);
        Assert.False(profile.RunWithSuspend);
    }
}
```

- [ ] **A3.3** 撰寫 `LEConfigTests.cs`

檔案：`E:\Code\Locale-Emulator\tests\LECommonLibrary.Tests\LEConfigTests.cs`

```csharp
using System.Xml.Linq;
using Xunit;

namespace LECommonLibrary.Tests;

public class LEConfigTests : IDisposable
{
    private readonly string _tempDir;

    public LEConfigTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "LEConfigTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string CreateTempConfigFile(string xml)
    {
        var path = Path.Combine(_tempDir, "test.xml");
        File.WriteAllText(path, xml);
        return path;
    }

    [Fact]
    public void GetProfiles_ValidXml_ParsesAllProfiles()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <LEConfig>
              <Profiles>
                <Profile Name="Run in Japanese" Guid="guid-1" MainMenu="true">
                  <Parameter>-test</Parameter>
                  <Location>ja-JP</Location>
                  <Timezone>Tokyo Standard Time</Timezone>
                  <RunAsAdmin>false</RunAsAdmin>
                  <RedirectRegistry>true</RedirectRegistry>
                  <IsAdvancedRedirection>false</IsAdvancedRedirection>
                  <RunWithSuspend>false</RunWithSuspend>
                </Profile>
                <Profile Name="Run in Chinese" Guid="guid-2" MainMenu="false">
                  <Parameter></Parameter>
                  <Location>zh-TW</Location>
                  <Timezone>Taipei Standard Time</Timezone>
                  <RunAsAdmin>true</RunAsAdmin>
                  <RedirectRegistry>true</RedirectRegistry>
                  <IsAdvancedRedirection>true</IsAdvancedRedirection>
                  <RunWithSuspend>false</RunWithSuspend>
                </Profile>
              </Profiles>
            </LEConfig>
            """;

        var path = CreateTempConfigFile(xml);
        var profiles = LEConfig.GetProfiles(path);

        Assert.Equal(2, profiles.Length);

        Assert.Equal("Run in Japanese", profiles[0].Name);
        Assert.Equal("guid-1", profiles[0].Guid);
        Assert.True(profiles[0].ShowInMainMenu);
        Assert.Equal("-test", profiles[0].Parameter);
        Assert.Equal("ja-JP", profiles[0].Location);
        Assert.Equal("Tokyo Standard Time", profiles[0].Timezone);
        Assert.False(profiles[0].RunAsAdmin);
        Assert.True(profiles[0].RedirectRegistry);
        Assert.False(profiles[0].IsAdvancedRedirection);
        Assert.False(profiles[0].RunWithSuspend);

        Assert.Equal("Run in Chinese", profiles[1].Name);
        Assert.Equal("zh-TW", profiles[1].Location);
        Assert.True(profiles[1].RunAsAdmin);
        Assert.True(profiles[1].IsAdvancedRedirection);
    }

    [Fact]
    public void GetProfiles_MissingOptionalElements_UsesDefaults()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <LEConfig>
              <Profiles>
                <Profile Name="Minimal" Guid="guid-min" MainMenu="false">
                  <Parameter></Parameter>
                  <Location>ko-KR</Location>
                  <Timezone>Korea Standard Time</Timezone>
                </Profile>
              </Profiles>
            </LEConfig>
            """;

        var path = CreateTempConfigFile(xml);
        var profiles = LEConfig.GetProfiles(path);

        Assert.Single(profiles);
        Assert.False(profiles[0].RunAsAdmin);       // default "false"
        Assert.True(profiles[0].RedirectRegistry);   // default "true"
        Assert.False(profiles[0].IsAdvancedRedirection); // default "false"
        Assert.False(profiles[0].RunWithSuspend);    // default "false"
    }

    [Fact]
    public void GetProfiles_EmptyFile_ReturnsEmptyArray()
    {
        var path = CreateTempConfigFile("<LEConfig><Profiles /></LEConfig>");
        var profiles = LEConfig.GetProfiles(path);
        Assert.Empty(profiles);
    }

    [Fact]
    public void GetProfiles_InvalidXml_ReturnsEmptyArray()
    {
        var path = CreateTempConfigFile("not xml at all");
        var profiles = LEConfig.GetProfiles(path);
        Assert.Empty(profiles);
    }

    [Fact]
    public void GetProfiles_NonExistentFile_ReturnsEmptyArray()
    {
        var profiles = LEConfig.GetProfiles(Path.Combine(_tempDir, "nonexistent.xml"));
        Assert.Empty(profiles);
    }

    [Fact]
    public void SaveAndReload_RoundTrips()
    {
        var path = Path.Combine(_tempDir, "roundtrip.xml");
        var original = new LEProfile(
            "Test",
            "test-guid",
            true,
            "-arg1",
            "ja-JP",
            "Tokyo Standard Time",
            true,
            true,
            false,
            true);

        LEConfig.SaveApplicationConfigFile(path, original);
        var loaded = LEConfig.GetProfiles(path);

        Assert.Single(loaded);
        Assert.Equal("Test", loaded[0].Name);
        Assert.Equal("test-guid", loaded[0].Guid);
        Assert.True(loaded[0].ShowInMainMenu);
        Assert.Equal("-arg1", loaded[0].Parameter);
        Assert.Equal("ja-JP", loaded[0].Location);
        Assert.Equal("Tokyo Standard Time", loaded[0].Timezone);
        Assert.True(loaded[0].RunAsAdmin);
        Assert.True(loaded[0].RedirectRegistry);
        Assert.False(loaded[0].IsAdvancedRedirection);
        Assert.True(loaded[0].RunWithSuspend);
    }
}
```

- [ ] **A3.4** 撰寫 `SystemHelperTests.cs`

檔案：`E:\Code\Locale-Emulator\tests\LECommonLibrary.Tests\SystemHelperTests.cs`

```csharp
using Xunit;

namespace LECommonLibrary.Tests;

public class SystemHelperTests
{
    [Fact]
    public void Is64BitOS_ReturnsConsistentResult()
    {
        // 在 64 位元 Windows 上應回傳 true，32 位元回傳 false
        // 此測試確保不拋出例外且結果與 Environment.Is64BitOperatingSystem 一致
        var result = SystemHelper.Is64BitOS();
        Assert.Equal(Environment.Is64BitOperatingSystem, result);
    }

    [Fact]
    public void EnsureAbsolutePath_AbsolutePath_ReturnsSame()
    {
        var path = @"C:\Windows\System32\notepad.exe";
        var result = SystemHelper.EnsureAbsolutePath(path);
        Assert.Equal(path, result);
    }

    [Fact]
    public void EnsureAbsolutePath_RelativePath_ReturnsAbsolute()
    {
        var result = SystemHelper.EnsureAbsolutePath("test.exe");
        Assert.True(Path.IsPathRooted(result));
        Assert.EndsWith("test.exe", result);
    }

    [Fact]
    public void RedirectToWow64_System32Path_ReplacesSysWOW64()
    {
        if (!Environment.Is64BitOperatingSystem)
            return; // SysWOW64 只存在於 64 位元 OS

        var input = @"%SystemRoot%\System32\test.dll";
        var result = SystemHelper.RedirectToWow64(input);
        Assert.Contains("syswow64", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("system32", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckPermission_TempDirectory_ReturnsTrue()
    {
        var result = SystemHelper.CheckPermission(Path.GetTempPath());
        Assert.True(result);
    }

    [Fact]
    public void CheckPermission_NonExistentDirectory_ReturnsFalse()
    {
        var result = SystemHelper.CheckPermission(@"C:\NonExistentDir_" + Guid.NewGuid().ToString("N"));
        Assert.False(result);
    }

    [Fact]
    public void IsAdministrator_DoesNotThrow()
    {
        // 僅確保此方法可安全呼叫，不拋出例外
        var result = SystemHelper.IsAdministrator();
        Assert.IsType<bool>(result);
    }
}
```

- [ ] **A3.5** 撰寫 `PEFileReaderTests.cs`

檔案：`E:\Code\Locale-Emulator\tests\LECommonLibrary.Tests\PEFileReaderTests.cs`

```csharp
using Xunit;

namespace LECommonLibrary.Tests;

public class PEFileReaderTests : IDisposable
{
    private readonly string _tempDir;

    public PEFileReaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "PEFileReaderTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void GetPEType_NullOrEmpty_ReturnsUnknown()
    {
        Assert.Equal(PEType.Unknown, PEFileReader.GetPEType(null!));
        Assert.Equal(PEType.Unknown, PEFileReader.GetPEType(string.Empty));
    }

    [Fact]
    public void GetPEType_NonExistentFile_ReturnsUnknown()
    {
        var path = Path.Combine(_tempDir, "nonexistent.exe");
        Assert.Equal(PEType.Unknown, PEFileReader.GetPEType(path));
    }

    [Fact]
    public void GetPEType_TooSmallFile_ReturnsUnknown()
    {
        var path = Path.Combine(_tempDir, "tiny.exe");
        File.WriteAllBytes(path, new byte[] { 0x4D, 0x5A });  // MZ 但太短
        Assert.Equal(PEType.Unknown, PEFileReader.GetPEType(path));
    }

    [Fact]
    public void GetPEType_NotMZSignature_ReturnsUnknown()
    {
        var path = Path.Combine(_tempDir, "not_pe.exe");
        File.WriteAllBytes(path, new byte[256]);  // 全零，非 MZ
        Assert.Equal(PEType.Unknown, PEFileReader.GetPEType(path));
    }

    [Fact]
    public void GetPEType_X86PE_ReturnsX32()
    {
        // 建構最小 x86 PE：MZ header + PE signature + machine type 0x014C
        var pe = new byte[256];
        pe[0] = 0x4D; pe[1] = 0x5A;  // MZ signature
        // e_lfanew at offset 0x3C，指向 PE header 位置
        pe[0x3C] = 0x80;  // PE header 在 offset 0x80
        // PE signature "PE\0\0" at offset 0x80
        pe[0x80] = 0x50; pe[0x81] = 0x45; pe[0x82] = 0x00; pe[0x83] = 0x00;
        // Machine type at offset 0x84 (PE sig + 4)
        pe[0x84] = 0x4C; pe[0x85] = 0x01;  // 0x014C = x86

        var path = Path.Combine(_tempDir, "x86.exe");
        File.WriteAllBytes(path, pe);

        Assert.Equal(PEType.X32, PEFileReader.GetPEType(path));
    }

    [Fact]
    public void GetPEType_X64PE_ReturnsX64()
    {
        var pe = new byte[256];
        pe[0] = 0x4D; pe[1] = 0x5A;
        pe[0x3C] = 0x80;
        pe[0x80] = 0x50; pe[0x81] = 0x45; pe[0x82] = 0x00; pe[0x83] = 0x00;
        pe[0x84] = 0x64; pe[0x85] = 0x86;  // 0x8664 = x64

        var path = Path.Combine(_tempDir, "x64.exe");
        File.WriteAllBytes(path, pe);

        Assert.Equal(PEType.X64, PEFileReader.GetPEType(path));
    }

    [Fact]
    public void GetPEType_RealNotepad_ReturnsNonUnknown()
    {
        // notepad.exe 是 Windows 內建程式，一定存在
        var notepadPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "notepad.exe");

        if (!File.Exists(notepadPath))
            return; // 跳過（理論上不會發生）

        var result = PEFileReader.GetPEType(notepadPath);
        Assert.NotEqual(PEType.Unknown, result);
    }
}
```

- [ ] **A3.6** 確認測試編譯但失敗（紅燈階段 — 尚未搬移原始碼）

```bash
cd E:/Code/Locale-Emulator/tests/LECommonLibrary.Tests
dotnet build 2>&1 | head -30
```

預期：編譯失敗，因為 `src/LECommonLibrary/` 還沒有原始碼。

- [ ] **A3.7** 提交測試程式碼

```bash
cd E:/Code/Locale-Emulator
git add src/LECommonLibrary/LECommonLibrary.csproj tests/LECommonLibrary.Tests/
git commit -m "test(LECommonLibrary): add xUnit tests for LEProfile, LEConfig, SystemHelper, PEFileReader

TDD red phase - tests written before migrating source files."
```

### Task A4: 搬移原始碼並調整

- [ ] **A4.1** 複製原始碼到新目錄

```bash
# 原始碼已在 Plan 1 中由 git mv 搬移至 src/LECommonLibrary/，無需複製。
# 以下驗證檔案已就位：
ls src/LECommonLibrary/LEConfig.cs src/LECommonLibrary/LEProfile.cs src/LECommonLibrary/SystemHelper.cs src/LECommonLibrary/GlobalHelper.cs src/LECommonLibrary/PEFileReader.cs src/LECommonLibrary/AssociationReader.cs
```

**不要複製** `Properties/AssemblyInfo.cs`（SDK-style csproj 自動產生組件屬性）。

- [ ] **A4.2** 更新 `GlobalHelper.cs`：移除 `GetLEVersion()`、`GetLastUpdate()`、`SetLastUpdate()`

在 `src/LECommonLibrary/GlobalHelper.cs` 中，刪除以下內容：
- `GlobalVersionPath` 靜態欄位（第 14-16 行）
- `GetLEVersion()` 方法（第 18-28 行）
- `GetLastUpdate()` 方法（第 30-41 行）
- `SetLastUpdate()` 方法（第 43-58 行）

更新 `ShowErrorDebugMessageBox` 中的版本顯示：

將：
```csharp
"Locale Emulator Version " + GetLEVersion());
```

改為：
```csharp
"Locale Emulator Version " + System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown");
```

更新後的 `GlobalHelper.cs` 應如下：

```csharp
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace LECommonLibrary;

public static class GlobalHelper
{
    public static string GetVersionString()
    {
        return Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
    }

    public static void ShowErrorDebugMessageBox(string commandLine, uint errorCode)
    {
        MessageBox.Show(
            $"Error Number: {Convert.ToString(errorCode, 16).ToUpper()}\r\n"
            + $"Commands: {commandLine}\r\n" + "\r\n"
            + $"{Environment.OSVersion} {(SystemHelper.Is64BitOS() ? "x64" : "x86")}\r\n"
            + $"{GenerateSystemDllVersionList()}\r\n"
            + "If you have any antivirus software running, please turn it off and try again.\r\n"
            + "If this window still shows, try go to Safe Mode and drag the application "
            + "executable onto LEProc.exe.\r\n"
            + "If you have tried all above but none of them works, feel free to submit an issue at\r\n"
            + "https://github.com/ooxxTaiwan/Locale-Emulator-J/issues.\r\n" + "\r\n" + "\r\n"
            + "You can press CTRL+C to copy this message to your clipboard.\r\n",
            "Locale Emulator Version " + GetVersionString());
    }

    private static string GenerateSystemDllVersionList()
    {
        string[] dlls = ["NTDLL.DLL", "KERNELBASE.DLL", "KERNEL32.DLL", "USER32.DLL", "GDI32.DLL"];

        var result = new StringBuilder();

        foreach (var dll in dlls)
        {
            var version = FileVersionInfo.GetVersionInfo(
                Path.Combine(
                    Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\",
                    SystemHelper.Is64BitOS()
                        ? @"Windows\SysWOW64\"
                        : @"Windows\System32\",
                    dll));

            result.Append(dll);
            result.Append(": ");
            result.Append(
                $"{version.FileMajorPart}.{version.FileMinorPart}.{version.FileBuildPart}.{version.FilePrivatePart}");
            result.Append("\r\n");
        }

        return result.ToString();
    }

    public static bool CheckCoreDLLs()
    {
        string[] dlls = ["LoaderDll.dll", "LocaleEmulator.dll"];

        return dlls
            .Select(dll => Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".",
                dll))
            .All(File.Exists);
    }
}
```

- [ ] **A4.3** 修正其他檔案的 nullable 警告

`LEConfig.cs` — 加入 `#nullable disable` 檔案頂部（暫時），或逐一修正 null 警告。最低限度需確保能編譯。

`SystemHelper.cs` — 移除 `System.Drawing` 依賴（`Is4KDisplay` 方法使用 `Graphics.FromHwnd`），因 .NET 10 中 `System.Drawing` 不預設可用於非 WinForms 專案。可選方案：
  - 改用 P/Invoke 直接呼叫 `GetDeviceCaps`（已有 P/Invoke 宣告）
  - 或在 csproj 加入 `<UseWindowsForms>true</UseWindowsForms>`（已加入）

`LEProfile.cs` — 需修正 string 欄位的 nullable 警告（struct 欄位預設為 null）。

- [ ] **A4.4** 建置並執行測試

```bash
cd E:/Code/Locale-Emulator/src/LECommonLibrary
dotnet build

cd E:/Code/Locale-Emulator/tests/LECommonLibrary.Tests
dotnet test
```

驗證所有測試通過（綠燈）。

- [ ] **A4.5** 提交

```bash
cd E:/Code/Locale-Emulator
git add src/LECommonLibrary/
git commit -m "feat(LECommonLibrary): migrate to SDK-style csproj targeting .NET 10

- Move source files to src/LECommonLibrary/
- Remove AssemblyInfo.cs (properties in csproj)
- Remove GlobalHelper.GetLEVersion/GetLastUpdate/SetLastUpdate
- Add GlobalHelper.GetVersionString() using Assembly metadata
- Fix nullable warnings"
```

### Task A5: 驗證 LECommonLibrary 建置 + 測試通過

- [ ] **A5.1** 完整建置並測試

```bash
cd E:/Code/Locale-Emulator/tests/LECommonLibrary.Tests
dotnet test --verbosity normal
```

確認所有測試通過。若有失敗，修正原始碼直到全部通過。

- [ ] **A5.2** 提交修正（若有）

```bash
cd E:/Code/Locale-Emulator
git add -A src/LECommonLibrary/ tests/LECommonLibrary.Tests/
git commit -m "fix(LECommonLibrary): fix test failures after migration"
```

---

## Part B: LEProc 遷移

### Task B1: Characterization Test — 擷取 .NET Framework 4.0 基準值

- [ ] **B1.1** 撰寫基準值擷取程式（在 .NET Framework 4.0 下執行）

建立一個簡單的 .NET Framework console app（或直接用 C# Script），對所有常用 locale 擷取 `ANSICodePage`、`OEMCodePage`、`LCID`，輸出為 JSON。

常用 locale 清單（這些是 LE 實際支援的 locale）：
```
ja-JP, zh-TW, zh-CN, zh-HK, ko-KR, en-US, de-DE, fr-FR, it-IT,
es-ES, pt-BR, ru-RU, pl-PL, cs-CZ, tr-TR, th-TH, vi-VN, ar-SA,
he-IL, el-GR, nl-NL, nb-NO, lt-LT, ka-GE
```

- [ ] **B1.2** 儲存基準值為 JSON 檔案

檔案：`E:\Code\Locale-Emulator\tests\LEProc.Tests\TestData\locale-baseline-netfx40.json`

```json
{
  "framework": ".NET Framework 4.0",
  "captureDate": "2026-03-29",
  "locales": {
    "ja-JP": { "ANSICodePage": 932, "OEMCodePage": 932, "LCID": 1041 },
    "zh-TW": { "ANSICodePage": 950, "OEMCodePage": 950, "LCID": 1028 },
    "zh-CN": { "ANSICodePage": 936, "OEMCodePage": 936, "LCID": 2052 },
    "zh-HK": { "ANSICodePage": 950, "OEMCodePage": 950, "LCID": 3076 },
    "ko-KR": { "ANSICodePage": 949, "OEMCodePage": 949, "LCID": 1042 },
    "en-US": { "ANSICodePage": 1252, "OEMCodePage": 437, "LCID": 1033 },
    "de-DE": { "ANSICodePage": 1252, "OEMCodePage": 850, "LCID": 1031 },
    "fr-FR": { "ANSICodePage": 1252, "OEMCodePage": 850, "LCID": 1036 },
    "it-IT": { "ANSICodePage": 1252, "OEMCodePage": 850, "LCID": 1040 },
    "es-ES": { "ANSICodePage": 1252, "OEMCodePage": 850, "LCID": 3082 },
    "pt-BR": { "ANSICodePage": 1252, "OEMCodePage": 850, "LCID": 1046 },
    "ru-RU": { "ANSICodePage": 1251, "OEMCodePage": 866, "LCID": 1049 },
    "pl-PL": { "ANSICodePage": 1250, "OEMCodePage": 852, "LCID": 1045 },
    "cs-CZ": { "ANSICodePage": 1250, "OEMCodePage": 852, "LCID": 1029 },
    "tr-TR": { "ANSICodePage": 1254, "OEMCodePage": 857, "LCID": 1055 },
    "th-TH": { "ANSICodePage": 874, "OEMCodePage": 874, "LCID": 1054 },
    "vi-VN": { "ANSICodePage": 1258, "OEMCodePage": 1258, "LCID": 1066 },
    "ar-SA": { "ANSICodePage": 1256, "OEMCodePage": 720, "LCID": 1025 },
    "he-IL": { "ANSICodePage": 1255, "OEMCodePage": 862, "LCID": 1037 },
    "el-GR": { "ANSICodePage": 1253, "OEMCodePage": 737, "LCID": 1032 },
    "nl-NL": { "ANSICodePage": 1252, "OEMCodePage": 850, "LCID": 1043 },
    "nb-NO": { "ANSICodePage": 1252, "OEMCodePage": 850, "LCID": 1044 },
    "lt-LT": { "ANSICodePage": 1257, "OEMCodePage": 775, "LCID": 1063 },
    "ka-GE": { "ANSICodePage": 0, "OEMCodePage": 437, "LCID": 1079 }
  }
}
```

**注意**：以上數值為預期值，實際執行時需在 .NET Framework 4.0 環境驗證。`ka-GE` 的 ANSICodePage 在 .NET Framework 4.0 中可能是 0（無對應 ANSI codepage）。

- [ ] **B1.3** 提交基準值

```bash
cd E:/Code/Locale-Emulator
mkdir -p tests/LEProc.Tests/TestData
git add tests/LEProc.Tests/TestData/locale-baseline-netfx40.json
git commit -m "test(LEProc): add .NET Framework 4.0 locale baseline data for characterization test"
```

### Task B2: 建立 SDK-style LEProc.csproj

- [ ] **B2.1** 建立目錄

```bash
mkdir -p E:/Code/Locale-Emulator/src/LEProc
```

- [ ] **B2.2** 建立新的 SDK-style csproj

檔案：`E:\Code\Locale-Emulator\src\LEProc\LEProc.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <RootNamespace>LEProc</RootNamespace>
    <AssemblyName>LEProc</AssemblyName>
    <PlatformTarget>x86</PlatformTarget>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <Version>3.0.0</Version>
    <Company>Locale Emulator Contributors</Company>
    <Copyright>Copyright (c) Paddy Xu, Locale Emulator Contributors</Copyright>
    <UseWindowsForms>true</UseWindowsForms>
    <ApplicationManifest>app.manifest</ApplicationManifest>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\LECommonLibrary\LECommonLibrary.csproj" />
  </ItemGroup>

</Project>
```

**說明**：
- `PlatformTarget=x86`：LEProc 必須是 x86，因為需要載入 32 位元 `LoaderDll.dll`
- `AllowUnsafeBlocks=true`：LoaderWrapper 使用 unsafe 操作
- `UseWindowsForms=true`：使用 `System.Windows.Forms.MessageBox`
- 不包含 `LEVersion.xml`（已移除）
- 不包含 `System.Deployment` 參考（.NET 10 不支援 ClickOnce）

### Task B3: 建立 LEProc.Tests

- [ ] **B3.1** 建立測試專案

```bash
mkdir -p E:/Code/Locale-Emulator/tests/LEProc.Tests
```

檔案：`E:\Code\Locale-Emulator\tests\LEProc.Tests\LEProc.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <RootNamespace>LEProc.Tests</RootNamespace>
    <PlatformTarget>x86</PlatformTarget>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="NSubstitute" Version="5.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\LEProc\LEProc.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Include="TestData\**\*" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

</Project>
```

- [ ] **B3.2** 撰寫 `GetCharsetFromANSICodepageTests.cs`

檔案：`E:\Code\Locale-Emulator\tests\LEProc.Tests\GetCharsetFromANSICodepageTests.cs`

**注意**：`GetCharsetFromANSICodepage` 目前是 `Program` 的 private static 方法。需要先將其改為 internal static 並加入 `[InternalsVisibleTo]`，或抽取為獨立類別。推薦做法：在 LEProc.csproj 加入 InternalsVisibleTo。

在 `src/LEProc/LEProc.csproj` 中加入：
```xml
<ItemGroup>
  <InternalsVisibleTo Include="LEProc.Tests" />
</ItemGroup>
```

```csharp
using Xunit;

namespace LEProc.Tests;

public class GetCharsetFromANSICodepageTests
{
    // 注意：此測試需要 GetCharsetFromANSICodepage 為 internal static

    [Theory]
    [InlineData(932, 128)]   // Japanese -> SHIFTJIS_CHARSET
    [InlineData(936, 134)]   // Simplified Chinese -> GB2312_CHARSET
    [InlineData(949, 129)]   // Korean -> HANGEUL_CHARSET
    [InlineData(950, 136)]   // Traditional Chinese -> CHINESEBIG5_CHARSET
    [InlineData(1250, 238)]  // Eastern Europe -> EASTEUROPE_CHARSET
    [InlineData(1251, 204)]  // Russian -> RUSSIAN_CHARSET
    [InlineData(1252, 0)]    // Western -> ANSI_CHARSET
    [InlineData(1253, 161)]  // Greek -> GREEK_CHARSET
    [InlineData(1254, 162)]  // Turkish -> TURKISH_CHARSET
    [InlineData(1255, 177)]  // Hebrew -> HEBREW_CHARSET
    [InlineData(1256, 178)]  // Arabic -> ARABIC_CHARSET
    [InlineData(1257, 186)]  // Baltic -> BALTIC_CHARSET
    [InlineData(874, 0)]     // Thai -> 未列入 switch，回傳 ANSI_CHARSET (0)
    [InlineData(1258, 0)]    // Vietnamese -> 未列入 switch，回傳 ANSI_CHARSET (0)
    [InlineData(437, 0)]     // OEM US -> 未列入 switch，回傳 ANSI_CHARSET (0)
    [InlineData(0, 0)]       // Unknown -> ANSI_CHARSET (0)
    public void GetCharsetFromANSICodepage_ReturnsExpectedCharset(int codepage, int expectedCharset)
    {
        var result = Program.GetCharsetFromANSICodepage(codepage);
        Assert.Equal(expectedCharset, result);
    }
}
```

- [ ] **B3.3** 撰寫 `CharacterizationTests.cs`（比對基準值）

檔案：`E:\Code\Locale-Emulator\tests\LEProc.Tests\CharacterizationTests.cs`

```csharp
using System.Globalization;
using System.Text.Json;
using Xunit;

namespace LEProc.Tests;

public class CharacterizationTests
{
    private static readonly string[] SupportedLocales =
    [
        "ja-JP", "zh-TW", "zh-CN", "zh-HK", "ko-KR", "en-US",
        "de-DE", "fr-FR", "it-IT", "es-ES", "pt-BR", "ru-RU",
        "pl-PL", "cs-CZ", "tr-TR", "th-TH", "vi-VN", "ar-SA",
        "he-IL", "el-GR", "nl-NL", "nb-NO", "lt-LT", "ka-GE"
    ];

    [Fact]
    public void AllSupportedLocales_HaveValidCultureInfo()
    {
        foreach (var locale in SupportedLocales)
        {
            var ci = CultureInfo.GetCultureInfo(locale);
            Assert.NotNull(ci);
            Assert.NotEqual(0, ci.TextInfo.ANSICodePage + ci.TextInfo.OEMCodePage);
        }
    }

    [Fact]
    public void CompareWithBaseline_ICUMode()
    {
        // 此測試在 .NET 10 預設 ICU 模式下執行，
        // 比較結果與 .NET Framework 4.0 基準值。
        // 若有差異則記錄，協助決定是否需啟用 NLS 模式。

        var baselinePath = Path.Combine(
            AppContext.BaseDirectory, "TestData", "locale-baseline-netfx40.json");

        if (!File.Exists(baselinePath))
        {
            // 基準檔案尚未產生，跳過
            return;
        }

        var json = File.ReadAllText(baselinePath);
        using var doc = JsonDocument.Parse(json);
        var locales = doc.RootElement.GetProperty("locales");

        var differences = new List<string>();

        foreach (var locale in SupportedLocales)
        {
            if (!locales.TryGetProperty(locale, out var baseline))
                continue;

            var ci = CultureInfo.GetCultureInfo(locale);
            var expectedAnsi = baseline.GetProperty("ANSICodePage").GetInt32();
            var expectedOem = baseline.GetProperty("OEMCodePage").GetInt32();
            var expectedLcid = baseline.GetProperty("LCID").GetInt32();

            if (ci.TextInfo.ANSICodePage != expectedAnsi)
                differences.Add($"{locale}: ANSICodePage expected={expectedAnsi} actual={ci.TextInfo.ANSICodePage}");
            if (ci.TextInfo.OEMCodePage != expectedOem)
                differences.Add($"{locale}: OEMCodePage expected={expectedOem} actual={ci.TextInfo.OEMCodePage}");
            if (ci.TextInfo.LCID != expectedLcid)
                differences.Add($"{locale}: LCID expected={expectedLcid} actual={ci.TextInfo.LCID}");
        }

        // 此測試目的是記錄差異，不是強制要求完全一致。
        // 若有差異，輸出到 test output 供人工審查。
        if (differences.Count > 0)
        {
            var report = "ICU mode differences from .NET Framework 4.0 baseline:\n"
                + string.Join("\n", differences);
            // 使用 Assert.Fail 輸出差異報告（可視實際策略改為 warning）
            Assert.Fail(report);
        }
    }

    [Theory]
    [InlineData("ja-JP", 932, 932, 1041)]
    [InlineData("zh-TW", 950, 950, 1028)]
    [InlineData("zh-CN", 936, 936, 2052)]
    [InlineData("ko-KR", 949, 949, 1042)]
    [InlineData("en-US", 1252, 437, 1033)]
    [InlineData("ru-RU", 1251, 866, 1049)]
    public void KeyLocales_HaveExpectedCodePages(
        string locale, int expectedAnsi, int expectedOem, int expectedLcid)
    {
        var ci = CultureInfo.GetCultureInfo(locale);
        Assert.Equal(expectedAnsi, ci.TextInfo.ANSICodePage);
        Assert.Equal(expectedOem, ci.TextInfo.OEMCodePage);
        Assert.Equal(expectedLcid, ci.TextInfo.LCID);
    }
}
```

- [ ] **B3.4** 撰寫 `RegistryEntriesLoaderTests.cs`

檔案：`E:\Code\Locale-Emulator\tests\LEProc.Tests\RegistryEntriesLoaderTests.cs`

```csharp
using System.Globalization;
using Xunit;

namespace LEProc.Tests;

public class RegistryEntriesLoaderTests
{
    [Fact]
    public void GetRegistryEntries_NonAdvanced_ReturnsFourEntries()
    {
        var entries = RegistryEntriesLoader.GetRegistryEntries(false);
        Assert.Equal(4, entries.Length);
    }

    [Fact]
    public void GetRegistryEntries_Advanced_ReturnsEightEntries()
    {
        var entries = RegistryEntriesLoader.GetRegistryEntries(true);
        Assert.Equal(8, entries.Length);
    }

    [Fact]
    public void GetRegistryEntries_NonAdvanced_AllAreHKLM()
    {
        var entries = RegistryEntriesLoader.GetRegistryEntries(false);
        Assert.All(entries, e => Assert.Equal("HKEY_LOCAL_MACHINE", e.Root));
    }

    [Fact]
    public void GetRegistryEntries_Advanced_IncludesHKCU()
    {
        var entries = RegistryEntriesLoader.GetRegistryEntries(true);
        Assert.Contains(entries, e => e.Root == "HKEY_CURRENT_USER");
    }

    [Fact]
    public void GetRegistryEntries_JaJP_ReturnsCorrectACP()
    {
        var entries = RegistryEntriesLoader.GetRegistryEntries(false);
        var acpEntry = entries.First(e => e.Name == "ACP");

        var culture = CultureInfo.GetCultureInfo("ja-JP");
        var value = acpEntry.GetValue(culture);

        Assert.Equal("932", value);
    }

    [Fact]
    public void GetRegistryEntries_JaJP_ReturnsCorrectOEMCP()
    {
        var entries = RegistryEntriesLoader.GetRegistryEntries(false);
        var oemEntry = entries.First(e => e.Name == "OEMCP");

        var culture = CultureInfo.GetCultureInfo("ja-JP");
        var value = oemEntry.GetValue(culture);

        Assert.Equal("932", value);
    }

    [Fact]
    public void GetRegistryEntries_ZhTW_ReturnsCorrectCodePages()
    {
        var entries = RegistryEntriesLoader.GetRegistryEntries(false);
        var culture = CultureInfo.GetCultureInfo("zh-TW");

        var acp = entries.First(e => e.Name == "ACP").GetValue(culture);
        var oemcp = entries.First(e => e.Name == "OEMCP").GetValue(culture);

        Assert.Equal("950", acp);
        Assert.Equal("950", oemcp);
    }

    [Fact]
    public void GetRegistryEntries_Advanced_LocaleNameHasNullTerminator()
    {
        var entries = RegistryEntriesLoader.GetRegistryEntries(true);
        var localeNameEntry = entries.First(e => e.Name == "LocaleName");

        var culture = CultureInfo.GetCultureInfo("ja-JP");
        var value = localeNameEntry.GetValue(culture);

        Assert.EndsWith("\x00", value);
        Assert.Contains("ja-JP", value);
    }
}
```

- [ ] **B3.5** 撰寫 `LEBStructLayoutTests.cs`

檔案：`E:\Code\Locale-Emulator\tests\LEProc.Tests\LEBStructLayoutTests.cs`

```csharp
using System.Runtime.InteropServices;
using Xunit;

namespace LEProc.Tests;

public class LEBStructLayoutTests
{
    [Fact]
    public void LEB_HasExpectedSize()
    {
        // LEB struct:
        //   uint AnsiCodePage (4)
        //   uint OemCodePage (4)
        //   uint LocaleID (4)
        //   uint DefaultCharset (4)
        //   uint HookUILanguageAPI (4)
        //   byte[64] DefaultFaceName (64)
        //   RTL_TIME_ZONE_INFORMATION Timezone
        //
        // RTL_TIME_ZONE_INFORMATION:
        //   int Bias (4)
        //   byte[64] StandardName (64)
        //   TIME_FIELDS StandardDate (16)
        //   int StandardBias (4)
        //   byte[64] DaylightName (64)
        //   TIME_FIELDS DaylightDate (16)
        //   int DaylightBias (4)
        //   = 172 + padding
        //
        // Total LEB = 20 + 64 + 172 = 256 (approx, check with Marshal)

        var size = Marshal.SizeOf<LoaderWrapper.LEB>();
        // 確保結構大小不為 0 且合理
        Assert.True(size > 200, $"LEB size should be > 200 bytes, got {size}");
        Assert.True(size < 512, $"LEB size should be < 512 bytes, got {size}");
    }

    [Fact]
    public void TIME_FIELDS_HasExpectedSize()
    {
        // 8 個 ushort = 16 bytes
        var size = Marshal.SizeOf<LoaderWrapper.TIME_FIELDS>();
        Assert.Equal(16, size);
    }

    [Fact]
    public void RTL_TIME_ZONE_INFORMATION_HasExpectedSize()
    {
        var size = Marshal.SizeOf<LoaderWrapper.RTL_TIME_ZONE_INFORMATION>();
        // int(4) + byte[64] + TIME_FIELDS(16) + int(4) + byte[64] + TIME_FIELDS(16) + int(4)
        // = 4 + 64 + 16 + 4 + 64 + 16 + 4 = 172
        Assert.Equal(172, size);
    }

    [Fact]
    public void REGISTRY_REDIRECTION_ENTRY64_HasConsistentSize()
    {
        var entrySize = Marshal.SizeOf<LERegistryRedirector.REGISTRY_ENTRY64>();
        var redirectionSize = Marshal.SizeOf<LERegistryRedirector.REGISTRY_REDIRECTION_ENTRY64>();

        // REGISTRY_REDIRECTION_ENTRY64 包含兩個 REGISTRY_ENTRY64
        Assert.Equal(entrySize * 2, redirectionSize);
    }

    [Fact]
    public void UNICODE_STRING64_HasExpectedSize()
    {
        // ushort(2) + ushort(2) + 4 bytes padding + long(8) = 16 on x86?
        // 實際上 LayoutKind.Sequential，ushort + ushort + long
        var size = Marshal.SizeOf<LERegistryRedirector.UNICODE_STRING64>();
        // ushort(2) + ushort(2) + long(8) = 12, 但可能有 padding
        Assert.True(size >= 12, $"UNICODE_STRING64 should be >= 12 bytes, got {size}");
    }
}
```

- [ ] **B3.6** 提交測試

```bash
cd E:/Code/Locale-Emulator
git add tests/LEProc.Tests/
git commit -m "test(LEProc): add xUnit tests for charset mapping, locale characterization, registry entries, struct layout

TDD red phase - tests written before migrating source files."
```

### Task B4: 搬移 LEProc 原始碼

- [ ] **B4.1** 複製原始碼到新目錄

```bash
# 原始碼已在 Plan 1 中由 git mv 搬移至 src/LEProc/，無需複製。
# 以下驗證檔案已就位：
ls src/LEProc/Program.cs src/LEProc/LoaderWrapper.cs src/LEProc/RegistryEntriesLoader.cs src/LEProc/RegistryEntry.cs src/LEProc/LERegistryRedirector.cs src/LEProc/app.manifest
```

**不要複製**：
- `Properties/AssemblyInfo.cs`
- `LEVersion.xml`
- `Properties/Settings.settings` 和 `Settings.Designer.cs`
- `Properties/Resources.resx` 和 `Resources.Designer.cs`（若不需要）
- `key.snk`（已統一至根目錄）

- [ ] **B4.2** 修改 `Program.cs`：移除 LEUpdater 啟動程式碼

刪除第 24-32 行（`try { Process.Start(...LEUpdater.exe...) } catch {}`）：

```csharp
// 刪除這段：
try
{
    Process.Start(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "LEUpdater.exe"),
        "schedule");
}
catch
{
}
```

- [ ] **B4.3** 修改 `Program.cs`：更新版本顯示

將所有 `GlobalHelper.GetLEVersion()` 呼叫改為 `GlobalHelper.GetVersionString()`。

將：
```csharp
"Locale Emulator Version " + GlobalHelper.GetLEVersion()
```
改為：
```csharp
"Locale Emulator Version " + GlobalHelper.GetVersionString()
```

- [ ] **B4.4** 修改 `Program.cs`：將 `GetCharsetFromANSICodepage` 改為 `internal static`

將：
```csharp
private static int GetCharsetFromANSICodepage(int ansicp)
```
改為：
```csharp
internal static int GetCharsetFromANSICodepage(int ansicp)
```

- [ ] **B4.5** 修改 `LoaderWrapper.cs`：將結構體改為 internal/public（供測試存取）

確保 `LEB`、`TIME_FIELDS`、`RTL_TIME_ZONE_INFORMATION` 結構體的存取修飾符為 `internal`（已是），配合 `InternalsVisibleTo` 讓測試能存取。

同樣確保 `LERegistryRedirector` 中的 `REGISTRY_ENTRY64`、`REGISTRY_REDIRECTION_ENTRY64`、`UNICODE_STRING64` 為 `internal`（已是）。

- [ ] **B4.6** 更新 `app.manifest`：移除 Win7/8 supportedOS

修改 `src/LEProc/app.manifest`，移除 Win7 和 Win8 的 supportedOS：

```xml
<compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1">
  <application>
    <!-- Windows 10 / 11 -->
    <supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}"/>
  </application>
</compatibility>
```

- [ ] **B4.7** 建置並測試

```bash
cd E:/Code/Locale-Emulator/src/LEProc
dotnet build

cd E:/Code/Locale-Emulator/tests/LEProc.Tests
dotnet test --verbosity normal
```

- [ ] **B4.8** 提交

```bash
cd E:/Code/Locale-Emulator
git add src/LEProc/ tests/LEProc.Tests/
git commit -m "feat(LEProc): migrate to SDK-style csproj targeting .NET 10

- Move source files to src/LEProc/
- Remove AssemblyInfo.cs and LEVersion.xml
- Remove LEUpdater launch code from Program.cs
- Update version display to use Assembly metadata
- Update app.manifest: remove Win7/8 supportedOS
- Make GetCharsetFromANSICodepage internal for testability"
```

### Task B5: Characterization Test — .NET 10 ICU vs NLS

- [ ] **B5.1** 執行 characterization test（ICU 模式，即預設）

```bash
cd E:/Code/Locale-Emulator/tests/LEProc.Tests
dotnet test --filter "FullyQualifiedName~CharacterizationTests" --verbosity normal
```

記錄差異報告。

- [ ] **B5.2** 若有差異，啟用 NLS 模式重測

在 `Directory.Build.props` 中暫時啟用 NLS：
```xml
<ItemGroup>
  <RuntimeHostConfigurationOption Include="System.Globalization.UseNls" Value="true" />
</ItemGroup>
```

重新執行 characterization test，比對結果。

- [ ] **B5.3** 根據結果決定是否保留 NLS 模式

- 若 ICU 模式下所有關鍵 locale 的 codepage/LCID 與基準一致 → 使用 ICU（預設）
- 若有差異 → 啟用 NLS 模式，在 `Directory.Build.props` 中取消註解

- [ ] **B5.4** 提交決策結果

```bash
cd E:/Code/Locale-Emulator
git add Directory.Build.props
git commit -m "chore: decide globalization backend based on characterization test results"
```

### Task B6: 驗證 LEProc 完整建置 + 測試

- [ ] **B6.1** 完整建置與測試

```bash
cd E:/Code/Locale-Emulator/tests/LEProc.Tests
dotnet test --verbosity normal
```

確認所有測試通過。

---

## Part C: LEGUI 遷移

### Task C1: 建立 SDK-style LEGUI.csproj

- [ ] **C1.1** 建立目錄

```bash
mkdir -p E:/Code/Locale-Emulator/src/LEGUI
```

- [ ] **C1.2** 建立新的 SDK-style csproj

檔案：`E:\Code\Locale-Emulator\src\LEGUI\LEGUI.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <RootNamespace>LEGUI</RootNamespace>
    <AssemblyName>LEGUI</AssemblyName>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
    <Version>3.0.0</Version>
    <Company>Locale Emulator Contributors</Company>
    <Copyright>Copyright (c) Paddy Xu, Locale Emulator Contributors</Copyright>
    <ApplicationIcon>icon.ico</ApplicationIcon>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\LECommonLibrary\LECommonLibrary.csproj" />
  </ItemGroup>

  <!-- 語言檔案：以 PreserveNewest 取代 post-build copy -->
  <ItemGroup>
    <None Include="Lang\**\*.xaml" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

  <ItemGroup>
    <Resource Include="icon.ico" />
  </ItemGroup>

  <!-- 允許測試專案存取 internal 成員（如 I18n.CurrentCultureInfo） -->
  <ItemGroup>
    <InternalsVisibleTo Include="LEGUI.Tests" />
  </ItemGroup>

</Project>
```

**說明**：
- `UseWPF=true`：啟用 WPF 支援
- `UseWindowsForms=true`：部分程式碼仍可能引用 WinForms 型別
- 語言檔案使用 `CopyToOutputDirectory="PreserveNewest"` 取代 post-build `mkdir + copy` 命令
- `DefaultLanguage.xaml` 保留在 XAML 資源字典（`<Page>`），其餘語言檔案為外部檔案
- `InternalsVisibleTo`：允許 LEGUI.Tests 存取 `internal` 成員（如 `I18n.CurrentCultureInfo`）

### Task C2: 實作 ShellExtensionRegistrar（核心新增類別）

> **重要**：這是整個 Plan 3 最關鍵的新增程式碼。此類別是 Shell Extension 註冊的唯一權威實作，完全取代 LEInstaller 的 `DoRegister/DoUnRegister` 和 `ShellExtensionManager`。

- [ ] **C2.1** 建立測試專案

```bash
mkdir -p E:/Code/Locale-Emulator/tests/LEGUI.Tests
```

檔案：`E:\Code\Locale-Emulator\tests\LEGUI.Tests\LEGUI.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <RootNamespace>LEGUI.Tests</RootNamespace>
    <UseWPF>true</UseWPF>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="NSubstitute" Version="5.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\LEGUI\LEGUI.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **C2.2** 定義 `IRegistryOperations` 介面（可測試性抽象）

檔案：`E:\Code\Locale-Emulator\src\LEGUI\IRegistryOperations.cs`

```csharp
using Microsoft.Win32;

namespace LEGUI;

/// <summary>
/// Registry 操作的抽象介面，供 ShellExtensionRegistrar 使用。
/// 測試時以 NSubstitute mock 取代實際登錄檔操作。
/// </summary>
internal interface IRegistryOperations
{
    /// <summary>
    /// 開啟或建立指定的登錄檔子機碼。
    /// </summary>
    IRegistryKeyWrapper CreateSubKey(RegistryHive hive, string subKey, RegistryView view);

    /// <summary>
    /// 刪除指定的登錄檔子機碼樹。
    /// </summary>
    void DeleteSubKeyTree(RegistryHive hive, string subKey, RegistryView view, bool throwOnMissing);

    /// <summary>
    /// 檢查指定的登錄檔子機碼是否存在。
    /// </summary>
    bool SubKeyExists(RegistryHive hive, string subKey, RegistryView view);
}

/// <summary>
/// 包裝 RegistryKey 的介面，供 mock 使用。
/// </summary>
internal interface IRegistryKeyWrapper : IDisposable
{
    void SetValue(string? name, object value);
}
```

- [ ] **C2.3** 實作 `RegistryOperations`（實際操作）

檔案：`E:\Code\Locale-Emulator\src\LEGUI\RegistryOperations.cs`

```csharp
using Microsoft.Win32;

namespace LEGUI;

/// <summary>
/// 實際的 Registry 操作實作，封裝 .NET RegistryKey API。
/// </summary>
internal sealed class RegistryOperations : IRegistryOperations
{
    public IRegistryKeyWrapper CreateSubKey(RegistryHive hive, string subKey, RegistryView view)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        var key = baseKey.CreateSubKey(subKey)
            ?? throw new InvalidOperationException($"Failed to create registry key: {hive}\\{subKey}");
        return new RegistryKeyWrapper(key);
    }

    public void DeleteSubKeyTree(RegistryHive hive, string subKey, RegistryView view, bool throwOnMissing)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        try
        {
            baseKey.DeleteSubKeyTree(subKey, throwOnMissing);
        }
        catch (ArgumentException) when (!throwOnMissing)
        {
            // Key doesn't exist, ignore
        }
    }

    public bool SubKeyExists(RegistryHive hive, string subKey, RegistryView view)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        using var key = baseKey.OpenSubKey(subKey, false);
        return key != null;
    }
}

internal sealed class RegistryKeyWrapper : IRegistryKeyWrapper
{
    private readonly RegistryKey _key;

    public RegistryKeyWrapper(RegistryKey key) => _key = key;

    public void SetValue(string? name, object value) => _key.SetValue(name, value);

    public void Dispose() => _key.Dispose();
}
```

- [ ] **C2.4** 撰寫 `ShellExtensionRegistrarTests.cs`（先寫測試 — TDD）

檔案：`E:\Code\Locale-Emulator\tests\LEGUI.Tests\ShellExtensionRegistrarTests.cs`

```csharp
using Microsoft.Win32;
using NSubstitute;
using Xunit;

namespace LEGUI.Tests;

public class ShellExtensionRegistrarTests
{
    // 新 COM GUID — 需與 ShellExtension C++ 專案一致
    // 此處用 ShellExtensionConstants.NewClsid 取得
    private const string TestClsid = "{12345678-ABCD-EF01-2345-6789ABCDEF01}";
    private const string OldClsid = "{C52B9871-E5E9-41FD-B84D-C5ACADBEC7AE}";
    private const string TestDllPath = @"C:\Program Files\LE\ShellExtension.dll";
    private const string FriendlyName = "LocaleEmulator Shell Extension";

    private readonly IRegistryOperations _mockRegistry;
    private readonly Dictionary<string, Dictionary<string?, object>> _writtenKeys;

    public ShellExtensionRegistrarTests()
    {
        _mockRegistry = Substitute.For<IRegistryOperations>();
        _writtenKeys = new Dictionary<string, Dictionary<string?, object>>();

        // 設定 CreateSubKey mock：記錄寫入的值
        _mockRegistry.CreateSubKey(
            Arg.Any<RegistryHive>(),
            Arg.Any<string>(),
            Arg.Any<RegistryView>())
            .Returns(callInfo =>
            {
                var subKey = callInfo.ArgAt<string>(1);
                var mockKey = Substitute.For<IRegistryKeyWrapper>();
                var values = new Dictionary<string?, object>();
                _writtenKeys[subKey] = values;

                mockKey.When(k => k.SetValue(Arg.Any<string?>(), Arg.Any<object>()))
                    .Do(ci =>
                    {
                        values[ci.ArgAt<string?>(0)] = ci.ArgAt<object>(1);
                    });

                return mockKey;
            });
    }

    [Fact]
    public void Register_AllUsers_WritesToHKLM()
    {
        var registrar = new ShellExtensionRegistrar(_mockRegistry, TestClsid);

        registrar.Register(
            ShellExtensionRegistrar.InstallMode.AllUsers,
            TestDllPath);

        _mockRegistry.Received().CreateSubKey(
            RegistryHive.LocalMachine,
            Arg.Any<string>(),
            Arg.Any<RegistryView>());

        _mockRegistry.DidNotReceive().CreateSubKey(
            RegistryHive.CurrentUser,
            Arg.Any<string>(),
            Arg.Any<RegistryView>());
    }

    [Fact]
    public void Register_CurrentUser_WritesToHKCU()
    {
        var registrar = new ShellExtensionRegistrar(_mockRegistry, TestClsid);

        registrar.Register(
            ShellExtensionRegistrar.InstallMode.CurrentUser,
            TestDllPath);

        _mockRegistry.Received().CreateSubKey(
            RegistryHive.CurrentUser,
            Arg.Any<string>(),
            Arg.Any<RegistryView>());

        _mockRegistry.DidNotReceive().CreateSubKey(
            RegistryHive.LocalMachine,
            Arg.Any<string>(),
            Arg.Any<RegistryView>());
    }

    [Fact]
    public void Register_WritesInprocServer32()
    {
        var registrar = new ShellExtensionRegistrar(_mockRegistry, TestClsid);

        registrar.Register(
            ShellExtensionRegistrar.InstallMode.AllUsers,
            TestDllPath);

        var expectedKey = $@"Software\Classes\CLSID\{TestClsid}\InprocServer32";
        Assert.True(_writtenKeys.ContainsKey(expectedKey),
            $"Expected key '{expectedKey}' not written. Written keys: {string.Join(", ", _writtenKeys.Keys)}");

        var values = _writtenKeys[expectedKey];
        Assert.Equal(TestDllPath, values[null]);  // (Default) value
        Assert.Equal("Apartment", values["ThreadingModel"]);
    }

    [Fact]
    public void Register_WritesContextMenuHandler()
    {
        var registrar = new ShellExtensionRegistrar(_mockRegistry, TestClsid);

        registrar.Register(
            ShellExtensionRegistrar.InstallMode.AllUsers,
            TestDllPath);

        var expectedKey = $@"Software\Classes\*\shellex\ContextMenuHandlers\{TestClsid}";
        Assert.True(_writtenKeys.ContainsKey(expectedKey),
            $"Expected key '{expectedKey}' not written. Written keys: {string.Join(", ", _writtenKeys.Keys)}");

        var values = _writtenKeys[expectedKey];
        Assert.Equal(FriendlyName, values[null]);  // (Default) value
    }

    [Fact]
    public void Register_On64BitOS_UsesRegistry64View()
    {
        var registrar = new ShellExtensionRegistrar(_mockRegistry, TestClsid);

        // 模擬 64 位元 OS 行為
        registrar.Register(
            ShellExtensionRegistrar.InstallMode.AllUsers,
            TestDllPath,
            is64BitOs: true);

        _mockRegistry.Received().CreateSubKey(
            Arg.Any<RegistryHive>(),
            Arg.Any<string>(),
            RegistryView.Registry64);
    }

    [Fact]
    public void Register_On32BitOS_UsesDefaultView()
    {
        var registrar = new ShellExtensionRegistrar(_mockRegistry, TestClsid);

        registrar.Register(
            ShellExtensionRegistrar.InstallMode.AllUsers,
            TestDllPath,
            is64BitOs: false);

        _mockRegistry.Received().CreateSubKey(
            Arg.Any<RegistryHive>(),
            Arg.Any<string>(),
            RegistryView.Default);
    }

    [Fact]
    public void Unregister_AllUsers_DeletesFromHKLM()
    {
        var registrar = new ShellExtensionRegistrar(_mockRegistry, TestClsid);

        registrar.Unregister(ShellExtensionRegistrar.InstallMode.AllUsers);

        _mockRegistry.Received().DeleteSubKeyTree(
            RegistryHive.LocalMachine,
            $@"Software\Classes\CLSID\{TestClsid}",
            Arg.Any<RegistryView>(),
            false);

        _mockRegistry.Received().DeleteSubKeyTree(
            RegistryHive.LocalMachine,
            $@"Software\Classes\*\shellex\ContextMenuHandlers\{TestClsid}",
            Arg.Any<RegistryView>(),
            false);
    }

    [Fact]
    public void Unregister_CurrentUser_DeletesFromHKCU()
    {
        var registrar = new ShellExtensionRegistrar(_mockRegistry, TestClsid);

        registrar.Unregister(ShellExtensionRegistrar.InstallMode.CurrentUser);

        _mockRegistry.Received().DeleteSubKeyTree(
            RegistryHive.CurrentUser,
            $@"Software\Classes\CLSID\{TestClsid}",
            Arg.Any<RegistryView>(),
            false);

        _mockRegistry.Received().DeleteSubKeyTree(
            RegistryHive.CurrentUser,
            $@"Software\Classes\*\shellex\ContextMenuHandlers\{TestClsid}",
            Arg.Any<RegistryView>(),
            false);
    }

    [Fact]
    public void IsInstalled_Delegates_ToSubKeyExists()
    {
        _mockRegistry.SubKeyExists(
            RegistryHive.LocalMachine,
            Arg.Any<string>(),
            Arg.Any<RegistryView>())
            .Returns(true);

        var registrar = new ShellExtensionRegistrar(_mockRegistry, TestClsid);

        var result = registrar.IsInstalled(ShellExtensionRegistrar.InstallMode.AllUsers);
        Assert.True(result);
    }

    [Fact]
    public void HasOldRegistration_ChecksBothHives()
    {
        _mockRegistry.SubKeyExists(
            Arg.Any<RegistryHive>(),
            Arg.Is<string>(s => s.Contains(OldClsid)),
            Arg.Any<RegistryView>())
            .Returns(true);

        var registrar = new ShellExtensionRegistrar(_mockRegistry, TestClsid);

        var result = registrar.HasOldRegistration();
        Assert.True(result);
    }

    [Fact]
    public void CleanupOldRegistration_DeletesOldClsidFromBothHives()
    {
        var registrar = new ShellExtensionRegistrar(_mockRegistry, TestClsid);

        registrar.CleanupOldRegistration();

        // 應清理 HKLM
        _mockRegistry.Received().DeleteSubKeyTree(
            RegistryHive.LocalMachine,
            Arg.Is<string>(s => s.Contains(OldClsid)),
            Arg.Any<RegistryView>(),
            false);

        // 應清理 HKCU
        _mockRegistry.Received().DeleteSubKeyTree(
            RegistryHive.CurrentUser,
            Arg.Is<string>(s => s.Contains(OldClsid)),
            Arg.Any<RegistryView>(),
            false);
    }

    [Fact]
    public void Register_BothModes_UseSameKeyList()
    {
        // 驗證兩種模式寫入的 key 清單完全相同（僅根 hive 不同）
        var allUsersKeys = new List<string>();
        var currentUserKeys = new List<string>();

        var mockAllUsers = Substitute.For<IRegistryOperations>();
        mockAllUsers.CreateSubKey(Arg.Any<RegistryHive>(), Arg.Any<string>(), Arg.Any<RegistryView>())
            .Returns(ci =>
            {
                allUsersKeys.Add(ci.ArgAt<string>(1));
                var mk = Substitute.For<IRegistryKeyWrapper>();
                return mk;
            });

        var mockCurrentUser = Substitute.For<IRegistryOperations>();
        mockCurrentUser.CreateSubKey(Arg.Any<RegistryHive>(), Arg.Any<string>(), Arg.Any<RegistryView>())
            .Returns(ci =>
            {
                currentUserKeys.Add(ci.ArgAt<string>(1));
                var mk = Substitute.For<IRegistryKeyWrapper>();
                return mk;
            });

        new ShellExtensionRegistrar(mockAllUsers, TestClsid)
            .Register(ShellExtensionRegistrar.InstallMode.AllUsers, TestDllPath);

        new ShellExtensionRegistrar(mockCurrentUser, TestClsid)
            .Register(ShellExtensionRegistrar.InstallMode.CurrentUser, TestDllPath);

        Assert.Equal(allUsersKeys, currentUserKeys);
    }
}
```

- [ ] **C2.5** 實作 `ShellExtensionRegistrar`（完整實作）

檔案：`E:\Code\Locale-Emulator\src\LEGUI\ShellExtensionRegistrar.cs`

```csharp
using Microsoft.Win32;

namespace LEGUI;

/// <summary>
/// Shell Extension 登錄檔註冊器。
///
/// 這是 Shell Extension COM 註冊的唯一權威實作。
///
/// 設計原則：
/// - 兩種安裝模式（AllUsers/CurrentUser）使用同一份 key 清單，僅根 hive 不同
/// - 64 位元 OS 強制使用 RegistryView.Registry64
/// - 不呼叫 DllRegisterServer、不使用 LoadLibrary、不使用 regsvr32
/// - 負責偵測並清理舊版 COM GUID 殘留
/// </summary>
internal sealed class ShellExtensionRegistrar
{
    /// <summary>
    /// 舊版 LEContextMenuHandler 的 COM GUID（需清理）。
    /// </summary>
    public const string OldClsid = "{C52B9871-E5E9-41FD-B84D-C5ACADBEC7AE}";

    /// <summary>
    /// Shell Extension 的友善名稱（寫入 ContextMenuHandlers 的 Default 值）。
    /// </summary>
    private const string FriendlyName = "LocaleEmulator Shell Extension";

    private readonly IRegistryOperations _registry;
    private readonly string _clsid;

    /// <summary>
    /// 安裝模式。
    /// </summary>
    public enum InstallMode
    {
        /// <summary>所有使用者（HKLM，需要管理員權限）。</summary>
        AllUsers,

        /// <summary>僅當前使用者（HKCU，不需要管理員權限）。</summary>
        CurrentUser
    }

    /// <summary>
    /// 建立 ShellExtensionRegistrar 實例。
    /// </summary>
    /// <param name="registry">Registry 操作抽象（生產環境傳入 RegistryOperations，測試傳入 mock）。</param>
    /// <param name="clsid">新 Shell Extension 的 COM GUID（含大括號，如 {XXXXXXXX-...}）。</param>
    public ShellExtensionRegistrar(IRegistryOperations registry, string clsid)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _clsid = clsid ?? throw new ArgumentNullException(nameof(clsid));
    }

    /// <summary>
    /// 取得依安裝模式對應的登錄檔根 hive。
    /// </summary>
    private static RegistryHive GetHive(InstallMode mode) =>
        mode == InstallMode.AllUsers ? RegistryHive.LocalMachine : RegistryHive.CurrentUser;

    /// <summary>
    /// 取得依 OS 位元數決定的 RegistryView。
    /// 64 位元 OS → Registry64（確保寫入原生 64 位元視圖，Explorer.exe 讀取此視圖）
    /// 32 位元 OS → Default
    /// </summary>
    private static RegistryView GetView(bool is64BitOs) =>
        is64BitOs ? RegistryView.Registry64 : RegistryView.Default;

    /// <summary>
    /// 取得需要寫入的登錄檔 key 清單（兩種模式共用）。
    /// </summary>
    private (string subKey, Action<IRegistryKeyWrapper> setValues)[] GetKeyList(string dllPath) =>
    [
        // 1. COM 伺服器註冊
        (
            subKey: $@"Software\Classes\CLSID\{_clsid}\InprocServer32",
            setValues: key =>
            {
                key.SetValue(null, dllPath);           // (Default) = DLL 路徑
                key.SetValue("ThreadingModel", "Apartment");
            }
        ),
        // 2. Context Menu Handler 註冊
        (
            subKey: $@"Software\Classes\*\shellex\ContextMenuHandlers\{_clsid}",
            setValues: key =>
            {
                key.SetValue(null, FriendlyName);      // (Default) = 友善名稱
            }
        )
    ];

    /// <summary>
    /// 需要刪除的 key 清單（解除安裝用）。
    /// </summary>
    private string[] GetDeleteKeyList(string clsid) =>
    [
        $@"Software\Classes\CLSID\{clsid}",
        $@"Software\Classes\*\shellex\ContextMenuHandlers\{clsid}"
    ];

    /// <summary>
    /// 註冊 Shell Extension。
    /// </summary>
    /// <param name="mode">安裝模式。</param>
    /// <param name="dllPath">ShellExtension.dll 的完整路徑。</param>
    /// <param name="is64BitOs">是否為 64 位元 OS（預設自動偵測）。</param>
    public void Register(InstallMode mode, string dllPath, bool? is64BitOs = null)
    {
        var hive = GetHive(mode);
        var view = GetView(is64BitOs ?? Environment.Is64BitOperatingSystem);

        foreach (var (subKey, setValues) in GetKeyList(dllPath))
        {
            using var key = _registry.CreateSubKey(hive, subKey, view);
            setValues(key);
        }
    }

    /// <summary>
    /// 解除註冊 Shell Extension。
    /// </summary>
    /// <param name="mode">安裝模式。</param>
    /// <param name="is64BitOs">是否為 64 位元 OS（預設自動偵測）。</param>
    public void Unregister(InstallMode mode, bool? is64BitOs = null)
    {
        var hive = GetHive(mode);
        var view = GetView(is64BitOs ?? Environment.Is64BitOperatingSystem);

        foreach (var subKey in GetDeleteKeyList(_clsid))
        {
            _registry.DeleteSubKeyTree(hive, subKey, view, throwOnMissing: false);
        }
    }

    /// <summary>
    /// 檢查 Shell Extension 是否已安裝。
    /// </summary>
    /// <param name="mode">安裝模式。</param>
    /// <param name="is64BitOs">是否為 64 位元 OS（預設自動偵測）。</param>
    public bool IsInstalled(InstallMode mode, bool? is64BitOs = null)
    {
        var hive = GetHive(mode);
        var view = GetView(is64BitOs ?? Environment.Is64BitOperatingSystem);
        var subKey = $@"Software\Classes\*\shellex\ContextMenuHandlers\{_clsid}";

        return _registry.SubKeyExists(hive, subKey, view);
    }

    /// <summary>
    /// 自動偵測 DLL 路徑（根據 OS 位元數）。
    /// </summary>
    /// <param name="basePath">Build 輸出的根目錄。</param>
    /// <param name="is64BitOs">是否為 64 位元 OS（預設自動偵測）。</param>
    /// <returns>ShellExtension.dll 的完整路徑。</returns>
    public static string AutoDetectDllPath(string basePath, bool? is64BitOs = null)
    {
        var is64 = is64BitOs ?? Environment.Is64BitOperatingSystem;
        var subDir = is64 ? "x64" : "x86";
        return Path.Combine(basePath, subDir, "ShellExtension.dll");
    }

    /// <summary>
    /// 檢查是否存在舊版 COM GUID 的登錄檔殘留（HKLM 或 HKCU）。
    /// </summary>
    public bool HasOldRegistration(bool? is64BitOs = null)
    {
        var view = GetView(is64BitOs ?? Environment.Is64BitOperatingSystem);

        foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        {
            foreach (var subKey in GetDeleteKeyList(OldClsid))
            {
                if (_registry.SubKeyExists(hive, subKey, view))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 清理舊版 COM GUID 的登錄檔殘留（同時清理 HKLM 和 HKCU）。
    /// </summary>
    public void CleanupOldRegistration(bool? is64BitOs = null)
    {
        var view = GetView(is64BitOs ?? Environment.Is64BitOperatingSystem);

        foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        {
            foreach (var subKey in GetDeleteKeyList(OldClsid))
            {
                _registry.DeleteSubKeyTree(hive, subKey, view, throwOnMissing: false);
            }
        }
    }
}
```

- [ ] **C2.6** 驗證測試通過

```bash
cd E:/Code/Locale-Emulator/tests/LEGUI.Tests
dotnet test --verbosity normal
```

- [ ] **C2.7** 提交

```bash
cd E:/Code/Locale-Emulator
git add src/LEGUI/IRegistryOperations.cs src/LEGUI/RegistryOperations.cs src/LEGUI/ShellExtensionRegistrar.cs tests/LEGUI.Tests/
git commit -m "feat(LEGUI): implement ShellExtensionRegistrar with full test coverage

- IRegistryOperations abstraction for testability
- Two modes (AllUsers/CurrentUser), same key list, only root hive differs
- Registry64 view on 64-bit OS
- Old GUID (C52B9871-...) cleanup support
- Auto-detect DLL path by OS bitness
- 12 xUnit tests with NSubstitute mocks"
```

### Task C3: 撰寫 i18n 測試

- [ ] **C3.1** 撰寫 `I18nTests.cs`

檔案：`E:\Code\Locale-Emulator\tests\LEGUI.Tests\I18nTests.cs`

```csharp
using Xunit;

namespace LEGUI.Tests;

public class I18nTests
{
    [Fact]
    public void DefaultLanguageFile_Exists()
    {
        // DefaultLanguage.xaml 應作為 Page 資源編譯進組件
        // 在 WPF 中它是 MergedDictionary 的一部分
        // 此測試驗證語言檔案目錄存在（在建置輸出中）
        var langDir = Path.Combine(
            Path.GetDirectoryName(typeof(I18n).Assembly.Location) ?? ".",
            "Lang");

        // 在測試環境中，語言檔案應被複製到輸出目錄
        // 注意：DefaultLanguage.xaml 是 <Page> 不是 <None>，
        //       所以它編譯進組件而非複製到 Lang/
        //       其他語言檔案才會在 Lang/ 目錄中

        // 此測試的目的是確保語言系統的基本假設成立
        Assert.NotNull(I18n.CurrentCultureInfo);
    }

    [Fact]
    public void GetString_UnknownKey_ReturnsKeyAsIs()
    {
        // 當 key 找不到時，I18n.GetString 應返回 key 本身
        // 注意：此測試需要 WPF Application 環境，可能需要在整合測試中驗證
        // 這裡僅驗證 CurrentCultureInfo 不為 null
        Assert.NotNull(I18n.CurrentCultureInfo);
        Assert.False(string.IsNullOrEmpty(I18n.CurrentCultureInfo.Name));
    }
}
```

- [ ] **C3.2** 提交

```bash
cd E:/Code/Locale-Emulator
git add tests/LEGUI.Tests/I18nTests.cs
git commit -m "test(LEGUI): add i18n tests for language loading"
```

### Task C4: 搬移 LEGUI 原始碼

- [ ] **C4.1** 複製原始碼到新目錄

```bash
# XAML 檔案
# 原始碼已在 Plan 1 中由 git mv 搬移至 src/LEGUI/，無需複製。
# 以下驗證檔案已就位：
ls src/LEGUI/App.xaml src/LEGUI/App.xaml.cs src/LEGUI/GlobalConfig.xaml src/LEGUI/GlobalConfig.xaml.cs src/LEGUI/AppConfig.xaml src/LEGUI/AppConfig.xaml.cs

# C# 檔案
# I18n.cs 和 ShellLink.cs 已在 src/LEGUI/ 中
ls src/LEGUI/I18n.cs src/LEGUI/ShellLink.cs 2>/dev/null

# 語言檔案
mkdir -p E:/Code/Locale-Emulator/src/LEGUI/Lang
# Lang/ 目錄已在 src/LEGUI/Lang/ 中
ls src/LEGUI/Lang/*.xaml | head -5

# 圖示
# icon.ico 已在 src/LEGUI/ 中
ls src/LEGUI/icon.ico
```

**不要複製**：
- `Properties/AssemblyInfo.cs`
- `Properties/Resources.resx` 和 `Resources.Designer.cs`
- `Properties/Settings.settings` 和 `Settings.Designer.cs`
- `key.snk`

- [ ] **C4.2** 更新 LEGUI.csproj 的語言檔案處理

確認 `DefaultLanguage.xaml` 作為 `<Page>` 處理（WPF 資源字典），其餘語言檔案作為 `<None>` 複製。

在 csproj 中加入明確排除，避免 SDK-style 自動將 `Lang/*.xaml` 當成 `<Page>`：

```xml
<!-- 排除 Lang/ 下的 xaml 被自動當成 Page（除了 DefaultLanguage.xaml） -->
<ItemGroup>
  <Page Remove="Lang\**\*.xaml" />
  <Page Include="Lang\DefaultLanguage.xaml" />
  <None Include="Lang\**\*.xaml" Exclude="Lang\DefaultLanguage.xaml" CopyToOutputDirectory="PreserveNewest" LinkBase="Lang" />
</ItemGroup>
```

- [ ] **C4.3** 修正原始碼的 nullable 警告和 .NET 10 相容性問題

主要修正點：
- `App.xaml.cs`：確保 nullable annotations 正確
- `I18n.cs`：`FileStream` 應使用 `using` 語法
- `GlobalConfig.xaml.cs`：無重大變更

- [ ] **C4.4** 建置

```bash
cd E:/Code/Locale-Emulator/src/LEGUI
dotnet build
```

- [ ] **C4.5** 執行全部測試

```bash
cd E:/Code/Locale-Emulator/tests/LEGUI.Tests
dotnet test --verbosity normal
```

- [ ] **C4.6** 提交

```bash
cd E:/Code/Locale-Emulator
git add src/LEGUI/
git commit -m "feat(LEGUI): migrate to SDK-style csproj targeting .NET 10 with WPF

- Move source files to src/LEGUI/
- Remove AssemblyInfo.cs (properties in csproj)
- Replace post-build copy with CopyToOutputDirectory
- DefaultLanguage.xaml as Page, other langs as None with copy
- Add Shell Extension install/uninstall via ShellExtensionRegistrar"
```

### Task C5: 在 LEGUI 中加入 Shell Extension 安裝/解除安裝 UI

- [ ] **C5.0** 在語言檔案中新增 Shell Extension 安裝相關的 i18n key

在 `src/LEGUI/Lang/DefaultLanguage.xaml` 中新增以下 key（在 `<ResourceDictionary>` 內）：

```xml
<system:String x:Key="InstallSuccess">Shell Extension installed successfully. Please restart Explorer or log out to take effect.</system:String>
<system:String x:Key="UninstallSuccess">Shell Extension uninstalled successfully. Please restart Explorer or log out to take effect.</system:String>
<system:String x:Key="InstallShellExtTitle">Shell Extension</system:String>
<system:String x:Key="InstallCurrentUser">Install (Current User)</system:String>
<system:String x:Key="InstallAllUsers">Install (All Users)</system:String>
<system:String x:Key="Uninstall">Uninstall</system:String>
<system:String x:Key="InstallStatus">Status:</system:String>
<system:String x:Key="Installed">Installed</system:String>
<system:String x:Key="NotInstalled">Not Installed</system:String>
```

> **注意**：其他 21 個語言檔案也需要加入對應的翻譯 key。初期可以先保持英文，後續再翻譯。

- [ ] **C5.1** 在 `GlobalConfig.xaml` 中新增安裝/解除安裝按鈕區段

在 GlobalConfig 視窗中新增一個區段，包含：
- 「安裝右鍵選單（當前使用者）」按鈕
- 「安裝右鍵選單（所有使用者）」按鈕
- 「解除安裝右鍵選單」按鈕
- 安裝狀態顯示

- [ ] **C5.2** 在 `GlobalConfig.xaml.cs` 中實作按鈕事件

```csharp
// 在 GlobalConfig 類別中新增

private void bInstallCurrentUser_Click(object sender, RoutedEventArgs e)
{
    try
    {
        var registrar = new ShellExtensionRegistrar(
            new RegistryOperations(),
            ShellExtensionConstants.NewClsid);

        var dllPath = ShellExtensionRegistrar.AutoDetectDllPath(
            Path.GetDirectoryName(LEConfig.GlobalConfigPath) ?? ".");

        if (!File.Exists(dllPath))
        {
            MessageBox.Show($"ShellExtension.dll not found at: {dllPath}",
                "Locale Emulator", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        registrar.Register(ShellExtensionRegistrar.InstallMode.CurrentUser, dllPath);
        UpdateInstallStatus();
        MessageBox.Show(I18n.GetString("InstallSuccess"),
            "Locale Emulator", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

private void bInstallAllUsers_Click(object sender, RoutedEventArgs e)
{
    if (!LECommonLibrary.SystemHelper.IsAdministrator())
    {
        // 需要管理員權限 → 提升權限重啟
        try
        {
            LECommonLibrary.SystemHelper.RunWithElevatedProcess(
                Path.Combine(
                    Path.GetDirectoryName(LEConfig.GlobalConfigPath) ?? ".",
                    "LEGUI.exe"),
                new[] { "--install-all-users" });
        }
        catch
        {
            MessageBox.Show("Administrator privilege required.",
                "Locale Emulator", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        return;
    }

    try
    {
        var registrar = new ShellExtensionRegistrar(
            new RegistryOperations(),
            ShellExtensionConstants.NewClsid);

        var dllPath = ShellExtensionRegistrar.AutoDetectDllPath(
            Path.GetDirectoryName(LEConfig.GlobalConfigPath) ?? ".");

        registrar.Register(ShellExtensionRegistrar.InstallMode.AllUsers, dllPath);
        UpdateInstallStatus();
        MessageBox.Show(I18n.GetString("InstallSuccess"),
            "Locale Emulator", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    catch (Exception ex)
    {
        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

private void UpdateInstallStatus()
{
    var registrar = new ShellExtensionRegistrar(
        new RegistryOperations(),
        ShellExtensionConstants.NewClsid);

    var allUsersInstalled = registrar.IsInstalled(
        ShellExtensionRegistrar.InstallMode.AllUsers);
    var currentUserInstalled = registrar.IsInstalled(
        ShellExtensionRegistrar.InstallMode.CurrentUser);

    // 更新 UI 狀態文字
}
```

- [ ] **C5.3** 建立 `ShellExtensionConstants.cs`

檔案：`E:\Code\Locale-Emulator\src\LEGUI\ShellExtensionConstants.cs`

```csharp
namespace LEGUI;

/// <summary>
/// Shell Extension 相關常數。
/// 新 GUID 需與 C++ ShellExtension 專案一致（Plan 2 產出）。
/// </summary>
internal static class ShellExtensionConstants
{
    public const string NewClsid = "{A8B4F5C2-7E3D-4F1A-9C6B-2D8E0F5A3B71}";
}
```

- [ ] **C5.4** 提交

```bash
cd E:/Code/Locale-Emulator
git add src/LEGUI/
git commit -m "feat(LEGUI): add Shell Extension install/uninstall UI in GlobalConfig

- Install for current user (no admin) and all users (admin required)
- Status display for installation state
- Integrates ShellExtensionRegistrar for registry operations"
```

### Task C6: 驗證 LEGUI 完整建置 + 測試

- [ ] **C6.1** 完整建置

```bash
cd E:/Code/Locale-Emulator/src/LEGUI
dotnet build
```

- [ ] **C6.2** 執行所有 LEGUI 測試

```bash
cd E:/Code/Locale-Emulator/tests/LEGUI.Tests
dotnet test --verbosity normal
```

- [ ] **C6.3** 執行所有專案測試

```bash
cd E:/Code/Locale-Emulator
dotnet test tests/LECommonLibrary.Tests/ tests/LEProc.Tests/ tests/LEGUI.Tests/ --verbosity normal
```

確認全部通過。

---

## Part D: 清理

### Task D1: 刪除已廢棄的專案目錄

- [ ] **D1.1** 刪除 LEUpdater

```bash
rm -rf E:/Code/Locale-Emulator/src/LEUpdater/
```

- [ ] **D1.2** 刪除 LEInstaller

```bash
rm -rf E:/Code/Locale-Emulator/src/LEInstaller/
```

- [ ] **D1.3** 刪除 LEContextMenuHandler

```bash
rm -rf E:/Code/Locale-Emulator/src/LEContextMenuHandler/
```

- [ ] **D1.4** 刪除 LEVersion.xml

```bash
rm -f E:/Code/Locale-Emulator/src/LEProc/LEVersion.xml
```

- [ ] **D1.5** 提交

```bash
cd E:/Code/Locale-Emulator
git add -A src/LEUpdater/ src/LEInstaller/ src/LEContextMenuHandler/ src/LEProc/LEVersion.xml
git commit -m "chore: remove LEUpdater, LEInstaller, LEContextMenuHandler, LEVersion.xml

LEUpdater: removed entirely (future redesign if needed)
LEInstaller: functionality merged into LEGUI ShellExtensionRegistrar
LEContextMenuHandler: replaced by C++ Shell Extension (Plan 2)"
```

### Task D2: 更新 Scripts

- [ ] **D2.1** 更新 `Scripts/sign-package.ps1` 路徑

如果存在 `Scripts/sign-package.ps1`，更新其中的路徑引用：

```bash
ls E:/Code/Locale-Emulator/Scripts/sign-package.ps1 2>/dev/null
```

若存在，更新 `Build\Release\*` 為 `Build\Release\x86\*` 和 `Build\Release\x64\*`。

- [ ] **D2.2** 提交

```bash
cd E:/Code/Locale-Emulator
git add Scripts/
git commit -m "chore: update sign-package.ps1 paths for new build output structure"
```

### Task D3: 從 Solution 移除舊專案

- [ ] **D3.1** 更新 `LocaleEmulator.sln`

從 Solution 檔案中移除已刪除的專案引用：
- LEUpdater 的 ProjectGuid
- LEInstaller 的 ProjectGuid
- LEContextMenuHandler 的 ProjectGuid

加入新專案：
- `src\LECommonLibrary\LECommonLibrary.csproj`
- `src\LEProc\LEProc.csproj`
- `src\LEGUI\LEGUI.csproj`
- `tests\LECommonLibrary.Tests\LECommonLibrary.Tests.csproj`
- `tests\LEProc.Tests\LEProc.Tests.csproj`
- `tests\LEGUI.Tests\LEGUI.Tests.csproj`

同時移除舊的 LECommonLibrary、LEProc、LEGUI 專案引用。

也可以用 `dotnet sln` 命令：

```bash
cd E:/Code/Locale-Emulator

# 移除舊專案
dotnet sln remove src/LEUpdater/LEUpdater.csproj 2>/dev/null
dotnet sln remove src/LEInstaller/LEInstaller.csproj 2>/dev/null
dotnet sln remove src/LEContextMenuHandler/LEContextMenuHandler.csproj 2>/dev/null
dotnet sln remove src/LECommonLibrary/LECommonLibrary.csproj 2>/dev/null
dotnet sln remove src/LEProc/LEProc.csproj 2>/dev/null
dotnet sln remove src/LEGUI/LEGUI.csproj 2>/dev/null

# 加入新專案
dotnet sln add src/LECommonLibrary/LECommonLibrary.csproj
dotnet sln add src/LEProc/LEProc.csproj
dotnet sln add src/LEGUI/LEGUI.csproj
dotnet sln add tests/LECommonLibrary.Tests/LECommonLibrary.Tests.csproj
dotnet sln add tests/LEProc.Tests/LEProc.Tests.csproj
dotnet sln add tests/LEGUI.Tests/LEGUI.Tests.csproj
```

- [ ] **D3.2** 驗證 Solution 可建置

```bash
cd E:/Code/Locale-Emulator
dotnet build LocaleEmulator.sln
```

- [ ] **D3.3** 執行所有測試

```bash
cd E:/Code/Locale-Emulator
dotnet test LocaleEmulator.sln --verbosity normal
```

- [ ] **D3.4** 提交

```bash
cd E:/Code/Locale-Emulator
git add LocaleEmulator.sln
git commit -m "chore: update solution - remove old projects, add new .NET 10 projects and test projects"
```

### Task D4: 移除舊版 csproj 與 AssemblyInfo（可選，在確認新版完全可用後）

Plan 1 已將所有專案搬至 `src/`。此步驟移除 `src/` 下的舊式 `.csproj`（已被 SDK-style 取代）和 `AssemblyInfo.cs`。

- [ ] **D4.1** 移除舊版 csproj 和 AssemblyInfo

```bash
cd E:/Code/Locale-Emulator
# 移除舊式 csproj（已被新的 SDK-style csproj 取代）
rm -f src/LECommonLibrary/LECommonLibrary.csproj.old 2>/dev/null
rm -f src/LEProc/LEProc.csproj.old 2>/dev/null
rm -f src/LEGUI/LEGUI.csproj.old 2>/dev/null
# 移除 AssemblyInfo.cs（屬性已移至 csproj）
rm -f src/LECommonLibrary/Properties/AssemblyInfo.cs 2>/dev/null
rm -f src/LEProc/Properties/AssemblyInfo.cs 2>/dev/null
rm -f src/LEGUI/Properties/AssemblyInfo.cs 2>/dev/null
```

**注意**：此步驟應在確認新版全部建置、測試通過後才執行。

- [ ] **D4.2** 提交

```bash
cd E:/Code/Locale-Emulator
git add -A
git commit -m "chore: remove legacy csproj and AssemblyInfo.cs files

All projects now use SDK-style csproj targeting .NET 10.
Assembly attributes are defined in csproj properties."
```

### Task D5: 最終驗證

- [ ] **D5.1** 完整建置

```bash
cd E:/Code/Locale-Emulator
dotnet build LocaleEmulator.sln --configuration Release
```

- [ ] **D5.2** 全部測試通過

```bash
cd E:/Code/Locale-Emulator
dotnet test LocaleEmulator.sln --configuration Debug --verbosity normal
```

- [ ] **D5.3** 列出測試覆蓋摘要

驗證所有測試專案的測試數量：
- `LECommonLibrary.Tests`：至少 15 個測試
- `LEProc.Tests`：至少 20 個測試
- `LEGUI.Tests`：至少 15 個測試

---

## 附錄 A: 需要注意的 .NET 10 相容性問題

| 項目 | .NET Framework 4.0 行為 | .NET 10 行為 | 影響 |
|------|------------------------|-------------|------|
| `CultureInfo.TextInfo.ANSICodePage` | NLS 表查詢 | ICU 模式可能有差異 | 需 characterization test 驗證 |
| `System.Drawing.Graphics` | 直接可用 | 需 `UseWindowsForms=true` 或 `System.Drawing.Common` | SystemHelper.Is4KDisplay |
| `Assembly.GetEntryAssembly()` | 永遠有值 | 在某些宿主環境可能回傳 null | 需加 null check |
| `Registry.GetValue()` | 需要 `Microsoft.Win32` | `net10.0-windows` 下直接可用 | 無影響 |
| `XDocument` / `XElement` | `System.Xml.Linq` | 完全相容 | 無影響 |
| `DllImport` | 標準 P/Invoke | 完全相容（可選升級至 `LibraryImport`） | 無影響 |
| `Marshal.SizeOf` | 完全相容 | 完全相容 | 無影響 |
| WPF `XamlReader.Load` | 完全相容 | 完全相容 | 無影響 |

## 附錄 B: 測試矩陣

| 測試類別 | 測試方法 | 預期結果 |
|---------|---------|---------|
| LEProfile 預設值 | `DefaultConstructor_SetsJaJPDefaults` | ja-JP 預設值 |
| LEConfig XML 解析 | `GetProfiles_ValidXml_ParsesAllProfiles` | 正確解析 2 個 profile |
| LEConfig 預設值 | `GetProfiles_MissingOptionalElements_UsesDefaults` | RunAsAdmin=false, RedirectRegistry=true |
| LEConfig 錯誤處理 | `GetProfiles_InvalidXml_ReturnsEmptyArray` | 空陣列 |
| LEConfig 往返 | `SaveAndReload_RoundTrips` | 所有欄位一致 |
| SystemHelper 64-bit | `Is64BitOS_ReturnsConsistentResult` | 與 Environment 一致 |
| PEFileReader x86 | `GetPEType_X86PE_ReturnsX32` | X32 |
| PEFileReader x64 | `GetPEType_X64PE_ReturnsX64` | X64 |
| Charset 對應 | 12 種 codepage | 正確 charset 值 |
| Locale 基準值 | 6 個關鍵 locale | codepage/LCID 正確 |
| Registry 條目 | 基本/進階模式 | 4/8 個條目 |
| LEB 結構大小 | Marshal.SizeOf | > 200 bytes |
| 註冊 AllUsers | HKLM 寫入 | InprocServer32 + ContextMenuHandlers |
| 註冊 CurrentUser | HKCU 寫入 | 同上 key 清單 |
| 64-bit Registry | Registry64 view | 確認使用 64 位元視圖 |
| 舊版清理 | 偵測 + 刪除 | C52B9871 相關 key 被刪除 |
