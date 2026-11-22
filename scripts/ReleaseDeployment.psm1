#Requires -Version 7.4
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Module for handling release deployments, including invoking commands, interacting with Azure, and performing health checks.
function Invoke-ReleaseCommand {
    param([string] $Command, [string[]] $Arguments, [string] $InputText)

    # Capture output without logging arguments, which can contain credentials.
    $start = [System.Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $Command
    if ($IsWindows -and $Command -eq 'az') {
        # The Windows Azure CLI launcher is az.cmd. Invoke its bundled Python
        # directly so arguments never pass through cmd.exe string parsing.
        $launcher = (Get-Command az -CommandType Application -ErrorAction Stop).Source
        if ([IO.Path]::GetExtension($launcher) -eq '.cmd') {
            $python = [IO.Path]::GetFullPath((Join-Path (Split-Path $launcher) '../python.exe'))
            if (-not (Test-Path -LiteralPath $python)) {
                throw 'Cannot locate the Windows Azure CLI bundled Python executable.'
            }
            $start.FileName = $python
            $Arguments = @('-IBm', 'azure.cli') + $Arguments
        }
    }
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.RedirectStandardInput = $true
    foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $start
    $started = $false
    try {
        $null = $process.Start()
        $started = $true
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if ($InputText) { $process.StandardInput.Write($InputText) }
        $process.StandardInput.Close()
        if (-not $process.WaitForExit(180000)) {
            $process.Kill($true)
            throw "$Command command timed out."
        }
        if ($process.ExitCode -ne 0) {
            throw "$Command command failed (exit $($process.ExitCode)). Check permissions, connectivity and resource state."
        }
        return $stdout.GetAwaiter().GetResult().Trim()
    }
    finally {
        try {
            if ($started -and -not $process.HasExited) { $process.Kill($true) }
        }
        finally { $process.Dispose() }
    }
}

function Invoke-ReleaseAzure {
    param([string[]] $Arguments)
    $json = Invoke-ReleaseCommand -Command 'az' -Arguments ($Arguments + @('--only-show-errors', '--output', 'json'))
    return ConvertFrom-Json -InputObject $json -AsHashtable -NoEnumerate
}

function Wait-ReleaseHealth {
    param([string] $Url, [string] $Version, [int] $Attempts = 30)
    for ($attempt = 0; $attempt -lt $Attempts; $attempt++) {
        try {
            $health = Invoke-RestMethod -Uri $Url -TimeoutSec 10
            if ($health.status -ceq 'Healthy' -and $health.version -ceq $Version) { return }
        }
        catch { } # Retry cold starts, transient HTTP errors and malformed responses.
        if ($attempt + 1 -lt $Attempts) { Start-Sleep -Seconds 10 }
    }
    throw 'Health check did not report the expected release version.'
}

function Invoke-ReleaseDeployment {
    $required = @('TAG_NAME', 'BUILD_VERSION', 'AZURE_TENANT_ID', 'AZURE_SUBSCRIPTION_ID',
        'AZURE_CLIENT_ID', 'AZURE_CLIENT_SECRET', 'ACR_NAME', 'ACR_LOGIN_SERVER',
        'IMAGE_REPOSITORY', 'AZURE_RESOURCE_GROUP', 'AZURE_CONTAINER_APP')
    foreach ($name in $required) {
        $value = [Environment]::GetEnvironmentVariable($name)
        if ([string]::IsNullOrWhiteSpace($value) -or $value.StartsWith('REPLACE_')) {
            throw "Missing configuration: $name"
        }
    }
    $tag = $env:TAG_NAME
    $version = $env:BUILD_VERSION
    if ($tag -cnotmatch '\Av(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\z' -or
        $tag.Substring(1) -cne $version -or $env:CHANGE_ID) {
        throw 'Only a validated release tag may deploy.'
    }

    $registry = $env:ACR_NAME
    $server = $env:ACR_LOGIN_SERVER
    $repository = $env:IMAGE_REPOSITORY
    $group = $env:AZURE_RESOURCE_GROUP
    $appName = $env:AZURE_CONTAINER_APP
    $auth = $null
    $deploymentLock = $null
    $previousConfig = @{}
    foreach ($name in @('AZURE_CONFIG_DIR', 'DOCKER_CONFIG', 'AZURE_CORE_COLLECT_TELEMETRY')) {
        $previousConfig[$name] = [Environment]::GetEnvironmentVariable($name)
    }
    try {
        # FileShare.None serializes tag jobs using the same agent and temp directory.
        $lockPath = Join-Path ([IO.Path]::GetTempPath()) 'poineer-server-release.lock'
        $deploymentLock = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate,
            [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
        $auth = Join-Path ([IO.Path]::GetTempPath()) ('poineer-auth-' + [guid]::NewGuid().ToString('N'))
        if ($IsWindows) {
            # Restrict credential files to the current Windows account.
            $directory = [IO.Directory]::CreateDirectory($auth)
            $acl = [System.Security.AccessControl.DirectorySecurity]::new()
            $acl.SetAccessRuleProtection($true, $false)
            $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
            $rule = [System.Security.AccessControl.FileSystemAccessRule]::new($identity, 'FullControl',
                'ContainerInherit, ObjectInherit', 'None', 'Allow')
            $acl.AddAccessRule($rule)
            Set-Acl -LiteralPath $directory.FullName -AclObject $acl
        }
        else {
            $mode = [IO.UnixFileMode]::UserRead -bor [IO.UnixFileMode]::UserWrite -bor [IO.UnixFileMode]::UserExecute
            $null = [IO.Directory]::CreateDirectory($auth, $mode)
        }
        $env:AZURE_CONFIG_DIR = Join-Path $auth 'azure'
        $env:DOCKER_CONFIG = Join-Path $auth 'docker'
        $env:AZURE_CORE_COLLECT_TELEMETRY = 'false'
        $null = Invoke-ReleaseCommand -Command 'az' -Arguments @('login', '--service-principal',
            '--username', $env:AZURE_CLIENT_ID, '--password', $env:AZURE_CLIENT_SECRET,
            '--tenant', $env:AZURE_TENANT_ID, '--allow-no-subscriptions', '--output', 'none', '--only-show-errors')
        $null = Invoke-ReleaseCommand -Command 'az' -Arguments @('account', 'set', '--subscription', $env:AZURE_SUBSCRIPTION_ID)

        $app = Invoke-ReleaseAzure @('containerapp', 'show', '--name', $appName, '--resource-group', $group)
        if ($app.properties.configuration.activeRevisionsMode -cne 'Single') {
            throw 'Deployment requires Single revision mode; traffic rules were not changed.'
        }
        $api = @($app.properties.template.containers | Where-Object name -CEQ 'api')
        if ($api.Count -ne 1) { throw 'Expected exactly one container named api.' }
        Write-Host "Previous image (rollback target): $($api[0].image)"

        $repositories = Invoke-ReleaseAzure @('acr', 'repository', 'list', '--name', $registry)
        if ($repositories -ccontains $repository) {
            $tags = Invoke-ReleaseAzure @('acr', 'repository', 'show-tags', '--name', $registry, '--repository', $repository)
            if ($tags -ccontains $tag) { throw 'Release tag already exists in ACR; it will not be overwritten.' }
        }
        $image = "${server}/${repository}:${tag}"
        $null = Invoke-ReleaseCommand -Command 'docker' -Arguments @('login', $server,
            '--username', $env:AZURE_CLIENT_ID, '--password-stdin') -InputText ($env:AZURE_CLIENT_SECRET + "`n")
        $null = Invoke-ReleaseCommand -Command 'docker' -Arguments @('tag', "${repository}:${tag}", $image)
        Write-Host "Publishing $image"
        $null = Invoke-ReleaseCommand -Command 'docker' -Arguments @('push', $image)
        $metadata = Invoke-ReleaseAzure @('acr', 'repository', 'show', '--name', $registry, '--image', "${repository}:${tag}")
        $digest = $metadata.digest
        if ($digest -cnotmatch '\Asha256:[a-f0-9]{64}\z') { throw 'Registry returned an invalid image digest.' }
        foreach ($reference in @("${repository}:${tag}", "${repository}@${digest}")) {
            $null = Invoke-ReleaseAzure @('acr', 'repository', 'update', '--name', $registry,
                '--image', $reference, '--write-enabled', 'false', '--delete-enabled', 'false')
        }
        $immutableImage = "${server}/${repository}@${digest}"
        Write-Host "Deploying $immutableImage"
        $updated = Invoke-ReleaseAzure @('containerapp', 'update', '--name', $appName,
            '--resource-group', $group, '--container-name', 'api', '--image', $immutableImage)
        $revision = $updated.properties.latestRevisionName
        if (-not $revision) { throw 'Update returned no revision name.' }
        $ready = $false
        for ($attempt = 0; $attempt -lt 30; $attempt++) {
            $state = Invoke-ReleaseAzure @('containerapp', 'show', '--name', $appName, '--resource-group', $group)
            $currentApi = @($state.properties.template.containers | Where-Object name -CEQ 'api')
            if ($state.properties.latestReadyRevisionName -ceq $revision -and
                $currentApi.Count -eq 1 -and $currentApi[0].image -ceq $immutableImage) {
                $ready = $true
                break
            }
            Start-Sleep -Seconds 10
        }
        if (-not $ready) { throw 'Timed out waiting for the new revision to become ready.' }
        $hostname = $state.properties.configuration.ingress.fqdn
        if (-not $hostname) { throw 'Container App has no public ingress hostname.' }
        $healthUrl = "https://${hostname}/api/health"
        Wait-ReleaseHealth -Url $healthUrl -Version $version
        Write-Host "Release $version verified at $healthUrl"
    }
    finally {
        foreach ($name in $previousConfig.Keys) {
            [Environment]::SetEnvironmentVariable($name, $previousConfig[$name])
        }
        try {
            if ($auth -and (Test-Path -LiteralPath $auth)) {
                # The only deletion target is the private directory created above.
                $resolvedAuth = [IO.Path]::GetFullPath($auth)
                $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)
                if ((Split-Path $resolvedAuth) -ne $tempRoot -or
                    (Split-Path $resolvedAuth -Leaf) -notmatch '\Apoineer-auth-[a-f0-9]{32}\z') {
                    throw 'Refusing to remove an authentication directory outside the temporary root.'
                }
                Remove-Item -LiteralPath $auth -Recurse -Force
            }
        }
        finally { if ($deploymentLock) { $deploymentLock.Dispose() } }
    }
}

Export-ModuleMember -Function Invoke-ReleaseDeployment
