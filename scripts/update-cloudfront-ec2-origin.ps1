param(
    [string]$DistributionId = "E39ZGQHXGCF905",
    [string]$Ec2PublicDnsName = "ec2-3-228-13-242.compute-1.amazonaws.com",
    [string]$Ec2OriginId = "EC2-api"
)

$ErrorActionPreference = "Stop"

function Write-JsonFile {
    param([string]$Path, [object]$Object)
    $json = $Object | ConvertTo-Json -Depth 20 -Compress:$false
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($Path, $json, $utf8NoBom)
}

Write-Host "Fetching CloudFront distribution $DistributionId ..." -ForegroundColor Cyan
$configPath = Join-Path $env:TEMP "cls-budget-cf-origin.json"
aws cloudfront get-distribution-config --id $DistributionId --output json | Set-Content $configPath -Encoding UTF8
$raw = Get-Content $configPath -Raw | ConvertFrom-Json
$etag = $raw.ETag
$config = $raw.DistributionConfig

$origin = $config.Origins.Items | Where-Object { $_.Id -eq $Ec2OriginId } | Select-Object -First 1
if (-not $origin) {
    throw "Origin '$Ec2OriginId' not found on distribution $DistributionId"
}

$oldHost = $origin.DomainName
if ($oldHost -eq $Ec2PublicDnsName) {
    Write-Host "EC2 origin already points to $Ec2PublicDnsName" -ForegroundColor Green
    return
}

Write-Host "Updating EC2 origin: $oldHost -> $Ec2PublicDnsName" -ForegroundColor Yellow
$origin.DomainName = $Ec2PublicDnsName

$updatePath = Join-Path $env:TEMP "cls-budget-cf-origin-update.json"
Write-JsonFile -Path $updatePath -Object $config

aws cloudfront update-distribution `
    --id $DistributionId `
    --if-match $etag `
    --distribution-config "file://$updatePath" `
    --output json | Out-Null
if ($LASTEXITCODE -ne 0) { throw "update-distribution failed." }

Write-Host "Waiting for CloudFront deployment (several minutes) ..." -ForegroundColor Yellow
aws cloudfront wait distribution-deployed --id $DistributionId

Write-Host ""
Write-Host "CloudFront /api/* now targets $Ec2PublicDnsName ($Ec2PublicDnsName)" -ForegroundColor Green
Write-Host "Deploy API with:"
Write-Host "  .\scripts\deploy-api-ec2.ps1 -Ec2PublicIp 3.228.13.242 -Password `$env:RDS_MASTER_PASSWORD -CorsOrigin https://d12onqucnncuke.cloudfront.net"
