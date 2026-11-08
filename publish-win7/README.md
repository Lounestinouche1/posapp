# Vectron POS — Windows 7 SP1 & POSReady 7 Terminal (x86 / x64)

Legacy Windows 7 Point of Sale application designed for classic hardware and Intel Atom/Celeron terminals.

## Overview
- **Binary:** `Vectron POS.exe` (~1.15 MB lightweight executable) + BCL runtime dependencies.
- **Runtime:** Microsoft .NET Framework 4.8.
- **Target Framework:** `net48`
- **Source Code:** Main project files at repository root ([`Program.cs`](../Program.cs), [`VectronPOS.csproj`](../VectronPOS.csproj)).

## How to Build Locally
Run the build script from the repository root:
```powershell
powershell -NoProfile -File ".opencode\tools\build-win.ps1" -Tfm net48 -Ship
```
This compiles the application and outputs `Vectron POS.exe` and its supporting DLL libraries directly into this folder.

## Folder Structure
- `Vectron POS.exe` — Main executable.
- `*.dll` — Supporting framework libraries (`System.Text.Json`, `System.Memory`, etc.).
- `client database\` — Local store configuration, tables, menu, operators, and ticket data (kept local and protected).
- `images\` — Product photos and category thumbnails.

## Pre-built Releases
Pre-packaged release ZIPs can be downloaded directly from the GitHub repository **Releases** tab without needing to compile.
