# Fast path to get production usable (~15 min). No email required — use /admin setup link.
param(
    [Parameter(Mandatory = $true)]
    [string]$RdsPassword,
    [string]$Ec2PublicIp = "3.228.13.242",
    [string]$CloudFrontUrl = "https://d12onqucnncuke.cloudfront.net",
    [string]$AdminApiKey,
    [string]$KeyPath = (Join-Path $PSScriptRoot "keys\cls-budget-api.pem")
)

$ErrorActionPreference = "Stop"
$rdsConn = "Host=cls-budget-db.cm7e8coy6c7r.us-east-1.rds.amazonaws.com;Port=5432;Database=postgres;Username=postgres;Password=$RdsPassword;SSL Mode=Require;Trust Server Certificate=true"

Write-Host ""
Write-Host "=== Quick prod: migrations + API + frontend ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "[1/3] Applying EF migrations to RDS..." -ForegroundColor Yellow
& (Join-Path $PSScriptRoot "migrate-supabase.ps1") -ConnectionString $rdsConn

Write-Host ""
Write-Host "[2/3] Deploying API to EC2..." -ForegroundColor Yellow
$deployArgs = @{
    Ec2PublicIp = $Ec2PublicIp
    Password = $RdsPassword
    CorsOrigin = $CloudFrontUrl
    KeyPath = $KeyPath
}
if ($AdminApiKey) { $deployArgs.AdminApiKey = $AdminApiKey }
& (Join-Path $PSScriptRoot "deploy-api-ec2.ps1") @deployArgs

Write-Host ""
Write-Host "[3/5] Updating CloudFront SPA index routing (/admin/ -> admin/index.html)..." -ForegroundColor Yellow
& (Join-Path $PSScriptRoot "update-cloudfront-spa-index.ps1")

Write-Host ""
Write-Host "[4/5] Updating CloudFront /api/* for admin headers..." -ForegroundColor Yellow
& (Join-Path $PSScriptRoot "update-cloudfront-api.ps1")

Write-Host ""
Write-Host "[5/5] Deploying frontend to S3/CloudFront..." -ForegroundColor Yellow
& (Join-Path $PSScriptRoot "deploy-frontend-s3.ps1") -CloudFrontUrl $CloudFrontUrl

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Green
Write-Host "1. Open $CloudFrontUrl/admin/"
Write-Host "2. Sign in with the Admin key printed above (or pass -AdminApiKey)"
Write-Host "3. Invite your Gmail to tenant MonaArthur - copy the setup link on screen"
Write-Host "4. Set password, then sign in at ${CloudFrontUrl}/login/"
Write-Host ""
