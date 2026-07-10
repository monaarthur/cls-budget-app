param(
    [string]$Ec2PublicIp,
    [string]$RdsConnectionString = "Host=cls-budget-db.cm7e8coy6c7r.us-east-1.rds.amazonaws.com;Port=5432;Database=postgres;Username=postgres;SSL Mode=Require;Trust Server Certificate=true",
    [string]$CloudFrontUrl
)

$ErrorActionPreference = "Stop"

function New-JwtSigningKey {
    return ([guid]::NewGuid().ToString("N") + [guid]::NewGuid().ToString("N"))
}

if (-not $Ec2PublicIp) {
    $Ec2PublicIp = Read-Host "EC2 public IP"
}
if (-not $CloudFrontUrl) {
    $CloudFrontUrl = Read-Host "CloudFront URL for CORS (optional, Enter to skip)"
}

$jwtKey = New-JwtSigningKey
$apiUrl = "http://$Ec2PublicIp"

Write-Host ""
Write-Host "=== CLS Budget EC2 API setup ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "1. Create EC2 (if not done):" -ForegroundColor Yellow
Write-Host "   .\scripts\create-ec2-api.ps1"
Write-Host ""

Write-Host "2. Deploy API:" -ForegroundColor Yellow
Write-Host "   .\scripts\deploy-api-ec2.ps1 ``"
Write-Host "     -Ec2PublicIp $Ec2PublicIp ``"
Write-Host "     -Password 'YOUR_RDS_PASSWORD' ``"
if ($CloudFrontUrl) {
    Write-Host "     -CorsOrigin '$CloudFrontUrl' ``"
}
Write-Host "     -JwtSigningKey '$jwtKey'"
Write-Host ""

Write-Host "=== Environment variables (set on EC2 container by deploy script) ===" -ForegroundColor Green
Write-Host "ASPNETCORE_ENVIRONMENT=Production"
Write-Host "Auth__Enabled=true"
Write-Host "Jwt__SigningKey=$jwtKey"
Write-Host "ConnectionStrings__BudgetDatabase=Host=cls-budget-db....;Password=YOUR_RDS_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
if ($CloudFrontUrl) {
    Write-Host "Cors__AllowedOrigins__0=$CloudFrontUrl"
}
Write-Host ""

Write-Host "=== GitHub secrets (S3/CloudFront frontend workflow) ===" -ForegroundColor Green
Write-Host "NEXT_PUBLIC_API_BASE_URL=$apiUrl"
Write-Host "NEXT_PUBLIC_AUTH_ENABLED=true (baked at build time via workflow env)"
Write-Host ""

Write-Host "=== Verify ===" -ForegroundColor Cyan
Write-Host "curl http://$Ec2PublicIp/api/v1/budget-payment-statuses"
Write-Host ""
Write-Host "Full guide: docs/DEPLOYMENT.md (section 3b)"
Write-Host ""
