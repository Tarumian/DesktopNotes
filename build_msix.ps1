param (
    [string]$PackageName = "269BA93C.Hushak",
    [string]$Publisher = "CN=70178408-4638-422F-8F2D-C129B0757B8E",
    [string]$PublisherDisplayName = "",
    [string]$Version = "1.5.0.0",
    [string]$DisplayName = "Hushak",
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [bool]$SelfContained = $false,
    [bool]$SignForLocalTesting = $true
)

if ([string]::IsNullOrWhiteSpace($PublisherDisplayName)) {
    $defaultPubNameBytes = [byte[]]@(0xD4, 0xB9, 0xD5, 0xA1, 0xD6, 0x80, 0xD5, 0xB8, 0xD6, 0x82, 0xD5, 0xB4, 0xD5, 0xA5, 0xD5, 0xA1, 0xD5, 0xB6, 0x20, 0xD4, 0xBE, 0xD6, 0x80, 0xD5, 0xA1, 0xD5, 0xA3, 0xD6, 0x80, 0xD5, 0xA5, 0xD6, 0x80)
    $PublisherDisplayName = [System.Text.Encoding]::UTF8.GetString($defaultPubNameBytes)
}

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

Write-Host "=== 1. Locating makeappx.exe & signtool.exe ===" -ForegroundColor Cyan
$MakeAppx = "$env:TEMP\msix_tools\bin\10.0.26100.0\x64\makeappx.exe"
$SignTool = "$env:TEMP\msix_tools\bin\10.0.26100.0\x64\signtool.exe"

if (-not (Test-Path $MakeAppx)) {
    Write-Host "Downloading Microsoft.Windows.SDK.BuildTools..." -ForegroundColor Yellow
    $zip = "$env:TEMP\msix_tools.zip"
    $dest = "$env:TEMP\msix_tools"
    Invoke-WebRequest -Uri "https://www.nuget.org/api/v2/package/Microsoft.Windows.SDK.BuildTools/10.0.26100.1742" -OutFile $zip
    Expand-Archive -Path $zip -DestinationPath $dest -Force
}

Write-Host "makeappx: $MakeAppx"

Write-Host "=== 2. Publishing .NET Application ===" -ForegroundColor Cyan
$PublishDir = Join-Path $ScriptDir "bin\msix_publish"
if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }

dotnet publish "$ScriptDir\Hushak.csproj" -c $Configuration -r $Runtime --self-contained $SelfContained -o $PublishDir

Write-Host "=== 3. Preparing MSIX Layout Directory ===" -ForegroundColor Cyan
$LayoutDir = Join-Path $ScriptDir "bin\msix_layout"
if (Test-Path $LayoutDir) { Remove-Item $LayoutDir -Recurse -Force }
New-Item -ItemType Directory -Path $LayoutDir | Out-Null

# Copy published binaries to layout
Copy-Item -Path "$PublishDir\*" -Destination $LayoutDir -Recurse -Force

# Create Assets directory inside layout
$AssetsDir = Join-Path $LayoutDir "Assets"
New-Item -ItemType Directory -Path $AssetsDir -Force | Out-Null

Write-Host "=== 4. Generating Visual Assets ===" -ForegroundColor Cyan
Add-Type -AssemblyName System.Drawing

function Create-LogoImage([string]$sourceIco, [string]$targetPng, [int]$width, [int]$height) {
    $bmp = New-Object System.Drawing.Bitmap $width, $height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    if (Test-Path "$ScriptDir\Assets\Hushak.ico") {
        $icon = New-Object System.Drawing.Icon ("$ScriptDir\Assets\Hushak.ico")
        $g.DrawIcon($icon, [System.Drawing.Rectangle]::new(0, 0, $width, $height))
        $icon.Dispose()
    }
    $g.Dispose()
    $bmp.Save($targetPng, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

$icoPath = "$ScriptDir\Assets\Hushak.ico"
Create-LogoImage $icoPath "$AssetsDir\Square150x150Logo.png" 150 150
Create-LogoImage $icoPath "$AssetsDir\Square44x44Logo.png" 44 44
Create-LogoImage $icoPath "$AssetsDir\StoreLogo.png" 50 50
Create-LogoImage $icoPath "$AssetsDir\Wide310x150Logo.png" 310 150
Create-LogoImage $icoPath "$AssetsDir\SplashScreen.png" 620 300

Write-Host "=== 5. Generating AppxManifest.xml ===" -ForegroundColor Cyan
$manifestContent = @"
<?xml version="1.0" encoding="utf-8"?>
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  IgnorableNamespaces="uap rescap">

  <Identity
    Name="$PackageName"
    Publisher="$Publisher"
    Version="$Version"
    ProcessorArchitecture="x64" />

  <Properties>
    <DisplayName>$DisplayName</DisplayName>
    <PublisherDisplayName>$PublisherDisplayName</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
  </Properties>

  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.22621.0" />
  </Dependencies>

  <Resources>
    <Resource Language="en-us" />
    <Resource Language="hy-am" />
    <Resource Language="ru-ru" />
  </Resources>

  <Applications>
    <Application Id="Hushak"
      Executable="Hushak.exe"
      EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements
        DisplayName="$DisplayName"
        Description="$DisplayName Application"
        BackgroundColor="transparent"
        Square150x150Logo="Assets\Square150x150Logo.png"
        Square44x44Logo="Assets\Square44x44Logo.png">
        <uap:DefaultTile Wide310x150Logo="Assets\Wide310x150Logo.png" />
        <uap:SplashScreen Image="Assets\SplashScreen.png" />
      </uap:VisualElements>
    </Application>
  </Applications>

  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
</Package>
"@

$manifestPath = Join-Path $LayoutDir "AppxManifest.xml"
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($manifestPath, $manifestContent, $utf8NoBom)

Write-Host "=== 6. Packing MSIX ===" -ForegroundColor Cyan
$OutputDir = Join-Path $ScriptDir "bin\Package"
if (-not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir | Out-Null }
$MsixPath = Join-Path $OutputDir "Hushak_$Version.msix"

if (Test-Path $MsixPath) { Remove-Item $MsixPath -Force }

& $MakeAppx pack /d $LayoutDir /p $MsixPath /nv /o

if ($LASTEXITCODE -ne 0) {
    throw "makeappx pack failed with exit code $LASTEXITCODE"
}

Write-Host "MSIX successfully created: $MsixPath" -ForegroundColor Green

if ($SignForLocalTesting) {
    Write-Host "=== 7. Signing MSIX with Self-Signed Certificate for Local Testing ===" -ForegroundColor Cyan
    $certSubject = $Publisher
    $cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq $certSubject } | Select-Object -First 1
    if (-not $cert) {
        Write-Host "Creating local self-signed test certificate..." -ForegroundColor Yellow
        $cert = New-SelfSignedCertificate -Type Custom -Subject $certSubject `
            -KeyUsage DigitalSignature `
            -FriendlyName "Hushak Test Cert" `
            -CertStoreLocation "Cert:\CurrentUser\My" `
            -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
    }
    
    # Export cert to file
    $cerPath = Join-Path $OutputDir "HushakTest.cer"
    Export-Certificate -Cert $cert -FilePath $cerPath -Force | Out-Null

    # Sign MSIX
    & $SignTool sign /fd SHA256 /a /sha1 $cert.Thumbprint $MsixPath
    Write-Host "MSIX signed with local certificate: $($cert.Thumbprint)" -ForegroundColor Green
    Write-Host "Certificate saved to: $cerPath (Install to 'Trusted People' if testing locally)" -ForegroundColor Yellow
}

Write-Host "`n>>> MSIX Package ready at: $MsixPath <<<" -ForegroundColor Green
