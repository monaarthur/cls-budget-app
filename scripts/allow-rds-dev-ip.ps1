param(
    [string]$SecurityGroupId = "sg-0f319031b99acc0b8",
    [string]$Region = "us-east-1",
    [string]$Cidr,
    [string]$Description = "CLS Budget dev laptop"
)

$ErrorActionPreference = "Stop"

if (-not $Cidr) {
    Write-Host "Detecting your public IP..." -ForegroundColor Cyan
    $ip = (Invoke-RestMethod -Uri "https://checkip.amazonaws.com" -TimeoutSec 15).Trim()
    $Cidr = "$ip/32"
}

Write-Host "Adding inbound PostgreSQL 5432 for $Cidr on $SecurityGroupId ..." -ForegroundColor Cyan
$out = aws ec2 authorize-security-group-ingress `
    --group-id $SecurityGroupId `
    --region $Region `
    --protocol tcp `
    --port 5432 `
    --cidr $Cidr 2>&1
if ($LASTEXITCODE -ne 0) {
    $text = $out | Out-String
    if ($text -match "already exists|Duplicate") {
        Write-Host "Rule already exists for $Cidr" -ForegroundColor Yellow
    }
    else {
        Write-Host $text
        throw "authorize-security-group-ingress failed."
    }
}

Write-Host ""
Write-Host "RDS should accept connections from $Cidr" -ForegroundColor Green
Write-Host "Test: .\scripts\test-rds-connection.ps1 -RdsConnectionString `"Host=cls-budget-db....`" -Password ..."
