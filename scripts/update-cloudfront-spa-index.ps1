param(
    [string]$DistributionId = "E39ZGQHXGCF905",
    [string]$FunctionName = "cls-budget-spa-index-rewrite"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$functionPath = Join-Path $repoRoot "scripts\cloudfront-spa-index-rewrite.js"

function Write-JsonFile {
    param([string]$Path, [object]$Object)
    $json = $Object | ConvertTo-Json -Depth 20 -Compress:$false
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($Path, $json, $utf8NoBom)
}

Write-Host "Ensuring CloudFront Function $FunctionName ..." -ForegroundColor Cyan

$existing = aws cloudfront list-functions --output json | ConvertFrom-Json
$fnMeta = $existing.FunctionList.Items | Where-Object { $_.Name -eq $FunctionName } | Select-Object -First 1

if ($fnMeta) {
    $desc = aws cloudfront describe-function --name $FunctionName --output json | ConvertFrom-Json
    $etag = $desc.ETag
    aws cloudfront update-function `
        --name $FunctionName `
        --if-match $etag `
        --function-config "Comment=Rewrite SPA paths to index.html,Runtime=cloudfront-js-2.0" `
        --function-code "fileb://$functionPath" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "update-function failed." }
    $desc = aws cloudfront describe-function --name $FunctionName --output json | ConvertFrom-Json
    $etag = $desc.ETag
}
else {
    aws cloudfront create-function `
        --name $FunctionName `
        --function-config "Comment=Rewrite SPA paths to index.html,Runtime=cloudfront-js-2.0" `
        --function-code "fileb://$functionPath" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "create-function failed." }
    $desc = aws cloudfront describe-function --name $FunctionName --output json | ConvertFrom-Json
    $etag = $desc.ETag
}

aws cloudfront publish-function --name $FunctionName --if-match $etag | Out-Null
if ($LASTEXITCODE -ne 0) { throw "publish-function failed." }

$desc = aws cloudfront describe-function --name $FunctionName --output json | ConvertFrom-Json
$functionArn = $desc.FunctionSummary.FunctionMetadata.FunctionARN
Write-Host "Function ARN: $functionArn" -ForegroundColor DarkGray

Write-Host "Attaching function to distribution $DistributionId default behavior ..." -ForegroundColor Cyan
$configPath = Join-Path $env:TEMP "cls-budget-cf-spa.json"
aws cloudfront get-distribution-config --id $DistributionId --output json | Set-Content $configPath -Encoding UTF8
$raw = Get-Content $configPath -Raw | ConvertFrom-Json
$etag = $raw.ETag
$config = $raw.DistributionConfig

$config.DefaultCacheBehavior.FunctionAssociations = @{
    Quantity = 1
    Items    = @(
        @{
            FunctionARN = $functionArn
            EventType   = "viewer-request"
        }
    )
}

$updatePath = Join-Path $env:TEMP "cls-budget-cf-spa-update.json"
Write-JsonFile -Path $updatePath -Object $config

aws cloudfront update-distribution `
    --id $DistributionId `
    --if-match $etag `
    --distribution-config "file://$updatePath" `
    --output json | Out-Null
if ($LASTEXITCODE -ne 0) { throw "update-distribution failed." }

Write-Host "Waiting for CloudFront deployment ..." -ForegroundColor Yellow
aws cloudfront wait distribution-deployed --id $DistributionId

Write-Host ""
Write-Host "SPA index rewrite enabled." -ForegroundColor Green
Write-Host "  /admin/ now requests /admin/index.html from S3 (not root /index.html)."
