function Invoke-PsqlQuery {
    param(
        [string]$Psql,
        [string]$DbHost,
        [int]$Port,
        [string]$Username,
        [string]$Database,
        [string]$Sql
    )

    $prev = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $raw = & $Psql -h $DbHost -p $Port -U $Username -d $Database -c $Sql 2>&1
        $text = ($raw | ForEach-Object {
            if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.ToString() } else { "$_" }
        }) -join [Environment]::NewLine

        return @{
            Text     = $text
            ExitCode = $LASTEXITCODE
        }
    }
    finally {
        $ErrorActionPreference = $prev
    }
}

function Test-PsqlFailure {
    param([string]$Text, [int]$ExitCode)

    if ($Text -match 'FATAL:|psql: error:|ERROR:\s') {
        return $true
    }
    if ($ExitCode -ne 0) {
        return $true
    }
    return $false
}

function Get-PgPsqlPath {
    param([string]$PgBin = $env:PG_BIN)

    if (-not $PgBin) {
        $defaultPg = "C:\Program Files\PostgreSQL\17\bin"
        if (Test-Path (Join-Path $defaultPg "psql.exe")) {
            $PgBin = $defaultPg
        }
    }

    $psql = if ($PgBin) { Join-Path $PgBin "psql.exe" } else { "psql" }
    if (-not (Get-Command $psql -ErrorAction SilentlyContinue)) {
        throw "psql not found. Install PostgreSQL client tools or set -PgBin."
    }
    return $psql
}
