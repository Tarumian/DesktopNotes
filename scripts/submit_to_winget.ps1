# scripts/submit_to_winget.ps1
# Helper script to prepare and guide submission of Hushak to microsoft/winget-pkgs

param (
    [string]$Version = "1.5.15"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
$ManifestDir = Join-Path $RootDir "manifests\t\Tarumian\Hushak\$Version"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   Winget Manifest Submission Helper for Hushak v$Version" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Check manifests exist
if (-not (Test-Path $ManifestDir)) {
    Write-Host "Manifest directory not found. Generating now..." -ForegroundColor Yellow
    & "$ScriptDir\update_winget_manifest.ps1" -Version $Version
}

# 2. Validate
Write-Host "`n1. Validating manifest files..." -ForegroundColor Cyan
& winget validate --manifest $ManifestDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "Validation failed. Please fix manifest issues before submitting."
    exit 1
}
Write-Host "[OK] All manifest files are 100% valid!" -ForegroundColor Green

Write-Host "`n2. Manifests location:" -ForegroundColor Cyan
Write-Host "   $ManifestDir" -ForegroundColor White
Get-ChildItem $ManifestDir | ForEach-Object { Write-Host "   - $($_.Name)" -ForegroundColor Gray }

Write-Host "`n----------------------------------------------------------" -ForegroundColor DarkGray
Write-Host "Ինչպես հրապարակել winget-ում (Two ways to submit):" -ForegroundColor Yellow
Write-Host "----------------------------------------------------------" -ForegroundColor DarkGray
Write-Host "ՏԱՐԲԵՐԱԿ 1. GitHub Actions (Ավտոմատ)" -ForegroundColor Green
Write-Host "  1. GitHub-ում բացեք Ձեր պահոցի Settings -> Secrets and variables -> Actions"
Write-Host "  2. Ստեղծեք նոր գաղտնիք (Secret) 'WINGET_TOKEN' անունով՝ Ձեր GitHub Personal Access Token-ով (public_repo իրավունքով):"
Write-Host "     Token ստեղծելու հղում՝ https://github.com/settings/tokens/new?scopes=public_repo"
Write-Host "  3. Ամեն անգամ, երբ GitHub-ում Release հրապարակեք (օր. v$Version), GitHub Actions-ը ավտոմատ PR կբացի microsoft/winget-pkgs-ում։`n"

Write-Host "ՏԱՐԲԵՐԱԿ 2. Ձեռքով PR բացել microsoft/winget-pkgs-ում (Manual PR)" -ForegroundColor Green
Write-Host "  1. Ֆորկ (Fork) արեք https://github.com/microsoft/winget-pkgs պահոցը ձեր GitHub էջում։"
Write-Host "  2. Պատճենեք 'manifests\t\Tarumian\Hushak\$Version' պանակը Ձեր ֆորկի նույն ճանապարհով։"
Write-Host "  3. Կատարեք commit և բացեք Pull Request դեպի microsoft/winget-pkgs:master:"
Write-Host "     PR Title: New package: Tarumian.Hushak version $Version"
Write-Host "==========================================================" -ForegroundColor Cyan
