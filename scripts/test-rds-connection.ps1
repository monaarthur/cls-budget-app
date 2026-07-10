param(
    [Parameter(Mandatory = $true)]
    [string]$RdsConnectionString,

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

Write-Host "Testing TCP to $($conn.Host):$($conn.Port) ..." -ForegroundColor Cyan
$tcp = Test-NetConnection -ComputerName $conn.Host -Port $conn.Port -WarningAction SilentlyContinue
Write-Host "  TcpTestSucceeded: $($tcp.TcpTestSucceeded)"
if (-not $tcp.TcpTestSucceeded) {
    Write-Host "Cannot reach RDS on port 5432. Check public access and security group (My IP on 5432)." -ForegroundColor Red
    exit 1
}

Write-Host "Testing login (SELECT 1) ..." -ForegroundColor Cyan
$env:PGPASSWORD = $conn.Password
$env:PGSSLMODE = "require"
try {
    $login = Invoke-PsqlQuery -Psql $psql -DbHost $conn.Host -Port ([int]$conn.Port) `
        -Username $conn.Username -Database $conn.Database -Sql "SELECT 1 AS ok;"
    Write-Host $login.Text
    if (Test-PsqlFailure -Text $login.Text -ExitCode $login.ExitCode) {
        Write-Host "Login failed. Reset RDS password in Console -> Modify, then retry." -ForegroundColor Red
        exit 1
    }
    Write-Host "Login OK - safe to run verify-rds-data.ps1 or import-rds-sql.ps1." -ForegroundColor Green
}
finally {
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:PGSSLMODE -ErrorAction SilentlyContinue
}
