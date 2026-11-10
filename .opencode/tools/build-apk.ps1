<#
  build-apk.ps1 — build the Vectron Waiter Android app and stage the signed APK at the repo root.

  Usage:
    .\build-apk.ps1               # build + copy to <repo>\Vectron-Waiter-v1.2.0.apk
    .\build-apk.ps1 -NoCopy       # build only

  NOTE (this PC): no emulator/device possible (BIOS VT-x is disabled and there is no
  hypervisor), so the APK can only be verified on a real tablet here.
#>
param([switch]$NoCopy)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot\..\..").Path
if (-not (Test-Path (Join-Path $root 'VectronPOS.csproj'))) { $root = Split-Path $PSScriptRoot -Parent }
if (-not (Test-Path (Join-Path $root 'VectronPOS.csproj'))) { $root = 'F:\vscode projects\VectronPOS' }
$proj = Join-Path $root 'publish-android\src\VectronTablet.csproj'
if (-not (Test-Path $proj)) { $proj = Join-Path $root 'VectronTablet\VectronTablet.csproj' }

$dotnet = 'C:\Users\serveur-it\.dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }
$jdk = 'C:\Java\jdk-17.0.20.1+1'
if (-not (Test-Path $jdk)) { $jdk = $env:JAVA_HOME }
$sdk = 'C:\Android\Sdk'
if (-not (Test-Path $sdk)) { $sdk = $env:ANDROID_HOME }

$env:JAVA_HOME = $jdk
$env:ANDROID_HOME = $sdk
$env:ANDROID_SDK_ROOT = $sdk

Write-Host "=== build net10.0-android (Release) ===" -ForegroundColor Cyan
& $dotnet publish $proj -f net10.0-android -c Release `
    -p:AndroidSdkDirectory="$sdk" -p:JavaSdkDirectory="$jdk"
if ($LASTEXITCODE -ne 0) { throw "android publish failed (exit $LASTEXITCODE)" }

$projDir = Split-Path $proj -Parent
$apk = Get-ChildItem (Join-Path $projDir 'bin\Release\net10.0-android') -Filter '*-Signed.apk' | Select-Object -ExpandProperty FullName -First 1
if (-not $apk -or -not (Test-Path $apk)) { throw "signed APK not found: $apk" }

if (-not $NoCopy) {
    $dest = Join-Path $root 'publish-android\Vectron-Waiter-v1.2.0.apk'
    Copy-Item $apk $dest -Force
    Copy-Item $apk (Join-Path $root 'publish-android\Vectron-Waiter.apk') -Force
    Copy-Item $apk (Join-Path $root 'Vectron-Waiter-v1.2.0.apk') -Force
    Write-Host ("staged -> {0} ({1:N1} MB)" -f $dest, ((Get-Item $dest).Length / 1MB)) -ForegroundColor Green
} else {
    Write-Host ("APK: {0} ({1:N1} MB)" -f $apk, ((Get-Item $apk).Length / 1MB)) -ForegroundColor Green
}
