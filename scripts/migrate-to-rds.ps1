param(
    [string]$RdsConnectionString,
    [string]$ConfigFile = (Join-Path $PSScriptRoot "..\backend\src\CLS.Budget.Api\appsettings.Development.json"),
    [string]$DumpFile = (Join-Path $PSScriptRoot "..\data\cls-budget-local.dump"),
    [switch]$SkipExport,
    [switch]$SkipCreate,
    [switch]$CreatePublicRds,
    [string]$RdsPassword,
    [string]$VpcSecurityGroupId
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=== CLS Budget: migrate local Postgres to AWS RDS ===" -ForegroundColor Cyan
Write-Host ""

# 1. Verify local migrations
Write-Host "[1/4] Verifying local EF migrations..." -ForegroundColor Yellow
$backendRoot = Resolve-Path (Join-Path $PSScriptRoot "..\backend")
Push-Location $backendRoot
try {
    dotnet ef database update `
        --project src/CLS.Budget.Migration `
        --startup-project src/CLS.Budget.Migration
    if ($LASTEXITCODE -ne 0) { throw "Local migration check failed." }
}
finally {
    Pop-Location
}

# 2. Export local DB
if (-not $SkipExport) {
    Write-Host ""
    Write-Host "[2/4] Exporting local ClsBudget..." -ForegroundColor Yellow
    & (Join-Path $PSScriptRoot "export-local-db.ps1") -ConfigFile $ConfigFile -OutputFile $DumpFile
} else {
    Write-Host ""
    Write-Host "[2/4] Skipping export (-SkipExport)." -ForegroundColor DarkGray
}

# 3. Create RDS (optional)
if (-not $SkipCreate) {
    Write-Host ""
    Write-Host "[3/4] Creating RDS instance (if not exists)..." -ForegroundColor Yellow
    $createArgs = @{
        MasterPassword = $RdsPassword
        VpcSecurityGroupId = $VpcSecurityGroupId
    }
    if ($CreatePublicRds) { $createArgs['PubliclyAccessible'] = $true }
    & (Join-Path $PSScriptRoot "create-rds-postgres.ps1") @createArgs
} else {
    Write-Host ""
    Write-Host "[3/4] Skipping RDS create (-SkipCreate)." -ForegroundColor DarkGray
}

# 4. Import to RDS
if ($RdsConnectionString) {
    Write-Host ""
    Write-Host "[4/4] Restoring dump to RDS..." -ForegroundColor Yellow
    & (Join-Path $PSScriptRoot "import-rds.ps1") -RdsConnectionString $RdsConnectionString -DumpFile $DumpFile
} else {
    Write-Host ""
    Write-Host "[4/4] Skipped restore — pass -RdsConnectionString to import." -ForegroundColor DarkGray
    Write-Host @"

When RDS is ready, run:
  .\scripts\import-rds.ps1 -RdsConnectionString "Host=....rds.amazonaws.com;Port=5432;Database=postgres;Username=postgres;Password=...;SSL Mode=Require;Trust Server Certificate=true"

Or re-run this script with -SkipExport -SkipCreate -RdsConnectionString "..."

"@
}

Write-Host ""
Write-Host "Done." -ForegroundColor Green
