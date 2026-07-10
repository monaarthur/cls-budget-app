function Parse-NpgsqlConnectionString {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConnectionString
    )

    $values = @{}
    foreach ($part in ($ConnectionString -split ';')) {
        $trimmed = $part.Trim()
        if (-not $trimmed) { continue }
        $eq = $trimmed.IndexOf('=')
        if ($eq -lt 1) { continue }
        $key = $trimmed.Substring(0, $eq).Trim()
        $value = $trimmed.Substring($eq + 1).Trim()
        $values[$key] = $value
    }

    $sslMode = $values['SSL Mode']
    if (-not $sslMode) { $sslMode = $values['Ssl Mode'] }

    [pscustomobject]@{
        Host     = $values['Host']
        Port     = if ($values['Port']) { $values['Port'] } else { '5432' }
        Database = $values['Database']
        Username = $values['Username']
        Password = $values['Password']
        SslMode  = if ($sslMode) { $sslMode } else { 'Require' }
    }
}
