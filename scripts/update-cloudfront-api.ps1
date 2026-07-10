param(
    [string]$DistributionId = "E39ZGQHXGCF905",
    [string]$Region = "us-east-1"
)

$ErrorActionPreference = "Stop"

function Write-JsonFile {
    param([string]$Path, [object]$Object)
    $json = $Object | ConvertTo-Json -Depth 20 -Compress:$false
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($Path, $json, $utf8NoBom)
}

# Managed policies: forward all viewer headers (incl. X-Admin-Api-Key); disable API caching.
$originRequestPolicyId = "216adef6-5c7f-47e4-b989-5492eafa07d3" # AllViewer
$cachePolicyId = "4135ea2d-6df8-44a3-9df3-4b5a84be39ad"         # CachingDisabled

Write-Host "Fetching CloudFront distribution $DistributionId ..." -ForegroundColor Cyan
$configPath = Join-Path $env:TEMP "cls-budget-cf-config.json"
aws cloudfront get-distribution-config --id $DistributionId --output json | Set-Content $configPath -Encoding UTF8
$raw = Get-Content $configPath -Raw | ConvertFrom-Json
$etag = $raw.ETag
$config = $raw.DistributionConfig

$apiBehavior = $config.CacheBehaviors.Items | Where-Object { $_.PathPattern -eq "/api/*" } | Select-Object -First 1
if (-not $apiBehavior) {
    throw "No /api/* cache behavior found on distribution $DistributionId"
}

$apiBehavior.OriginRequestPolicyId = $originRequestPolicyId
$apiBehavior.CachePolicyId = $cachePolicyId
$apiBehavior.AllowedMethods = @{
    Quantity      = 7
    Items         = @("GET", "HEAD", "OPTIONS", "PUT", "POST", "PATCH", "DELETE")
    CachedMethods = @{ Quantity = 2; Items = @("GET", "HEAD") }
}

$updatePath = Join-Path $env:TEMP "cls-budget-cf-update.json"
Write-JsonFile -Path $updatePath -Object $config

Write-Host "Updating /api/* behavior (forward all headers, no cache) ..." -ForegroundColor Cyan
aws cloudfront update-distribution `
    --id $DistributionId `
    --if-match $etag `
    --distribution-config "file://$updatePath" `
    --output json | Out-Null
if ($LASTEXITCODE -ne 0) { throw "update-distribution failed." }

Write-Host "Waiting for deployment ..." -ForegroundColor Yellow
aws cloudfront wait distribution-deployed --id $DistributionId

Write-Host ""
Write-Host "CloudFront API path is public via /api/* (includes /api/v1/admin/*)." -ForegroundColor Green
Write-Host "Admin UI: https://d12onqucnncuke.cloudfront.net/admin/"
Write-Host "Redeploy API if you have not since the latest admin/CORS fixes:"
Write-Host "  .\scripts\deploy-api-ec2.ps1 -Ec2PublicIp 3.228.13.242 -Password `$env:RDS_MASTER_PASSWORD -CorsOrigin https://d12onqucnncuke.cloudfront.net"
