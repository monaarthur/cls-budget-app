param(
    [string]$DbInstanceIdentifier = "cls-budget-db",
    [string]$MasterUsername = "postgres",
    [string]$MasterPassword,
    [string]$Region = "us-east-1",
    [string]$VpcSecurityGroupId,
    [string]$DbSubnetGroupName = "default",
    [string]$InstanceClass = "db.t4g.micro",
    [string]$EngineVersion = "16.6",
    [int]$AllocatedStorage = 20,
    [switch]$PubliclyAccessible,
    [switch]$SkipWait
)

$ErrorActionPreference = "Stop"

function Get-RdsEndpoint {
    param([string]$Identifier, [string]$AwsRegion)
    $json = aws rds describe-db-instances `
        --db-instance-identifier $Identifier `
        --region $AwsRegion `
        --output json | ConvertFrom-Json
    $instance = $json.DBInstances | Select-Object -First 1
    if (-not $instance) { return $null }
    return [pscustomobject]@{
        Endpoint = $instance.Endpoint.Address
        Port     = $instance.Endpoint.Port
        Status   = $instance.DBInstanceStatus
        Database = $instance.DBName
    }
}

Write-Host "Checking AWS credentials..." -ForegroundColor Cyan
$identity = aws sts get-caller-identity --output json 2>&1
if ($LASTEXITCODE -ne 0) {
    throw @"
AWS credentials are invalid or missing. Fix with one of:
  aws configure
  aws sso login --profile YOUR_PROFILE
Then re-run this script.
Original error: $identity
"@
}

$account = ($identity | ConvertFrom-Json).Account
Write-Host "AWS account: $account, region: $Region" -ForegroundColor Green

$existing = Get-RdsEndpoint -Identifier $DbInstanceIdentifier -AwsRegion $Region
if ($existing) {
    Write-Host "RDS instance '$DbInstanceIdentifier' already exists ($($existing.Status))." -ForegroundColor Yellow
    Write-Host "Endpoint: $($existing.Endpoint):$($existing.Port)"
    return $existing
}

if (-not $MasterPassword) {
    $MasterPassword = $env:RDS_MASTER_PASSWORD
}
if (-not $MasterPassword) {
    $secure = Read-Host "Enter master password for RDS (min 8 chars, store securely)" -AsSecureString
    $MasterPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
}
if ($MasterPassword.Length -lt 8) {
    throw "Master password must be at least 8 characters."
}

$awsCliArgs = @(
    "rds", "create-db-instance",
    "--db-instance-identifier", $DbInstanceIdentifier,
    "--db-instance-class", $InstanceClass,
    "--engine", "postgres",
    "--engine-version", $EngineVersion,
    "--master-username", $MasterUsername,
    "--master-user-password", $MasterPassword,
    "--allocated-storage", $AllocatedStorage,
    "--storage-type", "gp3",
    "--backup-retention-period", "7",
    "--no-multi-az",
    "--db-name", "postgres",
    "--region", $Region,
    "--output", "json"
)

if ($VpcSecurityGroupId) {
    $awsCliArgs += @("--vpc-security-group-ids", $VpcSecurityGroupId)
}

if ($PubliclyAccessible) {
    $awsCliArgs += "--publicly-accessible"
} else {
    $awsCliArgs += "--no-publicly-accessible"
}

if ($DbSubnetGroupName) {
    $awsCliArgs += @("--db-subnet-group-name", $DbSubnetGroupName)
}

Write-Host "Creating RDS PostgreSQL instance '$DbInstanceIdentifier' (this takes several minutes)..." -ForegroundColor Cyan
aws @awsCliArgs | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "aws rds create-db-instance failed."
}

if ($SkipWait) {
    Write-Host "Create requested. Check AWS Console for endpoint when status is 'available'."
    return
}

Write-Host "Waiting for instance to become available..." -ForegroundColor Yellow
aws rds wait db-instance-available `
    --db-instance-identifier $DbInstanceIdentifier `
    --region $Region

$info = Get-RdsEndpoint -Identifier $DbInstanceIdentifier -AwsRegion $Region
Write-Host ""
Write-Host "RDS is ready." -ForegroundColor Green
Write-Host "Endpoint: $($info.Endpoint):$($info.Port)"
Write-Host ""
Write-Host "Npgsql connection string (save in password manager, not Git):" -ForegroundColor Cyan
Write-Host "Host=$($info.Endpoint);Port=$($info.Port);Database=postgres;Username=$MasterUsername;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Allow your IP on RDS security group port 5432 (temporary, for import)"
Write-Host "  2. .\scripts\import-rds.ps1 -RdsConnectionString `"<connection string>`""
Write-Host "  3. Remove public access / restrict SG to EC2 only"

return $info
