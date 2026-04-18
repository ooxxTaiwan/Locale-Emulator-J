# 設計文件：LEGUI i18n 補全（Issue #21）

> **Issue**: ooxxTaiwan/Locale-Emulator-J#21
> **日期**: 2026-04-19
> **狀態**: 設計確認

---

## 1. 目標與動機

PR 29（Issue #19，LEGUI Shell Extension UI + TabControl 重構）合併進 master 後，`DefaultLanguage.xaml` 新增了大量 i18n key，但 21 個 locale 檔（`ja.xaml`、`zh-TW.xaml` 等）一個都沒跟上。同時 PR 29 在修錯字 `CREATE__SUSPENDED` → `CREATE_SUSPENDED` 時只動到 `DefaultLanguage.xaml`，21 個 locale 仍保留錯字。

本任務目標：

1. **補全所有 locale 的翻譯**，讓非英文使用者看到完整在地化 UI
2. **修正 `CREATE__SUSPENDED` 雙底線錯字** 在 21 locale 的 `WithCREATESUSPENDED`（rename 後為 `WithCreateSuspended`）值內
3. **抽出 LEGUI 程式碼中 20 處寫死的 user-facing 字串** 至 i18n 系統，連帶翻譯
4. **改善 4 個既有 key 命名** 的一致性問題
5. **改善 `WithCreateSuspended` 的 label 文字**（API 常數名搬離 UI label，留在 tooltip）
6. **修正 ShellExtension 翻譯品質**：`Submenu` 在 8 個 locale 翻成各自語言，與其他 13 個 locale 不一致 — 統一為產品名 `Locale Emulator`；同時修 fr / ja / zh-* / th / pt-BR 等的明顯錯誤與不一致
7. **加入完整性測試** 防止未來 i18n 漂移

**範圍校正**：Issue #21 body 原列 9 個 key，實際 gap 為 24 個（PR 29 共新增 24 個 key 至 `DefaultLanguage.xaml`）。本任務以實際 gap 為準。

**範圍邊界**：

- **包含**：LEGUI（WPF）的 i18n 補全 + ShellExtension（C++ COM）22 個 XML 檔的翻譯品質修正
- **不包含**：LEProc / LECommonLibrary 的 i18n（這兩個是 WinForms / 沒有 i18n 基礎建設，需要先做 WPF 遷移再 i18n）→ 已開 Issue #30 追蹤

---

## 2. 決策摘要

| 項目 | 決策 | 理由 |
|------|------|------|
| **範圍** | 同 PR 包：(a) 24 既有 gap key 翻譯、(b) typo 修正、(c) 寫死字串抽出 + 翻譯（15 新 key）、(d) 4 key rename、(e) `WithCreateSuspended` label 改良、(f) 完整性測試 | i18n 完整性一次到位，避免使用者看到「翻譯一半」的 UI |
| **翻譯策略** | LLM 翻譯全部 21 locale，使用者 review zh-TW / zh-CN / ja / en；其他 locale best-effort 上線 | 短 UI 字串 LLM 品質足夠；英文 fallback UX 更差；社群可後續 PR 修訂 |
| **平行化** | Lead-locale-first：先完成 zh-TW，使用者 review glossary，再平行 dispatch 20 agent 翻譯其餘 | glossary 經實戰驗證再放大；使用者 review 門檻前移 |
| **Glossary** | per-locale 子章節排版，鎖定 10 個跨 key 詞彙 | agent 只需 grep 自己的 locale 區段；matrix 對 LLM context 不友善 |
| **AppName 抽取** | 6 個 MessageBox 寫死 `"Locale Emulator"` 抽成單一 `AppName` key，所有 locale 都填 `Locale Emulator` | 產品名 → 不翻譯但允許未來改 |
| **Profile 詞彙** | 不強制統一，沿用各 locale 既有用法（zh-TW 用「設定」、zh-CN 用「配置」、ja 用「プロファイル」） | 改既有用法等於改全部既有翻譯，scope 失控 |
| **CREATE_SUSPENDED 在翻譯內** | 保留原 pattern，API 常數直接嵌入翻譯（如 zh-TW「Start process suspended (for debugging)」內保留英文常數）— 但 label **不**再以常數為主體 | API 常數對開發者可搜尋；label 對使用者要可讀 |
| **WithCreateSuspended label 改良** | 英文值改為 `Start process suspended (for debugging)`，與 `Run as administrator` 並列結構 | 原值 `Create process with CREATE_SUSPENDED` 對非程式設計師無意義；tooltip 已說明用途 |
| **Commit 切分** | 6 commits：rename / 抽字串 / zh-TW / 其他 20 locale / ShellExt 品質修正 / 測試 | rename 機械性可獨立；翻譯按 lead-then-bulk 切；ShellExt 獨立檔；測試最後加避免 bisect 紅燈 |
| **完整性測試擺最後 commit** | 否則 commit 1-4 期間測試會紅 | 避免 git bisect 體驗劣化 |
| **ShellExt 翻譯品質** | 22 檔都已有 4 個 key（無 completeness gap），但 `Submenu` 8 檔翻成各自語言、其他 13 檔保留 `Locale Emulator` — 統一為產品名 | 與 LEGUI 的 `AppName` 決議一致；產品名應跨 locale 一致 |
| **ShellExt 其他品質修正** | 在使用者明確授權下，順手修 fr typo、ja 用詞錯誤、zh-* 與 th 用詞精確化、pt-BR / ind 內部 capitalization 不一致 | 翻譯品質一次到位；不修純 stylistic 偏好 |
| **LEProc i18n** | 不納入本 PR，另開 Issue #30 追蹤（需先 LEProc → WPF 遷移再 i18n） | LEProc 是 WinForms、訊息 rare-trigger、需獨立架構決策 |
| **LEGUI 既有翻譯品質** | Phase B/C 翻譯時，agent 順手檢查負責的 locale 既有翻譯有無明顯錯誤（如 pt-BR `DebugOptions = "Advanced Options"` 沒翻），有就修 | 避免明顯錯誤遺留 |

---

## 3. Section A：高層方案

採方案 2「Prep → Lead locale (zh-TW) → 平行其餘 20 locale」。

### Phase 編排

```
Phase A：LEGUI 原始碼變更（serial，不含翻譯）
  A1. Rename 4 keys
  A2. 抽 15 個寫死字串 → DefaultLanguage 新 key + source code 改 DynamicResource
       + 改良 WithCreateSuspended label 文字
       ↓
Phase B：Lead locale zh-TW（serial）
  - 補 39 個新 key + 修 typo + 套用 4 rename
  - 早期驗證 glossary
       ↓ ⚠ Gate：使用者 review zh-TW 結果再放大
Phase C：平行翻譯 20 locale（LEGUI）
  - dispatch 20 agent，各自負責 1 個 locale
  - agent 順手檢查既有翻譯有無明顯錯誤（pt-BR 未翻譯等）
       ↓
Phase D：ShellExtension i18n 品質修正（serial，由我）
  - Submenu 8 檔統一為 "Locale Emulator"
  - fr / ja / zh-* / th 明顯錯誤修正
  - pt-BR / ind 內部 capitalization 一致性
       ↓
Phase E：加完整性測試（serial，LEGUI 22 檔）
       ↓
Phase F：最終驗證（serial）
```

---

## 4. Section B：從寫死字串抽出的新 key

共 **15 個新 key** 加到 `DefaultLanguage.xaml`，連帶 6 個 `.xaml`/`.cs` 改 `DynamicResource` / `I18n.GetString`：

| # | Key | 英文 default value | placeholder | 使用處 | 動作 |
|---|-----|-------------------|-------------|-------|------|
| 1 | `AppName` | `Locale Emulator` | — | 6 個 MessageBox title（含 `App.xaml.cs:19` unhandled exception handler 補上 title） | 新增 |
| 2 | `Ok` | `OK` | — | InputBox button bOk | 新增；`bCancel` 復用既有 `Cancel` key |
| 3 | `AboutTitle` | `Locale Emulator (ooxxTaiwan fork)` | — | `AboutPanel.xaml:6` | 新增 |
| 4 | `AboutVersion` | `Version ` (含尾空白；後接 `tVersion` 動態文字) | — | `AboutPanel.xaml:9` | 新增 |
| 5 | `AboutOriginalProject` | `Original project` | — | `AboutPanel.xaml:11` | 新增 |
| 6 | `AboutCore` | `Core (archived 2022-04)` | — | `AboutPanel.xaml:18` | 新增 |
| 7 | `AboutThisFork` | `This fork` | — | `AboutPanel.xaml:25` | 新增 |
| 8 | `AboutLicense` | `License: LGPL-3.0` | — | `AboutPanel.xaml:32` | 新增 |
| 9 | `AboutCoreLicense` | `Core portion: LGPL-3.0 / GPL-3.0` | — | `AboutPanel.xaml:33` | 新增 |
| 10 | `AppConfigTitle` | `LEGUI - ` (含尾空白；code 動態接檔名) | — | `AppConfig.xaml:5` Title | 新增 |
| 11 | `GlobalConfigTitle` | `LEGUI GLOBAL` | — | `GlobalConfig.xaml:5` Title | 新增 |
| 12 | `ErrorHomeDirNotWritable` | `Home directory is not writable.\nPlease move LE to another location and try again.\nHome directory: {0}` | `{0}` = path | `App.xaml.cs:68` | 新增 |
| 13 | `ErrorDirNotWritable` | `The directory is not writable.\nPlease use global profile instead.\nCurrent Directory: {0}` | `{0}` = path | `App.xaml.cs:76` | 新增 |
| 14 | `ErrorAdminRequired` | `LEGUI requires administrator privilege to write to the current directory.` | — | `App.xaml.cs:98` | 新增 |
| 15 | `ShortcutDescription` | `Run {0} with Locale Emulator` | `{0}` = filename | `AppConfig.xaml.cs:73` | 新增 |

**Source code 改動處**（6 檔）：

- `App.xaml.cs`：line 19, 68-74, 76-81, 98-101 — MessageBox 改用 `I18n.GetString` + `string.Format`
- `AboutPanel.xaml`：lines 5-33 — 7 個 TextBlock 改 `{DynamicResource ...}`
- `AppConfig.xaml`：line 5 — `Title="{DynamicResource AppConfigTitle}"`，code-behind 改用 `I18n.GetString("AppConfigTitle") + Path.GetFileName(...)`
- `AppConfig.xaml.cs`：line 73, 85, 114 — `SetDescription` 改用 `string.Format`，2 個 MessageBox 改用 `AppName`
- `GlobalConfig.xaml`：line 5 — `Title="{DynamicResource GlobalConfigTitle}"`
- `GlobalConfig.xaml.cs`：line 120 — MessageBox 改用 `AppName`
- `InputBox.xaml`：lines 10-11 — 改 `{DynamicResource Ok}` / `{DynamicResource Cancel}`
- `ShellExtensionPanel.xaml.cs`：line 21 — 改用 `AppName`

---

## 5. Section C：既有 key 重新命名

**4 個 rename**：

| Old key | New key | 原因 | 影響範圍 |
|---------|---------|------|---------|
| `ConfirmDel` | `ConfirmDelete` | 無意義縮寫；其他 key 都用全字 | `AppConfig.xaml.cs:113`、`GlobalConfig.xaml.cs:119`，22 XAML |
| `DebugOptions` | `AdvancedOptions` | 英文值是 "Advanced Options"，key 名誤導開發者 | `ProfileEditorControl.xaml:17`，22 XAML |
| `WithCREATESUSPENDED` | `WithCreateSuspended` | Win32 API 常數屬於 value 不屬於 key 名 | `ProfileEditorControl.xaml:22`，22 XAML |
| `WithCREATESUSPENDEDTip` | `WithCreateSuspendedTip` | 同上 | `ProfileEditorControl.xaml:22`，22 XAML |

**搭配 `WithCreateSuspended` 的英文 value 改良**：

- 舊（`DefaultLanguage.xaml:21`）：`Create process with CREATE_SUSPENDED`
- 新：`Start process suspended (for debugging)`
- Tooltip（`WithCreateSuspendedTip`）維持不變：`Create the target process in suspended state (advanced debugging option).`

21 locale 翻譯時，`WithCreateSuspended` 值需依新意改譯（不是把舊值的 `CREATE__SUSPENDED` 換成 `CREATE_SUSPENDED` 就完事；整句要重寫）。

---

## 6. Section D：Glossary（鎖定詞彙）

10 個跨 key / 高頻詞彙：

| 編號 | 英文 | 出現於 |
|------|------|-------|
| G1 | Install (動詞/名詞) | InstallSuccess, InstallCurrentUser, InstallAllUsers, InstallStatus, Installed, InstallShellExtTitle |
| G2 | Uninstall (動詞) | Uninstall, UninstallSuccess |
| G3 | Shell Extension | InstallShellExtTitle, ShellExt*（共 6 處） |
| G4 | Current User | InstallCurrentUser, ShellExtCurrentUserHeader |
| G5 | All Users | InstallAllUsers, ShellExtAllUsersHeader |
| G6 | Profile | TabProfile + 既有 ShowInMainMenu / SaveAsInstruction（沿用各 locale 既有詞） |
| G7 | About | TabAbout, AboutTitle, About* 系列 |
| G8 | OK (button) | Ok |
| G9 | Restart Explorer | InstallSuccess, UninstallSuccess 內訊息 |
| G10 | Installed / Not Installed | Installed, NotInstalled |

**保留為英文（不翻譯）**：`Locale Emulator`、`LEGUI`、`License: LGPL-3.0`、`LGPL-3.0 / GPL-3.0`、`CREATE_SUSPENDED`（若仍出現於翻譯內文）、URL、檔名 placeholder `{0}`。

### 6.1 Per-locale 詞彙對照

各 locale 的 G1–G10 用詞如下，翻譯時必須遵守。`Profile` 欄位來自該 locale 既有檔的觀察。

#### ca (Català)
- G1 Install: **Instal·lar** (動詞) / **Instal·lació** (名詞)
- G2 Uninstall: **Desinstal·lar**
- G3 Shell Extension: **Extensió de l'Explorador**
- G4 Current User: **Usuari actual**
- G5 All Users: **Tots els usuaris**
- G6 Profile: **perfil** (沿用既有)
- G7 About: **Quant a**
- G8 OK: **D'acord**
- G9 Restart Explorer: **Reiniciar l'Explorador**
- G10 Installed / Not Installed: **Instal·lat** / **No instal·lat**

#### cs (Čeština)
- G1 Install: **Instalovat** / **Instalace**
- G2 Uninstall: **Odinstalovat**
- G3 Shell Extension: **Rozšíření prostředí**
- G4 Current User: **Aktuální uživatel**
- G5 All Users: **Všichni uživatelé**
- G6 Profile: **profil** (沿用)
- G7 About: **O programu**
- G8 OK: **OK**
- G9 Restart Explorer: **Restartujte Explorer**
- G10 Installed / Not Installed: **Nainstalováno** / **Nenainstalováno**

#### de (Deutsch)
- G1 Install: **Installieren** / **Installation**
- G2 Uninstall: **Deinstallieren**
- G3 Shell Extension: **Shell-Erweiterung**
- G4 Current User: **Aktueller Benutzer**
- G5 All Users: **Alle Benutzer**
- G6 Profile: **Profil** (沿用)
- G7 About: **Info**
- G8 OK: **OK**
- G9 Restart Explorer: **Explorer neu starten**
- G10 Installed / Not Installed: **Installiert** / **Nicht installiert**

#### es (Español)
- G1 Install: **Instalar** / **Instalación**
- G2 Uninstall: **Desinstalar**
- G3 Shell Extension: **Extensión del Explorador**
- G4 Current User: **Usuario actual**
- G5 All Users: **Todos los usuarios**
- G6 Profile: **perfil** (沿用)
- G7 About: **Acerca de**
- G8 OK: **Aceptar**
- G9 Restart Explorer: **Reinicie el Explorador**
- G10 Installed / Not Installed: **Instalado** / **No instalado**

#### fr (Français)
- G1 Install: **Installer** / **Installation**
- G2 Uninstall: **Désinstaller**
- G3 Shell Extension: **Extension de l'Explorateur**
- G4 Current User: **Utilisateur actuel**
- G5 All Users: **Tous les utilisateurs**
- G6 Profile: **profil** (沿用)
- G7 About: **À propos**
- G8 OK: **OK**
- G9 Restart Explorer: **Redémarrez l'Explorateur**
- G10 Installed / Not Installed: **Installée** / **Non installée**

#### ind (Bahasa Indonesia)
- G1 Install: **Pasang** / **Pemasangan**
- G2 Uninstall: **Copot**
- G3 Shell Extension: **Ekstensi Shell**
- G4 Current User: **Pengguna saat ini**
- G5 All Users: **Semua pengguna**
- G6 Profile: **profil** (沿用)
- G7 About: **Tentang**
- G8 OK: **OK**
- G9 Restart Explorer: **Mulai ulang Explorer**
- G10 Installed / Not Installed: **Terpasang** / **Belum terpasang**

#### it (Italiano)
- G1 Install: **Installa** / **Installazione**
- G2 Uninstall: **Disinstalla**
- G3 Shell Extension: **Estensione Shell**
- G4 Current User: **Utente corrente**
- G5 All Users: **Tutti gli utenti**
- G6 Profile: **profilo** (沿用)
- G7 About: **Informazioni**
- G8 OK: **OK**
- G9 Restart Explorer: **Riavvia Explorer**
- G10 Installed / Not Installed: **Installata** / **Non installata**

#### ja (日本語)
- G1 Install: **インストール**
- G2 Uninstall: **アンインストール**
- G3 Shell Extension: **シェル拡張**
- G4 Current User: **現在のユーザー**
- G5 All Users: **すべてのユーザー**
- G6 Profile: **プロファイル** (沿用)
- G7 About: **バージョン情報**
- G8 OK: **OK**
- G9 Restart Explorer: **Explorer を再起動**
- G10 Installed / Not Installed: **インストール済み** / **未インストール**

#### ka (ქართული)
- G1 Install: **დაყენება**
- G2 Uninstall: **წაშლა**
- G3 Shell Extension: **გარსის გაფართოება**
- G4 Current User: **მიმდინარე მომხმარებელი**
- G5 All Users: **ყველა მომხმარებელი**
- G6 Profile: **პროფილი** (沿用)
- G7 About: **შესახებ**
- G8 OK: **კარგი**
- G9 Restart Explorer: **გადატვირთეთ Explorer**
- G10 Installed / Not Installed: **დაყენებული** / **არ არის დაყენებული**

#### ko (한국어)
- G1 Install: **설치**
- G2 Uninstall: **제거**
- G3 Shell Extension: **셸 확장**
- G4 Current User: **현재 사용자**
- G5 All Users: **모든 사용자**
- G6 Profile: **프로필** (沿用)
- G7 About: **정보**
- G8 OK: **확인**
- G9 Restart Explorer: **Explorer를 다시 시작**
- G10 Installed / Not Installed: **설치됨** / **설치되지 않음**

#### lt (Lietuvių)
- G1 Install: **Įdiegti** / **Įdiegimas**
- G2 Uninstall: **Pašalinti**
- G3 Shell Extension: **Apvalkalo plėtinys**
- G4 Current User: **Esamas vartotojas**
- G5 All Users: **Visi vartotojai**
- G6 Profile: **profilis** (沿用)
- G7 About: **Apie**
- G8 OK: **Gerai**
- G9 Restart Explorer: **Iš naujo paleiskite Explorer**
- G10 Installed / Not Installed: **Įdiegta** / **Neįdiegta**

#### nb (Norsk Bokmål)
- G1 Install: **Installer** / **Installasjon**
- G2 Uninstall: **Avinstaller**
- G3 Shell Extension: **Skall-utvidelse**
- G4 Current User: **Gjeldende bruker**
- G5 All Users: **Alle brukere**
- G6 Profile: **profil** (沿用)
- G7 About: **Om**
- G8 OK: **OK**
- G9 Restart Explorer: **Start Explorer på nytt**
- G10 Installed / Not Installed: **Installert** / **Ikke installert**

#### nl (Nederlands)
- G1 Install: **Installeren** / **Installatie**
- G2 Uninstall: **Verwijderen**
- G3 Shell Extension: **Shell-extensie**
- G4 Current User: **Huidige gebruiker**
- G5 All Users: **Alle gebruikers**
- G6 Profile: **profiel** (沿用)
- G7 About: **Info**
- G8 OK: **OK**
- G9 Restart Explorer: **Herstart Explorer**
- G10 Installed / Not Installed: **Geïnstalleerd** / **Niet geïnstalleerd**

#### pl (Polski)
- G1 Install: **Zainstaluj** / **Instalacja**
- G2 Uninstall: **Odinstaluj**
- G3 Shell Extension: **Rozszerzenie powłoki**
- G4 Current User: **Bieżący użytkownik**
- G5 All Users: **Wszyscy użytkownicy**
- G6 Profile: **profil** (沿用)
- G7 About: **Informacje**
- G8 OK: **OK**
- G9 Restart Explorer: **Uruchom ponownie Explorer**
- G10 Installed / Not Installed: **Zainstalowano** / **Nie zainstalowano**

#### pt-BR (Português do Brasil)
- G1 Install: **Instalar** / **Instalação**
- G2 Uninstall: **Desinstalar**
- G3 Shell Extension: **Extensão do Shell**
- G4 Current User: **Usuário atual**
- G5 All Users: **Todos os usuários**
- G6 Profile: **perfil** (沿用)
- G7 About: **Sobre**
- G8 OK: **OK**
- G9 Restart Explorer: **Reinicie o Explorer**
- G10 Installed / Not Installed: **Instalada** / **Não instalada**

#### ru (Русский)
- G1 Install: **Установить** / **Установка**
- G2 Uninstall: **Удалить**
- G3 Shell Extension: **Расширение оболочки**
- G4 Current User: **Текущий пользователь**
- G5 All Users: **Все пользователи**
- G6 Profile: **профиль** (沿用)
- G7 About: **О программе**
- G8 OK: **OK**
- G9 Restart Explorer: **Перезапустите Explorer**
- G10 Installed / Not Installed: **Установлено** / **Не установлено**

#### th (ไทย)
- G1 Install: **ติดตั้ง** / **การติดตั้ง**
- G2 Uninstall: **ถอนการติดตั้ง**
- G3 Shell Extension: **ส่วนขยายเชลล์**
- G4 Current User: **ผู้ใช้ปัจจุบัน**
- G5 All Users: **ผู้ใช้ทั้งหมด**
- G6 Profile: **โปรไฟล์** (沿用)
- G7 About: **เกี่ยวกับ**
- G8 OK: **ตกลง**
- G9 Restart Explorer: **รีสตาร์ท Explorer**
- G10 Installed / Not Installed: **ติดตั้งแล้ว** / **ยังไม่ได้ติดตั้ง**

#### tr-TR (Türkçe)
- G1 Install: **Yükle** / **Yükleme**
- G2 Uninstall: **Kaldır**
- G3 Shell Extension: **Kabuk Uzantısı**
- G4 Current User: **Geçerli kullanıcı**
- G5 All Users: **Tüm kullanıcılar**
- G6 Profile: **profil** (沿用)
- G7 About: **Hakkında**
- G8 OK: **Tamam**
- G9 Restart Explorer: **Explorer'ı yeniden başlatın**
- G10 Installed / Not Installed: **Yüklü** / **Yüklü değil**

#### zh-CN (简体中文)
- G1 Install: **安装**
- G2 Uninstall: **卸载**
- G3 Shell Extension: **Shell 扩展**
- G4 Current User: **当前用户**
- G5 All Users: **所有用户**
- G6 Profile: **配置** (沿用)
- G7 About: **关于**
- G8 OK: **确定**
- G9 Restart Explorer: **重启资源管理器**
- G10 Installed / Not Installed: **已安装** / **未安装**

#### zh-HK (繁體中文 — 香港)
- G1 Install: **安裝**
- G2 Uninstall: **解除安裝**
- G3 Shell Extension: **Shell 擴充功能**
- G4 Current User: **目前使用者**
- G5 All Users: **所有使用者**
- G6 Profile: **設定** (沿用)
- G7 About: **關於**
- G8 OK: **確定**
- G9 Restart Explorer: **重新啟動檔案總管**
- G10 Installed / Not Installed: **已安裝** / **未安裝**

#### zh-TW (繁體中文 — 台灣)
- G1 Install: **安裝**
- G2 Uninstall: **解除安裝**
- G3 Shell Extension: **Shell 擴充功能**
- G4 Current User: **目前使用者**
- G5 All Users: **所有使用者**
- G6 Profile: **設定** (沿用)
- G7 About: **關於**
- G8 OK: **確定**
- G9 Restart Explorer: **重新啟動檔案總管**
- G10 Installed / Not Installed: **已安裝** / **未安裝**

---

## 7. Section E：執行流程

### 7.1 Phase A：原始碼變更（serial）

#### A1：Rename 4 keys

修改順序（避免中途編譯失敗）：

1. 先改 `DefaultLanguage.xaml`（key 名）
2. 再改 source code references：
   - `AppConfig.xaml.cs:113`：`"ConfirmDel"` → `"ConfirmDelete"`
   - `GlobalConfig.xaml.cs:119`：`"ConfirmDel"` → `"ConfirmDelete"`
   - `ProfileEditorControl.xaml:17`：`DebugOptions` → `AdvancedOptions`
   - `ProfileEditorControl.xaml:22`：`WithCREATESUSPENDED` → `WithCreateSuspended`、`WithCREATESUSPENDEDTip` → `WithCreateSuspendedTip`
3. 最後 mass rename 21 locale 檔（翻譯尚未補，但 key 名統一）

**注意**：A1 完成後，21 locale 仍保留舊 key 對應的翻譯值（因為只改 key 名沒改翻譯）。`DefaultLanguage` 是新 key 名，因此 runtime lookup 找 `AdvancedOptions` 時會在 locale 找到（已 rename）→ 顯示舊翻譯。OK，沒視覺退化。

**對應 commit 1**：`refactor(LEGUI-i18n): rename 4 keys for naming consistency`

#### A2：抽 15 個寫死字串

按 `Section B` 表格逐項：

1. `DefaultLanguage.xaml`：加 15 個新 key + 改 `WithCreateSuspended` 的英文 value（`Start process suspended (for debugging)`）
2. 改 6 檔 source code 使用 `DynamicResource` / `I18n.GetString`
3. App.xaml.cs 的 3 個 MessageBox 改用 `string.Format(I18n.GetString("ErrorXxx"), path)` 格式
4. `App.xaml.cs:19` 補上 `AppName` title

**完成後狀態**：21 locale 缺新 15 key + 缺原 24 key（合計 **39 個新 key 待補**）+ `WithCreateSuspended` 既有 key 的值需依新意重譯

**對應 commit 2**：`feat(LEGUI-i18n): extract hardcoded user-facing strings to i18n keys`

### 7.2 Phase B：zh-TW lead locale（serial，由我）

修改 `src/LEGUI/Lang/zh-TW.xaml`：

1. 補全所有 `DefaultLanguage.xaml` 中存在但 zh-TW 缺的 key（含 A2 新增的 15 個）
2. 修 `WithCreateSuspended` 值為新意（不是替換 typo，而是依新英文重譯）
3. 套用 4 個 key rename
4. 嚴格遵守 zh-TW glossary（Section 6.1）

**完成後**：commit + 提交 PR 時會通知使用者 review zh-TW，再進 Phase C。

**對應 commit 3**：`feat(LEGUI-i18n): translate new keys to zh-TW + fix CREATE__SUSPENDED typo`

**Gate**：使用者 review zh-TW 結果與 glossary，確認無重大用詞偏差，再放大。

### 7.3 Phase C：平行翻譯 20 locale

dispatch 20 agents 平行，每 agent：

**輸入**：
- spec 路徑（`docs/superpowers/specs/2026-04-19-issue-21-i18n-translations-design.md`）
- 該 locale 代碼（如 `de`）
- 該 locale 既有 .xaml 路徑
- DefaultLanguage.xaml 路徑
- zh-TW.xaml 路徑（作為翻譯結構範本）

**任務**：
1. 讀取自己的 locale 既有檔，繼承所有現有翻譯
2. 補全所有 `DefaultLanguage.xaml` 中缺的 key
3. 套用 4 個 key rename（如該 locale 有舊 key）
4. 修 `WithCreateSuspended` 值為新意
5. 嚴格遵守自己 locale 的 glossary（Section 6.1）
6. **順手檢查既有翻譯有無明顯錯誤**：例如 pt-BR 的 `DebugOptions = "Advanced Options"`（沒翻成葡語）、明顯 typo、明顯 mistranslation。**只修明顯錯誤，不修純 stylistic 偏好。** 若有修，在 commit message 註明。
7. 寫回該 locale 的 .xaml 檔（**只寫自己的，不碰其他 locale**）

**輸出**：完成的該 locale .xaml 檔

**Locale 列表**（20，扣除 zh-TW）：ca, cs, de, es, fr, ind, it, ja, ka, ko, lt, nb, nl, pl, pt-BR, ru, th, tr-TR, zh-CN, zh-HK

**隔離保證**：
- 各 agent 寫不同檔，無共享狀態
- glossary 已凍結在 spec，agent 只讀
- 沒有 cross-file dependency

**對應 commit 4**：`feat(LEGUI-i18n): translate new keys to remaining 20 locales + fix CREATE__SUSPENDED typo`

### 7.4 Phase D：ShellExtension i18n 品質修正（serial，由我）

修改 `src/ShellExtension/Lang/*.xml`（22 檔），執行三類修正：

#### D1：Submenu 統一為產品名 `Locale Emulator`（8 檔）

13 檔已是 `Locale Emulator`，以下 8 檔需 revert 翻譯，與 LEGUI 的 `AppName` 決議一致：

| Locale | 原 Submenu | 改為 |
|--------|-----------|------|
| fr | `Émulateur local` | `Locale Emulator` |
| it | `Emulatore Locale` | `Locale Emulator` |
| ja | `ロケールエミュレータ` | `Locale Emulator` |
| ka | `ლოკალის ემულატორი` | `Locale Emulator` |
| ko | `로케일 에뮬레이터` | `Locale Emulator` |
| lt | `Lokalės emuliatorius` | `Locale Emulator` |
| nl | `Landinstellingen emulator` | `Locale Emulator` |
| ru | `Эмулятор локали` | `Locale Emulator` |

#### D2：明顯錯誤修正

| Locale | Key | 原值 | 改為 | 原因 |
|--------|-----|------|------|------|
| fr | `RunDefault` | `Éxecuter avec le profil de cette application` | `Exécuter avec le profil de cette application` | typo（`Éxecuter` 重音位置錯，正確為 `Exécuter`） |
| fr | `ManageAll` | `Gestion du profil global` | `Modifier la liste des profils globaux` | 原意為 "Edit Global Profile **List**"，原譯漏「list」與「複數」 |
| ja | `ManageAll` | `汎用性プロファイルリストを編集` | `グローバルプロファイル一覧を編集` | `汎用性`（versatility）非 `Global`；`一覧` 較 `リスト` 自然 |
| zh-TW | `ManageAll` | `管理通用設定清單` | `管理全域設定清單` | Microsoft Windows zh-TW 慣用 `全域` 表 Global |
| zh-HK | `ManageAll` | `管理通用設定清單` | `管理全域設定清單` | 同上（HK 也用「全域」） |
| zh-CN | `ManageAll` | `管理通用配置列表` | `管理全局配置列表` | Microsoft Windows zh-CN 慣用 `全局` 表 Global |
| th | `ManageAll` | `แก้ไขโปรไฟล์โดยรวม` | `แก้ไขรายการโปรไฟล์ทั่วไป` | 原譯為「edit profile in general」，缺「list」與「global」精確語意 |

#### D3：內部 capitalization 一致性（pt-BR / ind）

兩 locale 內部 capitalization 不一致（同檔內部分 title case、部分 sentence case），統一為 sentence case（該語言 UI 慣例）：

**pt-BR**：

| Key | 原值 | 改為 |
|-----|------|------|
| `RunDefault` | `Executar com Perfil de Aplicativo` | `Executar com perfil de aplicativo` |
| `ManageApp` | `Modificar Perfil de Aplicativo` | `Modificar perfil de aplicativo` |
| `ManageAll` | `Editar lista Global de Perfis` | `Editar lista global de perfis` |

**ind**（`ManageAll` 既有為 sentence case，無需改；只動 `RunDefault` / `ManageApp` 與其對齊）：

| Key | 原值 | 改為 |
|-----|------|------|
| `RunDefault` | `Jalankan dengan Profil Aplikasi` | `Jalankan dengan profil aplikasi` |
| `ManageApp` | `Ubah Profil Aplikasi` | `Ubah profil aplikasi` |

#### 不修動的部分

- `es / it / pl / tr-TR` 等使用 title case 但內部一致 — 尊重譯者既有風格
- `ka`（Georgian）、其他 locale 的非明顯錯誤 — 保留現狀（避免我母語非該 locale 引入新錯誤）
- 4 個 key 結構（XML schema、key 名）— 不動

**對應 commit 5**：`fix(ShellExt-i18n): standardize Submenu and fix translation quality across 22 locales`

### 7.5 Phase E：完整性測試

新增 `tests/LEGUI.Tests/LocaleCompletenessTests.cs`（見 Section F）。

修改 `tests/LEGUI.Tests/LEGUI.Tests.csproj`：加入 `<None Include="..\..\src\LEGUI\Lang\*.xaml" CopyToOutputDirectory="PreserveNewest" Link="Lang\%(Filename)%(Extension)" />`，將 Lang 拷至測試輸出目錄。

**Note**：本測試僅涵蓋 LEGUI XAML（22 檔）。ShellExt XML（22 檔）目前 4 個 key 完整無 gap，不加自動測試（未來若 ShellExt 主動加新 key，可另行擴展）。

**對應 commit 6**：`test(LEGUI): add i18n key completeness and non-empty value tests`

### 7.6 Phase F：最終驗證

見 Section G。

---

## 8. Section F：測試設計

新增 `tests/LEGUI.Tests/LocaleCompletenessTests.cs`：

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

**設計重點**：
- `[Theory]` + `MemberData` → 每 locale 一個獨立 test（21 個）
- 測試紅燈時可一眼看出哪個 locale 漏哪個 key
- `Directory.GetFiles` 動態掃 `Lang/` → 新增 locale 自動納管
- 用 `XDocument` 解析 XAML，避免引入額外相依
- `LangDir` 使用 `Assembly.Location` 取測試輸出目錄下的 `Lang/`（檔案透過 csproj `<None CopyToOutputDirectory>` 拷貝）

**csproj 修改**（`tests/LEGUI.Tests/LEGUI.Tests.csproj`）：

```xml
<ItemGroup>
  <None Include="..\..\src\LEGUI\Lang\*.xaml"
        CopyToOutputDirectory="PreserveNewest"
        Link="Lang\%(Filename)%(Extension)" />
</ItemGroup>
```

**測試行為**：
- 21 locale × 2 test → 42 個獨立 test 結果
- 既有 26 個 test 不變
- 預期最終 26 + 42 = 68 全綠

---

## 9. Section G：最終驗證

| 步驟 | 命令 / 動作 | 通過條件 |
|------|------------|---------|
| 1 | `dotnet build src/LEGUI/` | 0 warnings, 0 errors |
| 2 | `dotnet test tests/LEGUI.Tests/` | 68 test 全綠 |
| 3 | `dotnet test tests/LECommonLibrary.Tests/` | 既有 test 全綠 |
| 4 | `dotnet test tests/LEProc.Tests/ --arch x86` | 既有 test 全綠 |
| 5 | 視覺 smoke（Windows Sandbox，使用者執行） | UI 在 zh-TW / zh-CN / ja / en 下無英文殘留、無亂碼 |
| 6 | 翻譯品質抽查（使用者執行） | review zh-TW、zh-CN、ja、en 的 .xaml diff |

**視覺 smoke 流程**（依 `reference_windows_sandbox_smoke.md`）：

1. 將 `Build/Release/` 整個拷進 Windows Sandbox
2. Sandbox 內以系統 locale = ja-JP 啟動 LEGUI、screenshot 主視窗 + Shell Extension 分頁 + About 分頁
3. 重複 zh-TW、zh-CN
4. 不污染主機 locale 設定

---

## 10. Section H：Commit 規劃

| # | Commit message | 範圍 | 約變更檔案數 |
|---|----------------|------|------------|
| 1 | `refactor(LEGUI-i18n): rename 4 keys for naming consistency` | DefaultLanguage + 22 locale + 5 source code refs | ~28 |
| 2 | `feat(LEGUI-i18n): extract hardcoded user-facing strings to i18n keys` | DefaultLanguage（+15 key、改 WithCreateSuspended 值）+ 6 個 source code 改 DynamicResource / I18n | ~7 |
| 3 | `feat(LEGUI-i18n): translate new keys to zh-TW + fix CREATE__SUSPENDED typo` | zh-TW.xaml | 1 |
| 4 | `feat(LEGUI-i18n): translate new keys to remaining 20 locales + fix CREATE__SUSPENDED typo` | 20 locale .xaml | 20 |
| 5 | `fix(ShellExt-i18n): standardize Submenu and fix translation quality across 22 locales` | ShellExt 8 檔（Submenu）+ ~7 檔（明顯錯誤）+ pt-BR/ind（capitalization） | ~15（部分檔案有重疊） |
| 6 | `test(LEGUI): add i18n key completeness and non-empty value tests` | 新測試檔 + LEGUI.Tests.csproj | 2 |

**總計 6 commits**。

PR title（中文）：`fix(i18n): 補全 LEGUI 21 locale 翻譯、抽取寫死字串、修正 ShellExt 翻譯品質`

PR body 重點：
- 引用 Issue #21
- 範圍校正說明（從 9 key 擴大到 39 key + ShellExt 品質修正）
- glossary 摘要
- 各 locale 翻譯品質聲明（zh-TW/zh-CN/ja/en 經 review；其餘 best-effort）
- ShellExt Submenu 統一決策（與 LEGUI AppName 一致）
- LEProc i18n 已開 Issue #30 追蹤
- 完整性測試保障未來
- 6 commits 對應結構

---

## 11. 範圍外

- **LEProc 與 LECommonLibrary 的 i18n**：LEProc 是 WinForms（無 i18n 基礎建設）、LECommonLibrary 用 Forms MessageBox。要 i18n 需先把兩者遷移到 WPF（共用 LEGUI 既有 i18n 系統）— 屬於架構決策非翻譯工作，已開 **Issue #30** 追蹤。本 PR 不動。
- **ShellExtension 4 個 key 結構與 schema**：22 檔都已有完整 4 個 key（`RunDefault`/`Submenu`/`ManageApp`/`ManageAll`），無 completeness gap，不新增 key、不改 schema。
- **`IsAdvancedRedirection` / `SaveAsInstruction` / `SavedStatus` / `InstallShellExtTitle` 等其他 key 命名**：見 Section C 已說明保留理由（純 stylistic 不改）。
- **es / it / pl / tr-TR ShellExt 的 title case 譯文**：內部一致，尊重譯者風格，不強制改 sentence case。
- **ka 等我母語不夠強的 locale 的非明顯錯誤**：保留現狀，避免引入新錯誤。
- **追蹤 issue**：本任務不為「best-effort 翻譯」開後續 community-translation 追蹤 issue，社群有意見直接送 PR 修訂（README 已有翻譯指引）。

---

## 12. 決策 Q&A 紀錄

來自 brainstorming 階段的問答決策：

| Q | 議題 | 選項 | 結果 | 理由 |
|---|------|------|------|------|
| Q1 | 範圍擴大（9 → 24 key） | A 全補 / B 守原範圍 / C 分階段 | A | i18n 完整性一次到位 |
| Q2 | 寫死字串納入方式 | A 同 PR / B 分 PR / C 三階段 | A | UI 完整性、避免半翻譯 |
| Q3 | 翻譯品質策略 | A LLM 全翻 + 部分審 / B 只翻可審 / C 全 LLM 不審 | A，不開追蹤 issue | LLM 對短 UI 字串夠用 |
| Q4 | `AppName` key 是否翻譯 | 抽出但所有 locale 都填 `Locale Emulator` | 是 | 產品名慣例，但允許未來改 |
| Q5 | 完整性測試 | 加 / 不加 | 加 | 永久防漂移 |
| Q6 | Glossary 先後 | 先鎖再平行 | 是 | 共享假設凍結後才平行 |
| Q7 | 既有 key rename 範圍 | 4 個 vs 更多 | 4 個（保守） | 純 stylistic 不改 |
| Q8 | `WithCreateSuspended` label 改良 | D1 不動 / D2 改良 | D2 | 看不懂 = 不知如何使用 |
| Q9 | ShellExt 範圍認定 | A 補充 spec 措辭 / B 連翻譯品質一起修 / C 連 LEProc 一起做 | B + C，加上「進一步 stylistic 修正」 | 翻譯相關都算本 issue 範疇；LEProc 另開 Issue #30 |

---

## 13. 待辦清單映射

本 spec 對應 Issue #21 中的工作項目：

- [ ] 翻譯至 21 個語言檔（`src/LEGUI/Lang/*.xaml`） → Section 7.2 + 7.3
- [ ] Key 清單擴大為實際 24 + 新 15 = 39 key → Section 4 + 隱含於 Phase B/C

額外納入：
- [ ] 4 key rename → Section 5
- [ ] `WithCreateSuspended` label 改良 → Section 5
- [ ] 寫死字串抽取（20 處）→ Section 4
- [ ] `CREATE__SUSPENDED` typo 修正（21 locale）→ Section 7.2 + 7.3
- [ ] ShellExt Submenu 統一（8 檔）→ Section 7.4 D1
- [ ] ShellExt 明顯錯誤修正（fr/ja/zh-*/th）→ Section 7.4 D2
- [ ] ShellExt capitalization 一致性（pt-BR/ind）→ Section 7.4 D3
- [ ] 完整性測試 → Section 8

關聯 issue：
- **Issue #30**（LEProc → WPF + i18n）— 本 PR 不處理，已開新 issue 追蹤
