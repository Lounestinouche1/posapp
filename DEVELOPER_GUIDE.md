# TL POS Suite — Developer & Architecture Guide
**Author & Owner:** Lounes Tinouche (TL)  
**Dual Target Engines:** `net10.0-windows` (Modern Win 10/11) & `net48` (Windows 7 SP1)  
**Mobile Companion:** .NET MAUI Android (`net10.0-android`)  

---

## 1. Architectural Philosophy

**TL POS** is designed for extreme reliability in harsh, real-world retail and restaurant environments where sudden power cuts, unreliable Wi-Fi, and unskilled cashiers are everyday realities.

Key architectural pillars:
1. **Zero External Database Overhead:**  
   Unlike traditional systems requiring Microsoft SQL Server, PostgreSQL, or MySQL (which easily get corrupted on sudden power cuts and require Windows background services), TL POS uses a high-performance **atomic JSON persistence layer** (`PosStore`). All customer databases reside strictly in `client database\` and can be backed up or transferred simply by copying that folder.
2. **Dual-TFM Compilation:**  
   The same C# codebase targets both `.NET 10.0` (self-contained, ultra-fast 64-bit runtime for modern all-in-one capacitive terminals) and `.NET Framework 4.8` (for older Atom D525, POSReady 7, and Windows 7 SP1 touchscreens).
3. **Layered Single-File Shell (`Program.cs`):**  
   The core Windows POS interface and business logic live in `Program.cs` strictly layered top-to-bottom. This eliminates circular dependencies, allows sub-second hot compilation, and ensures atomic code distribution.

---

## 2. Codebase Organization & File Map

```
D:\VectronPOS\
├── Program.cs             <- The primary engine: Models, Skin, PosForm, Hardware, REST API
├── Lang.cs                <- Localization dictionary (EN, FR, AR with custom RTL glyph shaping)
├── License.cs             <- Asymmetric ECDSA hardware-fingerprint license gate
├── LicenseForm.cs         <- Activation and machine-code import dialog
├── TicketLayout.cs        <- WYSIWYG Ticket Designer & layout formatting models
├── Code39.cs / QrCode.cs  <- Native barcode and QR code renderers (zero external dependencies)
├── Net48Polyfills.cs      <- Shims enabling modern C# records/patterns on .NET 4.8
├── VectronPOS.csproj      <- Multi-target build definitions and publish rules
│
├── publish-android\src\   <- .NET MAUI Android Waiter tablet application
├── publish-kiosk\src\     <- Touchscreen Self-Ordering Kiosk terminal
│
├── docs\manual\           <- Official User & Admin Manuals (PDF, HTML, Markdown)
└── .opencode\tools\       <- Build automation scripts (build-win.ps1, build-apk.ps1, build-kiosk.ps1)
```

---

## 3. The 10 Architectural Zones of `Program.cs`

`Program.cs` is strictly organized top-to-bottom. You can search directly for these section banners:

| Section Banner | Focus & Responsibilities |
|---|---|
| **SECTION 01: ENTRY POINT** | Application lifecycle, safe restart (`Program.SafeRestart()`), and license gate check. |
| **SECTION 02: DATA MODELS** | Domain records: `Product`, `PosOrder`, `OrderItem`, `TableInfo`, `FeatureSettings` (~90 toggles). |
| **SECTION 03: BUSINESS PROFILES** | `BusinessProfiles` engine: 1-click profiles (Supermarket, Boutique, Cafe, Restaurant, Pizzeria, Pharmacy) & Custom Safeguard. |
| **SECTION 04: PERSISTENCE (`PosStore`)** | Atomic JSON reading/writing into `client database\`. Methods: `SaveProducts()`, `SaveOrders()`, `SaveFeatures()`. |
| **SECTION 05: DESIGN SYSTEM (`Skin`)** | Dark/Light themes, Auto/High/Lite graphics tiers, color tokens, and font scale. |
| **SECTION 06: CUSTOM CONTROLS** | Touch controls: `TLMark`, `GhostButton`, `ModernToggle`, `FeatureCard`, numeric keypads. |
| **SECTION 07: MAIN CASHIER SHELL (`PosForm`)** | The primary UI form: navigation rail, table floor plans, order pad, item modifiers, payments. |
| **SECTION 08: SETTINGS & ADMIN** | `SettingsForm`: store setup, operator management, tax rates, profile switching. |
| **SECTION 09: PRINTING ENGINE (`RawPrinter`)**| ESC/POS thermal printing, Windows print drivers, cash drawer kick pulse, EAN-13 weight-scale parser. |
| **SECTION 10: EMBEDDED REST API** | `QrOrderServer`: port 8080 HTTP server handling Android Waiter sync and Tailscale remote access. |

---

## 4. How the Mobile Waiter Sync Works (Port 8080)

The main Windows POS runs an embedded HTTP listener on port `8080` (`QrOrderServer`). It does not require IIS, Apache, or Docker.

### Key REST Endpoints:
* `GET /api/tablet/bootstrap?key=<ApiKey>` — Handshake; returns operator list, rooms, floor plans, and server timestamp.
* `GET /api/menu?key=<ApiKey>` — Fetches categories, active products, prices, and modifier sheets.
* `GET /api/tables?key=<ApiKey>` — Returns live table occupancy and open baskets.
* `POST /api/order?key=<ApiKey>` — Receives a submitted order from a tablet, writes it to `open-orders.json`, and triggers kitchen thermal printers.
* `GET /reports?key=<ApiKey>` — Web dashboard for remote owners to monitor sales and revenue.

---

## 5. Developer Recipes (How to Modify & Extend)

### Recipe A: Adding a New Feature Toggle
1. Open `Program.cs` → `SECTION 02 (FeatureSettings)`.
2. Add a boolean property:
   ```csharp
   public bool MyNewFeature { get; set; } = false;
   ```
3. Open `SECTION 08 (SettingsForm)` → `PopulateAdvanced()` or relevant tab.
4. Add the toggle card:
   ```csharp
   AddFeatureCard("Feature Title", "Description for the user", features.MyNewFeature, v => {
       features.MyNewFeature = v;
       saveFeatures(features);
   });
   ```
5. Use `store.Features.MyNewFeature` anywhere in `PosForm` to conditionally enable behavior.

### Recipe B: Adding a New Store Profile
1. Open `Program.cs` → `SECTION 03 (BusinessProfiles)`.
2. Define a new constant:
   ```csharp
   public const string Bakery = "bakery";
   ```
3. In `ApplyProfile(string profile, FeatureSettings f)`, add your preset toggles.
4. In `SettingsForm.PopulateProfiles()`, add a preset button calling `ApplyProfile(BusinessProfiles.Bakery, features)`.

---

## 6. Build Commands

Always execute builds using the PowerShell automation scripts in `.opencode\tools\`:

```powershell
# 1. Compile Windows POS (both .NET 10 and .NET 4.8)
powershell -NoProfile -ExecutionPolicy Bypass -File ".opencode\tools\build-win.ps1" -Tfm all

# 2. Compile and Ship binaries to release directories (preserves client database)
powershell -NoProfile -ExecutionPolicy Bypass -File ".opencode\tools\build-win.ps1" -Tfm all -Ship

# 3. Build Kiosk Totem
powershell -NoProfile -ExecutionPolicy Bypass -File ".opencode\tools\build-kiosk.ps1"

# 4. Build Android Waiter APK
powershell -NoProfile -ExecutionPolicy Bypass -File ".opencode\tools\build-apk.ps1"
```

---
*© 2026 TL POS Suite • Developed by Lounes Tinouche (TL)*
