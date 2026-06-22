param(
    [string]$ServiceName = "ClsBudgetApi",
    [string]$InstallPath = "C:\Services\ClsBudgetApi",
    [switch]$KeepFiles
)

$ErrorActionPreference = "Stop"

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    throw "Run this script in an elevated (Administrator) PowerShell session."
}

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    if ($existing.Status -eq "Running") {
        Stop-Service $ServiceName -Force
    }
    sc.exe delete $ServiceName | Out-Null
    Write-Host "Removed service $ServiceName."
}

if (-not $KeepFiles -and (Test-Path $InstallPath)) {
    Remove-Item $InstallPath -Recurse -Force
    Write-Host "Removed $InstallPath."
}

Write-Host "Uninstall complete."
