# Export local Postgres and import into production RDS (destructive on target tables).
param(
    [Parameter(Mandatory = $true)]
    [string]$RdsPassword,
    [string]$LocalConfigFile = (Join-Path $PSScriptRoot "local-api.appsettings.json"),
    [string]$RdsConnectionString = "Host=cls-budget-db.cm7e8coy6c7r.us-east-1.rds.amazonaws.com;Port=5432;Database=postgres;Username=postgres;SSL Mode=Require;Trust Server Certificate=true",
    [string]$SqlFile = (Join-Path $PSScriptRoot "..\data\cls-budget-local.sql"),
    [switch]$AllowDevIp,
    [switch]$SkipExport
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=== Sync local database -> RDS ===" -ForegroundColor Cyan
Write-Host "WARNING: This replaces data on RDS with your local export." -ForegroundColor Yellow
Write-Host ""

if ($AllowDevIp) {
    & (Join-Path $PSScriptRoot "allow-rds-dev-ip.ps1")
}

if (-not $SkipExport) {
    if (-not (Test-Path $LocalConfigFile)) {
        throw "Local config not found: $LocalConfigFile (run against local DB first)."
    }
    & (Join-Path $PSScriptRoot "export-local-db-sql.ps1") -ConfigFile $LocalConfigFile -OutputFile $SqlFile
}

$fullRdsConn = $RdsConnectionString
if ($RdsConnectionString -notmatch "Password=") {
    $fullRdsConn = "$RdsConnectionString;Password=$RdsPassword"
}

& (Join-Path $PSScriptRoot "test-rds-connection.ps1") -RdsConnectionString $fullRdsConn -Password $RdsPassword
if ($LASTEXITCODE -ne 0) { throw "RDS connection failed." }

& (Join-Path $PSScriptRoot "import-rds-sql.ps1") -RdsConnectionString $fullRdsConn -SqlFile $SqlFile -Password $RdsPassword

Write-Host ""
Write-Host "Sync complete. Verify: .\scripts\verify-rds-data.ps1 -Password `$env:RDS_MASTER_PASSWORD" -ForegroundColor Green
