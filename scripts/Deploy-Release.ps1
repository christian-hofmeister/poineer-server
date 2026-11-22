#Requires -Version 7.4
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'ReleaseDeployment.psm1') -Force

try { Invoke-ReleaseDeployment }
catch {
    # Avoid PowerShell's default error rendering, which can include secret arguments.
    $message = $_.Exception.Message
    if ($env:AZURE_CLIENT_SECRET) { $message = $message.Replace($env:AZURE_CLIENT_SECRET, '[REDACTED]') }
    [Console]::Error.WriteLine('Release deployment failed. ' + $message)
    exit 1
}
