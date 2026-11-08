# Vectron Kiosk — Self-Service Ordering Terminal (Windows 10 / 11)

Self-service ordering totem application designed for vertical touchscreen kiosks.

## Overview
- **Binary:** `Vectron Kiosk.exe` (~116 MB standalone single-file executable).
- **Features:** Touch-drag scrolling, category filters, interactive modifier selection, Dine-In / Takeaway routing, and order sync.
- **Target Framework:** `net10.0-windows`
- **Source Code:** Located inside the [`src/`](src/) subfolder (`VectronKiosk.csproj`).

## How to Build Locally
Run the Kiosk build script from the repository root:
```powershell
powershell -NoProfile -File ".opencode\tools\build-kiosk.ps1"
```
This compiles the application and outputs `Vectron Kiosk.exe` directly into this folder.

## Configuration
- `kiosk-config.json` — Specifies the POS server API URL, station ID, and kiosk mode.
- `images/kiosk/` — Studio product photography catalog.
