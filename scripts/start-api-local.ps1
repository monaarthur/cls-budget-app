param(
    [string]$ConfigFile = (Join-Path $PSScriptRoot "local-api.appsettings.json")
)

$ErrorActionPreference = "Stop"

$apiDir = Resolve-Path (Join-Path $PSScriptRoot "..\backend\src\CLS.Budget.Api")
$devSettings = Join-Path $apiDir "appsettings.Development.json"

if (Test-Path $ConfigFile) {
    Copy-Item $ConfigFile $devSettings -Force
    Write-Host "Using $ConfigFile"
}
elseif (-not (Test-Path $devSettings)) {
    throw @"
Missing local API config. Create one of:
  scripts\local-api.appsettings.json  (copy from local-api.appsettings.example.json)
  backend\src\CLS.Budget.Api\appsettings.Development.json

Also remove User env var ConnectionStrings__BudgetDatabase if it points at Supabase (it overrides appsettings).
"@
}

# Process-level overrides beat User-level Supabase env vars from migration testing
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:Auth__Enabled = "false"
Remove-Item Env:ConnectionStrings__BudgetDatabase -ErrorAction SilentlyContinue

Push-Location $apiDir
try {
    Write-Host "Starting CLS Budget API at http://localhost:5123"
    Write-Host "Press Ctrl+C to stop."
    Write-Host ""
    dotnet run --launch-profile http
}
finally {
    Pop-Location
}
