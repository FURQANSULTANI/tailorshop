$ErrorActionPreference = "Stop"

$newVersion = $PSScriptRoot
$dataFolder = Join-Path $env:ProgramData "TailorShop"
$dbFile     = Join-Path $dataFolder "golden_tailor.db"

Write-Host ""
Write-Host "National Tailor Update" -ForegroundColor Cyan
Write-Host "=================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Where is National Tailor currently installed?"
Write-Host "(the folder that contains the existing NationalTailor.exe)"
$target = (Read-Host "Path").Trim('"').Trim()

if (-not (Test-Path (Join-Path $target "NationalTailor.exe"))) {
    Write-Host "NationalTailor.exe not found in that folder. Nothing was changed." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

if ((Resolve-Path $target).Path -eq (Resolve-Path $newVersion).Path) {
    Write-Host "That is this same folder. Run this from the NEW version folder." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Get-Process NationalTailor -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process node -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -like "$target*" } | Stop-Process -Force
Start-Sleep -Seconds 2

if (Test-Path $dbFile) {
    $backupDir = Join-Path $dataFolder "Backups"
    New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
    $stamp = Get-Date -Format "yyyy-MM-dd_HHmm"
    Copy-Item $dbFile (Join-Path $backupDir "before_update_$stamp.db") -Force
    Write-Host "Data backed up before updating." -ForegroundColor Green
}

$session = Join-Path $target "WhatsAppService\.wwebjs_auth"
$keep    = Join-Path $env:TEMP "tailorshop_wwebjs_auth"
$hadSession = Test-Path $session
if ($hadSession) {
    if (Test-Path $keep) { Remove-Item -Recurse -Force $keep }
    Copy-Item $session $keep -Recurse -Force
}

Write-Host "Installing new version..." -ForegroundColor Cyan
Copy-Item "$newVersion\*" $target -Recurse -Force -Exclude "update.ps1"

if ($hadSession) {
    if (Test-Path $session) { Remove-Item -Recurse -Force $session }
    Copy-Item $keep $session -Recurse -Force
    Remove-Item -Recurse -Force $keep
    Write-Host "WhatsApp session kept - no need to scan the QR again." -ForegroundColor Green
}

Write-Host ""
Write-Host "Update complete. Customer data and license were not touched." -ForegroundColor Green
Write-Host "Data location: $dataFolder"
Write-Host ""
Read-Host "Press Enter to exit"
