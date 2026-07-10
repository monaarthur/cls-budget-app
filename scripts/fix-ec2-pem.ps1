param(
    [string]$KeyPath = (Join-Path $PSScriptRoot "keys\cls-budget-api.pem")
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "lib\Protect-SshKey.ps1")

if (-not (Test-Path $KeyPath)) {
    throw "Key not found: $KeyPath"
}

# icacls may lock the file to read-only; restore write for repair.
icacls $KeyPath /grant:r "$($env:USERNAME):(F)" | Out-Null

$content = [IO.File]::ReadAllText($KeyPath)
if ($content -match '-----BEGIN RSA PRIVATE KEY-----\s*(.+?)\s*-----END RSA PRIVATE KEY-----') {
    $b64 = ($matches[1] -replace '\s', '')
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('-----BEGIN RSA PRIVATE KEY-----')
    for ($i = 0; $i -lt $b64.Length; $i += 64) {
        $len = [Math]::Min(64, $b64.Length - $i)
        [void]$sb.AppendLine($b64.Substring($i, $len))
    }
    [void]$sb.AppendLine('-----END RSA PRIVATE KEY-----')
    [IO.File]::WriteAllText($KeyPath, $sb.ToString())
    Protect-SshKey -Path $KeyPath
    Write-Host "Fixed PEM formatting at $KeyPath" -ForegroundColor Green
}
else {
    throw "File does not look like an RSA PEM key."
}
