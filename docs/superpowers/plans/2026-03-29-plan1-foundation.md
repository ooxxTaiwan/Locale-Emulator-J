# Plan 1: Foundation — 目錄重整與 Core 基線

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 建立新的目錄結構，整合 Locale-Emulator-Core 原始碼，驗證 Core 可建置基線

**Architecture:** 將現有扁平結構遷移至 src/tests/docs 分層結構，將 upstream Core (commit ae7160d) 原樣納入並重現可建置狀態

**Tech Stack:** Git, MSBuild, MSVC v145/v141, .NET 10 SDK, 7-Zip

**Branch:** `feature/dotnet10-foundation`

**PR:** Part of ooxxTaiwan/Locale-Emulator-J#1

---

## 背景

Locale Emulator 目前是一個 .NET Framework 4.0 專案，六個 C# 專案散落在 repo 根目錄下（`LECommonLibrary/`、`LEProc/`、`LEGUI/`、`LEContextMenuHandler/`、`LEInstaller/`、`LEUpdater/`）。這個 Plan 的任務是：

1. 將現有專案搬入 `src/` 目錄
2. 將 upstream [Locale-Emulator-Core](https://github.com/xupefei/Locale-Emulator-Core) 的原始碼複製進 `src/Core/`
3. 建立新的建置基礎設施（`.gitignore`、`.gitattributes`、`Directory.Build.props`、`key.snk`）
4. 建立新的 `LocaleEmulator.sln`
5. 驗證 Core 可用 `_Compilers` 工具鏈建置
6. Spike 嘗試 v145 標準工具鏈

### 現有目錄結構

```
Locale-Emulator/
├── LocaleEmulator.sln
├── LECommonLibrary/          # .NET Framework 4.0 Class Library
│   ├── LECommonLibrary.csproj
│   └── key.snk
├── LEContextMenuHandler/     # .NET Framework 4.0 COM Shell Extension
│   ├── LEContextMenuHandler.csproj
│   └── key.snk
├── LEGUI/                    # .NET Framework 4.0 WPF Application
│   ├── LEGUI.csproj
│   └── key.snk
├── LEInstaller/              # .NET Framework 4.0 WinForms
│   ├── LEInstaller.csproj
│   └── key.snk
├── LEProc/                   # .NET Framework 4.0 Console/WinExe (x86)
│   ├── LEProc.csproj
│   └── key.snk
├── LEUpdater/                # .NET Framework 4.0 WinForms
│   ├── LEUpdater.csproj
│   └── key.snk
├── Build/
├── Scripts/
├── .gitignore
├── .gitattributes
├── CLAUDE.md
├── COPYING / COPYING.LESSER
└── README.md
```

### 目標目錄結構（本 Plan 完成後）

```
Locale-Emulator/
├── LocaleEmulator.sln              # 新建，包含 C# + C++ 專案
├── Directory.Build.props           # 共用 .NET 10 MSBuild 屬性
├── key.snk                         # 統一簽署金鑰（從專案中提取）
│
├── src/
│   ├── LECommonLibrary/            # 搬移自根目錄（原 csproj 不變）
│   ├── LEProc/                     # 搬移自根目錄（原 csproj 不變）
│   ├── LEGUI/                      # 搬移自根目錄（原 csproj 不變）
│   ├── LEContextMenuHandler/       # 搬移自根目錄（原 csproj 不變）
│   ├── LEInstaller/                # 搬移自根目錄（原 csproj 不變）
│   ├── LEUpdater/                  # 搬移自根目錄（原 csproj 不變）
│   │
│   └── Core/                       # 從 upstream clone 複製而來
│       ├── LoaderDll/
│       ├── LocaleEmulator/
│       ├── Loader/
│       ├── _Compilers/             # 解壓後的修改版 VS2015 工具鏈
│       ├── _Libs/                  # 解壓後的靜態程式庫
│       ├── _WDK/                   # WDK 標頭與程式庫
│       ├── _Headers/               # 自訂標頭
│       └── LocaleEmulatorCore.sln
│
├── Build/
├── Scripts/
├── docs/
├── .gitignore                      # 更新後
├── .gitattributes                  # 更新後
├── CLAUDE.md
├── COPYING / COPYING.LESSER
└── README.md
```

> **注意**：此 Plan 階段不修改任何 `.csproj` 的內容，不遷移 .NET 版本。所有 C# 專案保持 .NET Framework 4.0 格式。`Directory.Build.props` 先建立但尚未生效（因為舊式 csproj 不會自動讀取 `Directory.Build.props`）。.NET 10 遷移會在後續 Plan 處理。

---

## Task 1: 建立 feature branch

**預計時間**: 2 分鐘

- [ ] 確認目前在 `master` 分支且工作目錄乾淨

```bash
cd E:/Code/Locale-Emulator
git status
```

預期輸出（未追蹤檔案可忽略）：
```
On branch master
nothing to commit, working tree clean
```

- [ ] 從 `master` 建立並切換到新分支

```bash
git checkout -b feature/dotnet10-foundation
```

預期輸出：
```
Switched to a new branch 'feature/dotnet10-foundation'
```

---

## Task 2: 搬移現有 C# 專案至 src/

**預計時間**: 5 分鐘

將六個現有 C# 專案目錄從 repo 根目錄搬移至 `src/`。使用 `git mv` 保留 Git 歷史追蹤。

- [ ] 建立 `src/` 目錄

```bash
cd E:/Code/Locale-Emulator
mkdir -p src
```

- [ ] 逐一搬移六個專案目錄

```bash
cd E:/Code/Locale-Emulator
git mv LECommonLibrary src/LECommonLibrary
git mv LEContextMenuHandler src/LEContextMenuHandler
git mv LEGUI src/LEGUI
git mv LEInstaller src/LEInstaller
git mv LEProc src/LEProc
git mv LEUpdater src/LEUpdater
```

- [ ] 驗證搬移結果

```bash
ls src/
```

預期輸出：
```
LECommonLibrary  LEContextMenuHandler  LEGUI  LEInstaller  LEProc  LEUpdater
```

- [ ] 確認 Git 偵測到 rename

```bash
git status
```

預期輸出應顯示 `renamed:` 狀態（若檔案內容不變，Git 會偵測為 rename）。

- [ ] Commit

```bash
git add -A
git commit -m "$(cat <<'EOF'
chore: 搬移現有 C# 專案至 src/ 目錄

將六個 C# 專案從 repo 根目錄遷移至 src/ 子目錄，
為新的 src/tests/docs 分層結構做準備。

搬移的專案：
- LECommonLibrary
- LEContextMenuHandler
- LEGUI
- LEInstaller
- LEProc
- LEUpdater

此 commit 不修改任何程式碼，僅搬移檔案位置。
舊的 LocaleEmulator.sln 中的路徑尚未更新（後續 Task 會建立新的 sln）。

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: 統一 key.snk 至 repo 根目錄

**預計時間**: 3 分鐘

六個專案各有一份 `key.snk`，內容應完全相同。將其中一份複製到 repo 根目錄，移除各專案中的副本。

- [ ] 驗證所有 key.snk 內容相同

```bash
cd E:/Code/Locale-Emulator

# 計算所有 key.snk 的 SHA-256 hash，應該全部一樣
# 注意：sha256sum 在 Git Bash (Windows) 中可用
# PowerShell 替代方案：Get-FileHash -Algorithm SHA256 <檔案路徑>
sha256sum src/LECommonLibrary/key.snk src/LEContextMenuHandler/key.snk src/LEGUI/key.snk src/LEInstaller/key.snk src/LEProc/key.snk src/LEUpdater/key.snk
```

預期輸出：六行相同的 hash 值。

> **如果 hash 不同**：停下來，記錄差異，選擇 `LEProc/key.snk`（最關鍵的專案）作為基準，並在 commit message 中說明。

- [ ] 複製一份到根目錄

```bash
cp src/LEProc/key.snk E:/Code/Locale-Emulator/key.snk
```

- [ ] 移除各專案中的 key.snk

```bash
cd E:/Code/Locale-Emulator
git rm src/LECommonLibrary/key.snk
git rm src/LEContextMenuHandler/key.snk
git rm src/LEGUI/key.snk
git rm src/LEInstaller/key.snk
git rm src/LEProc/key.snk
git rm src/LEUpdater/key.snk
```

- [ ] 將根目錄的 key.snk 加入 Git

```bash
git add key.snk
```

- [ ] 驗證根目錄 key.snk 存在

```bash
ls -la E:/Code/Locale-Emulator/key.snk
```

- [ ] Commit

```bash
cd E:/Code/Locale-Emulator
git commit -m "$(cat <<'EOF'
chore: 統一 key.snk 至 repo 根目錄

將六個專案各自的 key.snk 統一為根目錄下的單一檔案。
所有 key.snk 的 SHA-256 hash 已驗證相同。
後續 Directory.Build.props 會引用 $(MSBuildThisFileDirectory)key.snk。

注意：現有舊式 csproj 中的 <AssemblyOriginatorKeyFile> 路徑
尚未更新（仍指向 key.snk 相對路徑），此時建置會失敗。
路徑修正會在 .NET 10 csproj 遷移時一併處理。

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: Clone Locale-Emulator-Core 並複製至 src/Core/

**預計時間**: 5 分鐘

從 upstream 抓取 Core 原始碼（commit `ae7160dc`），複製到 `src/Core/`，不使用 git submodule。

- [ ] Clone upstream repo 至臨時目錄

```bash
cd E:/Code/Locale-Emulator
git clone https://github.com/xupefei/Locale-Emulator-Core.git _tmp_core
```

- [ ] 切換到指定 commit

```bash
cd E:/Code/Locale-Emulator/_tmp_core
git checkout ae7160dc5deb97947396abcd784f9b98b6ee38b3
```

預期輸出：
```
HEAD is now at ae7160d ...
```

- [ ] 驗證 commit hash 正確

```bash
cd E:/Code/Locale-Emulator/_tmp_core
git log -1 --format="%H %s"
```

預期輸出開頭為：
```
ae7160dc5deb97947396abcd784f9b98b6ee38b3 ...
```

- [ ] 複製內容到 src/Core/（排除 .git 目錄）

```bash
cd E:/Code/Locale-Emulator
mkdir -p src/Core
# 複製內容並排除 .git 目錄
cp -r _tmp_core/* src/Core/
cp -r _tmp_core/.gitignore src/Core/ 2>/dev/null || true
cp -r _tmp_core/.gitattributes src/Core/ 2>/dev/null || true
# 確保不複製 .git 目錄
rm -rf src/Core/.git 2>/dev/null || true
```

> 替代方案：如果有 `rsync` 可用，可一步完成：
> ```bash
> rsync -av --exclude='.git' _tmp_core/ src/Core/
> ```

- [ ] 移除臨時 clone 目錄

```bash
rm -rf E:/Code/Locale-Emulator/_tmp_core
```

- [ ] 驗證 Core 目錄結構

```bash
ls E:/Code/Locale-Emulator/src/Core/
```

預期應包含以下目錄/檔案：
```
LoaderDll/
LocaleEmulator/
Loader/
_Compilers/
_Libs/
_WDK/
_Headers/
LocaleEmulatorCore.sln
...
```

- [ ] 確認 `.7z` 封存檔存在

```bash
find E:/Code/Locale-Emulator/src/Core -name "*.7z" -type f
```

預期輸出（檔名可能略有不同）：
```
E:/Code/Locale-Emulator/src/Core/_Compilers/_Compilers.7z
E:/Code/Locale-Emulator/src/Core/_Libs/_Libs.7z
E:/Code/Locale-Emulator/src/Core/_WDK/_WDK.7z
```

> 注意：實際的 `.7z` 檔案位置需以 clone 結果為準，upstream 的目錄結構中 `.7z` 可能在子目錄中。

- [ ] Commit（先 commit 原始 clone 內容，包含 .7z，不含解壓後檔案）

```bash
cd E:/Code/Locale-Emulator
git add src/Core/
git commit -m "$(cat <<'EOF'
[Core] 納入 Locale-Emulator-Core 原始碼 (commit ae7160d)

來源：https://github.com/xupefei/Locale-Emulator-Core
Commit：ae7160dc5deb97947396abcd784f9b98b6ee38b3 (2021-08-23)
授權：LGPL-3.0 / GPL-3.0
Repo 狀態：已被 owner 於 2022-04-15 封存 (archived)

原樣複製 upstream 內容至 src/Core/，不使用 git submodule。
包含三個 .7z 封存檔（_Compilers、_Libs、_WDK），下一步解壓。

此 commit 記錄 upstream 原始狀態，後續所有修改以獨立 commit 標註
[Core] 前綴，便於追溯。

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: 解壓 .7z 封存檔並記錄 SHA-256 hash

**預計時間**: 5 分鐘

解壓 Core 中的三個 `.7z` 封存檔，記錄原始封存檔的 SHA-256 hash 供溯源驗證。

### 前置條件

需要安裝 7-Zip 命令列工具。驗證：

```bash
7z --help 2>/dev/null || "C:/Program Files/7-Zip/7z.exe" --help 2>/dev/null
```

如果 `7z` 不在 PATH 中，後續指令需使用完整路徑 `"C:/Program Files/7-Zip/7z.exe"`。以下假設 `7z` 在 PATH 中。

- [ ] 記錄所有 .7z 封存檔的 SHA-256 hash

```bash
cd E:/Code/Locale-Emulator
# 注意：sha256sum 在 Git Bash (Windows) 中可用
# PowerShell 替代方案：Get-FileHash -Algorithm SHA256 <檔案路徑>
find src/Core -name "*.7z" -type f -exec sha256sum {} \;
```

將輸出記錄到一個文字檔中：

```bash
cd E:/Code/Locale-Emulator
find src/Core -name "*.7z" -type f -exec sha256sum {} \; > src/Core/ARCHIVES_SHA256.txt
```

`ARCHIVES_SHA256.txt` 範例內容（實際 hash 以計算結果為準）：

```
<hash1>  src/Core/_Compilers/_Compilers.7z
<hash2>  src/Core/_Libs/_Libs.7z
<hash3>  src/Core/_WDK/_WDK.7z
```

- [ ] 解壓 `_Compilers.7z`

```bash
cd E:/Code/Locale-Emulator/src/Core/_Compilers
7z x _Compilers.7z -y
```

預期：解壓出修改版 VS2015 工具鏈檔案（`cl.exe`、`link.exe`、`link.bak` 等）。

驗證：
```bash
ls E:/Code/Locale-Emulator/src/Core/_Compilers/
```

應包含 `cl.exe`、`link.exe` 等檔案。

- [ ] 解壓 `_Libs.7z`

```bash
cd E:/Code/Locale-Emulator/src/Core/_Libs
7z x _Libs.7z -y
```

預期：解壓出 `.lib` 靜態程式庫檔案。

驗證：
```bash
ls E:/Code/Locale-Emulator/src/Core/_Libs/
```

應包含 `.lib` 檔案。

- [ ] 解壓 `_WDK.7z`（如果存在）

```bash
# 先確認 _WDK 中是否有 .7z
find E:/Code/Locale-Emulator/src/Core/_WDK -name "*.7z" -type f
```

如果有 `.7z` 檔案：
```bash
cd E:/Code/Locale-Emulator/src/Core/_WDK
7z x *.7z -y
```

> **注意**：`_WDK` 目錄可能不含 `.7z`（upstream 結構中可能已是展開狀態）。如果沒有 `.7z`，跳過此步驟。

- [ ] 驗證解壓後的檔案清單

```bash
# 列出所有解壓後的關鍵檔案
find E:/Code/Locale-Emulator/src/Core/_Compilers -type f | head -20
find E:/Code/Locale-Emulator/src/Core/_Libs -type f | head -20
find E:/Code/Locale-Emulator/src/Core/_WDK -type f | head -20
```

- [ ] Commit 解壓後的檔案

```bash
cd E:/Code/Locale-Emulator
git add src/Core/ARCHIVES_SHA256.txt
git add src/Core/_Compilers/
git add src/Core/_Libs/
git add src/Core/_WDK/
git commit -m "$(cat <<'EOF'
[Core] 解壓 .7z 封存檔並記錄 SHA-256 hash

解壓以下封存檔供直接建置使用：
- _Compilers/_Compilers.7z → 修改版 VS2015 工具鏈（cl.exe、link.exe 等）
- _Libs/_Libs.7z → 未公開 API 靜態程式庫（.lib）
- _WDK（如有 .7z 則解壓）→ WDK 標頭與程式庫

原始 .7z 封存檔保留在 repo 中，其 SHA-256 hash 記錄於
src/Core/ARCHIVES_SHA256.txt 供溯源驗證。

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: 更新 .gitignore

**預計時間**: 3 分鐘

現有 `.gitignore` 是 VS 2013 時代的基本規則，需要大幅擴展以支援 .NET 10 SDK-style 專案和 C++ 建置。

- [ ] 讀取現有 `.gitignore` 確認內容

現有內容（30 行）：
```
Thumbs.db
*.obj
*.pdb
*.user
*.aps
*.pch
*.vspscc
*_i.c
*_p.c
*.ncb
*.suo
*.sln.docstates
*.tlb
*.tlh
*.bak
*.cache
*.ilk
*.log
[Bb]in
[Dd]ebug*/
*.lib
*.sbr
obj/
[Rr]elease*/
_ReSharper*/
[Tt]est[Rr]esult*
*.vssscc
$tf*/
.vs/
```

- [ ] 覆寫 `.gitignore` 為新版本

將 `E:/Code/Locale-Emulator/.gitignore` 替換為以下內容：

```gitignore
## ----- 建置輸出 -----
Build/
**/bin/
**/obj/

## ----- .NET 產出物 -----
*.runtimeconfig.json
*.deps.json

## ----- C++ 建置中間產物 -----
*.obj
*.pdb
*.pch
*.ilk
*.exp
*.iobj
*.ipdb
*.lib
*.sbr
x64/
Win32/

## ----- Visual Studio -----
.vs/
*.user
*.suo
*.sln.docstates
*.cache
_ReSharper*/
*.aps
*.ncb

## ----- Visual Studio C++ 特有 -----
*.tlb
*.tlh
*_i.c
*_p.c
*.vspscc
*.vssscc
$tf*/

## ----- NuGet -----
packages/

## ----- 測試結果 -----
[Tt]est[Rr]esult*/
*.trx
TestResults/
coverage/

## ----- OS 產出物 -----
Thumbs.db
Desktop.ini
.DS_Store

## ----- 臨時目錄 -----
_tmp_*/

## ----- 例外：Core 解壓後的二進位檔需要追蹤 -----
## _Compilers、_Libs、_WDK 中的 .lib / .exe 等二進位檔
## 是建置依賴，必須 commit。以下例外規則取消 *.lib 的忽略：
!src/Core/_Libs/**/*.lib
!src/Core/_Compilers/**
!src/Core/_WDK/**
```

> **重要**：上方的 `*.lib` 規則會忽略所有 `.lib` 檔案，但 Core 的 `_Libs` 目錄中的 `.lib` 是建置依賴，必須被 Git 追蹤。最後三行的 `!` 例外規則確保這些檔案不被忽略。

- [ ] 驗證 Core 的二進位檔沒有被忽略

```bash
cd E:/Code/Locale-Emulator
git status src/Core/_Libs/
git status src/Core/_Compilers/
```

預期：這些目錄中的檔案應該不在 `Untracked files` 中被忽略（如果已 commit 則不會顯示，如果尚未 commit 則應顯示為 untracked）。

> **注意**：`.gitignore` 的 negation 規則（`!` 開頭）只能取消同級或上層的忽略規則。如果 `*.lib` 在前面被列出，`!src/Core/_Libs/**/*.lib` 可以取消其效果。但如果路徑中有被忽略的父目錄（例如父目錄被其他規則忽略），negation 不會生效。執行時需驗證。

如果驗證發現 negation 規則無效，替代方案是改用 `git add -f` 強制追蹤，或將 `*.lib` 的忽略規則限定範圍：

```gitignore
# 替代方案：只在非 Core 目錄忽略 .lib
# 移除全域的 *.lib，改為：
src/**/*.lib
!src/Core/**/*.lib
```

- [ ] Commit

```bash
cd E:/Code/Locale-Emulator
git add .gitignore
git commit -m "$(cat <<'EOF'
chore: 更新 .gitignore 支援 .NET 10 SDK-style 與 C++ 建置

大幅擴展 .gitignore 規則：
- 新增 .NET 10 產出物（*.runtimeconfig.json、*.deps.json）
- 新增 C++ 建置中間產物（*.exp、*.iobj、*.ipdb、x64/、Win32/）
- 新增 Build/ 統一輸出目錄
- 新增 NuGet packages/ 目錄
- 新增測試結果目錄
- 保留 Core 二進位依賴的例外規則（_Compilers、_Libs、_WDK）

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 7: 更新 .gitattributes

**預計時間**: 3 分鐘

更新 `.gitattributes`，為 Core 的大型二進位檔預留 Git LFS 標記，並改善現有設定。

- [ ] 讀取現有 `.gitattributes` 確認內容

現有內容為 VS 2013 標準範本（64 行），主要設定 `* text=auto` 並註解掉了大部分規則。

- [ ] 覆寫 `.gitattributes` 為新版本

將 `E:/Code/Locale-Emulator/.gitattributes` 替換為以下內容：

```gitattributes
###############################################################################
# 預設行為：自動正規化行尾符號
###############################################################################
* text=auto

###############################################################################
# 程式碼檔案：明確標記為文字
###############################################################################
*.cs        text diff=csharp
*.cpp       text diff=cpp
*.h         text diff=cpp
*.hpp       text diff=cpp
*.c         text diff=c
*.xaml      text
*.xml       text
*.json      text
*.props     text
*.targets   text
*.sln       text eol=crlf
*.csproj    text eol=crlf
*.vcxproj   text eol=crlf
*.def       text

###############################################################################
# 二進位檔案
###############################################################################
*.snk       binary
*.ico       binary
*.png       binary
*.jpg       binary
*.gif       binary
*.dll       binary
*.exe       binary
*.7z        binary

###############################################################################
# Git LFS 追蹤（視需要啟用）
#
# Core 的 _Libs、_WDK 解壓後可能包含大型二進位檔（合計約數十 MB）。
# 若 repo 膨脹成為問題，取消以下註解啟用 LFS。
# 啟用前須先執行：git lfs install
###############################################################################
# src/Core/_Libs/**/*.lib filter=lfs diff=lfs merge=lfs -text
# src/Core/_Compilers/**/*.exe filter=lfs diff=lfs merge=lfs -text
# src/Core/_Compilers/**/*.dll filter=lfs diff=lfs merge=lfs -text
# src/Core/**/*.7z filter=lfs diff=lfs merge=lfs -text
```

- [ ] Commit

```bash
cd E:/Code/Locale-Emulator
git add .gitattributes
git commit -m "$(cat <<'EOF'
chore: 更新 .gitattributes 支援 C#/C++ 混合專案與 LFS 預備

- 明確標記 C#/C++/XAML/XML 等文字檔案的 diff 驅動
- 標記 .sln/.csproj/.vcxproj 強制 CRLF（Windows 專案檔慣例）
- 標記二進位檔案類型（.snk、.ico、.dll、.exe、.7z）
- 預留 Git LFS 規則（已註解），供 Core 大型二進位檔需要時啟用

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 8: 建立 Directory.Build.props

**預計時間**: 2 分鐘

在 repo 根目錄建立 `Directory.Build.props`，定義所有 .NET 10 專案的共用 MSBuild 屬性。

> **注意**：此檔案目前對舊式 `.csproj`（非 SDK-style）**不生效**。舊式 csproj 不會自動 import `Directory.Build.props`。此檔案將在後續 Plan 中，各專案遷移為 SDK-style csproj 時自動生效。

- [ ] 建立 `E:/Code/Locale-Emulator/Directory.Build.props`

寫入以下完整內容：

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

  <!-- UseNls 待驗證 -->
  <!--
  <ItemGroup>
    <RuntimeHostConfigurationOption Include="System.Globalization.UseNls" Value="true" />
  </ItemGroup>
  -->
</Project>
```

各屬性說明：

| 屬性 | 值 | 用途 |
|------|-----|------|
| `TargetFramework` | `net10.0-windows` | 目標 .NET 10 + Windows 專用 API（Registry、WPF 等） |
| `LangVersion` | `14` | C# 14，.NET 10 搭配版本 |
| `SignAssembly` | `true` | 強名稱簽署 |
| `AssemblyOriginatorKeyFile` | `$(MSBuildThisFileDirectory)key.snk` | 指向 repo 根目錄的統一 key.snk |
| `ImplicitUsings` | `enable` | 自動 using 常用命名空間 |
| `Nullable` | `enable` | 啟用 nullable reference types |

UseNls 區段（已註解）：
- `System.Globalization.UseNls = true` 會讓 .NET 10 使用 Windows NLS 而非 ICU 作為全球化後端
- 是否啟用需以 characterization test 比對 .NET Framework 4.0 與 .NET 10 的 `ANSICodePage`、`OEMCodePage`、`LCID` 差異後決定
- 此設定只影響 .NET 端的 `CultureInfo` 查詢，Core 本身直接操作 NLS 表不受此設定影響

- [ ] Commit

```bash
cd E:/Code/Locale-Emulator
git add Directory.Build.props
git commit -m "$(cat <<'EOF'
chore: 建立 Directory.Build.props 共用 .NET 10 MSBuild 屬性

定義所有 .NET 10 專案的共用建置屬性：
- TargetFramework: net10.0-windows
- LangVersion: 14 (C# 14)
- SignAssembly: true（引用根目錄 key.snk）
- ImplicitUsings / Nullable: enable

UseNls 設定暫時註解，需以 characterization test 驗證後決定。

注意：此檔案對現有舊式 csproj 不生效，將在各專案遷移為
SDK-style csproj 時自動啟用。

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 9: 建立新的 LocaleEmulator.sln

**預計時間**: 5 分鐘

建立新的 Solution 檔案，包含所有搬移後的 C# 專案以及 Core 的 C++ 專案。新 sln 替換舊的 sln（路徑已改變的專案需更新）。

- [ ] 備份舊的 sln 以供參考

```bash
cp E:/Code/Locale-Emulator/LocaleEmulator.sln E:/Code/Locale-Emulator/LocaleEmulator.sln.bak
```

- [ ] 確認 Core 中 C++ 專案的路徑

```bash
find E:/Code/Locale-Emulator/src/Core -name "*.vcxproj" -type f
```

預期找到（實際路徑以 clone 結果為準）：
```
src/Core/LoaderDll/LoaderDll.vcxproj
src/Core/LocaleEmulator/LocaleEmulator.vcxproj
src/Core/Loader/Loader.vcxproj
```

- [ ] 確認 Core sln 中 C++ 專案的 GUID

```bash
cat E:/Code/Locale-Emulator/src/Core/LocaleEmulatorCore.sln
```

記錄各 C++ 專案的 GUID（`{xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}` 格式）。
C++ 專案使用 `{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}` 作為 Project Type GUID。

- [ ] 覆寫 `LocaleEmulator.sln`

以下為新 sln 的模板。**實際的 C++ 專案 GUID 需要從 Core 的 sln 或 `.vcxproj` 中讀取後替換**。

C# 專案的 Project Type GUID: `{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}`
C++ 專案的 Project Type GUID: `{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}`

將 `E:/Code/Locale-Emulator/LocaleEmulator.sln` 替換為以下內容：

```sln
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 18
VisualStudioVersion = 18.0.0.0
MinimumVisualStudioVersion = 10.0.40219.1

# ============================================================
# C# 專案（搬移至 src/ 後的路徑）
# ============================================================
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "LECommonLibrary", "src\LECommonLibrary\LECommonLibrary.csproj", "{C4DEF32B-8FBB-41B6-80DC-C60339402323}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "LEProc", "src\LEProc\LEProc.csproj", "{A2CF5369-4665-49F6-B948-B44296002C3D}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "LEGUI", "src\LEGUI\LEGUI.csproj", "{33B8F5CA-34A7-4826-BAD9-7CEC63DF4D0C}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "LEContextMenuHandler", "src\LEContextMenuHandler\LEContextMenuHandler.csproj", "{0041073D-2FA7-4B35-B904-9C606C89C8CD}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "LEInstaller", "src\LEInstaller\LEInstaller.csproj", "{E218923D-59A4-4FA5-B4BC-E540290DA3F8}"
	ProjectSection(ProjectDependencies) = postProject
		{A0011409-0A25-4E95-BF6D-EE7CB9C287DA} = {A0011409-0A25-4E95-BF6D-EE7CB9C287DA}
		{C4DEF32B-8FBB-41B6-80DC-C60339402323} = {C4DEF32B-8FBB-41B6-80DC-C60339402323}
		{0041073D-2FA7-4B35-B904-9C606C89C8CD} = {0041073D-2FA7-4B35-B904-9C606C89C8CD}
		{A2CF5369-4665-49F6-B948-B44296002C3D} = {A2CF5369-4665-49F6-B948-B44296002C3D}
		{33B8F5CA-34A7-4826-BAD9-7CEC63DF4D0C} = {33B8F5CA-34A7-4826-BAD9-7CEC63DF4D0C}
	EndProjectSection
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "LEUpdater", "src\LEUpdater\LEUpdater.csproj", "{A0011409-0A25-4E95-BF6D-EE7CB9C287DA}"
EndProject

# ============================================================
# C++ 專案（Core — 從 src/Core/ 的 .vcxproj 取得實際 GUID）
# ============================================================
# 重要：以下 GUID 為預留位置，實際值必須從 src/Core/ 中各 .vcxproj
# 的 <ProjectGuid> 元素讀取。執行時替換 {CORE_LOADERDLL_GUID} 等。
#
# 讀取方法：
#   grep -i ProjectGuid src/Core/LoaderDll/LoaderDll.vcxproj
#   grep -i ProjectGuid src/Core/LocaleEmulator/LocaleEmulator.vcxproj
#   grep -i ProjectGuid src/Core/Loader/Loader.vcxproj
# ============================================================
Project("{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}") = "LoaderDll", "src\Core\LoaderDll\LoaderDll.vcxproj", "{CORE_LOADERDLL_GUID}"
EndProject
Project("{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}") = "LocaleEmulator", "src\Core\LocaleEmulator\LocaleEmulator.vcxproj", "{CORE_LOCALEEMULATOR_GUID}"
EndProject
Project("{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}") = "Loader", "src\Core\Loader\Loader.vcxproj", "{CORE_LOADER_GUID}"
EndProject

Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Debug|Win32 = Debug|Win32
		Debug|x64 = Debug|x64
		Release|Any CPU = Release|Any CPU
		Release|Win32 = Release|Win32
		Release|x64 = Release|x64
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		# --- LECommonLibrary (AnyCPU) ---
		{C4DEF32B-8FBB-41B6-80DC-C60339402323}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{C4DEF32B-8FBB-41B6-80DC-C60339402323}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{C4DEF32B-8FBB-41B6-80DC-C60339402323}.Debug|Win32.ActiveCfg = Debug|Any CPU
		{C4DEF32B-8FBB-41B6-80DC-C60339402323}.Debug|Win32.Build.0 = Debug|Any CPU
		{C4DEF32B-8FBB-41B6-80DC-C60339402323}.Debug|x64.ActiveCfg = Debug|Any CPU
		{C4DEF32B-8FBB-41B6-80DC-C60339402323}.Debug|x64.Build.0 = Debug|Any CPU
		{C4DEF32B-8FBB-41B6-80DC-C60339402323}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{C4DEF32B-8FBB-41B6-80DC-C60339402323}.Release|Any CPU.Build.0 = Release|Any CPU
		{C4DEF32B-8FBB-41B6-80DC-C60339402323}.Release|Win32.ActiveCfg = Release|Any CPU
		{C4DEF32B-8FBB-41B6-80DC-C60339402323}.Release|Win32.Build.0 = Release|Any CPU
		{C4DEF32B-8FBB-41B6-80DC-C60339402323}.Release|x64.ActiveCfg = Release|Any CPU
		{C4DEF32B-8FBB-41B6-80DC-C60339402323}.Release|x64.Build.0 = Release|Any CPU
		# --- LEProc (x86 only) ---
		{A2CF5369-4665-49F6-B948-B44296002C3D}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A2CF5369-4665-49F6-B948-B44296002C3D}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A2CF5369-4665-49F6-B948-B44296002C3D}.Debug|Win32.ActiveCfg = Debug|Any CPU
		{A2CF5369-4665-49F6-B948-B44296002C3D}.Debug|Win32.Build.0 = Debug|Any CPU
		{A2CF5369-4665-49F6-B948-B44296002C3D}.Debug|x64.ActiveCfg = Debug|Any CPU
		{A2CF5369-4665-49F6-B948-B44296002C3D}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A2CF5369-4665-49F6-B948-B44296002C3D}.Release|Any CPU.Build.0 = Release|Any CPU
		{A2CF5369-4665-49F6-B948-B44296002C3D}.Release|Win32.ActiveCfg = Release|Any CPU
		{A2CF5369-4665-49F6-B948-B44296002C3D}.Release|Win32.Build.0 = Release|Any CPU
		{A2CF5369-4665-49F6-B948-B44296002C3D}.Release|x64.ActiveCfg = Release|Any CPU
		# --- LEGUI (AnyCPU) ---
		{33B8F5CA-34A7-4826-BAD9-7CEC63DF4D0C}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{33B8F5CA-34A7-4826-BAD9-7CEC63DF4D0C}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{33B8F5CA-34A7-4826-BAD9-7CEC63DF4D0C}.Debug|Win32.ActiveCfg = Debug|Any CPU
		{33B8F5CA-34A7-4826-BAD9-7CEC63DF4D0C}.Debug|Win32.Build.0 = Debug|Any CPU
		{33B8F5CA-34A7-4826-BAD9-7CEC63DF4D0C}.Debug|x64.ActiveCfg = Debug|Any CPU
		{33B8F5CA-34A7-4826-BAD9-7CEC63DF4D0C}.Debug|x64.Build.0 = Debug|Any CPU
		{33B8F5CA-34A7-4826-BAD9-7CEC63DF4D0C}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{33B8F5CA-34A7-4826-BAD9-7CEC63DF4D0C}.Release|Any CPU.Build.0 = Release|Any CPU
		{33B8F5CA-34A7-4826-BAD9-7CEC63DF4D0C}.Release|Win32.ActiveCfg = Release|Any CPU
		{33B8F5CA-34A7-4826-BAD9-7CEC63DF4D0C}.Release|Win32.Build.0 = Release|Any CPU
		{33B8F5CA-34A7-4826-BAD9-7CEC63DF4D0C}.Release|x64.ActiveCfg = Release|Any CPU
		{33B8F5CA-34A7-4826-BAD9-7CEC63DF4D0C}.Release|x64.Build.0 = Release|Any CPU
		# --- LEContextMenuHandler (AnyCPU) ---
		{0041073D-2FA7-4B35-B904-9C606C89C8CD}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{0041073D-2FA7-4B35-B904-9C606C89C8CD}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{0041073D-2FA7-4B35-B904-9C606C89C8CD}.Debug|Win32.ActiveCfg = Debug|Any CPU
		{0041073D-2FA7-4B35-B904-9C606C89C8CD}.Debug|Win32.Build.0 = Debug|Any CPU
		{0041073D-2FA7-4B35-B904-9C606C89C8CD}.Debug|x64.ActiveCfg = Debug|Any CPU
		{0041073D-2FA7-4B35-B904-9C606C89C8CD}.Debug|x64.Build.0 = Debug|Any CPU
		{0041073D-2FA7-4B35-B904-9C606C89C8CD}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{0041073D-2FA7-4B35-B904-9C606C89C8CD}.Release|Any CPU.Build.0 = Release|Any CPU
		{0041073D-2FA7-4B35-B904-9C606C89C8CD}.Release|Win32.ActiveCfg = Release|Any CPU
		{0041073D-2FA7-4B35-B904-9C606C89C8CD}.Release|Win32.Build.0 = Release|Any CPU
		{0041073D-2FA7-4B35-B904-9C606C89C8CD}.Release|x64.ActiveCfg = Release|Any CPU
		{0041073D-2FA7-4B35-B904-9C606C89C8CD}.Release|x64.Build.0 = Release|Any CPU
		# --- LEInstaller (AnyCPU) ---
		{E218923D-59A4-4FA5-B4BC-E540290DA3F8}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{E218923D-59A4-4FA5-B4BC-E540290DA3F8}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{E218923D-59A4-4FA5-B4BC-E540290DA3F8}.Debug|Win32.ActiveCfg = Debug|Any CPU
		{E218923D-59A4-4FA5-B4BC-E540290DA3F8}.Debug|Win32.Build.0 = Debug|Any CPU
		{E218923D-59A4-4FA5-B4BC-E540290DA3F8}.Debug|x64.ActiveCfg = Debug|Any CPU
		{E218923D-59A4-4FA5-B4BC-E540290DA3F8}.Debug|x64.Build.0 = Debug|Any CPU
		{E218923D-59A4-4FA5-B4BC-E540290DA3F8}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{E218923D-59A4-4FA5-B4BC-E540290DA3F8}.Release|Any CPU.Build.0 = Release|Any CPU
		{E218923D-59A4-4FA5-B4BC-E540290DA3F8}.Release|Win32.ActiveCfg = Release|Any CPU
		{E218923D-59A4-4FA5-B4BC-E540290DA3F8}.Release|Win32.Build.0 = Release|Any CPU
		{E218923D-59A4-4FA5-B4BC-E540290DA3F8}.Release|x64.ActiveCfg = Release|Any CPU
		{E218923D-59A4-4FA5-B4BC-E540290DA3F8}.Release|x64.Build.0 = Release|Any CPU
		# --- LEUpdater (AnyCPU) ---
		{A0011409-0A25-4E95-BF6D-EE7CB9C287DA}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A0011409-0A25-4E95-BF6D-EE7CB9C287DA}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A0011409-0A25-4E95-BF6D-EE7CB9C287DA}.Debug|Win32.ActiveCfg = Debug|Any CPU
		{A0011409-0A25-4E95-BF6D-EE7CB9C287DA}.Debug|Win32.Build.0 = Debug|Any CPU
		{A0011409-0A25-4E95-BF6D-EE7CB9C287DA}.Debug|x64.ActiveCfg = Debug|Any CPU
		{A0011409-0A25-4E95-BF6D-EE7CB9C287DA}.Debug|x64.Build.0 = Debug|Any CPU
		{A0011409-0A25-4E95-BF6D-EE7CB9C287DA}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A0011409-0A25-4E95-BF6D-EE7CB9C287DA}.Release|Any CPU.Build.0 = Release|Any CPU
		{A0011409-0A25-4E95-BF6D-EE7CB9C287DA}.Release|Win32.ActiveCfg = Release|Any CPU
		{A0011409-0A25-4E95-BF6D-EE7CB9C287DA}.Release|Win32.Build.0 = Release|Any CPU
		{A0011409-0A25-4E95-BF6D-EE7CB9C287DA}.Release|x64.ActiveCfg = Release|Any CPU
		{A0011409-0A25-4E95-BF6D-EE7CB9C287DA}.Release|x64.Build.0 = Release|Any CPU
		# --- Core/LoaderDll (Win32 only) ---
		{CORE_LOADERDLL_GUID}.Debug|Any CPU.ActiveCfg = Debug|Win32
		{CORE_LOADERDLL_GUID}.Debug|Win32.ActiveCfg = Debug|Win32
		{CORE_LOADERDLL_GUID}.Debug|Win32.Build.0 = Debug|Win32
		{CORE_LOADERDLL_GUID}.Debug|x64.ActiveCfg = Debug|Win32
		{CORE_LOADERDLL_GUID}.Release|Any CPU.ActiveCfg = Release|Win32
		{CORE_LOADERDLL_GUID}.Release|Win32.ActiveCfg = Release|Win32
		{CORE_LOADERDLL_GUID}.Release|Win32.Build.0 = Release|Win32
		{CORE_LOADERDLL_GUID}.Release|x64.ActiveCfg = Release|Win32
		# --- Core/LocaleEmulator (Win32 only) ---
		{CORE_LOCALEEMULATOR_GUID}.Debug|Any CPU.ActiveCfg = Debug|Win32
		{CORE_LOCALEEMULATOR_GUID}.Debug|Win32.ActiveCfg = Debug|Win32
		{CORE_LOCALEEMULATOR_GUID}.Debug|Win32.Build.0 = Debug|Win32
		{CORE_LOCALEEMULATOR_GUID}.Debug|x64.ActiveCfg = Debug|Win32
		{CORE_LOCALEEMULATOR_GUID}.Release|Any CPU.ActiveCfg = Release|Win32
		{CORE_LOCALEEMULATOR_GUID}.Release|Win32.ActiveCfg = Release|Win32
		{CORE_LOCALEEMULATOR_GUID}.Release|Win32.Build.0 = Release|Win32
		{CORE_LOCALEEMULATOR_GUID}.Release|x64.ActiveCfg = Release|Win32
		# --- Core/Loader (Win32 only) ---
		{CORE_LOADER_GUID}.Debug|Any CPU.ActiveCfg = Debug|Win32
		{CORE_LOADER_GUID}.Debug|Win32.ActiveCfg = Debug|Win32
		{CORE_LOADER_GUID}.Debug|Win32.Build.0 = Debug|Win32
		{CORE_LOADER_GUID}.Debug|x64.ActiveCfg = Debug|Win32
		{CORE_LOADER_GUID}.Release|Any CPU.ActiveCfg = Release|Win32
		{CORE_LOADER_GUID}.Release|Win32.ActiveCfg = Release|Win32
		{CORE_LOADER_GUID}.Release|Win32.Build.0 = Release|Win32
		{CORE_LOADER_GUID}.Release|x64.ActiveCfg = Release|Win32
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
```

> **重要：GUID 替換指令**
>
> 上方 sln 中有三個預留位置 GUID 需要替換為實際值：
> - `{CORE_LOADERDLL_GUID}` — 從 `src/Core/LoaderDll/LoaderDll.vcxproj` 的 `<ProjectGuid>` 讀取
> - `{CORE_LOCALEEMULATOR_GUID}` — 從 `src/Core/LocaleEmulator/LocaleEmulator.vcxproj` 的 `<ProjectGuid>` 讀取
> - `{CORE_LOADER_GUID}` — 從 `src/Core/Loader/Loader.vcxproj` 的 `<ProjectGuid>` 讀取
>
> 讀取方式：
> ```bash
> grep -i "<ProjectGuid>" src/Core/LoaderDll/LoaderDll.vcxproj
> grep -i "<ProjectGuid>" src/Core/LocaleEmulator/LocaleEmulator.vcxproj
> grep -i "<ProjectGuid>" src/Core/Loader/Loader.vcxproj
> ```
>
> 然後在 sln 檔案中全域替換。例如如果 LoaderDll 的 GUID 是 `{12345678-...}`：
> ```bash
> sed -i 's/{CORE_LOADERDLL_GUID}/{12345678-...}/g' LocaleEmulator.sln
> ```

- [ ] 刪除備份檔

```bash
rm E:/Code/Locale-Emulator/LocaleEmulator.sln.bak
```

- [ ] 驗證 sln 可被 MSBuild 解析（不建置，只列出專案）

```bash
cd E:/Code/Locale-Emulator
msbuild LocaleEmulator.sln /t:ValidateSolutionConfiguration /p:Configuration=Release /p:Platform="Any CPU" 2>&1 || echo "解析失敗，檢查 sln 格式"
```

> 替代驗證方式：用 `dotnet sln list` 只能列出 .NET 專案，但可驗證 sln 格式是否有效：
> ```bash
> dotnet sln LocaleEmulator.sln list
> ```

- [ ] Commit

```bash
cd E:/Code/Locale-Emulator
git add LocaleEmulator.sln
git commit -m "$(cat <<'EOF'
chore: 重建 LocaleEmulator.sln 包含 C# + C++ 專案

建立新的 Solution 檔案，反映搬移後的目錄結構：
- 六個 C# 專案路徑更新為 src/ 前綴
- 新增三個 Core C++ 專案（LoaderDll、LocaleEmulator、Loader）
- Solution 組態矩陣新增 Win32 和 x64 平台
  - C# 專案：Debug/Release | Any CPU（Win32/x64 映射至 Any CPU）
  - C++ 專案：Debug/Release | Win32（x64 不建置）

C++ 專案目前可能無法建置（需 _Compilers 工具鏈設定），
Core 建置驗證在後續 Task 處理。

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 10: 驗證 Core 可用 _Compilers 工具鏈建置

**預計時間**: 5 分鐘

嘗試使用 Core 自帶的 `_Compilers` 修改版工具鏈，在目前的 VS 環境下建置 Core 專案。

### 前置理解

Core 的三個 `.vcxproj` 在 `<PropertyGroup>` 中將 `..\_Compilers` 放在 `ExecutablePath` 的最前面，這代表建置時會優先使用 `_Compilers` 目錄中的 `cl.exe` 和 `link.exe`（修改版 VS2015 工具鏈）。

- [ ] 檢查 Core vcxproj 的 ExecutablePath 設定

```bash
grep -n "ExecutablePath" E:/Code/Locale-Emulator/src/Core/LoaderDll/LoaderDll.vcxproj
grep -n "ExecutablePath" E:/Code/Locale-Emulator/src/Core/LocaleEmulator/LocaleEmulator.vcxproj
grep -n "ExecutablePath" E:/Code/Locale-Emulator/src/Core/Loader/Loader.vcxproj
```

確認 `_Compilers` 在路徑中。注意搬移到 `src/Core/` 後，相對路徑 `..\_Compilers` 是否仍然正確——因為三個 vcxproj 和 `_Compilers` 現在都在 `src/Core/` 下，`..\_Compilers` 應該仍指向 `src/Core/_Compilers`。

> 如果路徑不正確（例如 upstream 的 `_Compilers` 在 repo 根目錄），需要調整。確認各 vcxproj 中 `_Compilers` 的相對路徑。

- [ ] 檢查 PlatformToolset 設定

```bash
grep -n "PlatformToolset" E:/Code/Locale-Emulator/src/Core/LoaderDll/LoaderDll.vcxproj
grep -n "PlatformToolset" E:/Code/Locale-Emulator/src/Core/LocaleEmulator/LocaleEmulator.vcxproj
grep -n "PlatformToolset" E:/Code/Locale-Emulator/src/Core/Loader/Loader.vcxproj
```

預期值為 `v140_xp` 或 `v141`。

- [ ] 嘗試用 MSBuild 建置 Core（Release|Win32）

開啟 Visual Studio Developer Command Prompt（或設定環境），然後：

```bash
cd E:/Code/Locale-Emulator
msbuild src/Core/LoaderDll/LoaderDll.vcxproj /p:Configuration=Release /p:Platform=Win32 2>&1
```

**可能的結果與處理方式**：

| 結果 | 處理 |
|------|------|
| 建置成功 | 記錄 `LoaderDll.dll` 的位置與大小 |
| 找不到 PlatformToolset v140_xp | VS 2026 可能沒有 v140 工具組。嘗試安裝 VS 2015 Build Tools 或修改為 v141/v145 |
| `_Compilers` 路徑錯誤 | 修正 vcxproj 中的相對路徑 |
| link.exe 報錯 | 記錄錯誤訊息，這可能是修改版 link.exe 與現有環境不相容 |
| 缺少 SDK/WDK 標頭 | 檢查 `_WDK` 和 `_Headers` 路徑設定 |

- [ ] 如果 LoaderDll 建置成功，繼續建置 LocaleEmulator

```bash
msbuild src/Core/LocaleEmulator/LocaleEmulator.vcxproj /p:Configuration=Release /p:Platform=Win32 2>&1
```

- [ ] 如果 LocaleEmulator 建置成功，繼續建置 Loader

```bash
msbuild src/Core/Loader/Loader.vcxproj /p:Configuration=Release /p:Platform=Win32 2>&1
```

- [ ] 記錄建置結果

建立筆記（不需 commit），記錄：
1. 建置是否成功
2. 使用的 PlatformToolset 版本
3. `_Compilers` 路徑是否需要調整
4. 產出檔案的位置和大小
5. 遇到的任何警告或錯誤

- [ ] 如果需要修正 vcxproj 路徑，Commit

```bash
cd E:/Code/Locale-Emulator
git add src/Core/
git commit -m "$(cat <<'EOF'
[Core] 修正搬移後的建置路徑

調整 vcxproj 中的相對路徑，使 _Compilers、_Libs、_WDK、_Headers
在 src/Core/ 目錄結構下正確解析。

（根據實際修改內容補充具體說明）

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 11: Spike — 嘗試 v145 標準工具鏈建置 Core

**預計時間**: 5 分鐘

嘗試不使用 `_Compilers` 修改版工具鏈，改用 VS 2026 標準的 v145 平台工具組建置 Core。這是一個 spike（探索性實驗），目的是評估能否擺脫修改版工具鏈的依賴。

> **重要**：此 Task 是實驗性的，結果可能是失敗。失敗不代表問題，只代表目前需要繼續使用 `_Compilers`。

- [ ] 備份原始 vcxproj

```bash
cd E:/Code/Locale-Emulator/src/Core
cp LoaderDll/LoaderDll.vcxproj LoaderDll/LoaderDll.vcxproj.bak
cp LocaleEmulator/LocaleEmulator.vcxproj LocaleEmulator/LocaleEmulator.vcxproj.bak
cp Loader/Loader.vcxproj Loader/Loader.vcxproj.bak
```

- [ ] 修改 vcxproj（三個檔案）

需要修改的項目（每個 vcxproj 都要做）：

1. **PlatformToolset**：改為 `v145`
2. **ExecutablePath**：移除 `_Compilers` 路徑（讓 MSBuild 使用標準工具鏈）
3. **XP 相容**：移除 `_xp` 後綴（已不需要 XP 支援）

```bash
# 以 LoaderDll 為例，檢查需要修改的行
grep -n "PlatformToolset\|ExecutablePath\|_xp" src/Core/LoaderDll/LoaderDll.vcxproj
```

- [ ] 嘗試建置

```bash
cd E:/Code/Locale-Emulator
msbuild src/Core/LoaderDll/LoaderDll.vcxproj /p:Configuration=Release /p:Platform=Win32 2>&1
```

- [ ] 記錄 spike 結果

| 項目 | 結果（填入） |
|------|-------------|
| v145 是否可用 | |
| 建置是否成功 | |
| 錯誤訊息（如有）| |
| 連結器問題（如有）| |
| 結論（繼續 v145 或回退 _Compilers）| |

- [ ] 根據結果決定下一步

**如果 v145 建置成功**：
- 保留修改後的 vcxproj
- Commit 並在 message 中記錄 spike 結果

```bash
cd E:/Code/Locale-Emulator
rm src/Core/LoaderDll/LoaderDll.vcxproj.bak
rm src/Core/LocaleEmulator/LocaleEmulator.vcxproj.bak
rm src/Core/Loader/Loader.vcxproj.bak
git add src/Core/
git commit -m "$(cat <<'EOF'
[Core] spike 成功：遷移至 v145 標準工具鏈

將三個 C++ 專案的 PlatformToolset 從 v140_xp/v141 改為 v145（VS 2026）。
移除 ExecutablePath 中的 _Compilers 路徑，改用標準 MSVC 工具鏈。
移除 XP 相容設定（最低支援 Windows 10）。

建置驗證：Release|Win32 成功產出 LoaderDll.dll、LocaleEmulator.dll、Loader.exe。

（根據實際情況補充產出檔案大小、與 _Compilers 版本的差異等）

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

**如果 v145 建置失敗**：
- 還原 vcxproj 備份
- 記錄失敗原因
- 不 commit

```bash
cd E:/Code/Locale-Emulator/src/Core
mv LoaderDll/LoaderDll.vcxproj.bak LoaderDll/LoaderDll.vcxproj
mv LocaleEmulator/LocaleEmulator.vcxproj.bak LocaleEmulator/LocaleEmulator.vcxproj
mv Loader/Loader.vcxproj.bak Loader/Loader.vcxproj
```

> 在後續 Plan 中，可以更深入研究修改版 `link.exe` 與標準版的差異（比對 `link.bak` vs `link.exe`），找出能否用標準工具鏈加特定連結器旗標取代。

---

## Task 12: 修正 C# 專案的內部參考路徑

**預計時間**: 5 分鐘

搬移至 `src/` 後，C# 專案之間的 `<ProjectReference>` 和 `<Reference>` 中的相對路徑可能失效。雖然這些舊式 csproj 最終會在後續 Plan 中被 SDK-style 取代，但為了在過渡期間能用 `msbuild` 建置 sln，需要修正路徑。

- [ ] 檢查各 csproj 中的跨專案參考

```bash
grep -rn "ProjectReference\|HintPath" E:/Code/Locale-Emulator/src/LEProc/LEProc.csproj
grep -rn "ProjectReference\|HintPath" E:/Code/Locale-Emulator/src/LEGUI/LEGUI.csproj
grep -rn "ProjectReference\|HintPath" E:/Code/Locale-Emulator/src/LEInstaller/LEInstaller.csproj
grep -rn "ProjectReference\|HintPath" E:/Code/Locale-Emulator/src/LEContextMenuHandler/LEContextMenuHandler.csproj
grep -rn "ProjectReference\|HintPath" E:/Code/Locale-Emulator/src/LEUpdater/LEUpdater.csproj
```

- [ ] 修正路徑

專案參考的典型模式：

舊路徑（搬移前）：
```xml
<ProjectReference Include="..\LECommonLibrary\LECommonLibrary.csproj">
```

搬移後此路徑仍然有效，因為所有專案都在 `src/` 下，相對關係不變（`src/LEProc/../LECommonLibrary/` = `src/LECommonLibrary/`）。

> **因此**：如果所有 `<ProjectReference>` 都使用 `..\\<ProjectName>\\` 格式的相對路徑，搬移後相對位置不變，不需要修改。

但如果有引用 repo 根目錄下的檔案（例如 `key.snk`），路徑會失效。

- [ ] 檢查 key.snk 引用

```bash
grep -rn "key.snk\|AssemblyOriginatorKeyFile" E:/Code/Locale-Emulator/src/*/Properties/AssemblyInfo.cs E:/Code/Locale-Emulator/src/*/*.csproj
```

舊式 csproj 中 key.snk 的引用路徑通常是 `key.snk`（相對於 csproj 所在目錄）。由於我們已在 Task 3 將 key.snk 從各專案目錄移除並統一到根目錄，這些引用會失效。

**暫時處理方式**（避免修改舊式 csproj）：在各專案目錄中建立指向根目錄 key.snk 的相對路徑 symlink 或複製一份。

但更務實的做法是：**不修正**。這些舊式 csproj 在後續 Plan 會被 SDK-style csproj 完全取代，屆時 `Directory.Build.props` 會自動處理 key.snk 路徑。目前只要 Core C++ 建置能通過即可。

- [ ] 驗證 C# 專案間的參考關係是否因搬移而破壞

```bash
cd E:/Code/Locale-Emulator
# 嘗試還原 NuGet 並建置（可能因 key.snk 失敗，但可確認參考路徑）
msbuild src/LECommonLibrary/LECommonLibrary.csproj /t:Build /p:Configuration=Release 2>&1 | tail -20
```

預期：可能因 key.snk 路徑失效而失敗，但不應因 ProjectReference 路徑失效而失敗。

記錄結果。如果有需要修正的參考路徑，進行修正並 Commit：

```bash
cd E:/Code/Locale-Emulator
git add src/
git commit -m "$(cat <<'EOF'
chore: 修正搬移至 src/ 後的專案參考路徑

（根據實際修正內容補充說明）

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

> **如果無需修正**：跳過此 commit。

---

## Task 13: 最終驗證與清理

**預計時間**: 3 分鐘

- [ ] 確認 repo 結構正確

```bash
cd E:/Code/Locale-Emulator
ls -la
ls -la src/
ls -la src/Core/
```

預期根目錄結構：
```
.gitattributes
.gitignore
Build/
CLAUDE.md
COPYING
COPYING.LESSER
Directory.Build.props
key.snk
LocaleEmulator.sln
README.md
Scripts/
docs/
src/
```

預期 `src/` 結構：
```
Core/
LECommonLibrary/
LEContextMenuHandler/
LEGUI/
LEInstaller/
LEProc/
LEUpdater/
```

- [ ] 確認沒有遺留的臨時檔案

```bash
cd E:/Code/Locale-Emulator
ls _tmp_* 2>/dev/null && echo "警告：有臨時目錄未清理" || echo "OK：無臨時目錄"
ls src/Core/*.bak 2>/dev/null && echo "警告：有備份檔未清理" || echo "OK：無備份檔"
```

- [ ] 確認 Git 狀態

```bash
cd E:/Code/Locale-Emulator
git status
git log --oneline feature/dotnet10-foundation --not master
```

預期 `git log` 輸出（commit 順序由新到舊，數量和內容取決於實際執行結果）：
```
xxxxxxx [Core] spike 成功/失敗：...（如有）
xxxxxxx [Core] 修正搬移後的建置路徑（如有）
xxxxxxx [Core] 解壓 .7z 封存檔並記錄 SHA-256 hash
xxxxxxx [Core] 納入 Locale-Emulator-Core 原始碼 (commit ae7160d)
xxxxxxx chore: 更新 .gitattributes ...
xxxxxxx chore: 更新 .gitignore ...
xxxxxxx chore: 建立 Directory.Build.props ...
xxxxxxx chore: 統一 key.snk 至 repo 根目錄
xxxxxxx chore: 搬移現有 C# 專案至 src/ 目錄
```

- [ ] 確認工作目錄乾淨

```bash
git status
```

預期：
```
On branch feature/dotnet10-foundation
nothing to commit, working tree clean
```

---

## Commit 策略摘要

| Commit | 前綴 | 說明 |
|--------|------|------|
| 1 | `chore:` | 搬移 C# 專案至 src/ |
| 2 | `chore:` | 統一 key.snk |
| 3 | `[Core]` | 納入 upstream Core 原始碼 |
| 4 | `[Core]` | 解壓 .7z 並記錄 SHA-256 hash |
| 5 | `chore:` | 更新 .gitignore |
| 6 | `chore:` | 更新 .gitattributes |
| 7 | `chore:` | 建立 Directory.Build.props |
| 8 | `chore:` | 重建 LocaleEmulator.sln |
| 9 | `[Core]` | 修正建置路徑（如需要） |
| 10 | `[Core]` | v145 spike 結果（如成功） |
| 11 | `chore:` | 修正 C# 參考路徑（如需要） |

`[Core]` 前綴用於所有 Core 相關的變更，便於日後追溯哪些 commit 修改了 upstream 程式碼。

---

## 已知風險與決策點

| 風險 | 影響 | 處理 |
|------|------|------|
| Core `.7z` 解壓後 repo 大幅膨脹 | Git clone 變慢 | 監控大小，必要時啟用 .gitattributes 中的 LFS 規則 |
| `_Compilers` 修改版工具鏈與 VS 2026 不相容 | Core 無法建置 | 先記錄錯誤，嘗試安裝 VS 2015 Build Tools 或調整環境 |
| v145 spike 失敗 | 需繼續依賴 `_Compilers` | 不是阻擋性問題，`_Compilers` 已解壓可用 |
| `.gitignore` negation 規則對 Core 二進位檔無效 | Core 依賴檔案無法被 Git 追蹤 | 改用 `git add -f` 或調整規則範圍 |
| C# 專案搬移後 key.snk 路徑失效導致建置失敗 | 舊式 csproj 無法建置 | 可接受——後續 Plan 會遷移為 SDK-style csproj |

---

## 下一步

Plan 1 完成後，repo 已具備：
- 新的 `src/` 目錄結構
- Core 原始碼（已解壓，可建置）
- 共用建置基礎設施

接下來可平行執行：
- **Plan 2（C++ Shell Extension）**：用原生 C++ 重寫右鍵選單 Shell Extension（x86+x64）
- **Plan 3（.NET 10 Migration）**：將 LECommonLibrary、LEProc、LEGUI 遷移至 .NET 10

兩者互不依賴，可由不同 agent 在獨立 worktree 中同時進行。
