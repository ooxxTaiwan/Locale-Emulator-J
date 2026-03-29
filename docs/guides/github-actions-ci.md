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

**在我們的專案中**：有 11 個 Step，從 checkout 到上傳 artifact。

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

### 3.9 Step 5: Build Debug|Win32

```yaml
- name: Build Solution (Debug|Win32)
  run: msbuild LocaleEmulator.sln /p:Configuration=Debug /p:Platform=Win32 /m /v:minimal
```

- `/p:Configuration=Debug`：使用 Debug 組態（包含測試專案）
- `/p:Platform=Win32`：建置 Win32 平台（C++ 專案編譯為 x86，.NET 專案映射為 AnyCPU，靠 csproj 的 PlatformTarget 控制實際位元數）
- `/m`：啟用多核心平行建置
- `/v:minimal`：最少輸出量（減少 log 雜訊）

### 3.10 Step 6: .NET Tests

```yaml
- name: Run .NET tests
  run: dotnet test LocaleEmulator.sln --no-build --configuration Debug --logger "console;verbosity=detailed"
```

- `dotnet test`：自動探索並執行所有 xUnit 測試專案
- `--no-build`：不重複建置（已在 Step 5 以 Debug 建置過）
- `--configuration Debug`：測試專案只在 Debug 組態建置（spec solution matrix 定義）
- `--logger`：輸出詳細測試結果到 console

### 3.11 Step 7: C++ Tests

```yaml
- name: Run C++ tests (Google Test)
  run: |
    $testExes = Get-ChildItem -Path "Build\Debug\x86" -Filter "*Tests*.exe" -Recurse
    ...
  shell: pwsh
```

- `run: |`：多行指令
- `shell: pwsh`：使用 PowerShell Core 執行
- 在 `Build\Debug\x86` 中搜尋（測試專案只在 Debug 建置）
- Google Test 的 exe 直接跑就好，不需要特別的 test runner

### 3.12 Steps 8-9: Release Builds

```yaml
- name: Build Solution (Release|Win32)
  run: msbuild LocaleEmulator.sln /p:Configuration=Release /p:Platform=Win32 /m /v:minimal

- name: Build Solution (Release|x64)
  run: msbuild LocaleEmulator.sln /p:Configuration=Release /p:Platform=x64 /m /v:minimal
```

Release 建置用於產品輸出。x64 組態只會建置 ShellExtension（其他專案在 Solution 組態中標記為「不建置」）。

### 3.13 Step 10: Smoke Tests

```yaml
- name: Smoke Tests
  run: .\tests\SmokeTest\Invoke-SmokeTest.ps1
  shell: pwsh
  if: success()
```

- `if: success()`：只在前面所有 step 都成功時才執行
- 執行 Smoke Test 腳本，驗證 LEProc 的 locale hook 是否正確

### 3.14 Step 11: Upload Artifacts

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
  CI                                    #15     master    2m 30s    (passed)
  CI                                    #14     PR #5     1m 45s    (failed)
```

- 綠色勾 = 成功
- 紅色叉 = 失敗
- 黃色圈 = 執行中

點進去可以看到每個 Job 的狀態。

### 6.3 查看 Job 詳情

點選 Job（例如「Build & Test」），展開後可以看到：

- 每個 Step 的名稱和狀態（勾/叉）
- 點選 Step 可以展開完整的 console 輸出
- 如果 Step 失敗，會自動展開失敗的那步

### 6.4 在 PR 上查看

發 PR 時，CI 結果會直接顯示在 PR 頁面底部：

```
  CI / Build & Test (pull_request) — All checks have passed
```

或

```
  CI / Build & Test (pull_request) — 1 failure
```

點「Details」可以跳到該 run 的詳情頁面。

### 6.5 下載 Artifact

如果 Workflow 有上傳 artifact（我們的 Step 11）：

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
