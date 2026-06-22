param(
    [string]$ServiceName = "ClsBudgetApi",
    [string]$DisplayName = "CLS Budget API",
    [string]$InstallPath = "C:\Services\ClsBudgetApi",
    [string]$ConfigFile = (Join-Path $PSScriptRoot "local-service.appsettings.json")
)

$ErrorActionPreference = "Stop"

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    throw "Run this script in an elevated (Administrator) PowerShell session."
}

if (-not (Test-Path $ConfigFile)) {
    throw "Missing $ConfigFile — copy local-service.appsettings.example.json and set your Postgres password."
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$apiProject = Join-Path $repoRoot "backend\src\CLS.Budget.Api\CLS.Budget.Api.csproj"

Write-Host "Publishing API to $InstallPath ..."
dotnet publish $apiProject -c Release -o $InstallPath

Copy-Item $ConfigFile (Join-Path $InstallPath "appsettings.Development.json") -Force

$dotnet = (Get-Command dotnet).Source
$dll = Join-Path $InstallPath "CLS.Budget.Api.dll"
$binPath = "`"$dotnet`" `"$dll`""

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "Stopping and removing existing service ..."
    if ($existing.Status -eq "Running") {
        Stop-Service $ServiceName -Force
    }
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

Write-Host "Creating Windows Service $ServiceName ..."
sc.exe create $ServiceName binPath= $binPath start= auto DisplayName= $DisplayName | Out-Null
sc.exe description $ServiceName "CLS Budget ASP.NET Core API (local always-on)" | Out-Null
$envKey = "HKLM:\SYSTEM\CurrentControlSet\Services\$ServiceName"
New-ItemProperty -Path $envKey -Name "Environment" -Value @(
    "ASPNETCORE_ENVIRONMENT=Development",
    "DOTNET_ENVIRONMENT=Development"
) -PropertyType MultiString -Force | Out-Null

Write-Host "Starting service ..."
Start-Service $ServiceName

Write-Host ""
Write-Host "CLS Budget API is running as a Windows Service." -ForegroundColor Green
Write-Host "URL: http://localhost:5123 (or Urls in your local-service.appsettings.json)"
Write-Host "Manage: Get-Service $ServiceName | Stop-Service | Start-Service"
Write-Host "Docs: docs/LOCAL-SERVICE.md"
Write-Host ""
