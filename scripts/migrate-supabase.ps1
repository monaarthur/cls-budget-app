param(
    [Parameter(Mandatory = $true)]
    [string]$ConnectionString
)

$ErrorActionPreference = "Stop"

$backendRoot = Resolve-Path (Join-Path $PSScriptRoot "..\backend")
Push-Location $backendRoot

try {
    Write-Host "Running EF migrations against Supabase..."
    dotnet ef database update `
        --project src/CLS.Budget.Migration `
        --startup-project src/CLS.Budget.Migration `
        --connection $ConnectionString
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet ef database update failed with exit code $LASTEXITCODE"
    }
    Write-Host "Migrations applied successfully."
}
finally {
    Pop-Location
}
