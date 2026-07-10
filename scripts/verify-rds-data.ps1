param(
    [string]$RdsConnectionString = "Host=cls-budget-db.cm7e8coy6c7r.us-east-1.rds.amazonaws.com;Port=5432;Database=postgres;Username=postgres;SSL Mode=Require;Trust Server Certificate=true",
    [string]$Password
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "lib\Parse-NpgsqlConnectionString.ps1")
. (Join-Path $PSScriptRoot "lib\Psql-Helpers.ps1")

$psql = Get-PgPsqlPath

$conn = Parse-NpgsqlConnectionString -ConnectionString $RdsConnectionString
if ($Password) {
    $conn | Add-Member -NotePropertyName Password -NotePropertyValue $Password -Force
}
if (-not $conn.Password -and $env:RDS_MASTER_PASSWORD) {
    $conn | Add-Member -NotePropertyName Password -NotePropertyValue $env:RDS_MASTER_PASSWORD -Force
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

$env:PGPASSWORD = $conn.Password
$env:PGSSLMODE = "require"
try {
    Write-Host "Step 1: login test (SELECT 1) ..." -ForegroundColor Cyan
    $login = Invoke-PsqlQuery -Psql $psql -DbHost $conn.Host -Port ([int]$conn.Port) `
        -Username $conn.Username -Database $conn.Database -Sql "SELECT 1 AS ok;"
    Write-Host $login.Text
    if (Test-PsqlFailure -Text $login.Text -ExitCode $login.ExitCode) {
        Write-Host ""
        Write-Host "Login failed. Check RDS master password (reset in RDS Console -> Modify if needed)." -ForegroundColor Red
        throw $login.Text
    }

    Write-Host ""
    Write-Host "Step 2: list public tables ..." -ForegroundColor Cyan
    $tables = Invoke-PsqlQuery -Psql $psql -DbHost $conn.Host -Port ([int]$conn.Port) `
        -Username $conn.Username -Database $conn.Database `
        -Sql "SELECT tablename FROM pg_tables WHERE schemaname = 'public' ORDER BY tablename;"
    Write-Host $tables.Text
    if (Test-PsqlFailure -Text $tables.Text -ExitCode $tables.ExitCode) {
        throw $tables.Text
    }

    if ($tables.Text -notmatch 'Budget') {
        Write-Host ""
        Write-Host "Budget table not found - re-run import:" -ForegroundColor Yellow
        Write-Host "  .\scripts\export-local-db-sql.ps1"
        Write-Host "  .\scripts\import-rds-sql.ps1 -RdsConnectionString ... -Password ..."
        throw "Budget table missing on RDS. Data import required."
    }

    $queries = @(
        'SELECT COUNT(*) AS budget_count FROM "Budget";',
        'SELECT COUNT(*) AS user_count FROM "AppUser";',
        'SELECT COUNT(*) AS account_count FROM "Accounts";'
    )

    Write-Host ""
    Write-Host "Step 3: row counts ..." -ForegroundColor Cyan
    foreach ($sql in $queries) {
        Write-Host $sql -ForegroundColor DarkCyan
        $result = Invoke-PsqlQuery -Psql $psql -DbHost $conn.Host -Port ([int]$conn.Port) `
            -Username $conn.Username -Database $conn.Database -Sql $sql
        Write-Host $result.Text
        if (Test-PsqlFailure -Text $result.Text -ExitCode $result.ExitCode) {
            if ($result.Text -match 'does not exist') {
                Write-Host ""
                Write-Host "Tables missing - re-run import:" -ForegroundColor Yellow
                Write-Host "  .\scripts\export-local-db-sql.ps1"
                Write-Host "  .\scripts\import-rds-sql.ps1 -RdsConnectionString ... -Password ..."
            }
            throw $result.Text
        }
        Write-Host ""
    }

    Write-Host "RDS data verification complete." -ForegroundColor Green
}
finally {
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:PGSSLMODE -ErrorAction SilentlyContinue
}
