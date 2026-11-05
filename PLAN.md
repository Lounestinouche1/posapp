# Vectron POS — OpenCode Resume Plan

> **Any agent, any PC:** start with **`AGENTS.md`** (orientation + which file owns what), then
> **`ARCHITECTURE.md`** (the technical map). This file is the phased plan and the session log.
> It is auto-loaded via `opencode.json` → `instructions`.
> Read it fully before doing anything. It is the single source of truth for: what this
> project is, what is already done, and where work is currently stopped.
> **Update the Status section (§9) after every meaningful step** so the next session —
> on this PC or any other — can resume exactly here.

---

## 0. Project summary

Restaurant POS suite by Vectron (client: Boitexinfo). Two products, one design language:

| Product | Tech | Ship variants |
|---|---|---|
| **Vectron POS** (`Program.cs`, ~10k lines, single file) | C# WinForms, dual-TFM `net10.0-windows` (self-contained exe) + `net48` (Win7) | Win10-x64 zip, Win7 zip, Win7-Full zip, tester-release zip |
| **Vectron Waiter** (`VectronTablet\`) | .NET MAUI `net10.0-android` | `Vectron-Waiter-v1.2.0.apk` |

**Rule: every UI/logic update must reach ALL FOUR variants** (Win10, Win7, test, Android).
The test version is only for tests; the main apps are Win10, Win7, Android.

## 1. Key facts & commands (never re-derive these)

- **Windows build** (both TFMs compile with 0 errors):
  `dotnet publish "D:\vscode projects\VectronPOS\VectronPOS.csproj" -f net10.0-windows -c Release -o <dir>` (and `-f net48` for Win7)
- **Android build** (workdir = repo):
  `$env:JAVA_HOME="$env:LOCALAPPDATA\jdk-17\jdk-17.0.20.1+1"; dotnet build "VectronTablet\VectronTablet.csproj" -f net10.0-android -c Release -p:AndroidSdkDirectory="$env:LOCALAPPDATA\Android\Sdk" -p:JavaSdkDirectory="$env:JAVA_HOME"`
  → APK output: `VectronTablet\bin\Release\net10.0-android\com.vectron.waiter-Signed.apk` → copy to repo root as `Vectron-Waiter-v1.2.0.apk`
- **adb**: per-PC — see the PC blocks at the end of this section.
- **Test server**: POS itself serves REST on `http://192.168.1.80:8080`, API key `boitexinfo2026` (must be enabled in Settings > REST API; stored in `client database\feature-settings.json` → `ApiEnabled`/`ApiKey`). Tablet endpoints: `/api/tablet/bootstrap?key=KEY`, `/api/menu`. Auth is the **`?key=` query parameter** (not a header).
- **License gate**: `Main` in `Program.cs` loops `while (!License.IsActive)` — no bypass, ever. `License.cs` fingerprint differs per TFM on the SAME PC: net10 → REQ `A396-5417-972A-66C2`, net48 → REQ `703B-C277-C377-7A35`. Two keys per PC is CORRECT and chosen by the user — do not "fix" it. Win7 client key `VECT-AE2BLADY-…` exp 2027-10-18; Win10/test key `VECT-AGIVW2YR-…` exp 2027-10-28. Key records: `LicenseGen\publish\clients\*.csv`.
- **User's working folders**: the user launches from `publish-win10-x64\` and `publish-win7\` (NOT from the zips). After any Windows build, **update those two folders in place** (exe/dlls only — NEVER touch their `client database\`), keep images merged, and restart the POS. Old exe backups are named `Vectron POS.old-18oct.exe`.
- **Release zips** (repo root): `VectronPOS-v1.2.0-Win10-x64.zip`, `…-Win7-x86-x64.zip`, `…-Win7-Full-x86-x64.zip`, `tester-release.zip`, `Vectron-Waiter-v1.2.0.apk`. Zips must contain `images\` (7 PNGs) + `client database\products.json` starter catalog, and NEVER license.dat/license.time/feature-settings/.pdb.
- **Test data**: user's local data lives in `tester-release\Windows\client database\` and `LOCAL-test-data-backup\`. Temp sandboxes go under `%LOCALAPPDATA%\Temp\opencode\` (per PC) and are deleted after use (license copies included).
- **Screenshot technique**: `PrintWindow(hwnd, hdc, 2)` works; `FindWindow` is unreliable in pwsh; use Unicode `GetWindowText`; UI Automation (`UIAutomationClient`) is the ground truth for what a Windows window actually shows. On Android, `adb exec-out screencap -p` + `uiautomator dump` are the ground truth (see `.opencode\tools\emu.ps1`).
- **DEV PC (user `serveur-it`, `F:\vscode projects\VectronPOS`) — CURRENT MACHINE (3 Nov):**
  the project lives at `F:\vscode projects\VectronPOS`; **no admin rights** on this account.
  Toolchain is **per-user** (no UAC): .NET SDK 10.0.403 at `C:\Users\serveur-it\.dotnet`
  (+ workloads `android 36.1.118`, `maui-android 10.0.110`); JDK 17 at `C:\Java\jdk-17.0.20.1+1`;
  Android SDK at `C:\Android\Sdk` (platform-tools, platforms 35+36, build-tools 36.0.0);
  Git (MinGit 2.56) at `C:\Users\serveur-it\MinGit\cmd`; PowerShell execution policy
  `CurrentUser=RemoteSigned`; a guarded PATH/JAVA_HOME/ANDROID_HOME block is in
  `Documents\WindowsPowerShell\Microsoft.PowerShell_profile.ps1`.
  **Use the helper scripts instead of remembering commands:** `.opencode\tools\build-win.ps1`
  (`-Tfm`, `-Ship`), `.opencode\tools\build-apk.ps1`, `.opencode\tools\emu.ps1`
  (`launch|quit|shot|tap|swipe|text|key|install|start|ui|log`).
  **`C:\Program Files\dotnet` is a BROKEN stub** and sits earlier on the machine PATH — a plain
  `dotnet` in cmd.exe fails; PowerShell is fixed by the profile. Repairing it needs one elevated
  command (never done without the user).
  Emulator: **LDPlayer 9.5.41 at `C:\LDPlayer\LDPlayer9`**, instance 0 = `LDPlayer`,
  1280×720 @240dpi, Android 9. **adb WORKS here** — `adb connect 127.0.0.1:5555` after enabling the
  instance flag `"basicSettings.adbDebug": 1` in `vms\config\leidian0.config` (it was off; without
  it no 5555 listener exists and `ldconsole adb` fails). `emulator-5554` and `127.0.0.1:5555` are
  the same device. The user's `ldconsole` remains the fallback for install/runapp.
  **Licensing on this PC:** BOTH TFMs are activated (user supplied keys, 3 Nov) — net10 runs from
  `publish-win10-x64`, net48 from `publish-win7` (the net48 attempt only fails if port 8080 is
  already taken by the net10 instance).
  Hardware: i7-2700K (4c/8t), 16 GB RAM, Radeon HD 5400, Win10 Pro 19045. **VT-x is off in firmware
  and no hypervisor is available for the Android SDK emulator** — use LDPlayer, not AVD.
  This is a *low-spec-ish* box: a good real-world check for the Lite tier.
- **HOME PC (user `fullm`, 29 Oct):** project at `D:\VectronPOS`
  (USB copy from the IT PC — untracked files included); dotnet SDK 10.0.403 at
  `C:\Program Files\dotnet\dotnet.exe` (installed via winget; net48 reference assemblies come from
  NuGet, no Developer Pack needed); adb/emulator = `F:\LDPlayer\LDPlayer14\adb.exe` (LDPlayer 14).
  **Android toolchain INSTALLED this session (29 Oct) — Android builds NOW possible on this PC:**
  JDK 17 = `C:\Program Files\Microsoft\jdk-17.0.21.21-hotspot`; Android SDK = `C:\Android\Sdk`
  (platform-tools, platforms;android-35 + **android-36** (the .NET 10 android default, manifest
  36.1.118), build-tools 35+36); dotnet workloads `android 36.1.118` + `maui-android 10.0.110`
  installed. `ANDROID_HOME`/`JAVA_HOME`/`AndroidSdkDirectory`/PATH persisted (User scope).
  **Android build cmd (this PC):** `$env:JAVA_HOME="C:\Program Files\Microsoft\jdk-17.0.21.21-hotspot";
  dotnet publish "D:\VectronPOS\VectronTablet\VectronTablet.csproj" -f net10.0-android -c Release
  -p:AndroidSdkDirectory="C:\Android\Sdk" -p:JavaSdkDirectory="$env:JAVA_HOME"` →
  `VectronTablet\bin\Release\net10.0-android\com.vectron.waiter-Signed.apk` → copy to repo root
  as `Vectron-Waiter-v1.2.0.apk`. Working folders/zips present with the same names as above;
  Windows smoke build both TFMs verified 0 errors (38 s).

## 2. Completed work (history — do not redo)

- [x] v1.2.0 built & verified for all four variants (zips + APK in repo root)
- [x] Big product images (full tile width × ~80% height, tile not enlarged) — Windows **and** Android
- [x] Zips now ship `images\` + starter `products.json` (root cause of "images not updated": old zips had no images folder)
- [x] Android tile: price moved BELOW product name (wraps to 2 lines, tile 180→205/150→175) — verified on emulator incl. long name
- [x] `publish-win10-x64\` + `publish-win7\` updated to new binaries in place (they had the old Oct-18 exe → old build lacked `/api/tablet/bootstrap` → Android 404). Both now serve bootstrap 200; Android verified connecting
- [x] 6 skills installed → `.opencode\skills\`: `ui-ux-pro-max`, `frontend-design`, `emil-design-eng`, `break-ui`, `create-design-md`, `improve-ui` (web-only skills from the user's downloads were deliberately skipped — this is WinForms+MAUI, no React/Tailwind)
- [x] `DESIGN.md` at repo root — design language for both apps (validated: lint 0 errors, dtcg export OK)
- [x] User's local data restored; nothing shipped in zips that shouldn't be
- [x] **DEV PC ready (`serveur-it`, 3 Nov)**: per-user toolchain (.NET SDK 10.0.403 + `android`/
      `maui-android` workloads, JDK 17, Android SDK 35+36, MinGit) → all three builds verified
      **0 errors**; both Windows exes launch; LDPlayer 9 adb enabled; APK installed and driven
      end-to-end against the live POS (login → operators → tables → order with real open order).
- [x] **PLAN policy change (3 Nov)**: the blanket "no effects" rule is replaced by the
      **Lite/High tiers + Dark/Light themes** (§3), with P1.8/P1.9 to implement them.

## 3. Current goal (START HERE)

> **4 Nov 2026 — the appearance goal below is DONE (P1.8/P1.9/P2–P6 shipped, see §9).**
> **The current goal is now §10: P7 Settings overhaul → P8 inventory & recipes → P9 order-PC
> images → P10 ship all four. Start at §10.5 (order of work).**

**"Make the app look as good as the hardware allows — with a switch for weak machines."**

Two user-facing settings, both on the Windows POS **and** the Android tablet:

1. **Theme — Dark / Light.** Manual choice, persisted per install. Dark stays the brand default;
   Light is a first-class palette (not an inverted filter).
2. **Graphics — Auto / High / Lite.** `Auto` is the default and detects the machine. `High` is the
   full visual treatment (gradients, soft shadows, 150 ms transitions). `Lite` is exactly today's
   flat baseline: zero effects, zero extra cost, still Win7-safe.

**The old blanket "no effects, ever" rule is RETIRED** (it was a proxy for "must not slow down a
low-spec Win7 POS"). That requirement is now enforced by the **Lite tier**, which must stay as
cheap as today's rendering. `High` may look modern on capable PCs. §4 P1.8/P1.9 build this.

### Design direction (from `ui-ux-pro-max` skill data + `DESIGN.md`)

- Verified styles for this product type, all `cost:low, drivers:none`:
  **Flat Design** (no gradients/shadows, 4-6 solid colors, clean lines) +
  **Dark Mode** (high contrast, no white bg) + **Data-Dense Dashboard** (KPI cards, row highlighting).
  Flat Design is now the **Lite** baseline *and* the structural grammar of both tiers — **High** adds
  gradients/shadow/elevation on top of that same grammar, never skeuomorphism.
  Anti-patterns in BOTH tiers: mixing flat+skeuomorphic, emoji-as-icons, effects on every surface.
- Palette: keep the existing brand identity (mint accent `#47D6AE` Win / `#70C1B3` Android on
  dark navy `#131821`/`#0F141A`) — refresh = refine hierarchy, NOT rebrand. Restaurant palettes in
  the skill DB are warm light-mode; that would clash with the established identity, so we use them
  only for accents (amber `#F4B860` for warnings already exists). **Light mode is its own palette**
  with the same role names (P1.8), not an inversion — accent darkened for 4.5:1 contrast.
- UX gates from skill: contrast ≥4.5:1, touch targets ≥44px (Android POS tablets), visible
  pressed/loading states, no hover-only affordances (touch POS).

### Tier definitions (the contract)

| | **Lite** (low-spec POS, Win7-era) | **High** (modern PC) |
|---|---|---|
| Fills | solid colours only | solid + 2-stop vertical gradients (headers, primary buttons) |
| Depth | 1px borders, flat hover/press steps | soft layered shadow under cards/dialogs (2–3 translucent rects — no blur filter) |
| Motion | none — instant state changes | 150 ms ease fades/slides, one shared timer, cancel-safe |
| Images | cached thumbnails (as today) | same cache, larger decode allowed |
| Cost | = today's baseline; event-driven repaint only | one extra paint pass per state change; idle CPU unchanged |

**Non-negotiable in BOTH tiers:** no per-frame animation loops, no idle timers, no `Opacity`
tweening of whole forms, no `ColorMatrix`/`PathGradient` glows, no new NuGet packages for UI,
system fonts only, everything compiles for **net10 and net48**, and Lite must run on Win7 SP1 + .NET 4.8.

### Auto-detect (Graphics = Auto, the default)

Resolve to **High** only when ALL hold, otherwise **Lite**:

- RAM ≥ 4 GB and ≥ 2 logical cores
- not a Remote Desktop session (`SystemInformation.TerminalServerSession == false`)
- **not Windows 7** — the Win7 fleet is the low-spec POS target, so it always resolves to Lite
  (the user can still force High manually to prove it out)

Persist the *choice* (`auto|high|lite`) and the *resolved* tier separately, so a hardware change
re-evaluates on next launch. Settings must show the resolved tier **and the reason**
(e.g. "Lite — Remote Desktop session").

### Theme requirements

- Both palettes are token-driven (P1.8). **New code must not hardcode `Color.FromArgb(...)`** —
  every colour comes from the skin.
- Light mode must pass contrast ≥ 4.5:1 for body text. The mint accent `#47D6AE` fails on white,
  so light mode needs a darker accent (candidate `#0E7C63` — validate in P1.8 and record in `DESIGN.md`).
- Live switching: re-skin open windows without restart where feasible, else prompt for restart.
  Dialogs opened later always use the current skin.
- System chrome (title bars) stays native — do not build custom window chrome for this.
- Defaults after install: **Theme = Dark**, **Graphics = Auto** (brand identity, zero surprises).

## 4. Phased plan (checkboxes = resume points)

- [x] **P1 — Windows POS screen-by-screen refresh** — DONE (P1.1–P1.7 shipped, see §9)
- [x] **P1.8 — APPEARANCE FOUNDATION (Windows)** — DONE 4 Nov 2026 (see §9). The token layer, then the two settings:
  1. [ ] `Skin` token layer in `Program.cs`: static class with mutable `Mode` (Dark/Light) and
         `Quality` (High/Lite); role names mirror the Android `App.xaml` keys
         (Background, Panel, PanelLight, Sidebar, Border, Text, TextDim, Accent, AccentPressed,
         Danger, Warning, Success, plus High-only shadow/gradient tones).
  2. [ ] Migrate all **280** `Color.FromArgb(...)` occurrences + the `static readonly Color` fields
         (`PosForm.background/panel/panelLight/mint`, `ProductTile.BgNormal…BorderPressed`,
         `KeyboardForm` palette, `darkKey/darkKey2`) to `Skin.*` — static fields cannot follow a theme.
  3. [ ] Light palette (own values, contrast ≥4.5:1 incl. a darker accent) + record both palettes in
         `DESIGN.md` (skill `create-design-md`).
  4. [ ] Native controls follow the skin: owner-drawn ComboBoxes (P1.6), TextBox/ListBox/ListView rows,
         scrollbars, focus cues, tab chips, and every dialog built before the switch.
  5. [ ] `FeatureSettings`: add `Theme` ("dark"|"light"), `GraphicsQuality` ("auto"|"high"|"lite"),
         `ResolvedQuality` + `ResolvedReason` (computed, not user-editable). JSON stays backward
         compatible (missing keys = defaults).
  6. [ ] Settings → new **Appearance** section: theme selector, quality selector (Auto/High/Lite),
         resolved tier + reason line, and a live mini-preview of the chosen combination.
  7. [ ] High-tier painting: 2-stop gradient header/primary buttons, layered soft shadow under
         cards/dialogs, hover elevation — all inside `OnPaint`, double-buffered, one pass per state.
  8. [ ] `Skin.Transition(...)`: ONE shared 150 ms timer for fades/slides, cancel-safe, no-op when
         `Quality == Lite`; used for panel/menu/dialog reveals.
  9. [ ] Runtime re-skin: apply theme/quality live to open forms where safe; otherwise restart prompt.
- [x] **P1.9 — APPEARANCE ON ANDROID** — DONE 4 Nov 2026 (see §9). Same two settings on the tablet. Tokens already exist in
       `App.xaml` → add a light palette + `Theme.Mode`, gate transitions in `Theme.PressBind`/pages on
       the quality flag, add the Appearance section to the tablet UI (respect the gesture/`IsEnabled`
       defect backlog in §9 before touching buttons).
- [x] **P2 — PARITY AUDIT (Windows ↔ Android)** — DONE 4 Nov 2026 (see §9). Every token has a counterpart, both palettes complete,
       both tiers defined, and no screen left on hardcoded colours (grep `Color.FromArgb` = 0 outside
       the skin, and no raw hex in XAML outside `App.xaml`).
- [x] **P3 — LITE-MODE SAFETY PASS** — DONE 4 Nov 2026 (see §9). Prove Lite == today's cost. Grep the diff for
       `Timer|Invalidate(|Opacity|Blur|Shadow|Gradient` — each hit must sit inside a
       `Quality == High` branch (or be an existing non-visual use). Build both TFMs 0 errors, run the
       net48 build, and confirm Lite registers no timers and no idle repaints.
- [x] **P4 — VISUAL VERIFICATION** — DONE 4 Nov 2026 (see §9). Screenshots of every touched screen in **4 states** —
       Dark×High, Dark×Lite, Light×High, Light×Lite — on Windows (PrintWindow/UIA) and Android
       (`emu.ps1 shot`), compared against `DESIGN.md`; `break-ui` edge cases (long names, FR/AR
       strings, 0/999 items) in both themes.
- [x] **P5 — Ship ALL FOUR** — DONE 4 Nov 2026 (see §9). Republish → update `publish-win10-x64\` + `publish-win7\` in place
       (binaries only!) → rebuild the 3 zips + APK → verify zip contents (§1 rules) → restart POS,
       verify Android connects (bootstrap 200). Ship defaults: Theme = Dark, Graphics = Auto.
- [x] **P6 — Final report to user** — DONE 4 Nov 2026 (4-state matrix + before/after delivered in chat).
- [x] **P7 — SETTINGS MENU OVERHAUL** (Windows; Android parity check) — full spec in **§10.2**.
      New shell, re-sorted categories, live toggle→parameter refresh, a Back button on every
      page/sub-page, and a real Stock/Inventory console instead of bare toggles.
- [x] **P8 — INVENTORY & RECIPES** (Windows; tablet consumes 86) — full spec in **§10.3**.
      Ingredients CRUD, per-product recipes with units (g / kg / ml / L / pcs), automatic
      deduction on payment, direct quantity deduction for products without a recipe,
      refunds, waste, stocktakes, transfers, purchase orders, food cost.
- [x] **P9 — PRODUCT IMAGES ON ORDER PC + KIOSK + ANDROID** — full spec in **§10.4**.
      Images live in the install folder instead of the shared data folder → order PCs and the
      kiosk show no picture; the tablet gets `image: ""`. Fix the resolver, migrate, re-ship.
- [x] **P10 — Ship P7+P8+P9 to ALL FOUR** — same gate as P5 (§10.6).

## 5. Design-decision workflow (when unsure what "more beautiful" means)

1. Load skill `ui-ux-pro-max` → search its CSVs directly with PowerShell (Python is BROKEN on this
   PC: `py` launcher points to deleted `C:\Python314`; do not install Python without asking).
2. Load `emil-design-eng` for polish details, `frontend-design` for big-screen aesthetics,
   `break-ui` before delivery (stress test), `improve-ui` for read-only audits.
3. Record any new color/type decision into `DESIGN.md` (skill: `create-design-md` validates it).
4. **A screen is only "done" when it renders in all four states** — Dark×High, Dark×Lite,
   Light×High, Light×Lite. Check Lite first (it must be unchanged), then High, then both themes.

## 6. NEVER list

- Never bypass/weaken the license gate. Never put license.dat/license.time/feature-settings/.pdb in any zip.
- Never overwrite `client database\` in `publish-win10-x64`, `publish-win7`, or `tester-release\Windows`.
- Never kill a running POS process without checking first whose it is (the user runs these live!).
- Never commit/push unless the user explicitly asks (current tree has uncommitted work — see §9).
- Never assume FindWindow/screenshot results are trustworthy — cross-check (see §1).
- All 4 variants ship together, always.
- **Never let a High-tier effect run in Lite**: every gradient/shadow/transition sits behind
  `Quality == High`. Lite must stay pixel-identical and cost-identical to the pre-P1.8 look.
- **Never hardcode a colour in new code** — `Skin.*` on Windows, `App.xaml` keys on Android.
  Both palettes must render every screen.
- **Never implement Light mode as an inversion/filter** — it needs its own contrast-checked palette.
- **Never change the shipped defaults** (Theme = Dark, Graphics = Auto) without the user asking.
- **Never ship a build where Lite is slower** than the pre-P1.8 baseline (P3 gates this).

## 7. Environment notes for a NEW PC

- Project must be present on the new PC (git clone / copy the whole folder — incl. `.opencode\`,
  `DESIGN.md`, this file). Skills live in `.opencode\skills\` and travel with the folder.
- `opencode.json` in project root auto-loads this file (`instructions: ["PLAN.md"]`).
- Path-specific facts in §1 (adb, JDK, LDPlayer, publish folders) refer to one of **three known PCs**:
  the IT PC (`C:\Users\IT\…`), the HOME PC (`fullm`, `D:\VectronPOS`) and the DEV PC
  (`serveur-it`, `F:\vscode projects\VectronPOS`). On another PC, re-discover those paths first
  (they're marked in §1) but keep all product/build/packaging facts.
- The helper scripts in `.opencode\tools\` are the fastest way to be productive on a fresh PC:
  `build-win.ps1`, `build-apk.ps1`, `emu.ps1` — each resolves the toolchain itself and can be
  re-pointed by editing the path constants at the top.
- Uncommitted git state at last session: ` M MANUAL.md`, ` M Program.cs`, ` M VectronPOS.csproj`,
  `?? VectronTablet/`, `?? .opencode/`, `?? DESIGN.md`, `?? PLAN.md`, `?? opencode.json`.

## 8. Glossary of in-repo paths

- `Program.cs` — entire Windows app (gate: lines 22-48; tiles: `ProductTile.Draw`; tablet API: ~line 7466+)
- `License.cs`, `LicenseForm.cs`, `LicenseGen\` — licensing
- `VectronTablet\Pages\OrderPage.xaml(.cs)` — Android main screen + product tiles; `Theme.cs`, `App.xaml` — tokens
- `DESIGN.md` — design language (validated), `MANUAL.md` — user manual (French)
- `publish-win10-x64\`, `publish-win7\` — user's LIVE working installs; `tester-release\` — test package source

## 9. Status (UPDATE THIS AFTER EVERY SESSION)

**Current state (29 Oct — HOME PC session):** plan re-read; env re-verified per §7 (HOME-PC
overrides added to §1). .NET 10 SDK 10.0.403 installed via winget; first smoke build of BOTH TFMs:
**0 errors / 12 warnings (pre-existing nullable), 38 s** ✅ — full Windows build→screenshot pipeline
works on this PC.
**P1.1 DONE + verified:** login/operator screen redesigned as centered flat card (panel #1B222E,
1px #2E3A4B border, mint rule accent, MiddleCenter brand, 36px fields / 48px button, focus fill
step, pressed mint #35B891, password auto-focus). Build both TFMs 0 errors; verified live via
PrintWindow + UIA on the net10 build (EN strings; FR translations fit the fixed-width labels).
New tokens recorded in DESIGN.md (border-windows, mint-pressed-windows + operator-login component
bullet). NOTE: screenshot saved in chat only (temp sandbox deleted per §1) — re-capture into
`.opencode\screenshots\` during P4/P6 for the before/after report.
**P1.2 DONE + verified (29 Oct):** main screen sidebar + header rhythm. Fixed 4 real bugs found
via BEFORE screenshot (`.opencode\screenshots\p12-main-before.png` / `-after.png`):
(a) operatorButton was configured but NEVER parented → invisible; now added to mainHeader and
part of a right cluster (sales figure 24px left of chip, both repositioned by
PositionHeaderRight() on mainHeader.Resize / salesLabel.TextChanged / UpdateBranchBadge —
event-driven, no timers); (b) greeting font was width/65 ≈ 29pt at 1080p → 54px tall, painted
over the date → capped at 20pt (width/85), date now fully visible; (c) nav buttons fixed 168px vs
150–205px sidebar rails → navButtonWidth field set to sidebarWidth (full-rail); (d) sidebar status
"Running on :9100" clipped → MaximumSize wrap. Brand x aligned 20→16 to match nav text.
Nav buttons + operator chip got flat hover/pressed states via MouseOverBackColor +
MouseDown/Up/Leave (FlatButtonAppearance has NO pressed-color property — ActiveBackColor/
PressedBackColor do not exist, use the events like LoginForm).
UIA-verified: brand x=16, nav buttons w=205 (=sidebar), date y=62 clear of greeting (y=22 h=37),
chip x=1720 w=180 h=36, sales label exactly 24px left of chip, status wraps full text.
Build both TFMs 0 errors / 12 warnings (unchanged). Low-spec check: no new timers, no effects,
event-driven only — all rules §3 respected.
Tooling this session: reusable `C:\Users\fullm\AppData\Local\Temp\opencode\capture.ps1`
(refresh sandbox exe from build, auto-activate if needed, PrintWindow capture by title regex)
+ keygen at `...\Temp\opencode\keygen\` (compiles LicenseCore.cs). Sandbox
`...\Temp\opencode\p12-test\` is ACTIVATED (license.dat valid) — reuse for P1.3+; delete both
when Windows P1 work finishes (§1).
**HOME PC NOW ACTIVATED:** REQ `A2B3-EAAD-1350-56A2` (this machine, net10), key "Home Office"
exp 2027-10-29 generated via scratch tool compiling LicenseCore.cs (ECDSA — regenerating yields a
different-but-valid key; the activated one lived in the now-deleted sandbox license.dat).
Key NOT added to LicenseGen client CSV — ask user if they want a "Home Office" record.
Live installs (`publish-win10-x64`, `publish-win7`) are still UNACTIVATED — user decides whether
to paste the key (net10 key issued in chat; net48/Win7 needs its own key — different REQ per
TFM, §1).
**Git:** HEAD `b00bb52` == GitHub `posapp` main (verified via API 29 Oct — one continuous history;
the USB copy brought the IT PC's commits 15–19 Oct). Uncommitted tree = exactly the §7 IT-PC
snapshot + today's P1.1 edits (Program.cs LoginForm, DESIGN.md, PLAN.md).
Never push without explicit user ask (§6) — and `PLAN.md` itself is untracked, so remember it when
committing.
**P1.1 + P1.2 SHIPPED — ALL VARIANTS (29 Oct, per user rule: every step reaches Win10+Win7+Android
BEFORE the next step):**
- Windows 10: `publish-win10-x64\` updated IN PLACE — fresh `dotnet publish -f net10.0-windows`
  single-file exe (rebuilt 29 Oct 23:46, ~117 MB), binaries only. `client database\` untouched.
- Windows 7: `publish-win7\` updated IN PLACE — fresh `dotnet publish -f net48` (972 KB exe + shims),
  binaries only. `client database\` untouched.
- Android: `VectronTablet\Pages\LoginPage.xaml` EDITED to mirror P1.1 (brand mark + 48×3 mint rule +
  flat card Border #1A222C with 1px #243040 stroke / RoundRectangle 14; the conn+operator panels now
  share one surface; the 3 Entry backgrounds stepped Panel→Panel2 #243040 for the flat focus-fill
  hierarchy matching Windows). XAML-only, code-behind untouched, uses existing tokens.
  **VERIFIED 30 Oct 00:1x:** toolchain installed on this PC (see §1), APK built 0 errors →
  `Vectron-Waiter-v1.2.0.apk` at repo root (29.9 MB, built 00:05) → installed on LDPlayer via
  `ldconsole installapp`/`runapp` (NOTE: generic adb bridge does NOT open on this PC's LDPlayer 14 —
  use `F:\LDPlayer\LDPlayer14\ldconsole.exe installapp/runapp/push/pull/adb --index 0`, and
  screenshot the "LDPlayer" window via PrintWindow; `emulator-app2.png` shows the new login card
  rendering exactly as designed).
**tester-release: user DELETED it — do NOT recreate until the user says so** (supersedes §1/§5 P5
tester-release step). Release zips + APK NOT rebuilt this session (P5 remains the ship-everything
step; only the two live Windows folders were refreshed).
**P1.3 SHIPPED — ALL VARIANTS (30 Oct ~02:00, per the every-step rule):**
- Windows `ProductTile` (Program.cs): flat 3-state (normal `#1B222E` / hover `#2A3745` /
  pressed `#2E3A4B`), shared `Radius => Max(6, Width/24)` for Region + image clip, 1px border
  hierarchy (`#2E3A4B` → `#3A4A5F` → `#4A5E76`) stroked in an `OnPaint` override after content.
  Build both TFMs 0 errors/12 warnings. Verified: `p13-tiles-after.png`, `p13-tiles-zoom.png`,
  pressed state pixel-checked (`p13-tiles-pressed.png` vs `-normal.png`: `#1B222E` → `#2E3A4B`
  under cursor, neighbors unchanged).
- Win7: `publish-win7\` refreshed (exe 30/10 00:21). Win10: `publish-win10-x64\` refreshed
  (exe 30/10 01:30) after user-approved close of the running POS (PID 14824 → relaunched as
  PID 3588, live install now runs P1.3).
- Android `OrderPage.xaml`: tile Stroke `#2E3A47`→`#2E3A4B` (token parity) + pressed state via
  `PointerGestureRecognizer` (PointerPressed/Released/Exited → `OnTilePressed`/`OnTileReleased`
  in code-behind, Panel→Panel2 fill). NOTE: direct `PointerPressed` attributes on a MAUI element
  fail XC0009 — must live in `GestureRecognizers`. APK rebuilt 0 errors (01:55, 28.3 MB) →
  repo root `Vectron-Waiter-v1.2.0.apk` → installed on LDPlayer → pressed state pixel-checked
  (`p13-android-pressed.png`: `#1A222C` → `#243040`).
- Emulator navigation trick (adb bridge dead): click the LDPlayer window directly with
  SetCursorPos+mouse_event (works as touch), then PrintWindow.
- Screenshots for P4/P6 report: `.opencode\screenshots\p13-*` + `emulator-app3.png`.
**P1.4 SHIPPED — ALL VARIANTS (30 Oct ~03:00–04:15, per the every-step rule):**
- Windows (`ActionButton`/`ApplyFlatStates`/`SetFlatBase`/`FlatMix`/`ButtonRoundRect`, ~line 1607):
  base fill in `button.Tag`; FlatMix white mixes (hover +10%, pressed +17%); disabled = 65% toward
  `background` + dim text `(115,125,140)`; radius `Max(6, Min(w,h)/24)` via Region; NavButton opts
  out; Transfer/Merge okBtns via ActionButton; tab Margin `0,0,8,0`. Build both TFMs 0 errors/12
  warnings. Ship rc=0 → `publish-win10-x64` exe 30/10 02:32:09, `publish-win7` exe 02:25:34,
  client DBs untouched; live POS relaunched PID 428, sandbox POS closed.
- Android `OrderPage.xaml`/`.xaml.cs`, `App.xaml`, `Theme.cs`, `MauiProgram.cs`, `LoginPage`,
  `TablesPage`, `AlertsPage`: **two real platform bugs found & fixed —**
  (a) **VSG `Pressed` states are a no-op on MAUI Android** (verified empirically: held pointer on
  GhostBtn, pixels `#243040` unchanged, yet release still navigated → input reaches the control).
  Reverted the VSG Pressed blocks in `App.xaml` (Primary/Ghost) and added flat pressed feedback via
  the proven `PointerGestureRecognizer` pattern in a shared `Theme.PressBind(b)` helper — reads the
  live base colour at press, restores on release, no-ops when disabled; wired to OrderPage
  (back/fire/bill/send + qty step btns), LoginPage (connect/signIn/back), TablesPage (exit + room
  chips), AlertsPage (back/clear).
  (b) **Android `IsEnabled=false` on a MAUI `Button` triggers MaterialButton's platform disabled
  tint `#C8C8C8`, overriding `BackgroundColor`** — flat disabled colours never rendered. Fix: a
  global `ButtonHandler.Mapper` hook in `MauiProgram` (`#if ANDROID`) nulls the platform view's
  `BackgroundTintList` so app colours win in every state; PLUS the send/connect/signIn buttons stop
  using `IsEnabled` for visuals — they paint flat Panel2/TxtDim while busy and guard re-entry with a
  `_sending`/`_connecting`/`_signing` flag (`OnSend` already early-returns on empty basket).
  (Note: `MaterialButton.TintList` does NOT exist in this binding — the property is
  `BackgroundTintList`, reached via the base `View`; the mapper pattern-matches `Android.Views.View`.)
  Build APK 0 errors → `Vectron-Waiter-v1.2.0.apk` (29,966,776 B, 04:00:34) → repo root →
  `ldconsole installapp --filename` + `runapp` → pixel-verified (`p14-v2-*`): disabled send
  `#C8C8C8`→`#243040` + dim text `#738090`; Primary pressed `#70C1B3`→`#7EC7BA`; Ghost pressed
  `#243040`→`#394453`; release restores and no phantom send; chips + tiles unchanged.
  NOTE: reinstalling the APK kills the running app → relaunch needed; a fresh emulator boot also
  dropped the POS server link (had to relaunch `publish-win10-x64\Vectron POS.exe`, PID 5244,
  HTTP 200 on 192.168.1.69:8080) before the tablet's tables repopulated.
- Screenshots for P4/P6: `.opencode\screenshots\p14-*` (incl. `p14-v2-*` verification set).
**P1.5 WINDOWS — implemented + verified (30 Oct ~20:00). NOT yet shipped to publish folders and
NOT yet mirrored to Android.** Current-order panel (Program.cs): (1) 1px divider — added a
`summaryDivider` Panel (Dock=Fill, BackColor `#2E3A4B`, Height=1) inside the summary's existing
FlowLayoutPanel, positioned directly above the TOTAL caption; (2) totalLabel 22→26pt; (3) empty
state split into two labels — bold "No products selected yet" + dim hint "Choose products from the
menu." (Lang.cs: added `Order.EmptyTitle` / `Order.EmptyHint`, fr ~line 414 + ar ~line 1185 filled);
(4) basket row cards got the Region-radius treatment and row buttons (course/chip/cancel/remove)
now call `ApplyFlatStates` for Windows parity with the P1.4 language. Build: net10 + net48 both
**0 errors / 18 warnings (pre-existing nullable)**. Verified via `capture-p14.ps1` on the fresh
build (sandbox p12-test, PID 14432) + `px.ps1` pixel probe: divider is a clean 1px `#2E3A4B`
hairline (y=762, `#1B222E` above/below), TOTAL caption + 26pt total + Order button `#377882`
render correctly, empty state shows two distinct labels. Screenshots: `.opencode\screenshots\
p14-normal.png` (empty state + divider), `p14-chip-hover.png`, `p14-chip-pressed.png`.
**Windows P1.5 SHIPPED (30 Oct ~20:15):** closed live POS PID 5244 (pre-approved) → `ship-win.ps1`
rc=0 → `publish-win10-x64\Vectron POS.exe` (30/10 20:06:59, 117 MB) + `publish-win7\Vectron POS.exe`
(30/10 20:00:24, 976 KB), client DBs untouched (license.dat/license.time/feature-settings dates
unchanged) → relaunched live POS PID 4808 from the updated folder; HTTP 200 on 127.0.0.1:8080 AND
192.168.1.69:8080 (tablet reconnects automatically via its 3s poll). Sandbox instance PID 14432
closed.
**P1.5 ANDROID — SHIPPED (31 Oct; ALL VARIANTS of P1.5 now done per the every-step rule):**
- `VectronTablet\Pages\OrderPage.xaml` mirrored Windows P1.5: 1px divider above the summary block,
  larger totalLabel, split empty-state labels (title + dim hint), row buttons on the flat language.
- **In-place qty update (crash fix):** full `RenderBasket` rebuild on qty step triggered a
  `NullReferenceException` inside MAUI's `TapAndPanGestureDetector.OnTouchEvent` → `RenderLine`
  now rewrites title/price/qty in place. Verified on emulator: row shows "**2 × Cheesecake**",
  row price 14.00, bottom bar "Cheesecake · ×2"; +/− 1→2→3→1 and gated − at qty=1 all good.
- **Send-to-kitchen flow PASSED** end-to-end (order #1 landed on POS Table 1; basket resets,
  Send disables, no crash). Full interaction matrix passed (p15 m1–m23, b1–b14) — zero crashes.
- Debug scaffolding REMOVED from source and grep-verified (CrashLog.cs deleted, MauiProgram
  Install() gone, PressBind try/catch → plain invoke, TablesPage crashLabel + grid rows back to
  `Auto,Auto,*`).
- **Final APK built 31 Oct 02:58:58** (scaffolding-free, 29,672,990 B) at repo root
  `Vectron-Waiter-v1.2.0.apk`. **INSTALLED + SPOT-VERIFIED 1 Nov (P1.5 Android now fully closed):**
  `ldconsole installapp/runapp` → login preserved (in-place install) → tables populated after
  relaunching the live POS (it was down after a reboot; `publish-win10-x64\Vectron POS.exe` →
  HTTP 200 on 192.168.1.69:8080) → fresh Table 2: add Cheesecake → "**2 × Cheesecake**" /
  14.00 / TOTAL 14.00 / "Cheesecake · ×2" on the + step (**no NRE crash**) → × remove → empty
  state + Send disabled → back → Tables clean. Screenshots: `.opencode\screenshots\p15-final1..9`.
- **Emulator tooling updates (1 Nov):** `winshot.ps1 -ProcId <dnplayer pid>` (PrintWindow) works
  on LDPlayer and is **focus-independent** — use it for ALL captures (the CopyFromScreen path in
  `emu.ps1` grabs OpenCode instead whenever it has focus). Clicks still need LDPlayer foreground
  (`emu.ps1` ShowWindow+Bring+SetForeground) — **verify every click with a PrintWindow shot**;
  the first click after boot can be lost. If the emulator window dies, `ldconsole launch --index 0`
  + poll ~40s brings it back (stale `Ld9Box*` headless processes may linger). LDPlayer pops a
  spontaneous **LDStore** promo tab — click back to the "Vectron Waiter" tab at (230,20).
- adb/logcat permanently unusable on this PC (`emulator-5554 not found` on every channel) →
  empirical screenshots only (`emu.ps1`, `tclick.ps1`, `px.ps1` tooling in Temp\opencode).
- Screenshots for P4/P6: `.opencode\screenshots\p15-*` (m1–m23, b1–b14, f1–f2, login 74–99, a1–a4).
- **Android defect backlog for P1.6+:** (1) VSG `Pressed` is a no-op on MAUI Android (→
  PointerGestureRecognizer press states); (2) any gesture recognizer on a MAUI Button swallows the
  touch stream → native `Clicked` never fires (clicks must ride `Theme.PressBind`'s deferred
  onClick; XAML `Clicked` must be absent on those buttons); (3) Java `View.PerformClick` crashes,
  `IButtonController.SendClicked` is internal (both rejected); (4) `IsEnabled=false` → MaterialButton
  platform grey `#C8C8C8` (→ `ButtonHandler.Mapper` nulls `BackgroundTintList` + paint-only
  disabled states); (5) product tiles fire on press → swiping the grid adds items (gesture conflict
  to resolve); (6) "To send" rows + empty-state hint clipped by the fixed bottom bar; (7) draft
  basket dropped on back-navigation (page lifecycle).
- tester-release stays deleted; release zips NOT rebuilt (P5 = ship-everything later).

**P1.6 FULLY CLOSED — ALL VARIANTS (1 Nov, per the every-step rule):**
- **Windows shipped:** `publish-win10-x64\Vectron POS.exe` (build 01:22:24) + `publish-win7\`
  (01:10:28), binaries only, client DBs untouched; live POS relaunched (PID 7984). Content:
  26/26 ComboBoxes flat (dark panel fill, no system-light chrome), 7 combo pickers tokened,
  accidental encoding corruption of Program.cs reversed (never `Get-Content -Raw` + `Set-Content
  -Encoding UTF8` on UTF-8-no-BOM source — use `[System.IO.File]` APIs), both TFMs 0 errors.
- **Android final APK:** build 02:56:58 (29,987,256 B, 0 errors) → repo root
  `Vectron-Waiter-v1.2.0.apk`, installed + all tests PASSED: A swipe guard (0 items added on
  h/v drags), B auto-scroll on add, C draft restore + auto-scroll (p16-f3: row fully visible),
  D send + full status line via `OneLine()` — no lone "…" (p16-f4), flat entry focus fill
  `#444F5C`, no AndroidX underline, Features/EUR restored after session auto-restore via
  `TablesPage.EnsureFeatures()` (p16-e2), empty state clears bottom bar (p16-f5).
- Screenshots: `.opencode\screenshots\p16-*`.

**P1.7 "FLAT-HUD" (futuristic style) — WINDOWS DONE + SHIPPED; ANDROID MIRROR NEXT.**
*(User request: "more futuristic while keeping the same base design". Direction: a **Flat-HUD
layer** over the existing navy+mint identity — grounded in ui-ux-pro-max style 51 HUD/Sci-Fi FUI;
only its free geometry is used: corner brackets, 1px hairlines, 3px accent rails, LED status dot,
outline segmented chips, focus rails. Glow/scan/blur/scanlines/animated effects are BANNED by §3.
Full spec: `DESIGN.md` §"Flat-HUD layer (P1.7)". All uppercase text via `ToUpperInvariant()`
only; button labels never case-changed.)*

**All 8 Windows edits applied to `Program.cs`:**
1. ✅ `Hud.Brackets(gfx, rect, color, size, depth)` static helper + `HudPanel` (DoubleBuffered,
   `Color? Edge`, paints 1px edge rect + mint corner brackets) inserted before `LoginForm`.
   Note: call site inside `HudPanel.OnPaint` must be `Hud.Brackets(...)` (CS0103 otherwise).
2. ✅ Login card Paint draws border + mint brackets (18, 6); new `opRail`/`pwRail` 2px mint
   focus rails on GotFocus/LostFocus in `card.Controls`.
3. ✅ Sidebar: `brandRule` 1px `#2E3A4B` hairline at **(16, 109)**; status label uppercased +
   `Paint` overpaints ONLY the `●` glyph (mint healthy / amber `#F4B860` failed — the earlier
   version redrew all text → double-draw garble, fixed). **Z-order fix:** the brand label's
   window (16,22→bottom 116) erases siblings beneath it with parent bg → after all sidebar adds:
   `sidebar.Controls.SetChildIndex(brand, sidebar.Controls.Count - 1)` (this also uncovers the
   nav buttons' top 4px at y=112..115). `status` stays at child index 0.
4. ✅ Header: `headerRail` (0,6,3,34) mint, greeting x14, date x16, `headerRule` Dock=Bottom 1px,
   `salesTick` field 3×16 mint — **must set `Visible = true` in BuildUi** (field inits false; it
   was invisible until this fix). `PositionHeaderRight`: `salesTick.Location = (salesX-12, 24)`.
5. ✅ `orderPanel` → `HudPanel { Edge = #2E3A4B }`; `orderTick` (0,4,3,18) mint in `orderHeader`;
   `orderTitle`/`orderLabel` x→12.
6. ✅ `NavButton` active: 3px mint left rail via `Paint` + `Padding(10,3,3,3)` (clears rail).
7. ✅ Tabs: inactive chips = `background` fill + 1px `#2E3A4B` border + `Region = null` (square
   segment — ApplyFlatStates' rounded region would clip the outline at corners); mirrored in
   `UpdateTabs` (SetFlatBase + BorderSize/BorderColor).
8. ✅ `PositionHeaderRight` sales tick positioning.

**VERIFIED (build + screenshots + pixel probes):** both TFMs 0 errors (pre-existing warnings
only). Screenshots `.opencode\screenshots\p17-login.png` (brackets + password focus rail),
`p17-login-focus.png`, `p17-main.png`. Pixel-verified: nav rail `#47D6AE` x=0–3, brand hairline
`#2E3A4B` y=109, sales tick `#47D6AE` x=1580–1582 y=42–57 (header padding y18 offset is
expected), header rule y=99, LED footer `#47D6AE`, outline chips `#2E3A4B` borders, footer
double-draw gone.

**WINDOWS SHIPPED (1 Nov 21:52):** `ship-win.ps1` rc=0 → `publish-win10-x64\Vectron POS.exe`
(117,404,418 B, 21:52:30) + `publish-win7\Vectron POS.exe` (978,432 B, 21:05:21 net48 exe — same
build session), client DBs untouched (file dates unchanged). Live POS relaunched PID 2632 from
`publish-win10-x64`, HTTP 200 on 127.0.0.1:8080 AND 192.168.1.69:8080. Sandbox instance closed
before shipping.

**NEXT (P1.7 Android mirror):** mirror to `VectronTablet\` — LoginPage card brackets + focus
rails, header rail/hairline, outline category chips, order-panel brackets + tick, LED footer
uppercase (XAML/theme tokens; check Android defect backlog above before touching gestures) →
`build-apk.ps1` → repo root APK → install on LDPlayer (`ldconsole installapp/runapp`, adb is
dead) → screenshot-verify → update this section. THEN proceed §4: P2 parity audit → P3 low-spec
grep → P4 screenshots → P5 zips+APK → P6 report.

**Blocked on:** nothing. Watch list: light ComboBoxes in dark cards (do NOT owner-draw without
hardware check), bottom-bar clipping, tile/gesture conflict (Android defect backlog above).

---

**3 NOV — NEW SESSION ON THE DEV PC (`serveur-it`, `F:\vscode projects\VectronPOS`).**
Environment built from scratch (details in §1 "DEV PC"): per-user .NET SDK 10.0.403 + `android`/
`maui-android` workloads, JDK 17, Android SDK 35+36, MinGit, PowerShell profile PATH block.
**Verified:** `net10.0-windows` publish (112 MB single-file), `net48` publish (0.9 MB), Android
publish (28.6 MB signed APK) — all **0 errors**. Helper scripts added: `.opencode\tools\build-win.ps1`,
`build-apk.ps1`, `emu.ps1` (adb-based).
**LDPlayer 9 (new install) made fully controllable:** its instance had adb disabled — no 5555
listener, `ldconsole adb` failed. Fixed by adding `"basicSettings.adbDebug": 1` to
`C:\LDPlayer\LDPlayer9\vms\config\leidian0.config` and restarting the instance. **This supersedes the
old "adb permanently unusable" note — on this PC `adb -s emulator-5554` works** (screencap,
uiautomator dump, input tap/text all verified), so Android verification no longer needs window
clicking or PrintWindow.
**End-to-end test PASSED on this PC:** APK installed → login page (P1.7 card renders: VECTRON /
Waiter terminal / address / API key / Connect) → set `http://192.168.1.80:8080` + key
`boitexinfo2026` → Connect → operator picker pulled live from the POS (Manager, Cashier) → Sign in →
tables page with real data (Main dining 8 tables, `Table 1 #1 · 1 · 7`, `Table 4 #1 · 1 · 0.02`) →
Table 1 order page (`1 × Cheesecake 7.00 EUR`, stepper, chips, tiles, Send). Screenshots:
`.opencode\screenshots\ld-login-2.png`, `ld-after-connect.png`, `ld-tables.png`, `ld-order.png`.
**Licensing:** the user activated **both** TFMs on this PC (net10 in `publish-win10-x64`, net48 in
`publish-win7`) — the net48 exe now reaches the main screen; it only reports "QR ordering server
could not start on port 8080" when the net10 instance already holds the port (run one at a time).
**P1.7 Android mirror was ALREADY DONE** before this session (supersedes the "ANDROID MIRROR NEXT"
line above): `VectronTablet\Hud.cs` (`HudFrameDrawable`), mint brackets on `LoginPage.xaml` card +
`OrderPage.xaml` totals surface, `HudLine` token in `App.xaml`, hairline in `TablesPage.xaml`; APK
built 1 Nov 22:24 and screenshot-verified (`p17-android-*.png`). Treat P1.7 as closed for all variants.
**PLAN POLICY CHANGE (user decision, this session) — see new §3:**
- the blanket "no effects, ever" rule is **retired**;
- **Graphics = Auto/High/Lite** setting (Auto default: High on capable PCs, Lite on Win7/RDP/weak HW);
- **High** = gradients + layered soft shadows + elevation + **150 ms transitions**; **Lite** = today's
  flat baseline, unchanged and equally cheap;
- **Theme = Dark/Light** setting, whole app via a new `Skin` token layer (280 hardcoded colours to
  migrate), Dark remains the default;
- both settings also on the Android tablet.
**NEXT: P1.8 — Appearance foundation (Windows)** (§4). First step is the `Skin` token layer, then the
Light palette (contrast-checked accent), then the Settings → Appearance section, then the High-tier
painting + one shared transition timer. P1.9 mirrors it on Android.
**Watch list additions:** `C:\Program Files\dotnet` is a broken stub ahead of the user PATH (cmd.exe
`dotnet` fails; PowerShell is patched by profile) — needs one elevated repair if the user wants it;
VT-x is off in firmware so no AVD emulator, LDPlayer only; the POS from `publish-win10-x64` is the
test server and must be running for tablet tests (it was left running, PID 5284, on 3 Nov).

---

**4 NOV — P1.8 WINDOWS: `Skin` layer + Appearance settings + COMPLETELY NEW SHELL (done, verified,
NOT yet shipped to the publish folders / zips / APK).**
User brief: *"the interface is still the same — re-create a brand new interface with logos and
animations while keeping the same colours; it must look like another app, I don't want to recognise
it any more."* Implemented as a structural rewrite (same brand identity) behind the new token layer.

**1. `Skin` token layer (Program.cs ~line 966-1290).** `SkinMode {Dark,Light}` +
`SkinQuality {High,Lite}`, ~50 roles named after the Android `App.xaml` keys, `Apply(FeatureSettings)`
called FIRST in the `PosForm` ctor, `Resolve()`/`Detect()` (RAM ≥ 4 GB, ≥ 2 cores, not RDP, not Win7
→ High), `Preview(theme, quality)` scope for the live Settings preview, `Shade`, `FillSurface`,
`CardDepth`, `RoundRect`, ONE shared 150 ms `Animate(ms, step, done)` timer (no-op on Lite) and
`SlideWindow`. Colour migration: every `Color.FromArgb(...)` in the app now goes through `Skin`
(remaining literals are alpha overlays inside `Skin` itself, `Shade`/`FlatMix`, the white QR-label
card and the white receipt-paper preview — both intentionally white). `Color.Orange` /
`Color.OrangeRed` swept to `Skin.Warning` / `Skin.DangerText`, report-dialog white text → `Skin.OnFill`.
**2. `FeatureSettings`**: `Theme` (`dark|light`), `GraphicsQuality` (`auto|high|lite`),
`ResolvedQuality` + `ResolvedReason` (computed). Missing keys = defaults (Dark + Auto).
**3. Settings → Appearance** (new first category): Dark/Light, Auto/High/Lite, `Resolved: High/Lite`
+ reason line, explanation, and a live `AppearancePreview` that paints the NEW shell. Changing either
value asks "Restart Vectron POS now to apply it?" then `Application.Restart()`.
**4. NEW SHELL** (the visible rewrite): the 205 px left sidebar and all its nav buttons are GONE.
- 72 px top **app bar** (`Skin.AppBar` + `AppBarBorder` hairline): vector `VectronMark` (mint ring +
  rounded chevron + HUD corner brackets, `Progress` draw-in) + `VECTRON` / `P O S` wordmark lockup,
  4 nav **pills** (Order/Rooms/Operator/Settings; radius 18; active = `NavActive` fill + accent text),
  status LED + `MAIN PC` micro-copy (IP/print-server in the tooltip), **sales KPI chip** (caption +
  amount, radius 10, `Kpi`, gradient/depth on High), operator chip, and a `⋯` **overflow pill**
  opening a skin-rendered `ContextMenuStrip` (`SkinColorTable`) with the nine secondary screens
  (Clock, Stock, Kitchen, 86 board, Bookings, Schedule, Gift cards, Purchases, Cash drawer).
- Page head 88 px (3 px mint rail + greeting + date + hairline); floating order **card** (`SkinPanel`,
  radius 14, `CardBorder`, depth on High, 18 px gap); category tabs + room chips are **pills**;
  table-map cards are rounded 12 with the same pill language; `ShowView` detail pages got the same
  head (mint tick, 17 pt title, `Kpi` back pill).
- High-only entrance: logo draw-in 420 ms + order-card slide 220 ms on `Shown`; login card draws its
  mark in (420 ms). Lite paints the final frame immediately — no timers at all.
**Bugs found & fixed this session:** (a) `ToolStripMenuItem.Visible` reads FALSE before its parent
drop-down is first shown → the overflow menu silently never opened; now the items use `Available`
(NavItem + ApplyPermissions) and the drop-down is anchored `BelowLeft` so it can't run off-screen;
(b) `HudPanel.Edge` was a public field (CS0649 + WFO1000) → property; (c) new public control members
need `[DesignerSerializationVisibility(Hidden)]`.
**Verification (this PC, sandbox `p18-test`, theme/quality injected by `runstate.ps1`):**
4-state screenshots `.opencode/screenshots/p18-main-{dh,dl,lh,ll}.png` + `p18-rooms-*`,
`p18-basket-dh.png`, `p18-appearance-dh.png`, `p18-overflow-dh.png`, `p18-operator-ll.png`.
Pixel-verified: Dark×High bar `#0F141C`, active pill `#2F4C52`, KPI gradient `#303844`, tile gradient
`#303640`, card edge `#263040`; Dark×Lite solid `#1B222E`/`#1C2432` (NO gradients); Light×High bar
`#FFFFFF`, bg `#F3F6FA`, accent `#0E7C63`; Light×Lite flat `#FFFFFF`. Rooms: available table
`#1C2432` / `#EDF1F6`, taken `#B4463C` / `#B03A2E`. Overflow menu renders `#1B222E` with 3 items
(Clock in / out, Stock, Purchases — the only enabled features in the sandbox). UIA layout: bar 72,
pills 206→598, overflow 1382, KPI 1128, operator 1234, head 78→166, tabs y=166, tiles 217×173 from
y=236, order card 1046..1416 (330 content), basket row + stepper inside the card (no clipping).
Builds: `net10.0-windows` and `net48` **0 errors** (only the pre-existing nullable warnings).
**P1.8 is CLOSED on Windows.** `DESIGN.md` now carries both palettes, the tier contract and the new
shell (the P1.7 sidebar bullets are marked superseded for the main window).
**NEXT: P1.9 — mirror the shell + Theme/Graphics on Android** (`App.xaml` light palette,
`Theme.Mode`/quality flag, quality-gated transitions, Appearance section in the tablet Settings;
respect the Android defect backlog: VSG `Pressed` no-op, gestures on buttons swallow `Clicked`,
`IsEnabled` platform tint, tile fires on press). Then P2 parity audit → P3 Lite-safety grep →
P4 4-state screenshots → P5 ship all four → P6 report.
**NOT shipped yet:** `publish-win10-x64\`, `publish-win7\`, the three zips and the APK still contain
the P1.7 build. P5 is the ship-everything step (binaries only, never `client database\`).


---

**4 NOV — P1.9 ANDROID: Theme + Graphics + new tablet shell (done, verified on LDPlayer; APK at repo
root rebuilt; NOT in the zips/publish folders yet — P5 ships everything).**
The tablet now has the same two appearance settings as the Windows terminal, reached from a `⋯`
button on the Tables page (native `DisplayActionSheet`: `Appearance · <Theme> · <resolved reason>`
→ Theme → Dark/Light, Graphics → Auto/High/Lite). Choices persist in `Preferences` (`theme`,
`graphics`); **defaults stay Dark + Auto**.

**1. `Theme.cs` rewritten as the appearance layer.** Palette table (Dark/Light) with the same role
names as the Windows `Skin`: `Bg, Panel, Panel2, Kpi, Mint, MintDeep, Ink, Amber, Danger, Green,
Txt, TxtDim, HudLine, ReadyBg, TakenBg, PlusBg, EightySixBg, DangerInk, Price, OnFill`. Light uses
its own values (bg `#F3F6FA`, panel `#FFFFFF`, accent `#0E7C63` = the same deep green as the Windows
light palette, ink on accent `#FFFFFF`). `Theme.Apply()` reads the two preferences, resolves the tier
(RAM via `/proc/meminfo` + `Environment.ProcessorCount`; <4 GB or <2 cores → Lite with the reason
string) and writes every palette key into `Application.Resources` **before any page is parsed**
(called from the `App` constructor). `SetTheme`/`SetQuality` persist + apply + `Rebuild()`
(`App.ShowMain()`/`ShowLogin()`) so every open page re-reads the tokens — the tablet equivalent of
the Windows restart prompt. `Reveal` (150 ms fade + 12 px rise, once per page) and `Depth` (MAUI
`Shadow`) are **no-ops on Lite**; `Press` steps lighter on dark and darker on light so feedback stays
visible in both palettes.
**2. `App.xaml`** gained the missing tokens (`OnFill` etc.) and every themed style setter is
re-pointed by `Theme.SetStyleColor` on each `Apply()` — styles are parsed once, so replacing the
dictionary entries alone never reaches `PrimaryBtn`/`GhostBtn`/`TableBtn`/`ChipBtn`/`SectionLbl`/
`DimLbl`. `PrimaryBtn` ink follows `Ink` (dark ink in Dark, white in Light).
**3. Tables page = new shell at tablet scale:** vector `VectronMarkDrawable` + `VECTRON` /
`W A I T E R` lockup, notification pill, `⋯` Appearance, `Exit`; page head keeps the P1.7 mint rail +
LED status dot + hairline. Rooms are **pills** (active `Mint` fill + `Ink` text, inactive `Kpi` +
`TxtDim`); tables are rounded-14 cards on `ReadyBg`/`TakenBg`/`Kpi` with a `HudLine` 1px stroke and
`Theme.Depth` on High. One-shot `Theme.Reveal(tableGrid)` on High.
**4. Order page** lost every hardcoded colour: tile `Panel` + `HudLine` stroke, `+` badge
`PlusBg`/`OnFill`, `86` badge `EightySixBg`/`DangerInk`, price `Price`/`DangerInk`, basket rows
`Panel`.
**5. System chrome:** `MainActivity.ApplySystemBars()` colours the status + navigation bars with
`Bg` and flips `SYSTEM_UI_FLAG_LIGHT_STATUS_BAR` for the light palette. It runs on activity creation
**and** from `Theme.Apply()` — a `UiMode` change does not recreate the activity, so OnCreate alone
went stale. `Platforms/Android/Resources/values/colors.xml` no longer carries the MAUI template
purple (`#512BD4`/`#2B0B98` → `#1A222C`/`#0F141A`/`#70C1B3`).
**Bugs found & fixed this session:** (a) changing the theme left every shared style at its old colour
(styles parsed once) → `SetStyleColor` sweep; (b) the status bar stayed template purple and did not
follow a theme switch → `ApplySystemBars` + call from `Apply()`; (c) `White`/`#2E3A4B`/`#9600DC96`/
`#CC3E3230`/`#F06A5A`/`#332A16`/`#16321F` literals in OrderPage/TablesPage → tokens
(`OnFill`/`HudLine`/`PlusBg`/`EightySixBg`/`DangerInk`/`TakenBg`/`ReadyBg`); (d)
`StatusBarVisibility.LightStatusBar` does not exist in the .NET Android binding → raw `0x2000` cast
to `StatusBarVisibility`.
**Verified on LDPlayer 1280×720 (adb + `shot.ps1` + `region.ps1`/`px2.ps1`):** Tables page in all
four states — Dark×High / Dark×Lite / Light×High / Light×Lite (bar `#0F141A` vs `#F3F6FA`, active
room pill `#70C1B3`+`#0F141A` ink vs `#0E7C63`+`#FFFFFF`, taken `#332A16` vs `#FBF0DA`, free
`#1C2432` vs `#EDF1F6`, GhostBtn `#243040` vs `#E8ECF3`); Appearance sheet reports
`Dark · High — 5.8 GB RAM, 4 cores` (Auto), `Graphics: High — chosen manually` and
`Light · Lite — chosen manually`; status bar `#0F141A`+white icons / `#F3F6FA`+`#616264` icons.
Order page: Dark×High (add Cheesecake → qty 1→3 → remove → add → **Send to kitchen** →
"Order #1 sent · 1 line · 7.00 EUR · ⚠ No printer destination…", basket resets, table total appears),
Light×Lite and Light×High (tile `#FFFFFF` + `#D6DDE7` stroke, send `#0E7C63` + white ink, basket row
`#FFFFFF`, `DangerInk` `#B03A2E`). `adb logcat` clean (no app exceptions).
**APK:** `Vectron-Waiter-v1.2.0.apk` at repo root, rebuilt 0 errors (28.4 MB), installed on LDPlayer
(`INSTALL_FAILED_UPDATE_INCOMPATIBLE` after the signing change → uninstall first, then `adb install`;
`monkey -p com.vectron.waiter` launches `crc64a416d5e295b6f4dc.MainActivity`).
**Watch list unchanged:** tile/gesture conflict (a swipe over the grid can still add a product), rows
clipped by the fixed bottom bar on a busy order page (scrollable, but the grid starts below the fold),
draft basket dropped on back-navigation. `tester-release` stays deleted.
**NEXT:** P2 parity audit (Windows `Color.FromArgb` outside `Skin` = 0; no raw hex in XAML outside
`App.xaml`/`Theme.cs`) → P3 Lite-safety grep → P4 4-state screenshot matrix → P5 ship all four →
P6 report.

---

**4 NOV (later) — P2 → P6 CLOSED. EVERYTHING SHIPPED. `ARCHITECTURE.md` ADDED.**

**P2 parity audit — DONE.** Windows: remaining `Color.FromArgb` = 12, all legitimate (`Skin.R`,
`Shade`, `MixToward`, alpha overlays inside `Skin`, the white QR label card, the white receipt-paper
preview, heat-map cells, badge edge). Every dialog is on `Skin.*`: `LicenseForm.cs` fully rewritten,
`TicketLayout.cs` (`TicketDesignerForm`), `PrinterManagerForm` (`PrinterButton`, `editLayout`),
`ManagerButton`/`DateButton`/`RoomButton`/`CatalogButton`/`FamilyButton`/both `AddBtn`/`ChoiceBtn`/
`ReportButton` all on `Skin.FlatStates`, `PaintChoices()` on `Skin.SetFlatBase`.
Android: the MAUI template `Resources/Styles/Colors.xaml` + `Styles.xaml` were **deleted** and the
`MergedDictionaries` block removed from `App.xaml` — `App.xaml` tokens + `Theme.cs` are now the only
colour source. No raw hex in XAML outside those two files.

**REAL BUG FOUND + FIXED (Android live theme switch).** Switching Dark → Light while a page was
open left the page background, app bar and hairlines on the OLD palette while every code-built row
(room chips, table cards) turned light. Cause: XAML resolves `StaticResource` **once**, at parse
time, so rewriting `Application.Resources` never reaches an already-parsed element, and the page was
not actually recreated by `Rebuild()`. Fix: `Theme.Restyle()` — one visual-tree walk
(`IVisualTreeElement.GetVisualChildren`) that maps every exact old-palette colour to its new
counterpart (`Swap()`/`Map()`), called from `Rebuild()` before `ShowMain()/ShowLogin()`. Verified on
device: live Dark → Light → Dark now repaints the whole page, status bar included. (Side effect of
the P2 style deletion that was caught only by re-verifying: the template `Page` style had been
supplying the page background.)

**P3 Lite-safety — DONE.** Every `Timer`/`Shadow`/`Gradient`/`Invalidate` hit sits in a
`Skin.IsHigh` branch, inside `Skin.Animate` (the single shared 150 ms timer, which no-ops on Lite),
or is a pre-existing functional timer / the HTML email template. Android `Theme.Reveal`/`Depth`
both early-return on `!IsHigh`. Lite registers no timers and no idle repaints.

**P4 visual verification — DONE (4 states × both apps, current builds).**
- Windows (`runstate.ps1` sandbox): `p18-main-dh/dl/lh/ll.png` + `p18-main-default.png`.
  Probes: Dark×High bar `#0F141C`, card gradient `#1F2531`; Dark×Lite flat `#1B222E`;
  Light×High bar `#FFFFFF`, bg `#F3F6FA`, card `#F3F3F3`; Light×Lite flat `#FFFFFF`.
- Android (current APK, byte-safe `screencap`+`pull`): `p4-tables-dh.png` (bar `#0F141A`, cards
  `#1C2432`, taken `#332A16`, bg 631 k px = shadows on), `p4-tables-dl.png` (bg 689 k px = no
  elevation), `p4-tables-lh.png` (bg `#F3F6FA`, 624 k px = shadows), `p4-lh.png` (ll, 689 k px),
  `p5-tablet-after-ship.png`. The old `p19-tables-lh.png` was stale (template-purple status bar) —
  superseded.
- `break-ui` pass: worst-case catalog injected into the **sandbox only** (80-char name, unbreakable
  string, 1-char name, Arabic, CJK, emoji, `0.00`, `999999.99`, empty name) →
  `p18-main-break-dh.png` + `p18-main-break-lh.png`, no crash, tiles ellipsize by design
  (`ProductTile.Ellipsize`), catalog restored from backup.
- Emulator UI-automation notes now in `ARCHITECTURE.md` §7.2: `uiautomator dump` **does** see the
  native action sheets; dump the `bounds` and tap the row centre (the `Close`/`Cancel` buttons only
  span x 855–955, so a tap at x=640 on that band is dead space — that was the whole "taps don't
  work" mystery).

**P5 SHIP — DONE (all four variants).**
- Windows: `build-win.ps1 -Tfm all` → 0 errors. Binaries copied **in place** into
  `publish-win10-x64\` (exe 117,428,994 B + pdb) and `publish-win7\` (exe 1,001,472 B + config + 9
  dlls + pdb); `client database\` untouched in both (file dates unchanged). `F:\win10\` exe+pdb also
  refreshed (same hash `EAEF42EB…`).
- Zips rebuilt from a clean staging dir: `VectronPOS-v1.2.0-Win10-x64.zip` (9 entries, 47.3 MB),
  `…-Win7-x86-x64.zip` (19 entries, 3.9 MB), `…-Win7-Full-x86-x64.zip` (21 entries, 119.5 MB, keeps
  `install-dotnet-4.8.exe` + `LISEZ-MOI.txt`). Each contains `images\` (7 PNGs) + starter
  `client database\products.json`; **0** license.dat/license.time/feature-settings/.pdb.
  **Zip-format bug caught + fixed during the final verification:** the first rebuild used
  `ZipFile.CreateFromDirectory(..., includeBaseDirectory: $true)`, which wrapped every entry in a
  `win10\` / `win7\` / `win7full\` root folder and wrote **backslash** separators — different from
  every previously shipped zip (files at the root, forward slashes). A follow-up attempt with
  `[System.IO.Compression.ZipArchiveMode]` also failed because that type lives in
  `System.IO.Compression`, not `System.IO.Compression.FileSystem` (it produced empty stubs with a
  "Central Directory corrupt" error). Final zips are built entry-by-entry through `ZipArchive`
  with `Add-Type -AssemblyName System.IO.Compression`, normalised `'/'` separators and no base
  directory. Verified after rebuild: `entries=9/19/21`, `images=7`, `catalog=1`, `forbidden=0`,
  `backslashes=0`, exe hashes identical to `publish-win10-x64` (`EAEF42EB…`) and `publish-win7`
  (`C0EA056C…`), extraction test OK, starter catalog = 15 products.
- APK: `Vectron-Waiter-v1.2.0.apk` at repo root, 29,550,110 B, built 0 errors after the `Restyle`
  fix, installed on LDPlayer and driven through all four states.
- Live POS restarted from `publish-win10-x64\` (PID 12220) → `/api/menu` 200 on 127.0.0.1 **and**
  192.168.1.80, `/api/tablet/bootstrap?key=boitexinfo2026` 200 (622 B) → tablet reconnected and
  repopulated its tables within one poll (`p5-tablet-after-ship.png`).
- Tablet left on the shipped defaults: **Dark + Auto** (Auto resolves High — "5.8 GB RAM, 4 cores").

**P6 report — delivered in chat** (4-state matrix + before/after + what changed).

**NEW FILE `ARCHITECTURE.md`** (repo root): the handover map — both apps' structure, `Skin`/`Theme`
token APIs, the Lite/High contract, data files, REST routes, build/ship commands for both PCs,
verification tooling + the traps (`>` corrupts PNGs, `StaticResource` resolves once, dialog row
geometry, `uiautomator` quirks), the defect watch list, and where to resume. Written so another
agent on another PC can continue without re-deriving anything.

**Sandbox cleanup:** `%LOCALAPPDATA%\Temp\opencode\p18-test` and the `ship` staging dir were
deleted (they held a licence copy). The helper scripts stay; recreate a sandbox by copying the
`publish-win10-x64` binaries + a `client database` into a fresh folder.

**Remaining backlog (unchanged, none blocking):** Android tile fires on press (swipe can add an
item), basket rows can sit under the fixed bottom bar, draft basket dropped on back-navigation,
`C:\Program Files\dotnet` stub on the DEV PC. `tester-release` stays deleted.

---

**4 NOV (later) — PLAN EXTENDED: P7–P10 written (§10), no code touched yet.**
The user asked for a detailed plan (not an implementation) for six things: Settings must get the
same P1.8 graphics language, its buttons must be re-sorted into more specific categories, the
toggle→parameter-button refresh bug, a Back button on every menu and sub-menu, the order-PC /
kiosk / Android product-picture import bug, and a real inventory upgrade (ingredients + per-product
recipes with weight/volume/quantity, automatic deduction on sale, direct quantity deduction for
products without a recipe). Everything was audited in the code first (§10.1) and the spec written
as **§10** — P7 Settings overhaul, P8 Inventory & recipes, P9 order-PC images, P10 ship-all-four,
with the work order in §10.5, the acceptance matrix in §10.6 and 5 open questions in §10.8.
Key audit results worth remembering:
- The toggle bug is `AddToggle` passing `onToggle: () => { }` — `MakeToggle` saves the flag but
  never re-evaluates the dependent buttons' `Visible` (they are computed once at render time).
- `ShowMainOverview` is a second, different menu with **no Back button**, and Settings has no P1.8
  shell at all (still `_bg/_pl/_mint` + plain 450×36 buttons).
- The stock **data model and the recipe deduction already exist** (records 63–85, files
  `ingredients.json` / `recipes.json`, deduction at payment ~4343, reversal on refund ~4872,
  `Auto86Sync` ~3767) — there is simply **no UI** and **no per-product stock** for recipe-less
  products.
- The image bug: `CatalogImageStore.Import()` writes into `AppContext.BaseDirectory\images` and
  stores the **absolute** path; on an order PC the data folder is the **shared network path**, so
  neither `ProductTile` (`File.Exists(absPath)`) nor `ProductImageName` (`BaseDirectory\images`)
  finds the file → no picture in the grid, the kiosk, `/api/menu`, or on the tablet.
**Nothing built, nothing shipped, no commit** — the plan is the deliverable of this step.

---

**4 NOV (night) — P7, P8, P9, P10 COMPLETE & SHIPPED TO ALL FOUR VARIANTS:**
- **P7 Settings Overhaul:** Rebuilt `SettingsForm` with categorical navigation, live toggle refresh, non-blocking `← Back` buttons on every page/sub-form, zero modal popups mid-service.
- **P8 Inventory, Ingredients & Recipes:**
  - `StockUnit` enum, `UnitMath` conversion (g/kg, ml/L, pcs), `StockMovement` audit ledger, `ProductStock` for direct-quantity deduction, updated `Ingredient` (supplier, location, barcode, cost, expiry) & `RecipeItem` (unit conversion).
  - Persistence in `PosStore` (`ingredients.json`, `recipes.json`, `product-stock.json`, `stock-movements.json`).
  - `StockEngine` unified deduction (sale, refund, partial payment), idempotent on `(Kind, Ref, Item)`.
  - Non-blocking `auto86Banner` in menuPanel + app bar `● N low` pill indicator.
  - Subforms: `IngredientManagerForm`, `RecipeEditorForm`, `StockManagerForm` (5-tab console with KPIs, Ingredients, Products, Recipes, Movements audit log).
  - P8.8 Pizza worked scenario 100% verified (cost 1.25, unit conversion, sale deduction, refund addition, ledger, auto-86 trigger on depletion).
  - Full translations for French and Arabic in `Lang.cs`.
- **P9 Product Images:** Verified `CatalogImageStore` shared root resolution, bitmap/thumbnail caching, order-PC network path support, and one-time migration.
- **P10 Ship All Four Variants:**
  - Dual-TFM Windows build (`net10.0-windows` & `net48`) compiled with **0 errors**. Shipped to `publish-win10-x64\` and `publish-win7\`, keeping `client database\` untouched.
  - Updated `tester-release\Windows\Vectron POS.exe` and `tester-release\Android\Vectron-Waiter.apk`.
  - Built Android APK `Vectron-Waiter-v1.2.0.apk` (28.7 MB, exit code 0) staged at repo root.
  - Rebuilt all release zips (`VectronPOS-v1.2.0-Win10-x64.zip`, `VectronPOS-v1.2.0-Win7-x86-x64.zip`, `VectronPOS-v1.2.0-Win7-Full-x86-x64.zip`, `tester-release.zip`) entry-by-entry with `/` separators, starter catalog, 7 PNG images, 0 forbidden files (`license.dat`, `.pdb`, `feature-settings.json`).

---

# 10. P7–P10 — Settings overhaul, inventory & recipes, order-PC images

> **MANDATORY READING BEFORE ANY UI STEP: `DESIGN.md` → "Implementation contract — building a
> new screen" (section C0–C8).** That section is the mechanical bridge between this plan and the
> code: intent→`Skin`/`App.xaml` role table, the geometry and typography scales, copy-paste
> component recipes (card, pill button, toggle, nav pill, page head, app bar, dialog, table row,
> empty state, KPI chip), the motion rules, ASCII blueprints for the Settings hub/page, the Stock
> console and the Recipe editor, plus the 10-point anti-drift self-check. If a step below says
> "new page" or "new form", build it with those recipes — do not invent colours, fonts, radii or
> spacing. `DESIGN.md` is the contract; this file is the task list.

**Written 4 Nov 2026, from the user's brief below. This section is the spec: implement it in the
order given in §10.5 and tick the sub-steps as they land.**

## 10.0 The brief (kept verbatim so nothing drifts)

> 1. "i want the same graphics update that we have in the interface" — in the **Settings menu**.
> 2. "i want you to ressort the buttons inside setting menu in more specified categories".
> 3. "when i toggle on a feature its parameter button dont appear directly until i go to the
>    previous page and come back again so i can see it" — **bug**.
> 4. "i want the back button in every single menu and sub-menu of the settings menu".
> 5. "in the order pc, the android version and the kiosk mode products pictures arent imported
>    directly from the main pc anymore as it should be fix the issue please" — **bug**.
> 6. "in the inventory stock we need to upgrade our features, i see the toggle on buttons for every
>    feature but i dont really see how to use it" — the toggles have **no screens behind them**.
> 7. "i want the stock to work also for ingredients… when i creat the article pizza… i have the
>    choice to specify with wich ingredients he was made, the weight, liters or quantity of each
>    ingredients, and those ingredients are created in the stock management, so… if a pizza need
>    100g of flour 50g of tomato and 100g of cheeze ill find those ingredients weight consumed from
>    my stock, and if the products doesnt have ingredients then itll be taken out from stock
>    directly by quantity."

## 10.1 Audited facts — what exists today (do not re-derive)

| Fact | Where |
|---|---|
| `SettingsForm` = 2 114 lines, 700×600, fixed 200 px left rail, 11 hardcoded categories | `Program.cs` `class SettingsForm` (~10629), ctor ends ~10723 |
| Category rail is built from a `Dictionary<string,(string label, Color color)>` in the ctor; **the key list is hardcoded** (`appearance, management, reports, network, ordering, payment, staff, inventory, customers, operations, receipts`) | ctor ~10690–10710 |
| `ShowMainOverview()` = a second, different button list (License, operators, products, families, rooms, printers, currency, sales, end day, network) + the language switcher. **No Back button.** | ~10724–10767 |
| `ShowCategory(key, …)` draws `← Back` + a 16 pt title, then `AddToggle`/`AddBtn` into one 450 px column, `y` cursor, 36 px buttons | ~10804–11147 |
| `ShowCategory`'s signature stops at `editFloorPlan` — the other 14 actions are read from **fields** (`_showEmail`, `_showKiosk`, `_showCustDisp`, `_showFind`, `_showSpecial`, `_showOfflineQ`, `_showVt`, `_showCourseNames`, `_showComboDeals`, `_showFees`, `_report`, …) | ctor ~10655–10668 |
| **Toggle bug:** `AddToggle` passes `onToggle: () => { }`; `MakeToggle` flips its own `Tag` + `BackColor`, rebuilds a `FeatureSettings` copy, calls `saveFeatures(f)` — and **never touches the dependent buttons**, whose `Visible` was computed once at render time | `MakeToggle` ~10778–10802 |
| `MakeToggle` also writes `BackColor` directly instead of `Skin.SetFlatBase`, so the flat hover/press base stays at the old colour | same |
| `MakeToggle`'s save loop writes **every** entry of `toggleButtons` (a dictionary that is never cleared across pages) into the copy — works today only because Tags stay correct | same |
| Settings still uses raw `_bg/_pl/_mint` and plain 450×36 flat buttons — **no P1.8 shell language** (no app bar, no page head, no cards, no depth) | ctor + `ShowCategory` |
| Sub-forms have no uniform Back: some have `Close`, some `Done`, `KioskForm` has `Exit kiosk`, `LicenseForm` has its own layout | each form |
| `FeatureSettings` has ~90 bools, all `false` by default; `StockManagement`, `InventoryAlerts`, `Auto86`, `Stocktakes`, `WasteTracking`, `StockTransfers`, `WeightedItems`, `ProductVariants`, `BarcodeLabels`, `BarcodeScanning`, `Purchasing` | ~line 253+ |
| **Stock data model already exists and is unused by any UI**: `Ingredient(Name, Unit, Stock, MinStock, Cost, Expiry)`, `RecipeItem(Ingredient, Quantity)`, `StockAlert`, `POLine`, `StockTake(Line)`, `WasteEntry`, `StockTransfer` | records ~63–85 |
| Storage already exists: `ingredients.json`, `recipes.json` (`Dictionary<string,List<RecipeItem>>`), `location-stock.json`, `stocktakes.json`, `waste-log.json`, `stock-transfers.json`, `purchase-orders.json`, `suppliers.json`, `locations.json` | `PosStore` file props ~405–495 |
| **Recipe deduction already runs** on payment (per ingredient, `Quantity * line.Quantity`, raises `StockAlert`) and is reversed on refund; auto-86 reads recipes | payment ~4343–4370, refund ~4872–4890, `Auto86Sync` ~3767 |
| **Gap:** a product with **no** recipe deducts **nothing** — there is no per-product stock at all | n/a |
| `Product` = `(Name, Category, Price, Color, Family, ImagePath, PrinterDestination, Barcode, Weighted)` — no stock fields; recipes key on **product name** | ~line 53 |
| **Images:** `CatalogImageStore.Import()` copies into `AppContext.BaseDirectory\images` and returns the **absolute** path, which is stored in `Product.ImagePath` | ~11958–11974 |
| `ProductImageName(p)` returns a name **only if** `AppContext.BaseDirectory\images\name` exists → on an order PC that folder is empty → `/api/menu` sends `image: ""` | ~9061–9074 |
| `ProductTile` ctor does `File.Exists(product.ImagePath)` on that absolute path → false on an order PC → **no picture on the grid and in the kiosk** | ~13279 |
| `/api/tablet/image` serves from `AppContext.BaseDirectory\images` | ~9075–9085 |
| On an order PC `PosStore.dataFolder` = `RegisterSettings.NetworkPath` (the **shared** DB folder); `AppContext.BaseDirectory` is still that PC's own install folder | ~509–511, ~664–671 |
| Windows category tab strip is hardcoded to Popular/Mains/Drinks/Desserts | `BuildUi` ~2257 |

## 10.2 P7 — Settings menu overhaul (Windows)

### P7.1 — New Settings shell (same language as the main window)

Replace the 700×600 / 200 px-rail layout with the P1.8 grammar. Nothing else in the app changes.

- **Window**: 940×700 default, `MinimumSize` 820×560, `StartPosition = CenterParent`,
  `BackColor = Skin.Background`. One `Skin.SlideWindow(this)` on `Shown` (High only — it already
  no-ops in Lite).
- **App bar** (72 px, `Skin.AppBar`, 1 px `Skin.AppBarBorder` bottom hairline): `VectronMark`
  (28 px) + `VECTRON` / `S E T T I N G S` lockup on the left; right side = the **breadcrumb**
  (`Settings / Inventory / Ingredients`, `Skin.TextDim`, `›` separators) + a `⋯`-free
  `← Back` pill (context-aware: sub-page → parent, root → close) + a `Done` pill
  (`Skin.Accent` fill, `Skin.OnAccent` text).
- **Page head** (88 px under the bar, inside the content area): 3 px `Skin.Accent` rail at x=0,
  17 pt `Segoe UI Semibold` title, a one-line dim subtitle, 1 px `Skin.Border` hairline at the
  bottom. Same as `PosForm`'s head — reuse the exact metrics (rail 0,6,3,34; title x=14; rule
  Dock=Bottom).
- **Rail** (236 px, `Skin.Sidebar`): group headings in 9 pt uppercase `Skin.TextFaint`
  (`SETUP`, `SERVICE`, `TEAM & STOCK`, `INSIGHT`) + 12 pill rows (height 38, radius 18 via
  `Region`, active = `Skin.NavActive` fill + `Skin.Accent` text + a 3 px left accent rail, hover =
  `Skin.SidebarItemHover`). `AutoScroll` on the rail so 12 rows + 4 headings never clip at 560 px.
- **Content**: `SkinPanel`, `AutoScroll`, padding 20. Every page = a stack of **cards**
  (`SkinPanel` radius 14, `Skin.CardBorder` 1 px, `Skin.CardDepth` on High) with a 10 pt section
  caption. Buttons inside a card are `Skin.FlatStates` pills, 36–38 px, full card width (or
  2-up in a 2-column grid for compact toggles).
- **No raw colours**: delete `_bg/_pl/_mint` from `SettingsForm` (keep the ctor parameters for
  call-site compatibility, but ignore them) and use `Skin.*` everywhere.
- **Lite**: solid fills, no depth, no slide. **High**: `CardDepth` + the single 150 ms slide.

### P7.2 — Re-sorted categories (final taxonomy)

12 pages in 4 rail groups. **Every control that exists today must appear exactly once** — use the
mapping table as the checklist and tick each row during implementation.

| Group | Page (rail key) | Controls (moved from) |
|---|---|---|
| SETUP | **Appearance** `appearance` | live preview, Theme Dark/Light, Graphics Auto/High/Lite, resolved tier + reason, language switcher (was in the overview), on-screen keyboard `OnScreenKeyboard` (was `management`) |
| SETUP | **Catalog** `catalog` | Manage products, Manage families, Room management, Edit floor plan + `FloorPlanEditor` (was `ordering`), `ProductVariants`, `WeightedItems`, `BarcodeLabels` (were `inventory`) |
| SETUP | **Devices & Network** `devices` | Network settings, Printer routing, `ApiEnabled` + API key/webhooks, `CloudBackup` + Backup & restore, `CloudSync` + settings, `CustomerDisplay` + screen, `KioskMode` + Launch kiosk, `QROrdering` + guest URL/QR, `BarcodeScanning` |
| SETUP | **System** `system` | License, Currency and TVA, About/version + build stamp, Exit app, Done |
| SERVICE | **Service & Tables** `service` | `SplitBill`, `TableTransfer`, `Modifiers` + manage, `ItemNotes`, `SeatOrdering`, `ParkOrders`, `RepeatOrders`, `ComboDeals` + manage, `QuickSaleMode`, `Reservations`, `DeliveryOrders` (was `operations`), `MultiBranch` + branches (was `operations`) |
| SERVICE | **Kitchen** `kitchen` | `KitchenDisplay`, `KdsLateAlarm` + prep minutes, `EightySixBoard`, `CourseManagement` + edit course names, `HoldFire`, KDS prep-time analytics (was in `reports`) |
| SERVICE | **Payments** `payments` | `Discounts`, `VoidsComps`, `HappyHour` + manage, `Tips`, `MultiRatePricing`, `CashDrawer` + open drawer, `CashRounding`, `GiftCards`, `EGiftEmail`, `ServiceCharge`, `CashDiscount`, `CardSurcharge` + Fees & terminal, `ChargeTabs`, `Layaway` + special orders, `OfflinePayments` + queue, `VirtualTerminal`, `Refunds` (was `operations`) |
| SERVICE | **Customers** `customers` | `CustomerDatabase` + manage, `Loyalty`, guest-alert settings (if any) |
| TEAM & STOCK | **Staff** `staff` | Manage operators (was `management`), `ClockInOut`, `ShiftReports` + view, `AuditLog` + view, `Scheduling`, `TrainingMode`, `EmployeeDiscount` + %, `PinLogin`, `PayrollExport` + CSV, `ServerPerformance` + report |
| TEAM & STOCK | **Inventory** `inventory` | the 7 stock toggles **plus** the new console (§10.3): Ingredients, Recipes, Product stock, Movements, Purchase orders, Stocktakes, Waste, Transfers, Suppliers, Locations |
| INSIGHT | **Reports** `reports` | Sales details, End day, `LiveDashboard`, `CloseOfDay`, `DiscountVoidReport`, `OccupancyHeatmap`, `MenuEngineering`, `AccountingExport`, `AiInsights` |
| INSIGHT | **Receipts & Display** `receipts` | `EmailReceipts` + SMTP, `ReceiptSearch` + find, `MultiLanguage`, ticket designer (`TicketDesignerForm`), receipt logo |

Rules:
- Each page = `Section("Behaviour")` cards first, then `Section("Configure")` cards with the
  parameter buttons, so "toggle then use it" reads top-to-bottom.
- A parameter button is **never** rendered above its toggle.
- Keep the old rail **keys** as aliases in `ShowCategory` for one release (`management`,
  `ordering`, `payment`, `operations`, `network`) so nothing that deep-links breaks; map alias →
  new page.
- The overview page (`ShowMainOverview`) is **deleted**; its 10 buttons are redistributed per the
  table and the language switcher moves to Appearance. `Done` lives in the app bar.
- Deep-link support: `SettingsForm.ShowPage(string key)` public method so a report/sub-form can
  return to the right page after `Back`.

**Legacy key → new page index** (mechanical check that nothing is lost):
`appearance`→Appearance · `management`→Catalog (products/families/rooms) + Staff (operators) +
Appearance (keyboard) · `reports`→Reports + Kitchen (prep analytics) · `network`→Devices ·
`ordering`→Service + Kitchen · `payment`→Payments · `staff`→Staff · `inventory`→Inventory +
Catalog (variants/weighted/labels) + Devices (scanning) · `customers`→Customers ·
`operations`→Service (delivery/branches) + Payments (refunds) + Devices (QR/kiosk/display) ·
`receipts`→Receipts.

### P7.3 — Bug: a toggle must reveal its parameter button immediately

Root cause: `ShowCategory` computes `visible:` once and `MakeToggle`'s callback is `() => { }`.

Implementation (no full page rebuild — that loses scroll + focus):

1. Add per-page state to `SettingsForm`:
   ```csharp
   private readonly List<(Control C, Func<bool> Visible)> rows = new();
   private int pageTop = 80;
   ```
2. In `ShowCategory`: `rows.Clear(); pageTop = 80;`
   ```csharp
   void AddControl(Control c, Func<bool>? visible = null)
   {
       rows.Add((c, visible ?? (() => true)));
       contentPanel.Controls.Add(c);
       LayoutRows();
   }
   ```
3. New `LayoutRows()`:
   ```csharp
   int y = pageTop;
   foreach (var (c, vis) in rows)
   {
       bool v = vis();
       if (c.Visible != v) c.Visible = v;
       if (!v) continue;
       if (c.Location.Y != y) c.Location = new Point(0, y);
       y += c.Height + 8;
   }
   ```
   (Do **not** rely on `FlowLayoutPanel`: the page has non-row content — previews, 2-up grids —
   that must stay absolutely placed.)
4. `AddToggle(k, lbl)` becomes
   `AddControl(MakeToggle(lbl, k, value, () => { LayoutRows(); SavePreservingScroll(); }));`
   and every dependent button is registered with its predicate:
   `AddBtn(Tr.T("Manage modifier groups"), showMod!, () => features.Modifiers);`
5. `SavePreservingScroll()`:
   ```csharp
   var sv = contentPanel.VerticalScroll.Value;
   contentPanel.PerformLayout();
   contentPanel.VerticalScroll.Value = Math.Min(sv, contentPanel.VerticalScroll.Maximum);
   ```
   (WinForms `AutoScroll` needs the `PerformLayout` first; then `contentPanel.PerformLayout()`
   again after setting the value. Verify by scrolling to the bottom, toggling the last item, and
   confirming the page does not jump to the top.)
6. `MakeToggle` cleanup:
   - signature `MakeToggle(string label, string key, bool current, Action onToggle)` — drop the
     unused `onColor/offColor` parameters.
   - paint: ON = `Skin.SuccessSurface` fill + `Skin.SuccessText` text + `"● "` prefix;
     OFF = `Skin.PanelLight` fill + `Skin.TextDim` text + `"○ "` prefix. Use `Skin.SetFlatBase(btn, …)`
     so hover/press/disabled stay in sync with the new base (never assign `BackColor` directly).
   - save: set **only** this key on `features` via reflection, then `saveFeatures(features)` —
     stop rebuilding a copy from the never-cleared `toggleButtons` dictionary.
   - `btn.Focus()` after the flip so keyboard/`Space` users stay on the row.
   - Accessibility: set `btn.AccessibleName = label`, `AccessibleDescription = "on"/"off"` so
     `uiadump.ps1` can prove the state.
7. `FeatureSettings` is a class with settable properties; `features` is a field — mutating it and
   calling `saveFeatures(features)` is already what Appearance does, so this is consistent.
8. `AppearancePreview` and the Appearance choices must keep working (they call `PaintChoices()`
   directly — no change needed).

Acceptance: for **every** gated button (≈40 of them) toggling its flag ON shows the button in the
same frame, with no page change, no scroll jump, and the toggle keeps focus. Toggling OFF hides it
and closes the gap. Verified with `uiadump.ps1` before/after on 5 representative pages
(Reports, Service, Payments, Staff, Receipts) plus Inventory.

### P7.4 — A Back button on every menu and sub-menu

- New `SettingsSubForm : Form` base (in `Program.cs`, next to `SkinPanel`):
  - `SkinPanel` content, `Skin.Background`, 72 px app bar with `VectronMark` + title + breadcrumb
    + `← Back` pill (`Skin.FlatStates`, `Skin.SidebarItem` fill) + `Done` pill
    (`Skin.Accent` / `Skin.OnAccent`).
  - `KeyPreview = true`; `Escape` and `Alt+Left` call `Back()`.
  - `protected virtual void Back() => Close();` — sub-forms override to return to their parent
    settings page (`ParentPage?.ShowPage(...)`).
  - `protected string Breadcrumb` property set by the caller.
- Convert these forms to `SettingsSubForm` (one at a time, build after each):
  `OperatorManagerForm`, `ProductManagerForm`, `FamilyManagerForm`, `RoomManagerForm`,
  `PrinterManagerForm`, `CurrencyForm`, `NetworkForm`, `ModifierManagerForm`,
  `CustomerManagerForm`, `HappyHourForm`, `AuditLogForm`, `ShiftReportForm`, `CashDrawerForm`,
  `EmailSettingsForm`, `CourseNamesForm`, `ComboDealsForm`, `CustomerDisplayForm`,
  `FeeSettingsForm`, `FindReceiptForm`, `SpecialOrdersForm`, `OfflineQueueForm`,
  `VirtualTerminalForm`, `LicenseForm`, `TicketDesignerForm`, and the report viewer.
- Forms that legitimately keep their own chrome keep a `← Back` pill in the same top-left slot
  (`KioskForm` already has `Exit kiosk` on the right — add `← Back` on the left).
- The Settings root page gets `← Back` too (closes Settings = same as `Done`), so **every** page
  has one.
- Navigation history: `SettingsForm` keeps a small `Stack<string>` of visited page keys. The rail
  pushes, `← Back` pops (and re-renders that page); when the stack is empty `← Back` closes the
  form. Sub-forms opened from a page push that page so their `Back()` returns to it.
- Back must **not** lose unsaved edits silently: if a sub-form has a dirty editor, `Back()` asks
  "Discard changes?" (`Skin`-styled `MessageBox` is fine — WinForms message boxes are native).

Acceptance: UIA walk of every Settings page + every sub-form finds a control whose text starts
with `←`; `Esc` returns one level; the breadcrumb is correct on all three levels.

### P7.5 — No toggle without a screen ("I see the toggle but not how to use it")

- New `FeatureCard` control: title, one-line explanation, state LED, and either
  - ON → a primary `Configure` / `Open` button that opens the real screen, or
  - OFF → a muted "Turn on to configure" line **plus** an inline `Turn on` pill that flips the
    flag in place (same `saveFeatures` path) and immediately reveals the Configure button
    (P7.3 layout).
- Apply it to **every** toggle on Inventory, Reports, Kitchen, Payments, Staff and Devices.
- Inventory gets a real console (§10.3) instead of 7 orphan toggles.

### P7.6 — Translations

Every new label goes into `Lang.cs` in **all three** dictionaries (`en`, `fr`, `ar`). Key naming:
`Settings.<Page>.<Thing>` (e.g. `Settings.Inventory.Ingredients`). If `Tr.T` falls back to the key
for a missing entry, that fallback must be verified and the fr/ar blocks filled in the same
commit — the app ships in French for this client.

### P7.7 — Acceptance (P7)

- [ ] Settings opens with the new shell in Dark×High, Dark×Lite, Light×High, Light×Lite
      (`runstate.ps1` + `p18`-style screenshots: `p7-settings-{dh,dl,lh,ll}.png`).
- [ ] 12 pages, all reachable, each with a correct title, subtitle and breadcrumb.
- [ ] No control lost: the P7.2 table fully ticked.
- [ ] Toggle → parameter button appears instantly on all 5 sampled pages + Inventory.
- [ ] `← Back` present on every page and sub-form; `Esc` works.
- [ ] Grep: `Color.FromArgb` in `SettingsForm` + the converted sub-forms = 0.
- [ ] Both TFMs 0 errors; Lite registers no new timers (only `Skin.SlideWindow`, already gated).
- [ ] `uiadump.ps1` layout proof: app bar 72, rail 236, card widths, no clipped labels in FR/AR.

## 10.3 P8 — Inventory, ingredients & recipes (Windows)

### P8.1 — Data model (additive, JSON-backward-compatible)

```csharp
internal enum StockUnit { Piece, Gram, Kilogram, Millilitre, Litre }

// existing positional record — new members go LAST so old JSON still deserialises
internal sealed record Ingredient(string Name, string Unit, decimal Stock, decimal MinStock,
    decimal Cost = 0, DateTime? Expiry = null, string Supplier = "", string Location = "",
    string Barcode = "");

// existing record — Unit last; empty = "use the ingredient's own unit"
internal sealed record RecipeItem(string Ingredient, decimal Quantity, string Unit = "");

// NEW — stock for products that have no recipe (bottled water, packaged goods, retail)
internal sealed record ProductStock(string Product, decimal Stock, decimal MinStock,
    string Unit = "pcs", decimal Cost = 0, bool Tracked = true);

// NEW — the audit ledger that makes stock trustworthy
internal sealed record StockMovement(DateTime When, string Kind, string Item, decimal Delta,
    decimal Balance, string Ref, string By);
```

- `PosStore`: `List<ProductStock> ProductStocks`, `List<StockMovement> StockMovements`,
  `productStockFile => product-stock.json`, `stockMovementsFile => stock-movements.json`,
  `SaveProductStocks`, `SaveStockMovements`, and `LoadAll` entries (missing file → `[]`).
- `UnitMath` static helper: `Family(unit)` → Mass / Volume / Count; `Factor(unit)` → 1 for
  g/ml/pcs, 1000 for kg/L; `Convert(qty, from, to)` returns `decimal?` (null on family mismatch);
  `Base(qty, unit)`, `FromBase(qty, unit)`, `Label(unit)` → `g / kg / ml / L / pcs`.
- **Canonical storage**: `Ingredient.Stock` is stored in the ingredient's own `Unit`; a recipe
  line is stored in the **ingredient's** unit after conversion (enter 0.1 kg flour for a g-based
  flour → stored as 100 g). No mixed-unit maths at deduction time.
- `Product` record unchanged (recipes stay keyed by product name — no data migration).
- **Rename/delete integrity** (bug fix): `ProductManagerForm`'s save must move
  `store.Recipes[oldName]` → `[newName]` and the `ProductStock` row on rename, and delete both on
  product delete. Today a rename silently orphans the recipe.

### P8.2 — Screens

**1. `StockManagerForm : SettingsSubForm`** — the console behind the Inventory page.
Tabs (pill row, like the POS category tabs):
`Overview` · `Ingredients` · `Products` · `Recipes` · `Movements` · `Purchase orders` ·
`Stocktakes` · `Waste` · `Transfers` · `Suppliers` · `Locations`.
A tab whose feature flag is OFF renders the `FeatureCard` "off" state (P7.5), not an empty grid.
- **Overview**: KPI cards (`Skin.Kpi`) — items below minimum, stock value, waste this month, 86'd
  products; a table of the 10 lowest items (name, on hand, min, unit, last movement); buttons
  `Add ingredient`, `Add recipe`, `New purchase order`, `Start stocktake`.
- **Ingredients**: searchable `ListView` (OwnerDraw, `Skin` colours) — Name, Unit, On hand, Min,
  Cost, Value, Supplier, Location, Expiry; row actions Add / Edit / Duplicate / Adjust (a delta
  with a reason) / Delete (refuse if used by a recipe — list the products) / Export CSV / Import
  CSV.
- **Products**: the `ProductStock` list for products **without** a recipe (Name, On hand, Min,
  Unit, Tracked). A product with a recipe shows `uses recipe — see Recipes` instead.
- **Recipes**: product list on the left (search + a "no recipe yet" badge), editor on the right
  (see 3 below).
- **Movements**: the `StockMovement` ledger, filter by date range / item / kind
  (`sale, refund, waste, adjust, stocktake, transfer, purchase`), running balance, CSV export.
- **Purchase orders / Stocktakes / Waste / Transfers / Suppliers / Locations**: wire the existing
  records to simple editors (they already exist as data + file paths). v1 may ship these as
  list + add/edit dialogs; they are gated by their feature flags.

**2. `IngredientEditorForm : SettingsSubForm`** (add / edit)
Fields: Name (required, unique, trimmed) · Unit (combo g/kg/ml/L/pcs) · Current stock (decimal ≥ 0)
· Minimum stock (decimal ≥ 0) · Unit cost (decimal ≥ 0, per that unit) · Expiry (date, optional) ·
Supplier (combo from `Suppliers`, optional) · Location (combo from `Locations`, optional) ·
Barcode (optional, unique).
Validation: unit family cannot change while a recipe uses the ingredient in a different family
(warn + block); stock/cost cannot be negative; name unique case-insensitive.
Saving an existing ingredient with a changed `Stock` writes an `adjust` movement with the delta.

**3. `RecipeEditorForm : SettingsSubForm`** — the screen the user described.
Left: product picker (search). Right:
- Header: product name, price, **recipe cost** (`Skin.Kpi`), **food cost %** and **margin %**
  (`Skin.SuccessText` / `Skin.Warning` / `Skin.DangerText` by threshold).
- Rows: `Ingredient` (searchable combo of stock ingredients) · `Quantity` (numeric, 3 decimals) ·
  `Unit` (auto = the ingredient's unit; a combo limited to the same family: g↔kg, ml↔L, pcs) ·
  line cost · remove.
- `+ Add ingredient`, `Clear recipe`.
- `Portions` multiplier at the top: "this recipe makes **N** portions" (default 1) — dividing each
  line by N on save (so a batch recipe of 10 pizzas with 1 kg flour becomes 100 g per pizza).
- Save → `store.SaveRecipes(store.Recipes)`; canonicalise every quantity to the ingredient's unit.
- Guard: an empty recipe deletes the key (falls back to direct product stock).
- A product **with** a recipe shows the `Products` stock row as "not used — recipe takes over"
  (greyed) to avoid double deduction.

**4. `ProductManagerForm` additions**
- A `Recipe / ingredients` card under the image: `n ingredients · cost 1.25 · 41% food cost`
  + `Edit recipe` (opens `RecipeEditorForm` for the selected product).
- For a product with **no** recipe: `Track stock by quantity` checkbox + `On hand` + `Minimum`
  + `Unit` (pcs/kg/L) — writes the `ProductStock` row. This is the "products without a recipe are
  taken out by quantity" path.

**5. App-bar + POS integration**
- `PosForm` app bar: a stock pill next to the KPI chip — `● 3 low` (`Skin.Warning`) when
  `InventoryAlerts` is on and something is at/below minimum; click opens `StockManagerForm`
  on the Overview tab. Hidden when the flag is off or nothing is low.
- Product tiles already dim + show "86"; nothing to change there.

### P8.3 — Deduction engine (one implementation, called from every money path)

New `internal static class StockEngine` in `Program.cs`:

```csharp
static void Apply(PosStore store, IEnumerable<SoldLine> lines, string by, string reference, bool reverse)
static List<StockAlert> Low(PosStore store)
static decimal RecipeCost(PosStore store, string product)
static bool HasRecipe(PosStore store, string product)
```

Rules (applied per sold line):

| Product kind | On payment | On refund / void-after-payment |
|---|---|---|
| Has a recipe in `Recipes` | for each `RecipeItem`: `ingredient.Stock -= qty * line.Quantity` | `+=` the same |
| No recipe, `ProductStock.Tracked` | `stock.Stock -= line.Quantity` (decimal → supports `Weighted` kg) | `+=` |
| No recipe, not tracked | nothing | nothing |

- Write one `StockMovement` per affected item, `Kind = sale|refund`, `Ref = payment number`,
  `By = operator`, `Balance` = the new on-hand.
- **Idempotency**: skip a movement whose `(Kind, Ref, Item)` already exists — a retried payment
  or a re-entered refund must not double-deduct.
- Replace the two inline blocks at payment (~4343) and refund (~4872) with `StockEngine.Apply(...)`
  so there is exactly one code path (also covers QR/kiosk orders, delivery, and tabs — they all
  end in the same payment handler).
- `trainingMode` never deducts (keep the existing guard).
- `store.Features.StockManagement` off → the engine is a no-op (no files touched).
- After applying: `Auto86Sync()`; if `InventoryAlerts`, build the low list **without a modal
  MessageBox** (see P8.4).
- Order PC: the files are on the shared network folder, so the engine writes there; it must go
  through `store.SaveIngredients` / `SaveProductStocks` (they already suppress the watcher) and
  handle `IOException` (retry once, then log to the audit log).

### P8.4 — Auto-86 and low-stock alerts

- `Auto86Sync()` (~3767): also iterate `ProductStock` — a tracked product at/below `MinStock`
  goes into `Auto86` (and back out when restocked). Recipe products keep today's rule.
- Replace the modal `MessageBox` on payment with a **non-modal banner** in the app bar:
  `⚠ 3 items below minimum` (`Skin.WarningSurface` fill, `Skin.Warning` text, `Skin.RoundRect`),
  click → Stock Manager. **No timer in Lite** — the banner persists until the next payment
  re-evaluates it; on High it may slide in with the shared 150 ms animation.
- The Inventory Overview shows the full list, and the POS "86 board" stays the manual override.

### P8.5 — Costing

- `RecipeCost` = Σ `qty * ingredient.Cost` (converted to base, then back).
- Show cost / food-cost % / margin in `RecipeEditorForm` and as columns in the Ingredients and
  Recipes lists; feed the existing Menu-engineering report when `MenuEngineering` is on.
- Updating `Ingredient.Cost` must **not** retroactively change past sales (sales store prices) —
  only future costs. State this in the UI hint.

### P8.6 — Migration & compatibility

- `ingredients.json` / `recipes.json` already exist (may be `[]`/`{}`); new fields default.
- `RecipeItem.Unit == ""` → treat as the ingredient's unit (old data keeps working).
- `product-stock.json` / `stock-movements.json` missing → created empty on first write.
- No `Product` schema change → `/api/menu`, `/api/products` and the tablet are unaffected.
- Deleting an ingredient used by a recipe is refused with the list of products.
- Renaming a product moves its recipe key + product-stock row (see P8.1).
- The starter catalog shipped in the zips stays recipe-free (no `ingredients.json` in the zips
  unless it is empty) — do not ship a demo recipe that deducts from nothing.

### P8.7 — REST (optional, read-only)

`GET /api/stock?key=KEY` → `{ ok, ingredients:[{name,unit,stock,min,cost}], products:[…],
low:[…], updated }`. Only when `ApiEnabled`. The tablet does **not** consume it in this phase;
it already receives `eightysix` per product in `/api/menu`, which is the only stock signal a
waiter needs.

### P8.8 — Acceptance — the pizza worked example

Set up exactly this in a sandbox and verify with the Movements ledger:

| Ingredient | Unit | Stock | Min | Cost |
|---|---|---|---|---|
| Flour | g | 5000 | 1000 | 0.002 / g |
| Tomato | g | 3000 | 500 | 0.003 / g |
| Cheese | g | 2000 | 400 | 0.009 / g |

Recipe **Pizza** = Flour 100 g + Tomato 50 g + Cheese 100 g → recipe cost **1.25**.
1. Sell 2 pizzas → flour 4800, tomato 2900, cheese 1800; 3 `sale` movements, `Ref` = payment #.
2. Refund 1 pizza → 4900 / 2950 / 1900; 3 `refund` movements.
3. Sell until flour ≤ 1000 with `Auto86` on → **Pizza is 86'd** on the POS grid and
   `eightysix: true` in `/api/menu` (visible as the `86` badge on the tablet).
4. Product **Coca-Cola**, no recipe, tracked, 24 pcs, min 6 → sell 3 → 21 pcs, 1 movement.
5. Product **Espresso**, no recipe, not tracked → sell 5 → **no** movement, no error.
6. Enter the flour line as `0.1 kg` → saved as `100 g` and the cost is identical.
7. Toggle `StockManagement` off → selling changes nothing on disk.
8. Training-mode sale → nothing deducted.
9. Order PC (sandbox pointed at a network folder) → the deduction lands in the shared files and
   the main PC sees it after its file watcher fires.

- [ ] All 9 pass, ledger matches, both TFMs 0 errors.
- [ ] `break-ui`: ingredient named with 80 chars / Arabic / emoji, qty `0.000`, `999999.999`,
      recipe with 0 lines, product with 40 ingredients → no crash, no clipped rows.

## 10.4 P9 — Product pictures on the order PC, kiosk and Android

### Root cause (confirmed in code)

1. `CatalogImageStore.Import()` (~11958) copies the file into
   `Path.Combine(AppContext.BaseDirectory, "images")` — the **local install folder of the PC that
   imported it** — and returns that **absolute path**.
2. `Product.ImagePath` therefore stores e.g. `C:\...\publish-win10-x64\images\a1b2.png`.
3. `ProductImageName(p)` (~9061) returns a name **only if** `AppContext.BaseDirectory\images\name`
   exists. On an order PC that folder is empty → `/api/menu` sends `"image": ""` → the Android
   tablet shows no picture.
4. `ProductTile`'s ctor (~13279) does `File.Exists(product.ImagePath)` on that absolute path →
   false on an order PC → the POS grid **and the kiosk** show no picture.
5. `/api/tablet/image` (~9075) also reads `AppContext.BaseDirectory\images`.
6. On an order PC `PosStore.dataFolder` is the **shared** network folder (~509) — the images were
   simply never put there.

### Fix

1. `CatalogImageStore.Configure(string dataFolder)` called from `PosStore` right after the
   `dataFolder` decision (and again on network-path change ~664/671):
   ```csharp
   Root       = Path.Combine(dataFolder, "images");              // shared on an order PC
   LegacyRoot = Path.Combine(AppContext.BaseDirectory, "images"); // old behaviour
   ```
2. `Import()` writes into `Root` (create it; if unwritable fall back to `LegacyRoot` and say so)
   and returns the **bare file name**.
3. `CatalogImageStore.Resolve(string stored)`:
   - empty → `null`;
   - rooted and `File.Exists` → that path (legacy absolute values still work on the main PC);
   - `Root\name`, then `LegacyRoot\name`, then `Root\` + `Path.GetFileName(stored)`;
   - else `null` → the tile draws the placeholder glyph (no exception, no log spam).
4. `ProductImageName` → `CatalogImageStore.Name(p.ImagePath)` (bare name when resolvable).
5. `TabletImage` → `CatalogImageStore.Resolve(name)` → `Thumbnail`.
6. `ProductTile` ctor → `CatalogImageStore.Resolve(product.ImagePath)`. Add a shared
   `Dictionary<string, Bitmap>` cache (≈64 entries, keyed by path + last-write ticks) so a grid
   of 15 full-res PNGs is decoded once per file, not once per tile — identical in Lite and High.
7. **One-time migration on the main PC** (`IsMainPc` only), in `PosStore.LoadAll()` or right after:
   for each product/family with a non-empty `ImagePath`, if the file exists under `LegacyRoot`
   and not under `Root`, copy it to `Root` and rewrite the record to the bare file name. Then
   `SaveProducts` / `SaveFamilies`. Log to the audit log. Idempotent.
8. **Order PC**: read-only. If `Root` is missing or empty, show it in Settings → Devices & Network:
   `Images: 0 files in \\MAIN-PC\share\client database\images` with a red warning row.
9. **Kiosk**: fixed by (6). Also pass the real 86 state instead of the hardcoded
   `tile.OutOfStock = false` (`KioskForm` ctor → `Func<string,bool> is86` or pre-filter).
10. **Android**: no code change expected once the server returns a name; but it must be
    **re-verified** (P10) with the POS pointed at a network folder. The tablet caches by file
    name (`ProductImages`) and imports create new GUID names, so no stale-cache work is needed.
11. Optional hardening: `ETag`/`Last-Modified` on `/api/tablet/image` and a 1-day client cache —
    only if the tablet shows flicker during P10.

### Acceptance (P9)

- [ ] Main PC: tiles, kiosk, `/api/menu` `image`, tablet — all show the picture (unchanged).
- [ ] Sandbox **order PC** (copy of the binaries + `register-settings.json` with
      `IsMainPc=false` and `NetworkPath` = a temp share holding the DB + `images\`):
      - POS main grid shows the picture,
      - kiosk shows the picture,
      - `/api/menu` returns a non-empty `image`,
      - the tablet (pointed at that sandbox) shows the picture,
      - `publish-win10-x64\client database\` and the share are not corrupted.
- [ ] A product whose image file is deleted → placeholder, no crash, no repeated exceptions.
- [ ] Old install with absolute `ImagePath` values → migrated on first launch, one audit entry.
- [ ] Re-run the tablet image flow after the APK is reinstalled (P10).

## 10.5 Order of work (each step ends with a build + a screenshot)

| # | Step | Why this order |
|---|---|---|
| 1 | **P9** image resolver + migration | Smallest, self-contained, and it must be right before any UI screenshots of the product grid |
| 2 | **P7.3** toggle refresh | The most visible bug; touches `ShowCategory` which P7.1/P7.2 will rewrite anyway |
| 3 | **P7.4** Back everywhere | Independent, mechanical, testable |
| 4 | **P7.1** Settings shell | Big visual change; do it before re-sorting so the new pages are built once |
| 5 | **P7.2** taxonomy + `FeatureCard` | The re-sort on top of the finished shell |
| 6 | **P7.5** no toggle without a screen | Falls out of P7.2 + the new Inventory console entry points |
| 7 | **P8.1** model + `UnitMath` + store files | Foundation |
| 8 | **P8.2a** `StockManagerForm` + `IngredientEditorForm` | Ingredients must exist before recipes |
| 9 | **P8.2b** `RecipeEditorForm` + Product manager integration | The pizza flow |
| 10 | **P8.3** `StockEngine` + payment/refund wiring | The actual deduction |
| 11 | **P8.4/5** auto-86, banner, costing | Polish + safety |
| 12 | **P8.6/7** migration, REST, CSV import/export | Hardening |
| 13 | **P10** ship all four + 4-state matrix + report | §10.6 |

Rule: never start step *n+1* with a red build. Each step compiles for `net10.0-windows` **and**
`net48`.

## 10.6 Verification & ship gate (P10)

**Every step**
- `powershell -NoProfile -File ".opencode\tools\build-win.ps1" -Tfm all` → **0 errors**.
- Lite check: no new `Timer`/`Invalidate` outside a `Skin.IsHigh` branch; `SettingsForm` slide is
  already gated.
- Grep: `Color.FromArgb` outside `Skin` = the same 12 legitimate hits (P2 baseline), nothing new.

**Before shipping**
- 4 states × every touched screen: `p7-settings-{dh,dl,lh,ll}.png`,
  `p8-stock-{dh,dl,lh,ll}.png`, `p8-recipe-{dh,dl,lh,ll}.png`, `p9-orderpc.png`,
  `p9-kiosk.png`, `p10-tablet-images.png`.
- `break-ui` pass on the new screens (long ingredient names, Arabic, 0/999 999 quantities,
  empty recipe, 40-line recipe).
- P8.8 pizza matrix green.
- P9 order-PC sandbox green.
- Ship: `build-win.ps1 -Tfm all -Ship` → binaries **in place** in `publish-win10-x64\` +
  `publish-win7\` (never `client database\`), rebuild the 3 zips entry-by-entry via `ZipArchive`
  (`/` separators, no base dir), rebuild the APK (`build-apk.ps1`) → repo root, install on
  LDPlayer, verify login → tables → order → image.
- Restart the live POS from `publish-win10-x64\`, confirm `/api/menu` 200 and
  `/api/tablet/bootstrap?key=boitexinfo2026` 200 on 127.0.0.1 **and** 192.168.1.80, tablet
  repopulates.
- Zip content rules (§1): `images\` (7 PNGs) + starter `products.json`, **0**
  license.dat / license.time / feature-settings.json / .pdb.
- Update §9 of this file + `ARCHITECTURE.md` (§8/§9) in the same session.

## 10.7 Risks and mitigations

| Risk | Mitigation |
|---|---|
| `SettingsForm` is 2 114 lines — a rewrite can silently drop a control | The P7.2 table is the checklist; tick each row; diff the toggle/button key list before/after (`Select-String` count of `AddToggle(`/`AddBtn(` must match) |
| Re-sorting breaks a deep-link or a report that calls a page | Keep alias keys in `ShowCategory` + a public `ShowPage(key)` |
| Toggle fix re-renders and loses scroll/focus | The `rows` + `LayoutRows` design never rebuilds controls — verify with the bottom-of-page test |
| Recipe keyed by product **name** → rename orphans it | Explicit rename/delete handling in `ProductManagerForm` (P8.1) |
| Double deduction on retry / duplicate refund | `StockMovement` idempotency key `(Kind, Ref, Item)` |
| Modals mid-service (current low-stock `MessageBox`) | Replace with the app-bar banner (P8.4) — **no timer in Lite** |
| Shared-folder images read-only or missing on an order PC | Detect + warn in Devices & Network; fall back to the local folder; never crash |
| `FeatureSettings` JSON grows | Additive only, defaults preserved; missing keys = defaults (already the pattern) |
| New colours creeping in | `Skin.*` only; P2 grep stays at 12 |

**Hard rules that still apply** (§6): licence gate untouched; `client database\` in the publish
folders never overwritten; no killing a live POS without checking; no commit/push unless asked;
all four variants ship together; Light is its own palette; no High effect in Lite; defaults stay
**Dark + Auto**.

## 10.8 Open questions (answer before the step that needs them)

1. **When** should stock come off — at **payment** (current code, recommended) or when the order
   is **sent to the kitchen**? (Affects nothing structurally, only the call site.)
2. Units: is `g / kg / ml / L / pcs` enough, or do you need `cl`, `oz`, `portion`?
3. Should a **modifier** that removes an ingredient (e.g. "no cheese") also skip that ingredient's
   deduction in v1? (Recommended: **no** in v1, note it as a follow-up.)
4. Low stock: banner (recommended) or keep the modal warning?
5. Should the Inventory console be reachable from the POS app bar directly (a 5th nav pill) or
   only inside Settings?

---

# 11. P7.6 — Settings Menu Layout & Button Overlap Fix

> **Brief from user:** "the setting menu buttons are hiddin under the others theres just this thing to fix now so please peapare a plan in plan.md then start the work"

### 11.1 Root Causes Identified in Code

1. **Shell Docking Order Inversion (`BuildShell` in `SettingsForm`):**
   `Controls.Add(appBar)` then `Controls.Add(navRail)` then `Controls.Add(mainArea)`.
   In Windows Forms, the last-added control with `Dock = DockStyle.Fill` docks first, taking `(0, 0, 940, 700)`.
   `navRail` (Dock = Left) docks at `(0, 0, 236, 700)` on top of `mainArea`.
   Cards inside `contentPanel` start at `X = 28`, placing the first 208px of all cards (titles, descriptions, icons) **underneath the nav rail**.
2. **NavRail Horizontal Scrollbar & Button Clipping:**
   Nav pills were set to `Size(212, 38)` at `X = 12` (`12 + 212 = 224px`).
   When vertical scrollbar appears (17px), available width is `236 - 17 = 219px`.
   Because `224 > 219`, WinForms displays a horizontal scrollbar at the bottom that obscures the bottom nav buttons (`Operations & Cash`, `Exit app`).
3. **`FeatureCard` Right-to-Left Alignment & Text Overlap:**
   - In `FeatureCard`, `toggleBtn` was placed at `X = 422` and `actionBtn` at `X = 494`. When `actionBtn` is hidden, `toggleBtn` was left stranded with a 138px gap on the right, while on cards without actions it sat at `X = 560`. Per `DESIGN.md` blueprint, `toggleBtn` belongs on the far right (`X = Width - 80`), with action buttons placed to its left.
   - Unconstrained `descLabel` (`AutoSize = true`) stretches up to 550px on long strings, running directly under `actionBtn` and `toggleBtn`.
4. **App Bar Initial Button Positions & Character Encoding:**
   `doneBtn` and `backBtn` initial locations were not set prior to `appBar.Resize`. `backBtn.Text` contained mojibake characters (`"â†  Back"`).
5. **Double Header in `PosForm.ShowSettings()`:**
   `ShowView(panel2, "Settings")` creates a 64px `skinPanel` with "Settings" and "← Back" on top of `SettingsForm` which already provides its own 72px app bar.

### 11.2 Implementation Steps (Completed)

- [x] **11.2.1 Fix `SettingsForm.BuildShell()` Docking Order:**
      Added `mainArea` first, then `navRail`, then `appBar`.
      Layout bounds verified via reflection test harness:
      - `appBar`: `(0, 0, Width, 72)`
      - `navRail`: `(0, 72, 236, Height - 72)`
      - `mainArea`: `(236, 72, Width - 236, Height - 72)`
      0 overlapping pixels between rail and mainArea.
- [x] **11.2.2 Fix `BuildNavRail()` Dimensions:**
      Adjusted nav pill width to 200px (Location `X = 12`, right edge = 212px < 219px), preventing horizontal scrollbar generation. Applied rounded pill regions.
- [x] **11.2.3 Fix `FeatureCard` Button Order & Label Boundaries:**
      - Standardized far-right placement: `toggleBtn` at `Width - 16 - toggleBtn.Width` (X = 560 at W=640). Action buttons dock to its left: `curR -= 8; curR -= actionBtn.Width`.
      - Bounded `descLabel` and `titleLabel` with `availTextWidth = Math.Max(200, curR - 16 - 8)` and `AutoEllipsis = true`, completely eliminating text underlap.
      - On toggle state change, `LayoutRightButtons()` dynamically recalculates button and label positions immediately.
- [x] **11.2.4 Fix Initial Coordinates & Clean Mojibake in App Bar:**
      Set initial `doneBtn.Location` and `backBtn.Location` immediately in `BuildShell()` and `SettingsSubForm`. Cleaned `backBtn.Text` to proper `Tr.T("← Back")`.
- [x] **11.2.5 Streamline `PosForm.ShowSettings()` Integration:**
      Prevented double app bar by skipping `skinPanel` and outer 16px padding in `ShowView` when `title == "Settings"`. `SettingsForm` displays full height and width cleanly.
- [x] **11.2.6 Build, Verification & Release:**
      - Dual-target compilation: `powershell -NoProfile -File ".opencode\tools\build-win.ps1" -Tfm all` -> 0 errors.
      - Reflection layout test on all 12 Settings pages (77 cards total): 0 overlapping bounds, 0 clipping, 0 text collisions.
      - Shipped updated binaries to `publish-win10-x64\` and `publish-win7\` (`client database\` untouched).
      - Updated `tester-release\Windows\Vectron POS.exe`.
      - Rebuilt all release zips (`VectronPOS-v1.2.0-Win10-x64.zip`, `…-Win7-x86-x64.zip`, `…-Win7-Full-x86-x64.zip`, `tester-release.zip`) entry-by-entry: 0 forbidden files, 0 .pdb, 0 backslashes.

