# Vectron Waiter — Android Tablet Application (.NET MAUI)

Mobile waiter ordering application for Android tablets and handheld POS devices.

## Overview
- **Binary:** `Vectron-Waiter-v1.2.0.apk` (~118 MB).
- **Target Framework:** `net10.0-android` (.NET MAUI).
- **Source Code:** Located inside the [`src/`](src/) subfolder (`VectronTablet.csproj`).
- **Communication:** Connects via REST API to the main Windows POS server on port `8080` (`/api/menu`, `/api/orders`, `/api/tables`, `/api/waiter/call`).

## How to Build the APK Locally
Run the Android build script from the repository root:
```powershell
powershell -NoProfile -File ".opencode\tools\build-apk.ps1"
```
This builds and stages the signed APK directly into this folder.

## Installation
1. Copy `Vectron-Waiter-v1.2.0.apk` to the Android tablet.
2. Enable "Install Unknown Apps" in Android Settings.
3. Tap the APK to install.
4. Launch the app and enter the server IP of the main Windows POS terminal.
