param(
    [Parameter(Mandatory = $true)]
    [string]$ConnectionString
)

$ErrorActionPreference = "Stop"

$backendRoot = Join-Path $PSScriptRoot ".." "backend"
Push-Location $backendRoot

try {
    $env:ConnectionStrings__BudgetDatabase = $ConnectionString
    Write-Host "Running EF migrations against Supabase..."
    dotnet ef database update `
        --project src/CLS.Budget.Migration `
        --startup-project src/CLS.Budget.Api
    Write-Host "Migrations applied successfully."
}
finally {
    Pop-Location
}
