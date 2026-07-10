param(
    [string]$CloudFrontUrl,
    [string]$Ec2PublicIp = "100.59.50.231",
    [string]$KeyPath = (Join-Path $PSScriptRoot "keys\cls-budget-api.pem")
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "lib\Protect-SshKey.ps1")

if (-not $CloudFrontUrl) {
    throw "Pass -CloudFrontUrl (e.g. https://d123.cloudfront.net)"
}
$CloudFrontUrl = $CloudFrontUrl.TrimEnd("/")
Protect-SshKey -Path $KeyPath

$corsLine = "Cors__AllowedOrigins__0=$CloudFrontUrl"
$cmd = "grep -q '^Cors__AllowedOrigins__0=' /opt/cls-budget-api/cls-budget-api.env && sudo sed -i 's|^Cors__AllowedOrigins__0=.*|$corsLine|' /opt/cls-budget-api/cls-budget-api.env || echo '$corsLine' | sudo tee -a /opt/cls-budget-api/cls-budget-api.env > /dev/null; docker restart cls-budget-api"

Write-Host "Setting CORS on EC2 to $CloudFrontUrl ..." -ForegroundColor Cyan
& ssh -i $KeyPath -o StrictHostKeyChecking=accept-new ec2-user@$Ec2PublicIp $cmd
if ($LASTEXITCODE -ne 0) { throw "Failed to update EC2 CORS." }
Write-Host "EC2 CORS updated and API restarted." -ForegroundColor Green
