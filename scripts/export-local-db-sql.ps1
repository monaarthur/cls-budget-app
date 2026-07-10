param(
    [string]$ConfigFile = (Join-Path $PSScriptRoot "..\backend\src\CLS.Budget.Api\appsettings.Development.json"),
    [string]$OutputFile = (Join-Path $PSScriptRoot "..\data\cls-budget-local.sql"),
    [string]$PgBin = $env:PG_BIN
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "lib\Parse-NpgsqlConnectionString.ps1")

if (-not $PgBin) {
    $defaultPg = "C:\Program Files\PostgreSQL\17\bin"
    if (Test-Path (Join-Path $defaultPg "pg_dump.exe")) {
        $PgBin = $defaultPg
    }
}

$pgDump = if ($PgBin) { Join-Path $PgBin "pg_dump.exe" } else { "pg_dump" }
if (-not (Test-Path $pgDump)) {
    throw "pg_dump not found. Set -PgBin or install PostgreSQL client tools."
}

$config = Get-Content $ConfigFile -Raw | ConvertFrom-Json
$conn = Parse-NpgsqlConnectionString -ConnectionString $config.ConnectionStrings.BudgetDatabase

$outputDir = Split-Path $OutputFile -Parent
if ($outputDir -and -not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

$rawFile = [System.IO.Path]::ChangeExtension($OutputFile, ".raw.sql")

Write-Host "Exporting '$($conn.Database)' to plain SQL (for RDS PostgreSQL 15) ..." -ForegroundColor Cyan

$env:PGPASSWORD = $conn.Password
try {
    & $pgDump `
        -h $conn.Host `
        -p $conn.Port `
        -U $conn.Username `
        -d $conn.Database `
        -Fp --no-owner --no-acl --clean --if-exists `
        -f $rawFile

    if ($LASTEXITCODE -ne 0) {
        throw "pg_dump failed with exit code $LASTEXITCODE"
    }
}
finally {
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
}

# Strip PostgreSQL 17+ settings that RDS 15 does not recognize.
$pg17OnlyPatterns = @(
    'transaction_timeout'
)

Write-Host "Filtering PG 17-only settings for RDS 15 compatibility ..."
$lineCount = 0
$filtered = Get-Content $rawFile | Where-Object {
    $line = $_
    $skip = $false
    foreach ($pattern in $pg17OnlyPatterns) {
        if ($line -match $pattern) { $skip = $true; break }
    }
    if (-not $skip) { $lineCount++; $true }
}
$filtered | Set-Content $OutputFile -Encoding utf8
Remove-Item $rawFile -Force

$sizeMb = [math]::Round((Get-Item $OutputFile).Length / 1MB, 2)
Write-Host "Export complete ($sizeMb MB, $lineCount lines): $OutputFile" -ForegroundColor Green
Write-Host "Import with: .\scripts\import-rds-sql.ps1 -RdsConnectionString ... -Password ..."
