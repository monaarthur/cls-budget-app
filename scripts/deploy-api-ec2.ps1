param(
    [string]$Ec2PublicIp,
    [string]$RdsConnectionString = "Host=cls-budget-db.cm7e8coy6c7r.us-east-1.rds.amazonaws.com;Port=5432;Database=postgres;Username=postgres;SSL Mode=Require;Trust Server Certificate=true",
    [string]$Password,
    [string]$CorsOrigin,
    [string]$JwtSigningKey,
    [string]$AdminApiKey,
    [string]$KeyPath = (Join-Path $PSScriptRoot "keys\cls-budget-api.pem"),
    [string]$SshUser = "ec2-user",
    [int]$HostPort = 80,
    [int]$ContainerPort = 8080
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
. (Join-Path $PSScriptRoot "lib\Parse-NpgsqlConnectionString.ps1")
. (Join-Path $PSScriptRoot "lib\Protect-SshKey.ps1")

function New-JwtSigningKey {
    return ([guid]::NewGuid().ToString("N") + [guid]::NewGuid().ToString("N"))
}

function Invoke-Ssh {
    param([string]$Target, [string]$Key, [string]$Command)
    & ssh -i $Key -o StrictHostKeyChecking=accept-new -o ConnectTimeout=15 "$SshUser@$Target" $Command
    if ($LASTEXITCODE -ne 0) { throw "SSH command failed: $Command" }
}

function Invoke-Scp {
    param([string]$Source, [string]$Target, [string]$Key, [string]$DestPath)
    & scp -i $Key -o StrictHostKeyChecking=accept-new -r $Source "${SshUser}@${Target}:$DestPath"
    if ($LASTEXITCODE -ne 0) { throw "SCP failed: $Source -> ${Target}:$DestPath" }
}

if (-not $Ec2PublicIp) {
    $Ec2PublicIp = Read-Host "EC2 public IP (from create-ec2-api.ps1 output)"
}
if (-not (Test-Path $KeyPath)) {
    throw "SSH key not found at $KeyPath. Run .\scripts\create-ec2-api.ps1 first."
}
Protect-SshKey -Path $KeyPath

$conn = Parse-NpgsqlConnectionString -ConnectionString $RdsConnectionString
if ($Password) {
    $conn | Add-Member -NotePropertyName Password -NotePropertyValue $Password -Force
}
if (-not $conn.Password -and $env:RDS_MASTER_PASSWORD) {
    $conn | Add-Member -NotePropertyName Password -NotePropertyValue $env:RDS_MASTER_PASSWORD -Force
}
if (-not $conn.Password -and $env:PGPASSWORD) {
    $conn | Add-Member -NotePropertyName Password -NotePropertyValue $env:PGPASSWORD -Force
}
if (-not $conn.Password) {
    try {
        $secure = Read-Host "Enter RDS master password" -AsSecureString
        $plain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
        $conn | Add-Member -NotePropertyName Password -NotePropertyValue $plain -Force
    }
    catch { }
}
if (-not $conn.Password) {
    throw "RDS password required. Pass -Password, or set RDS_MASTER_PASSWORD / PGPASSWORD."
}

$dbConn = "Host=$($conn.Host);Port=$($conn.Port);Database=$($conn.Database);Username=$($conn.Username);Password=$($conn.Password);SSL Mode=Require;Trust Server Certificate=true"
if (-not $JwtSigningKey) { $JwtSigningKey = New-JwtSigningKey }
if (-not $AdminApiKey) { $AdminApiKey = New-JwtSigningKey }
if (-not $CorsOrigin) {
    try {
        $CorsOrigin = Read-Host "CloudFront or frontend URL for CORS (e.g. https://d123.cloudfront.net, or Enter to skip)"
    }
    catch { }
}

$publishDir = Join-Path $env:TEMP "cls-budget-api-publish"
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
New-Item -ItemType Directory -Path $publishDir | Out-Null

Write-Host "Publishing API..." -ForegroundColor Cyan
Push-Location $repoRoot
try {
    dotnet publish backend/src/CLS.Budget.Api/CLS.Budget.Api.csproj -c Release -o $publishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }
}
finally {
    Pop-Location
}

Write-Host "Waiting for SSH on $Ec2PublicIp (Docker bootstrap may take 1-2 min)..." -ForegroundColor Yellow
$ready = $false
for ($i = 0; $i -lt 24; $i++) {
    try {
        Invoke-Ssh -Target $Ec2PublicIp -Key $KeyPath -Command "docker info >/dev/null 2>&1 && echo ready"
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
    }
    catch { }
    Start-Sleep -Seconds 10
}
if (-not $ready) {
    throw "EC2 host not ready. Wait a few minutes and retry, or SSH manually: ssh -i `"$KeyPath`" $SshUser@$Ec2PublicIp"
}

Write-Host "Copying published API to EC2..." -ForegroundColor Cyan
Invoke-Ssh -Target $Ec2PublicIp -Key $KeyPath -Command "sudo rm -rf /opt/cls-budget-api/*"
Invoke-Scp -Source "$publishDir\*" -Target $Ec2PublicIp -Key $KeyPath -DestPath "/opt/cls-budget-api/"

$frontendBaseUrl = if ($CorsOrigin) { $CorsOrigin } else { "http://localhost:3000" }
$envFile = Join-Path $env:TEMP "cls-budget-api.env"
$envLines = @(
    "ASPNETCORE_ENVIRONMENT=Production",
    "ASPNETCORE_URLS=http://0.0.0.0:$ContainerPort",
    "Auth__Enabled=true",
    "Jwt__SigningKey=$JwtSigningKey",
    "ConnectionStrings__BudgetDatabase=$dbConn",
    "PasswordReset__FrontendBaseUrl=$frontendBaseUrl",
    "Admin__ApiKey=$AdminApiKey"
)
if ($CorsOrigin) { $envLines += "Cors__AllowedOrigins__0=$CorsOrigin" }
Set-Content -Path $envFile -Value $envLines -Encoding UTF8
Invoke-Scp -Source $envFile -Target $Ec2PublicIp -Key $KeyPath -DestPath "/opt/cls-budget-api/cls-budget-api.env"

$runScript = "docker rm -f cls-budget-api 2>/dev/null || true; docker pull mcr.microsoft.com/dotnet/aspnet:9.0; docker run -d --name cls-budget-api --restart unless-stopped -p ${HostPort}:${ContainerPort} --env-file /opt/cls-budget-api/cls-budget-api.env -v /opt/cls-budget-api:/app -w /app mcr.microsoft.com/dotnet/aspnet:9.0 dotnet CLS.Budget.Api.dll"

Write-Host "Starting API container..." -ForegroundColor Cyan
Invoke-Ssh -Target $Ec2PublicIp -Key $KeyPath -Command $runScript

Start-Sleep -Seconds 5
Write-Host "Checking API health..." -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "http://${Ec2PublicIp}/api/v1/budget-payment-statuses" -UseBasicParsing -TimeoutSec 20
    Write-Host "HTTP $($response.StatusCode) - API is responding." -ForegroundColor Green
}
catch {
    Write-Host "API may still be starting. Check logs:" -ForegroundColor Yellow
    Write-Host "  ssh -i `"$KeyPath`" $SshUser@$Ec2PublicIp `"docker logs cls-budget-api`""
}

Write-Host ""
Write-Host "=== Deployment summary ===" -ForegroundColor Green
Write-Host "API URL:     http://$Ec2PublicIp"
Write-Host "JWT key:     $JwtSigningKey  (save in password manager)"
Write-Host "RDS host:    $($conn.Host)"
if ($CorsOrigin) { Write-Host "CORS origin: $CorsOrigin" }
Write-Host "Admin key:   $AdminApiKey  (for /admin UI)"
Write-Host ""
Write-Host "Set GitHub secret NEXT_PUBLIC_API_BASE_URL=http://$Ec2PublicIp (or HTTPS after adding a domain/ALB)."
Write-Host "For HTTPS in production, add nginx + Let's Encrypt or an Application Load Balancer."
