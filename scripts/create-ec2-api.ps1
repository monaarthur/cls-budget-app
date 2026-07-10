param(
    [string]$InstanceName = "cls-budget-api",
    [string]$KeyName = "cls-budget-api",
    [string]$Region = "us-east-1",
    [string]$VpcId = "vpc-06bb8f03ddc1c6eeb",
    [string]$RdsSecurityGroupId = "sg-0f319031b99acc0b8",
    [string]$InstanceType = "t3.micro",
    [switch]$SkipWait
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$keysDir = Join-Path $PSScriptRoot "keys"
$keyPath = Join-Path $keysDir "$KeyName.pem"
. (Join-Path $PSScriptRoot "lib\Protect-SshKey.ps1")

function Get-MyPublicIp {
    return (Invoke-RestMethod -Uri "https://checkip.amazonaws.com" -TimeoutSec 10).Trim()
}

function Get-OrCreateKeyPair {
    param([string]$Name, [string]$Path, [string]$AwsRegion)

    $prev = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $null = aws ec2 describe-key-pairs --key-names $Name --region $AwsRegion --output json 2>&1
    $keyExists = ($LASTEXITCODE -eq 0)
    $ErrorActionPreference = $prev

    if ($keyExists) {
        if (-not (Test-Path $Path)) {
            throw "Key pair '$Name' exists in AWS but $Path is missing. Delete the key pair in EC2 or restore the .pem file."
        }
        Protect-SshKey -Path $Path
        Write-Host "Using existing key pair '$Name'." -ForegroundColor Yellow
        return $Path
    }

    New-Item -ItemType Directory -Force -Path (Split-Path $Path) | Out-Null
    $json = aws ec2 create-key-pair --key-name $Name --region $AwsRegion --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0 -or -not $json.KeyMaterial) { throw "Failed to create key pair '$Name'." }

    # JSON KeyMaterial preserves PEM newlines; --output text on Windows does not.
    [IO.File]::WriteAllText($Path, $json.KeyMaterial)
    & (Join-Path $PSScriptRoot "fix-ec2-pem.ps1") -KeyPath $Path | Out-Null
    Protect-SshKey -Path $Path
    Write-Host "Created key pair '$Name' -> $Path" -ForegroundColor Green
    Write-Host "Keep this file private; it is gitignored." -ForegroundColor Yellow
    return $Path
}

function Get-OrCreateApiSecurityGroup {
    param(
        [string]$GroupName,
        [string]$Vpc,
        [string]$MyIp,
        [string]$AwsRegion
    )

    $sgId = aws ec2 describe-security-groups `
        --filters "Name=group-name,Values=$GroupName" "Name=vpc-id,Values=$Vpc" `
        --region $AwsRegion `
        --query "SecurityGroups[0].GroupId" `
        --output text 2>$null

    if ($sgId -and $sgId -ne "None") {
        Write-Host "Using existing security group '$GroupName' ($sgId)." -ForegroundColor Yellow
        return $sgId
    }

    $sgId = aws ec2 create-security-group `
        --group-name $GroupName `
        --description "CLS Budget API (HTTP + SSH)" `
        --vpc-id $Vpc `
        --region $AwsRegion `
        --query GroupId `
        --output text
    if ($LASTEXITCODE -ne 0) { throw "Failed to create security group." }

    aws ec2 authorize-security-group-ingress --group-id $sgId --region $AwsRegion `
        --protocol tcp --port 22 --cidr "$MyIp/32" | Out-Null
    aws ec2 authorize-security-group-ingress --group-id $sgId --region $AwsRegion `
        --protocol tcp --port 80 --cidr "0.0.0.0/0" | Out-Null

    Write-Host "Created security group '$GroupName' ($sgId)." -ForegroundColor Green
    return $sgId
}

function Ensure-RdsAllowsApiSecurityGroup {
    param([string]$RdsSg, [string]$ApiSg, [string]$AwsRegion)

    $rules = aws ec2 describe-security-groups --group-ids $RdsSg --region $AwsRegion --output json | ConvertFrom-Json
    $exists = $rules.SecurityGroups[0].IpPermissions | Where-Object {
        $_.FromPort -eq 5432 -and $_.UserIdGroupPairs.GroupId -contains $ApiSg
    }
    if ($exists) {
        Write-Host "RDS security group already allows API SG on 5432." -ForegroundColor Yellow
        return
    }

    aws ec2 authorize-security-group-ingress `
        --group-id $RdsSg `
        --region $AwsRegion `
        --protocol tcp `
        --port 5432 `
        --source-group $ApiSg | Out-Null
    Write-Host "Added RDS inbound rule: port 5432 from API security group." -ForegroundColor Green
}

Write-Host "Checking AWS credentials..." -ForegroundColor Cyan
$identity = aws sts get-caller-identity --output json 2>&1
if ($LASTEXITCODE -ne 0) { throw "AWS credentials invalid: $identity" }
Write-Host "Account: $(($identity | ConvertFrom-Json).Account), region: $Region" -ForegroundColor Green

$existing = aws ec2 describe-instances --region $Region `
    --filters "Name=tag:Name,Values=$InstanceName" "Name=instance-state-name,Values=pending,running,stopping,stopped" `
    --query "Reservations[0].Instances[0]" `
    --output json 2>$null | ConvertFrom-Json

if ($existing -and $existing.InstanceId) {
    Write-Host "Instance '$InstanceName' already exists: $($existing.InstanceId) ($($existing.State.Name))" -ForegroundColor Yellow
    $publicIp = $existing.PublicIpAddress
    if ($publicIp) { Write-Host "Public IP: $publicIp" }
    return [pscustomobject]@{
        InstanceId = $existing.InstanceId
        PublicIp   = $publicIp
        KeyPath    = $keyPath
        Region     = $Region
    }
}

$myIp = Get-MyPublicIp
Write-Host "Your public IP: $myIp (SSH will be allowed from this IP)" -ForegroundColor Cyan

$keyFile = Get-OrCreateKeyPair -Name $KeyName -Path $keyPath -AwsRegion $Region
$apiSg = Get-OrCreateApiSecurityGroup -GroupName "cls-budget-api-sg" -Vpc $VpcId -MyIp $myIp -AwsRegion $Region
Ensure-RdsAllowsApiSecurityGroup -RdsSg $RdsSecurityGroupId -ApiSg $apiSg -AwsRegion $Region

$ami = aws ssm get-parameters `
    --names /aws/service/ami-amazon-linux-latest/al2023-ami-kernel-default-x86_64 `
    --region $Region `
    --query "Parameters[0].Value" `
    --output text
if ($LASTEXITCODE -ne 0 -or -not $ami) { throw "Could not resolve Amazon Linux 2023 AMI." }

$userDataPath = Join-Path $PSScriptRoot "ec2\api-userdata.sh"
$userData = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes((Get-Content $userDataPath -Raw)))

Write-Host "Launching EC2 '$InstanceName' ($InstanceType)..." -ForegroundColor Cyan
$launchJson = aws ec2 run-instances `
    --image-id $ami `
    --instance-type $InstanceType `
    --key-name $KeyName `
    --security-group-ids $apiSg `
    --user-data $userData `
    --tag-specifications "ResourceType=instance,Tags=[{Key=Name,Value=$InstanceName}]" `
    --metadata-options "HttpTokens=required,HttpPutResponseHopLimit=2" `
    --region $Region `
    --output json | ConvertFrom-Json

$instanceId = $launchJson.Instances[0].InstanceId
Write-Host "InstanceId: $instanceId" -ForegroundColor Green

if ($SkipWait) {
    Write-Host "Instance launching. Run deploy-api-ec2.ps1 after it is running."
    return [pscustomobject]@{ InstanceId = $instanceId; PublicIp = $null; KeyPath = $keyFile; Region = $Region }
}

Write-Host "Waiting for instance to be running..." -ForegroundColor Yellow
aws ec2 wait instance-running --instance-ids $instanceId --region $Region

$desc = aws ec2 describe-instances --instance-ids $instanceId --region $Region --output json | ConvertFrom-Json
$publicIp = $desc.Reservations[0].Instances[0].PublicIpAddress

Write-Host ""
Write-Host "EC2 API host is ready." -ForegroundColor Green
Write-Host "  InstanceId: $instanceId"
Write-Host "  Public IP:  $publicIp"
Write-Host "  SSH key:    $keyFile"
Write-Host ""
Write-Host "Next: wait ~2 min for Docker bootstrap, then deploy:" -ForegroundColor Cyan
Write-Host "  .\scripts\setup-ec2-api.ps1 -Ec2PublicIp $publicIp"
Write-Host "  .\scripts\deploy-api-ec2.ps1 -Ec2PublicIp $publicIp -RdsConnectionString `"Host=...`" -Password `"...`""

return [pscustomobject]@{
    InstanceId = $instanceId
    PublicIp   = $publicIp
    KeyPath    = $keyFile
    Region     = $Region
}
