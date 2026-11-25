# scripts/package_web_release.ps1
param (
    [string]$Version = "",
    [string]$PageUrl = "https://tarumian.am/hushak/",
    [string]$DownloadBaseUrl = "",
    [string]$ReleaseNotesHy = "Կայքի (tarumian.am) և GitLab-ի թողարկում՝ ներկառուցված արդիացումների ստուգումով և բարելավված տեղադրիչով։",
    [string]$ReleaseNotesEn = "Hushak release for tarumian.am and GitLab with built-in update checker and improved installer."
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir

function Set-Utf8NoBom([string]$path, [string]$content) {
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, $content, $encoding)
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $csprojPath = Join-Path $RootDir "Hushak.csproj"
    if (Test-Path $csprojPath) {
        $xml = [xml](Get-Content $csprojPath -Raw)
        $Version = $xml.Project.PropertyGroup.Version
    }
    if ([string]::IsNullOrWhiteSpace($Version)) {
        throw "Could not determine Version from Hushak.csproj."
    }
}

if ([string]::IsNullOrWhiteSpace($DownloadBaseUrl)) {
    $DownloadBaseUrl = "https://gitlab.com/Tarumian/hushak/-/releases/v$Version/downloads"
}
$DownloadBaseUrl = $DownloadBaseUrl.TrimEnd('/')

$StandaloneExe = Join-Path $RootDir "publish\standalone\Hushak.exe"
$InstallerExe = Join-Path $RootDir "publish\installer\Hushak_Setup_v$Version.exe"
$LicenseFile = Join-Path $RootDir "LICENSE"

if (-not (Test-Path $StandaloneExe)) {
    throw "Standalone executable not found: $StandaloneExe"
}
if (-not (Test-Path $InstallerExe)) {
    throw "Installer executable not found: $InstallerExe"
}

$ReleaseDir = Join-Path $RootDir "publish\hushak"
if (Test-Path $ReleaseDir) {
    Remove-Item $ReleaseDir -Recurse -Force
}
New-Item -ItemType Directory -Path $ReleaseDir -Force | Out-Null

# 1. Copy Installer
$InstallerName = "Hushak_Setup_v$Version.exe"
$ReleaseInstallerPath = Join-Path $ReleaseDir $InstallerName
Copy-Item -Path $InstallerExe -Destination $ReleaseInstallerPath -Force

# 2. Build Portable ZIP
$PortableName = "Hushak_Portable_v$Version.zip"
$ReleasePortablePath = Join-Path $ReleaseDir $PortableName
$StagingDir = Join-Path $env:TEMP "hushak_portable_stage_$Version"
if (Test-Path $StagingDir) { Remove-Item $StagingDir -Recurse -Force }
New-Item -ItemType Directory -Path $StagingDir -Force | Out-Null
Copy-Item -Path $StandaloneExe -Destination (Join-Path $StagingDir "Hushak.exe") -Force
if (Test-Path $LicenseFile) {
    Copy-Item -Path $LicenseFile -Destination (Join-Path $StagingDir "LICENSE") -Force
}
Compress-Archive -Path "$StagingDir\*" -DestinationPath $ReleasePortablePath -CompressionLevel Optimal -Force
Remove-Item $StagingDir -Recurse -Force

# 3. Compute SHA-256 and sizes
$InstallerHash = (Get-FileHash -Path $ReleaseInstallerPath -Algorithm SHA256).Hash.ToLowerInvariant()
$PortableHash = (Get-FileHash -Path $ReleasePortablePath -Algorithm SHA256).Hash.ToLowerInvariant()
$InstallerSize = (Get-Item $ReleaseInstallerPath).Length
$PortableSize = (Get-Item $ReleasePortablePath).Length

# 4. Write SHA256SUMS.txt
$sumsContent = "$InstallerHash  $InstallerName`r`n$PortableHash  $PortableName`r`n"
Set-Utf8NoBom (Join-Path $ReleaseDir "SHA256SUMS.txt") $sumsContent

# 5. Write version.json (both in publish/hushak/ and repo root for GitLab raw URL)
$manifestObj = [ordered]@{
    version        = $Version
    releaseDate    = (Get-Date).ToString("yyyy-MM-dd")
    pageUrl        = $PageUrl
    downloadUrl    = "$DownloadBaseUrl/$InstallerName"
    portableUrl    = "$DownloadBaseUrl/$PortableName"
    sha256         = $InstallerHash
    portableSha256 = $PortableHash
    sizeBytes      = $InstallerSize
    portableBytes  = $PortableSize
    releaseNotes   = [ordered]@{
        hy = $ReleaseNotesHy
        en = $ReleaseNotesEn
    }
}
$jsonContent = $manifestObj | ConvertTo-Json -Depth 4
Set-Utf8NoBom (Join-Path $ReleaseDir "version.json") $jsonContent
Set-Utf8NoBom (Join-Path $RootDir "version.json") $jsonContent

Write-Host "`n=== Release package ready for Hushak (GitLab + tarumian.am) ===" -ForegroundColor Green
Write-Host "Folder: $ReleaseDir" -ForegroundColor Cyan
Write-Host "  - $InstallerName ($([math]::Round($InstallerSize / 1MB, 2)) MB) | SHA256: $InstallerHash"
Write-Host "  - $PortableName ($([math]::Round($PortableSize / 1MB, 2)) MB) | SHA256: $PortableHash"
Write-Host "  - version.json (also updated in repo root)"
Write-Host "  - SHA256SUMS.txt"
