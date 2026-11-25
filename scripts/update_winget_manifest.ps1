# scripts/update_winget_manifest.ps1
param (
    [string]$Version = "",
    [string]$InstallerPath = "",
    [string]$DownloadUrl = ""
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir

# Helper to write UTF-8 without BOM
function Set-Utf8NoBom([string]$path, [string]$content) {
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, $content, $encoding)
}

# 1. Determine version if not specified
if ([string]::IsNullOrWhiteSpace($Version)) {
    $csprojPath = Join-Path $RootDir "Hushak.csproj"
    if (Test-Path $csprojPath) {
        $xml = [xml](Get-Content $csprojPath -Raw)
        $Version = $xml.Project.PropertyGroup.Version
    }
    if ([string]::IsNullOrWhiteSpace($Version)) {
        $Version = "1.5.15"
    }
}

# 2. Determine installer path
if ([string]::IsNullOrWhiteSpace($InstallerPath)) {
    $InstallerPath = Join-Path $RootDir "publish\installer\Hushak_Setup_v$Version.exe"
}

if (-not (Test-Path $InstallerPath)) {
    Write-Error "Installer not found at '$InstallerPath'. Please build the installer first."
    exit 1
}

# 3. Calculate SHA256
Write-Host "Calculating SHA256 hash of installer..." -ForegroundColor Cyan
$sha256 = (Get-FileHash -Path $InstallerPath -Algorithm SHA256).Hash
Write-Host "File: $InstallerPath"
Write-Host "SHA256: $sha256" -ForegroundColor Green

# 4. Download URL
if ([string]::IsNullOrWhiteSpace($DownloadUrl)) {
    $DownloadUrl = "https://gitlab.com/Tarumian/hushak/-/releases/v$Version/downloads/Hushak_Setup_v$Version.exe"
}

# 5. Output directory
$ManifestDir = Join-Path $RootDir "manifests\t\Tarumian\Hushak\$Version"
if (-not (Test-Path $ManifestDir)) {
    New-Item -ItemType Directory -Path $ManifestDir -Force | Out-Null
}

# 6. Generate Tarumian.Hushak.yaml (version)
$versionYaml = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.version.1.9.0.schema.json

PackageIdentifier: Tarumian.Hushak
PackageVersion: $Version
DefaultLocale: en-US
ManifestType: version
ManifestVersion: 1.9.0
"@
Set-Utf8NoBom (Join-Path $ManifestDir "Tarumian.Hushak.yaml") $versionYaml

# 7. Generate Tarumian.Hushak.installer.yaml (installer)
$installerYaml = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.installer.1.9.0.schema.json

PackageIdentifier: Tarumian.Hushak
PackageVersion: $Version
InstallerType: inno
Scope: user
InstallModes:
  - interactive
  - silent
  - silentWithProgress
InstallerSwitches:
  Silent: /VERYSILENT /SUPPRESSMSGBOXES /NORESTART
  SilentWithProgress: /SILENT /SUPPRESSMSGBOXES /NORESTART
UpgradeBehavior: install
ProductCode: '{D1A39C25-9D22-4D39-8472-875BC31F58A0}_is1'
Installers:
  - Architecture: x64
    InstallerUrl: $DownloadUrl
    InstallerSha256: $sha256
ManifestType: installer
ManifestVersion: 1.9.0
"@
Set-Utf8NoBom (Join-Path $ManifestDir "Tarumian.Hushak.installer.yaml") $installerYaml

# 8. Generate Tarumian.Hushak.locale.en-US.yaml (en-US)
$localeEnYaml = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.defaultLocale.1.9.0.schema.json

PackageIdentifier: Tarumian.Hushak
PackageVersion: $Version
PackageLocale: en-US
Publisher: Tarumian
PublisherUrl: https://tarumian.am
Author: Ruben Tarumian
PackageName: Hushak
PackageUrl: https://tarumian.am/hushak/
License: MIT
LicenseUrl: https://gitlab.com/Tarumian/hushak/-/raw/main/LICENSE
Copyright: Copyright (c) 2026 Ruben Tarumian
ShortDescription: Lightweight, feature-rich desktop sticky notes application for Windows.
Description: |-
  Hushak is an elegant, multi-functional desktop sticky notes application for Windows built with .NET 8 WPF.
  Key features include:
  - Free note rotation and pinning
  - Rich text formatting and fonts
  - Ink drawing with pencil mode
  - Note stacks and organization
  - Full-text search across all notes
  - Excel export via ClosedXML
  - Multilingual interface (Armenian, English, Russian, and more)
Moniker: hushak
Tags:
  - sticky-notes
  - notes
  - desktop
  - productivity
  - armenian
  - wpf
ManifestType: defaultLocale
ManifestVersion: 1.9.0
"@
Set-Utf8NoBom (Join-Path $ManifestDir "Tarumian.Hushak.locale.en-US.yaml") $localeEnYaml

# 9. Generate Tarumian.Hushak.locale.hy-AM.yaml (hy-AM)
$localeHyYaml = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.locale.1.9.0.schema.json

PackageIdentifier: Tarumian.Hushak
PackageVersion: $Version
PackageLocale: hy-AM
Publisher: Թարումեան ծրագրեր
PublisherUrl: https://tarumian.am
Author: Ռուբեն Թարումյան
PackageName: Հուշակ
PackageUrl: https://tarumian.am/hushak/
License: MIT
LicenseUrl: https://gitlab.com/Tarumian/hushak/-/raw/main/LICENSE
Copyright: Copyright (c) 2026 Ռուբեն Թարումյան
ShortDescription: "Հարմարավետ, գեղեցիկ և բազմաֆունկցիոնալ գրասեղանի թերթիկներ Windows-ի համար"
Description: |-
  «Հուշակ»-ը նախատեսված է գրասեղանին արագ նշումներ կատարելու, կարևոր հիշեցումները միշտ աչքի առաջ պահելու, մատիտով ձեռագիր էսքիզներ անելու և բազմաթիվ թերթիկները տրցակների մեջ խմբավորելու համար։
  Հիմնական հնարավորությունները․
  - Թերթիկների ազատ պտտում և գամում
  - Տեքստի հարուստ ձևավորում և տառատեսակներ
  - Մատիտով ազատ նկարչություն
  - Թերթիկների տրցակներ
  - Որոնման հզոր համակարգ
  - Արտահանում Excel (ClosedXML)
  - Լիակատար հայերեն և բազմալեզու միջերես
Tags:
  - հուշակ
  - նշումներ
  - թերթիկներ
  - sticky-notes
ManifestType: locale
ManifestVersion: 1.9.0
"@
Set-Utf8NoBom (Join-Path $ManifestDir "Tarumian.Hushak.locale.hy-AM.yaml") $localeHyYaml

Write-Host "Winget manifest files generated in: $ManifestDir" -ForegroundColor Green

# 10. Run validation
Write-Host "Validating manifests with winget..." -ForegroundColor Cyan
& winget validate --manifest $ManifestDir
if ($LASTEXITCODE -eq 0) {
    Write-Host "`n[SUCCESS] Manifest is valid and ready for submission to microsoft/winget-pkgs!" -ForegroundColor Green
} else {
    Write-Warning "winget validation reported issues."
}
