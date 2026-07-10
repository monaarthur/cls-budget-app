function Protect-SshKey {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return }
    icacls $Path /inheritance:r | Out-Null
    icacls $Path /grant:r "$($env:USERNAME):(R)" | Out-Null
}
