param(
    [Parameter(Mandatory = $true)]
    [string]$RdsConnectionString,

    [string]$Password,

    [string]$DumpFile = (Join-Path $PSScriptRoot "..\data\cls-budget-local.dump"),

    [string]$PgBin = $env:PG_BIN
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "lib\Parse-NpgsqlConnectionString.ps1")

if (-not $PgBin) {
    $defaultPg = "C:\Program Files\PostgreSQL\17\bin"
    if (Test-Path (Join-Path $defaultPg "pg_restore.exe")) {
        $PgBin = $defaultPg
    }
}

$pgRestore = if ($PgBin) { Join-Path $PgBin "pg_restore.exe" } else { "pg_restore" }
if (-not (Get-Command $pgRestore -ErrorAction SilentlyContinue)) {
    throw "pg_restore not found. Install PostgreSQL client tools or set -PgBin."
}

if (-not (Test-Path $DumpFile)) {
    throw "Dump file not found: $DumpFile. Run .\scripts\export-local-db.ps1 first."
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

Write-Host "Restoring dump to RDS host $($conn.Host), database $($conn.Database) ..." -ForegroundColor Cyan
Write-Host "Ensure RDS security group allows your IP on port 5432 for this one-time import." -ForegroundColor Yellow

# RDS requires SSL; Npgsql "SSL Mode=Require" does not apply to pg_restore — use libpq env vars.
$pgSslMode = switch -Regex ($conn.SslMode) {
    'Disable' { 'disable' }
    'Prefer'  { 'prefer' }
    default   { 'require' }
}

$env:PGPASSWORD = $conn.Password
$env:PGSSLMODE = $pgSslMode
$prevErrorAction = $ErrorActionPreference
try {
    # pg_restore logs progress to stderr; with $ErrorActionPreference Stop, PS 5.1 aborts on the first line.
    $ErrorActionPreference = 'Continue'
    $restoreOutput = & $pgRestore `
        -h $conn.Host `
        -p $conn.Port `
        -U $conn.Username `
        -d $conn.Database `
        --no-owner --no-acl --clean --if-exists `
        -v `
        $DumpFile 2>&1

    $restoreText = ($restoreOutput | ForEach-Object {
        if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.ToString() } else { "$_" }
    }) -join [Environment]::NewLine

    if ($restoreText) {
        Write-Host $restoreText
    }

    if ($restoreText -match 'FATAL:|pg_restore: error:') {
        Write-Host ""
        $errorLine = ($restoreText -split [Environment]::NewLine | Where-Object {
            $_ -match 'FATAL:|pg_restore: error:'
        } | Select-Object -First 1)
        if ($errorLine) {
            Write-Host "Error: $errorLine" -ForegroundColor Red
        }
        Write-Host ""
        Write-Host "=== Suggested fix ===" -ForegroundColor Red
        if ($restoreText -match 'password authentication failed') {
            Write-Host @"
Wrong RDS master password (not local Postgres, not IAM keys).
  1. RDS Console -> cls-budget-db -> Modify -> New master password -> Apply immediately
  2. Re-run with: -Password 'your-new-password'  (single quotes if password has !)
"@
        }
        elseif ($restoreText -match 'no encryption|no pg_hba.conf entry') {
            Write-Host "SSL required. Re-run with updated import-rds.ps1 (sets PGSSLMODE=require automatically)."
        }
        elseif ($restoreText -match 'could not translate host name|Name or service not known') {
            Write-Host @"
RDS hostname does not resolve. Enable public access on cls-budget-db, wait until Available, then retry.
"@
        }
        elseif ($restoreText -match 'unrecognized configuration parameter|transaction_timeout') {
            Write-Host @"
PostgreSQL version mismatch: local pg_dump is v17, RDS is v15.
  Option A (recommended): .\scripts\export-local-db-sql.ps1 then .\scripts\import-rds-sql.ps1
  Option B: RDS -> Modify -> upgrade engine to PostgreSQL 17, then re-run import-rds.ps1
"@
        }
        elseif ($restoreText -match 'timeout|could not connect') {
            Write-Host @"
Cannot reach RDS on port 5432.
  1. RDS -> Modify -> Public access: Yes
  2. EC2 -> Security groups -> inbound PostgreSQL 5432 from My IP
"@
        }
        else {
            Write-Host "See pg_restore output above. Paste full output if you need help."
        }
        throw "pg_restore failed."
    }

    if ($LASTEXITCODE -ge 2) {
        throw "pg_restore failed with exit code $LASTEXITCODE"
    }
    if ($LASTEXITCODE -eq 1 -and $restoreText -match 'FATAL:|pg_restore: error:') {
        throw "pg_restore failed with exit code $LASTEXITCODE"
    }
}
finally {
    $ErrorActionPreference = $prevErrorAction
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:PGSSLMODE -ErrorAction SilentlyContinue
}

Write-Host "Restore finished. Verify row counts, then disable public RDS access." -ForegroundColor Green
Write-Host @"

Example checks (psql):
  SELECT COUNT(*) FROM ""Budgets"";
  SELECT COUNT(*) FROM ""Accounts"";
  SELECT COUNT(*) FROM ""AppUsers"";

"@
