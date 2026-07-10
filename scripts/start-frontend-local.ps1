param(
    [int]$Port = 3000
)

$ErrorActionPreference = "Stop"
$webRoot = Resolve-Path (Join-Path $PSScriptRoot "..\frontend\cls-budget-web")

# Use Next.js proxy to API (no CORS). Do not set NEXT_PUBLIC_API_BASE_URL here.
$env:NEXT_PUBLIC_AUTH_ENABLED = "false"
Remove-Item Env:NEXT_PUBLIC_API_BASE_URL -ErrorAction SilentlyContinue
Remove-Item Env:NEXT_OUTPUT -ErrorAction SilentlyContinue

Push-Location $webRoot
try {
    if (Test-Path "middleware.static-export.bak") {
        if (Test-Path "middleware.ts") { Remove-Item "middleware.ts" -Force }
        Move-Item "middleware.static-export.bak" "middleware.ts" -Force
        Write-Host "Restored middleware.ts from static-export backup." -ForegroundColor Yellow
    }
    if (Test-Path "api.static-export.bak") {
        if (Test-Path "app\api") { Remove-Item "app\api" -Recurse -Force }
        Move-Item "api.static-export.bak" "app\api" -Force
    }
    if (Test-Path "logo-actions.static-export.bak") {
        if (Test-Path "app\logo-actions") { Remove-Item "app\logo-actions" -Recurse -Force }
        Move-Item "logo-actions.static-export.bak" "app\logo-actions" -Force
    }
    Remove-Item ".next\dev\lock" -Force -ErrorAction SilentlyContinue

    Write-Host "Starting frontend at http://localhost:$Port (auth off, matches start-api-local.ps1)" -ForegroundColor Cyan
    Write-Host "API should be running: .\scripts\start-api-local.ps1" -ForegroundColor DarkGray
    Write-Host ""
    npx next dev -p $Port
}
finally {
    Pop-Location
}
