# Create a prod RDS user and print a CloudFront setup link — no /admin UI required.
param(
    [Parameter(Mandatory = $true)]
    [string]$Email,
    [string]$RdsPassword,
    [string]$DisplayName,
    [Guid]$TenantId = "00000000-0000-0000-0000-000000000001",
    [string]$ApiUrl = "http://localhost:5123",
    [string]$AdminApiKey,
    [string]$CloudFrontUrl = "https://d12onqucnncuke.cloudfront.net",
    [string]$RdsConnectionString = "Host=cls-budget-db.cm7e8coy6c7r.us-east-1.rds.amazonaws.com;Port=5432;Database=postgres;Username=postgres;SSL Mode=Require;Trust Server Certificate=true",
    [switch]$SkipConnectionTest,
    [switch]$AllowDevIp
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$rdsConfig = Join-Path $PSScriptRoot "local-api-rds.appsettings.json"

Write-Host ""
Write-Host "=== Bootstrap prod user (local API -> RDS) ===" -ForegroundColor Cyan
Write-Host ""

if ($AllowDevIp) {
    & (Join-Path $PSScriptRoot "allow-rds-dev-ip.ps1")
    Write-Host ""
}

if (-not $RdsPassword) {
    $RdsPassword = $env:RDS_MASTER_PASSWORD
}
if (-not $RdsPassword) {
    $secure = Read-Host "RDS master password" -AsSecureString
    $RdsPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
}

if (-not $SkipConnectionTest) {
    $connTest = "Host=cls-budget-db.cm7e8coy6c7r.us-east-1.rds.amazonaws.com;Port=5432;Database=postgres;Username=postgres;Password=$RdsPassword;SSL Mode=Require;Trust Server Certificate=true"
    & (Join-Path $PSScriptRoot "test-rds-connection.ps1") -RdsConnectionString $connTest -Password $RdsPassword
    if ($LASTEXITCODE -ne 0) {
        throw "Cannot reach RDS. Run: .\scripts\allow-rds-dev-ip.ps1 then retry."
    }
    Write-Host ""
}

if (-not $AdminApiKey) {
    $AdminApiKey = $env:CLS_BUDGET_ADMIN_API_KEY
}
if (-not $AdminApiKey -and (Test-Path $rdsConfig)) {
    $cfg = Get-Content $rdsConfig -Raw | ConvertFrom-Json
    $AdminApiKey = $cfg.Admin.ApiKey
}
if (-not $AdminApiKey) {
    throw "Admin API key required. Set in local-api-rds.appsettings.json, CLS_BUDGET_ADMIN_API_KEY, or -AdminApiKey."
}

if (-not $DisplayName) {
    $DisplayName = ($Email -split "@")[0]
}

Write-Host "Checking local API at $ApiUrl ..." -ForegroundColor Cyan
try {
    Invoke-WebRequest -Uri "$ApiUrl/api/v1/admin/tenants" `
        -Headers @{ "X-Admin-Api-Key" = $AdminApiKey } `
        -UseBasicParsing -TimeoutSec 10 | Out-Null
}
catch {
    throw @"
Local API not reachable at $ApiUrl with admin key.

Terminal 1:
  copy scripts\local-api-rds.appsettings.example.json scripts\local-api-rds.appsettings.json
  (edit RDS password + Admin:ApiKey; FrontendBaseUrl = $CloudFrontUrl)
  .\scripts\start-api-local-rds.ps1

Then re-run this script.
"@
}

Write-Host "Inviting $Email to tenant $TenantId ..." -ForegroundColor Cyan
& (Join-Path $PSScriptRoot "invite-tenant-user.ps1") `
    -ApiUrl $ApiUrl `
    -AdminApiKey $AdminApiKey `
    -TenantId $TenantId `
    -Email $Email `
    -DisplayName $DisplayName `
    -Role Owner

Write-Host ""
Write-Host "=== Next steps ===" -ForegroundColor Green
Write-Host "1. Open the setup link above in your browser (CloudFront reset-password)."
Write-Host "2. Set your password."
Write-Host "3. Sign in at $CloudFrontUrl/login/"
Write-Host ""
Write-Host "Browse prod data locally (auth off): .\scripts\start-api-local-rds.ps1 + .\scripts\start-frontend-local.ps1"
