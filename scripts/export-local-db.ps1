param(
    [string]$ConfigFile = (Join-Path $PSScriptRoot "..\backend\src\CLS.Budget.Api\appsettings.Development.json"),
    [string]$OutputFile = (Join-Path $PSScriptRoot "..\data\cls-budget-local.dump"),
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
if (-not (Get-Command $pgDump -ErrorAction SilentlyContinue)) {
    throw "pg_dump not found. Install PostgreSQL client tools or set -PgBin to the bin folder (e.g. 'C:\Program Files\PostgreSQL\17\bin')."
}

if (-not (Test-Path $ConfigFile)) {
    throw "Config not found: $ConfigFile. Copy appsettings.Development.json.example and set your local password."
}

$config = Get-Content $ConfigFile -Raw | ConvertFrom-Json
$conn = Parse-NpgsqlConnectionString -ConnectionString $config.ConnectionStrings.BudgetDatabase

$outputDir = Split-Path $OutputFile -Parent
if ($outputDir -and -not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

Write-Host "Exporting local database '$($conn.Database)' to $OutputFile ..." -ForegroundColor Cyan

$env:PGPASSWORD = $conn.Password
try {
    & $pgDump `
        -h $conn.Host `
        -p $conn.Port `
        -U $conn.Username `
        -d $conn.Database `
        -Fc --no-owner --no-acl `
        -f $OutputFile

    if ($LASTEXITCODE -ne 0) {
        throw "pg_dump failed with exit code $LASTEXITCODE"
    }
}
finally {
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
}

$sizeMb = [math]::Round((Get-Item $OutputFile).Length / 1MB, 2)
Write-Host "Export complete ($sizeMb MB): $OutputFile" -ForegroundColor Green
