$ErrorActionPreference = "Stop"

$root       = $PSScriptRoot
$outDir     = Join-Path $root "publish"
$nodeVer    = "v20.17.0"
$nodeDist   = "node-$nodeVer-win-x64"
$nodeZipUrl = "https://nodejs.org/dist/$nodeVer/$nodeDist.zip"

if (Test-Path $outDir) { Remove-Item -Recurse -Force $outDir }

Write-Host "[1/4] Publishing .NET app (self-contained)..." -ForegroundColor Cyan
dotnet publish "$root\TailorShop.csproj" -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=false -o $outDir | Out-Null

Write-Host "[2/4] Installing Node dependencies (no Chromium download)..." -ForegroundColor Cyan
$svcOut = Join-Path $outDir "WhatsAppService"
New-Item -ItemType Directory -Force -Path $svcOut | Out-Null
Copy-Item "$root\WhatsAppService\*.js"        $svcOut -Force
Copy-Item "$root\WhatsAppService\package.json" $svcOut -Force
Copy-Item "$root\WhatsAppService\.npmrc"       $svcOut -Force
Push-Location $svcOut
$env:PUPPETEER_SKIP_DOWNLOAD = "true"
npm install --omit=dev --no-audit --no-fund | Out-Null
Pop-Location

Write-Host "[3/4] Bundling portable Node runtime..." -ForegroundColor Cyan
$nodeDir = Join-Path $svcOut "node"
if (-not (Test-Path (Join-Path $nodeDir "node.exe"))) {
    $tmpZip = Join-Path $env:TEMP "$nodeDist.zip"
    if (-not (Test-Path $tmpZip)) { Invoke-WebRequest -Uri $nodeZipUrl -OutFile $tmpZip }
    Expand-Archive -Path $tmpZip -DestinationPath $env:TEMP -Force
    New-Item -ItemType Directory -Force -Path $nodeDir | Out-Null
    Copy-Item (Join-Path $env:TEMP "$nodeDist\node.exe") $nodeDir -Force
}

Write-Host "[4/4] Cleaning session data..." -ForegroundColor Cyan
Remove-Item -Recurse -Force (Join-Path $svcOut ".wwebjs_auth")  -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force (Join-Path $svcOut ".wwebjs_cache") -ErrorAction SilentlyContinue
Remove-Item -Force (Join-Path $outDir "golden_tailor.db")       -ErrorAction SilentlyContinue
Remove-Item -Force (Join-Path $outDir "whatsapp-service.log")   -ErrorAction SilentlyContinue

$size = "{0:N0} MB" -f ((Get-ChildItem $outDir -Recurse | Measure-Object Length -Sum).Sum / 1MB)
Write-Host ""
Write-Host "Done. Output: $outDir  ($size)" -ForegroundColor Green
Write-Host "Customer needs: Windows 10/11 only (Edge is built in). No .NET, no Node.js install required."
