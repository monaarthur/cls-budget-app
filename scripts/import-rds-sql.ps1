param(
    [Parameter(Mandatory = $true)]
    [string]$RdsConnectionString,

    [string]$Password,

    [string]$SqlFile = (Join-Path $PSScriptRoot "..\data\cls-budget-local.sql"),

    [string]$PgBin = $env:PG_BIN
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "lib\Parse-NpgsqlConnectionString.ps1")

if (-not $PgBin) {
    $defaultPg = "C:\Program Files\PostgreSQL\17\bin"
    if (Test-Path (Join-Path $defaultPg "psql.exe")) {
        $PgBin = $defaultPg
    }
}

$psql = if ($PgBin) { Join-Path $PgBin "psql.exe" } else { "psql" }
if (-not (Test-Path $psql)) {
    throw "psql not found. Set -PgBin or install PostgreSQL client tools."
}

if (-not (Test-Path $SqlFile)) {
    throw "SQL file not found: $SqlFile. Run .\scripts\export-local-db-sql.ps1 first."
}

$conn = Parse-NpgsqlConnectionString -ConnectionString $RdsConnectionString
if ($Password) {
    $conn | Add-Member -NotePropertyName Password -NotePropertyValue $Password -Force
}
if (-not $conn.Password -and $env:PGPASSWORD) {
    $conn | Add-Member -NotePropertyName Password -NotePropertyValue $env:PGPASSWORD -Force
}
if (-not $conn.Password) {
    $secure = Read-Host "Enter RDS master password" -AsSecureString
    $plain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
    $conn | Add-Member -NotePropertyName Password -NotePropertyValue $plain -Force
}

Write-Host "Importing $SqlFile to $($conn.Host) ..." -ForegroundColor Cyan

$env:PGPASSWORD = $conn.Password
$env:PGSSLMODE = "require"
$prev = $ErrorActionPreference
try {
    $ErrorActionPreference = "Continue"
    $out = & $psql `
        -h $conn.Host `
        -p $conn.Port `
        -U $conn.Username `
        -d $conn.Database `
        -v ON_ERROR_STOP=1 `
        -f $SqlFile 2>&1

    $text = ($out | ForEach-Object {
        if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.ToString() } else { "$_" }
    }) -join [Environment]::NewLine

    if ($text) { Write-Host $text }

    if ($text -match 'FATAL:|ERROR:' -and $text -notmatch 'already exists') {
        if ($text -match 'unrecognized configuration parameter') {
            Write-Host ""
            Write-Host "Version mismatch: dump has settings RDS PostgreSQL does not support." -ForegroundColor Red
            Write-Host "Re-run: .\scripts\export-local-db-sql.ps1 then import-rds-sql.ps1 again." -ForegroundColor Yellow
            Write-Host "Or upgrade RDS to PostgreSQL 17 to match local pg_dump 17." -ForegroundColor Yellow
        }
        throw "psql import failed."
    }
    if ($LASTEXITCODE -ne 0) {
        throw "psql failed with exit code $LASTEXITCODE"
    }
}
finally {
    $ErrorActionPreference = $prev
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:PGSSLMODE -ErrorAction SilentlyContinue
}

Write-Host "Import finished. Verify row counts, then disable public RDS access." -ForegroundColor Green
