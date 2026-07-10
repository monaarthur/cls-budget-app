param(
    [string]$BucketName = "cls-budget-app-prod",
    [string]$Region = "us-east-1",
    [string]$CloudFrontUrl,
    [string]$DistributionId,
    [switch]$SkipInvalidation,
    [switch]$AuthDisabled
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$webRoot = Join-Path $repoRoot "frontend\cls-budget-web"

if (-not $CloudFrontUrl) {
    $CloudFrontUrl = Read-Host "CloudFront URL (e.g. https://d123.cloudfront.net)"
}
$CloudFrontUrl = $CloudFrontUrl.TrimEnd("/")

if (-not $DistributionId) {
    $distJson = aws cloudfront list-distributions --output json | ConvertFrom-Json
    $hostName = ([Uri]$CloudFrontUrl).Host
    foreach ($item in $distJson.DistributionList.Items) {
        if ($item.DomainName -eq $hostName) {
            $DistributionId = $item.Id
            break
        }
    }
}

Write-Host "Building static frontend for $CloudFrontUrl ..." -ForegroundColor Cyan
Push-Location $webRoot
try {
    $env:NEXT_OUTPUT = "export"
    $env:NEXT_PUBLIC_AUTH_ENABLED = if ($AuthDisabled) { "false" } else { "true" }
    $env:NEXT_PUBLIC_API_BASE_URL = $CloudFrontUrl
    Write-Host "  NEXT_PUBLIC_AUTH_ENABLED=$($env:NEXT_PUBLIC_AUTH_ENABLED)" -ForegroundColor DarkGray

    if (Test-Path "middleware.ts") {
        Move-Item "middleware.ts" "middleware.static-export.bak" -Force
    }
    if (Test-Path "app\api") {
        Move-Item "app\api" "api.static-export.bak" -Force
    }
    if (Test-Path "app\logo-actions") {
        Move-Item "app\logo-actions" "logo-actions.static-export.bak" -Force
    }

    if (Test-Path "node_modules\.cache") { Remove-Item "node_modules\.cache" -Recurse -Force -ErrorAction SilentlyContinue }
    npm install
    if ($LASTEXITCODE -ne 0) { throw "npm install failed." }
    if (Test-Path ".next") { Remove-Item ".next" -Recurse -Force }
    npm run build
    if ($LASTEXITCODE -ne 0) { throw "npm run build failed." }

    if (-not (Test-Path "out")) {
        throw "Static export folder 'out' not found."
    }

    Write-Host "Syncing to s3://$BucketName ..." -ForegroundColor Cyan
    aws s3 sync out/ "s3://$BucketName" --delete --region $Region
    if ($LASTEXITCODE -ne 0) { throw "S3 sync failed." }
}
finally {
    if (Test-Path "middleware.static-export.bak") {
        Move-Item "middleware.static-export.bak" "middleware.ts" -Force
    }
    if (Test-Path "api.static-export.bak") {
        if (Test-Path "app\api") { Remove-Item "app\api" -Recurse -Force }
        Move-Item "api.static-export.bak" "app\api" -Force
    }
    if (Test-Path "logo-actions.static-export.bak") {
        if (Test-Path "app\logo-actions") { Remove-Item "app\logo-actions" -Recurse -Force }
        Move-Item "logo-actions.static-export.bak" "app\logo-actions" -Force
    }
    Remove-Item Env:NEXT_OUTPUT -ErrorAction SilentlyContinue
    Pop-Location
}

if ($DistributionId -and -not $SkipInvalidation) {
    Write-Host "Invalidating CloudFront cache..." -ForegroundColor Cyan
    aws cloudfront create-invalidation --distribution-id $DistributionId --paths "/*" | Out-Null
}

Write-Host ""
Write-Host "Frontend deployed." -ForegroundColor Green
Write-Host "  Site: $CloudFrontUrl"
Write-Host ""
Write-Host "Ensure EC2 API allows CORS from $CloudFrontUrl"
