#Requires -Version 7.4
# Offline tests use only PowerShell and .NET; no Pester installation is needed.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$module = Import-Module (Join-Path $PSScriptRoot 'ReleaseDeployment.psm1') -Force -PassThru
$passed = 0

function Assert-True([bool] $Condition, [string] $Message) {
    if (-not $Condition) { throw "Assertion failed: $Message" }
}

function Assert-Throws([scriptblock] $Action, [string] $Pattern) {
    try { & $Action }
    catch {
        if ($_.Exception.Message -notmatch $Pattern) { throw }
        return
    }
    throw "Expected failure matching: $Pattern"
}

function Test-Case([string] $Name, [scriptblock] $Action) {
    & $module {
        $script:Commands = [Collections.Generic.List[object]]::new()
        $script:AuthPaths = [Collections.Generic.List[string]]::new()
        $script:Tags = @()
        $script:Digest = 'sha256:' + ('a' * 64)
        $script:Mode = 'Single'
        $script:Ready = $true
        $script:Updated = $false
        $script:FailPush = $false
        $script:HealthChecked = $false
        $script:FailHealth = $false
        $script:SleepCount = 0
        function script:Start-Sleep { param($Seconds) $script:SleepCount++ }
        function script:Wait-ReleaseHealth {
            param($Url, $Version)
            if ($Url -cne 'https://api.example.test/api/health' -or $Version -cne '0.3.1') {
                throw 'Unexpected health arguments'
            }
            if ($script:FailHealth) { throw 'Health check failed' }
            $script:HealthChecked = $true
        }
        # Mock implementation of Invoke-ReleaseCommand for testing purposes.
        function script:Invoke-ReleaseCommand {
            param($Command, $Arguments, $InputText)
            $script:Commands.Add(@{ Command = $Command; Arguments = $Arguments })
            $script:AuthPaths.Add((Split-Path $env:AZURE_CONFIG_DIR))
            if ($Command -eq 'docker' -and $Arguments[0] -eq 'push' -and $script:FailPush) {
                throw 'Push failed'
            }
            if ($Command -eq 'az' -and $Arguments[0] -eq 'containerapp') {
                if ($Arguments[1] -eq 'update') { $script:Updated = $true }
                $image = if ($script:Updated) { "registry.azurecr.io/poineer-server@$script:Digest" } else { 'previous-image' }
                $readyRevision = if ($script:Ready) { 'new-revision' } else { 'previous-revision' }
                return @{
                    properties = @{
                        configuration = @{ activeRevisionsMode = $script:Mode; ingress = @{ fqdn = 'api.example.test' } }
                        template = @{ containers = @(@{ name = 'api'; image = $image }) }
                        latestRevisionName = 'new-revision'; latestReadyRevisionName = $readyRevision
                    }
                } | ConvertTo-Json -Depth 10 -Compress
            }
            if ($Command -eq 'az' -and $Arguments[0] -eq 'acr') {
                switch ($Arguments[2]) {
                    'list' { return '["poineer-server"]' }
                    'show-tags' { return ConvertTo-Json -InputObject @($script:Tags) -Compress }
                    'show' { return @{ digest = $script:Digest } | ConvertTo-Json -Compress }
                }
            }
            return '{}'
        }
    }
    & $Action
    & $module {
        foreach ($path in $script:AuthPaths) {
            if (Test-Path -LiteralPath $path) { throw 'Authentication directory was not cleaned up.' }
        }
    }
    $script:passed++
    Write-Host "PASS: $Name"
}

$configuration = @{
    TAG_NAME = 'v0.3.1'; BUILD_VERSION = '0.3.1'; AZURE_TENANT_ID = 'tenant'
    AZURE_SUBSCRIPTION_ID = 'subscription'; AZURE_CLIENT_ID = 'client'; AZURE_CLIENT_SECRET = 'test-secret'
    ACR_NAME = 'registry'; ACR_LOGIN_SERVER = 'registry.azurecr.io'; IMAGE_REPOSITORY = 'poineer-server'
    AZURE_RESOURCE_GROUP = 'group'; AZURE_CONTAINER_APP = 'app'; CHANGE_ID = ''
}
$previousEnvironment = @{}
foreach ($name in $configuration.Keys) {
    $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name)
    [Environment]::SetEnvironmentVariable($name, $configuration[$name])
}
$oldAzureConfig = $env:AZURE_CONFIG_DIR
$oldDockerConfig = $env:DOCKER_CONFIG
try {
    Test-Case 'Deploys by digest, verifies health, restores configuration' {
        Invoke-ReleaseDeployment
        Assert-True (& $module { $script:HealthChecked }) 'Health was not checked'
        Assert-True ([string]$env:AZURE_CONFIG_DIR -ceq [string]$oldAzureConfig) 'Azure config not restored'
        Assert-True ([string]$env:DOCKER_CONFIG -ceq [string]$oldDockerConfig) 'Docker config not restored'
        Assert-True (& $module {
            @($script:Commands | Where-Object {
                $_.Command -eq 'az' -and $_.Arguments[0] -eq 'containerapp' -and $_.Arguments[1] -eq 'update' -and
                $_.Arguments -ccontains "registry.azurecr.io/poineer-server@$script:Digest"
            }).Count -eq 1
        }) 'Expected one update using the digest'
    }
    Test-Case 'Existing release tag prevents push and update' {
        & $module { $script:Tags = @('v0.3.1') }
        Assert-Throws { Invoke-ReleaseDeployment } 'already exists'
        Assert-True (& $module {
            -not $script:Updated -and @($script:Commands | Where-Object {
                $_.Command -eq 'docker' -and $_.Arguments[0] -eq 'push'
            }).Count -eq 0
        }) 'An existing tag was published or deployed'
    }
    Test-Case 'Failed push prevents update' {
        & $module { $script:FailPush = $true }
        Assert-Throws { Invoke-ReleaseDeployment } 'Push failed'
        Assert-True (& $module { -not $script:Updated }) 'Updated after failed push'
    }
    Test-Case 'Multiple revision mode prevents publication' {
        & $module { $script:Mode = 'Multiple' }
        Assert-Throws { Invoke-ReleaseDeployment } 'Single revision mode'
        Assert-True (& $module { -not $script:Updated }) 'Updated multiple revision app'
    }
    Test-Case 'Invalid digest prevents update' {
        & $module { $script:Digest = 'invalid' }
        Assert-Throws { Invoke-ReleaseDeployment } 'invalid image digest'
        Assert-True (& $module { -not $script:Updated }) 'Updated invalid digest'
    }
    Test-Case 'Readiness timeout prevents health verification' {
        & $module { $script:Ready = $false }
        Assert-Throws { Invoke-ReleaseDeployment } 'Timed out'
        Assert-True (& $module { -not $script:HealthChecked -and $script:SleepCount -eq 30 }) 'Unbounded readiness wait'
    }
    Test-Case 'Failed health check fails deployment' {
        & $module { $script:FailHealth = $true }
        Assert-Throws { Invoke-ReleaseDeployment } 'Health check failed'
    }
    foreach ($invalid in @(
        @{ Name = 'CHANGE_ID'; Value = '83' },
        @{ Name = 'TAG_NAME'; Value = 'v01.3.1' },
        @{ Name = 'BUILD_VERSION'; Value = '0.3.2' },
        @{ Name = 'AZURE_TENANT_ID'; Value = 'REPLACE_WITH_TENANT_ID' }
    )) {
        Test-Case "Rejects invalid $($invalid.Name) before authentication" {
            $name = $invalid.Name
            $previous = [Environment]::GetEnvironmentVariable($name)
            try {
                [Environment]::SetEnvironmentVariable($name, $invalid.Value)
                Assert-Throws { Invoke-ReleaseDeployment } 'Only a validated|Missing configuration'
                Assert-True (& $module { $script:Commands.Count -eq 0 }) 'Authenticated invalid input'
            }
            finally { [Environment]::SetEnvironmentVariable($name, $previous) }
        }
    }

    # Reload real functions to exercise native process handling and health retries.
    Remove-Module $module
    $module = Import-Module (Join-Path $PSScriptRoot 'ReleaseDeployment.psm1') -Force -PassThru
    & $module {
        $pwsh = (Get-Process -Id $PID).Path
        $output = Invoke-ReleaseCommand -Command $pwsh -Arguments @('-NoProfile', '-Command', 'Write-Output success')
        if ($output -cne 'success') { throw 'Native command output was not captured.' }
        try {
            $null = Invoke-ReleaseCommand -Command $pwsh -Arguments @('-NoProfile', '-Command', "Write-Output 'test-secret'; exit 7")
            throw 'Expected native command failure'
        }
        catch {
            if ($_.Exception.Message -notmatch 'exit 7' -or $_.Exception.Message -match 'test-secret') { throw }
        }
        $script:Requests = 0
        function script:Start-Sleep { param($Seconds) }
        function script:Invoke-RestMethod {
            param($Uri, $TimeoutSec)
            $script:Requests++
            return @{ status = 'Healthy'; version = $(if ($script:Requests -eq 1) { '0.3.0' } else { '0.3.1' }) }
        }
        Wait-ReleaseHealth -Url 'https://api.example.test/api/health' -Version '0.3.1' -Attempts 2
        if ($script:Requests -ne 2) { throw 'Wrong version was accepted.' }
        function script:Invoke-RestMethod { param($Uri, $TimeoutSec) throw 'Unavailable' }
        try {
            Wait-ReleaseHealth -Url 'https://api.example.test/api/health' -Version '0.3.1' -Attempts 2
            throw 'Expected bounded health failure'
        }
        catch { if ($_.Exception.Message -notmatch 'Health check did not report') { throw } }
    }
    Write-Host "PASS: Native exit codes, output redaction and bounded health retries"
    Write-Host "All $($passed + 1) offline tests passed."
}
finally {
    foreach ($name in $previousEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name])
    }
    Remove-Module $module
}
