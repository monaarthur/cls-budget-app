param(
    [string]$BucketName = "cls-budget-app-prod",
    [string]$Ec2OriginHostname,
    [string]$Region = "us-east-1",
    [switch]$SkipWait
)

$ErrorActionPreference = "Stop"

function Write-JsonFile {
    param([string]$Path, [object]$Object)
    $json = $Object | ConvertTo-Json -Depth 12 -Compress
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($Path, $json, $utf8NoBom)
}

function Get-ExistingDistribution {
    $json = aws cloudfront list-distributions --output json | ConvertFrom-Json
    if (-not $json.DistributionList.Items) { return $null }
    foreach ($item in $json.DistributionList.Items) {
        if ($item.Comment -like "*CLS Budget*") {
            return $item
        }
    }
    return $null
}

Write-Host "Checking AWS credentials..." -ForegroundColor Cyan
aws sts get-caller-identity --output json | Out-Null
if ($LASTEXITCODE -ne 0) { throw "AWS credentials invalid." }

if (-not $Ec2OriginHostname) {
    $Ec2OriginHostname = aws ec2 describe-instances --region $Region `
        --filters "Name=tag:Name,Values=cls-budget-api" "Name=instance-state-name,Values=running" `
        --query "Reservations[0].Instances[0].PublicDnsName" --output text
    if ($LASTEXITCODE -ne 0 -or -not $Ec2OriginHostname -or $Ec2OriginHostname -eq "None") {
        throw "Could not resolve EC2 public DNS. Pass -Ec2OriginHostname."
    }
}
Write-Host "API origin: $Ec2OriginHostname" -ForegroundColor Cyan

$existing = Get-ExistingDistribution
if ($existing) {
    Write-Host "CloudFront distribution already exists." -ForegroundColor Yellow
    Write-Host "  Id:     $($existing.Id)"
    Write-Host "  Domain: $($existing.DomainName)"
    return [pscustomobject]@{
        DistributionId = $existing.Id
        DomainName     = $existing.DomainName
        Url            = "https://$($existing.DomainName)"
    }
}

$oacPath = Join-Path $env:TEMP "cls-budget-oac.json"
$oacId = $null
$listOac = aws cloudfront list-origin-access-controls --output json | ConvertFrom-Json
foreach ($item in $listOac.OriginAccessControlList.Items) {
    if ($item.Name -eq "cls-budget-s3-oac") {
        $oacId = $item.Id
        break
    }
}

if (-not $oacId) {
    Write-JsonFile -Path $oacPath -Object @{
        Name                              = "cls-budget-s3-oac"
        Description                       = "CLS Budget S3 OAC"
        SigningProtocol                   = "sigv4"
        SigningBehavior                   = "always"
        OriginAccessControlOriginType     = "s3"
    }
    Write-Host "Creating Origin Access Control..." -ForegroundColor Cyan
    $oacJson = aws cloudfront create-origin-access-control --origin-access-control-config "file://$oacPath" --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw "create-origin-access-control failed." }
    $oacId = $oacJson.OriginAccessControl.Id
}
else {
    Write-Host "Using existing Origin Access Control $oacId" -ForegroundColor Yellow
}

$s3Domain = "$BucketName.s3.$Region.amazonaws.com"
$callerRef = "cls-budget-$(Get-Date -Format 'yyyyMMddHHmmss')"
$ec2OriginId = "EC2-api"
$s3OriginId = "S3-$BucketName"

$distConfig = [ordered]@{
    CallerReference     = $callerRef
    Comment             = "CLS Budget frontend (S3) + API proxy (EC2)"
    DefaultRootObject   = "index.html"
    Origins             = @{
        Quantity = 2
        Items    = @(
            @{
                Id                    = $s3OriginId
                DomainName            = $s3Domain
                OriginAccessControlId = $oacId
                S3OriginConfig        = @{ OriginAccessIdentity = "" }
            },
            @{
                Id                 = $ec2OriginId
                DomainName         = $Ec2OriginHostname
                CustomOriginConfig = @{
                    HTTPPort               = 80
                    HTTPSPort              = 443
                    OriginProtocolPolicy   = "http-only"
                    OriginSslProtocols     = @{ Quantity = 1; Items = @("TLSv1.2") }
                    OriginReadTimeout      = 30
                    OriginKeepaliveTimeout = 5
                }
            }
        )
    }
    DefaultCacheBehavior = @{
        TargetOriginId       = $s3OriginId
        ViewerProtocolPolicy = "redirect-to-https"
        AllowedMethods       = @{
            Quantity      = 2
            Items         = @("GET", "HEAD")
            CachedMethods = @{ Quantity = 2; Items = @("GET", "HEAD") }
        }
        Compress      = $true
        CachePolicyId = "658327ea-f89d-4fab-a63d-7e88639e58f6"
    }
    CacheBehaviors = @{
        Quantity = 1
        Items    = @(
            @{
                PathPattern           = "/api/*"
                TargetOriginId        = $ec2OriginId
                ViewerProtocolPolicy  = "redirect-to-https"
                AllowedMethods        = @{
                    Quantity      = 7
                    Items         = @("GET", "HEAD", "OPTIONS", "PUT", "POST", "PATCH", "DELETE")
                    CachedMethods = @{ Quantity = 2; Items = @("GET", "HEAD") }
                }
                Compress              = $true
                CachePolicyId         = "4135ea2d-6df8-44a3-9df3-4b5a84be39ad"
                OriginRequestPolicyId = "216adef6-5c7f-47e4-b989-5492eafa07d3"
            }
        )
    }
    CustomErrorResponses = @{
        Quantity = 2
        Items    = @(
            @{ ErrorCode = 403; ResponsePagePath = "/index.html"; ResponseCode = "200"; ErrorCachingMinTTL = 0 },
            @{ ErrorCode = 404; ResponsePagePath = "/index.html"; ResponseCode = "200"; ErrorCachingMinTTL = 0 }
        )
    }
    Enabled    = $true
    PriceClass = "PriceClass_100"
}

$configPath = Join-Path $env:TEMP "cls-budget-cloudfront.json"
Write-JsonFile -Path $configPath -Object $distConfig

Write-Host "Creating CloudFront distribution (takes several minutes)..." -ForegroundColor Cyan
$createJson = aws cloudfront create-distribution --distribution-config "file://$configPath" --output json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw "create-distribution failed." }

$dist = $createJson.Distribution
$distId = $dist.Id
$domain = $dist.DomainName
$arn = $dist.ARN

Write-Host "Updating S3 bucket policy for CloudFront OAC..." -ForegroundColor Cyan
$bucketPolicy = @{
    Version   = "2012-10-17"
    Statement = @(
        @{
            Sid       = "AllowCloudFrontServicePrincipal"
            Effect    = "Allow"
            Principal = @{ Service = "cloudfront.amazonaws.com" }
            Action    = "s3:GetObject"
            Resource  = "arn:aws:s3:::$BucketName/*"
            Condition = @{
                StringEquals = @{
                    "AWS:SourceArn" = $arn
                }
            }
        }
    )
}
$policyPath = Join-Path $env:TEMP "cls-budget-bucket-policy.json"
Write-JsonFile -Path $policyPath -Object $bucketPolicy
aws s3api put-bucket-policy --bucket $BucketName --policy "file://$policyPath" | Out-Null
if ($LASTEXITCODE -ne 0) { throw "put-bucket-policy failed." }

if (-not $SkipWait) {
    Write-Host "Waiting for distribution to deploy..." -ForegroundColor Yellow
    aws cloudfront wait distribution-deployed --id $distId
}

Write-Host ""
Write-Host "CloudFront is ready." -ForegroundColor Green
Write-Host "  DistributionId: $distId"
Write-Host "  URL:            https://$domain"
Write-Host ""
Write-Host "Next:"
Write-Host "  .\scripts\deploy-frontend-s3.ps1 -CloudFrontUrl https://$domain -DistributionId $distId"
Write-Host "  .\scripts\update-ec2-cors.ps1 -CloudFrontUrl https://$domain"

return [pscustomobject]@{
    DistributionId = $distId
    DomainName     = $domain
    Url            = "https://$domain"
}
