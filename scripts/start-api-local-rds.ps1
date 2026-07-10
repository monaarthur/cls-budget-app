param(
    [string]$ConfigFile = (Join-Path $PSScriptRoot "local-api-rds.appsettings.json")
)

$ErrorActionPreference = "Stop"

$apiDir = Resolve-Path (Join-Path $PSScriptRoot "..\backend\src\CLS.Budget.Api")
$devSettings = Join-Path $apiDir "appsettings.Development.json"
$example = Join-Path $PSScriptRoot "local-api-rds.appsettings.example.json"

if (-not (Test-Path $ConfigFile)) {
    if (Test-Path $example) {
        throw @"
Missing $ConfigFile
Copy and edit:
  copy scripts\local-api-rds.appsettings.example.json scripts\local-api-rds.appsettings.json
Set RDS password, Admin:ApiKey, and PasswordReset:FrontendBaseUrl (CloudFront URL for setup links).
"@
    }
    throw "Missing $ConfigFile"
}

Copy-Item $ConfigFile $devSettings -Force
Write-Host "Using $ConfigFile (production RDS database)" -ForegroundColor Cyan

$config = Get-Content $ConfigFile -Raw | ConvertFrom-Json
$frontend = $config.PasswordReset.FrontendBaseUrl
if ($frontend) {
    Write-Host "Setup links will point to: $frontend" -ForegroundColor DarkGray
}

$env:ASPNETCORE_ENVIRONMENT = "Development"
Remove-Item Env:ConnectionStrings__BudgetDatabase -ErrorAction SilentlyContinue
Remove-Item Env:Auth__Enabled -ErrorAction SilentlyContinue

Push-Location $apiDir
try {
    Write-Host ""
    Write-Host "Starting CLS Budget API at http://localhost:5123 (RDS backend)" -ForegroundColor Cyan
    Write-Host "Press Ctrl+C to stop."
    Write-Host ""
    dotnet run --launch-profile http
}
finally {
    Pop-Location
}
