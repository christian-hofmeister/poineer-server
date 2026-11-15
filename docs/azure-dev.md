# Azure development deployment

[Issue #67](https://github.com/christian-hofmeister/poineer-server/issues/67)
uses one resource group for a Container Apps environment on the
[Consumption profile](https://learn.microsoft.com/azure/container-apps/workload-profiles-overview),
a Basic container registry, a managed identity, and a storage account.
The API accepts HTTPS and can scale to zero. Storage and the registry remain
billable when no API replica is running. Both Blob containers are private;
the API identity can read only `metadata` and pull images from the registry.
The storage account rejects Shared Key authentication.

Run the following Azure CLI commands in PowerShell 7 from the repository root.
Select the intended development subscription with `az account set` first.
Use a dedicated resource group. The deploying account needs permission to create
resources and role assignments and to queue ACR builds. The subscription must
have the `Microsoft.App`, `Microsoft.Storage`, `Microsoft.ContainerRegistry` and
`Microsoft.ManagedIdentity` providers registered.

## Provision and deploy

```powershell
$group = 'rg-poineer-dev'
$region = 'germanywestcentral'
$template = 'infra/dev/main.bicep'

az group create --name $group --location $region
az deployment group create --resource-group $group --name poineer-foundation --template-file $template
if ($LASTEXITCODE -ne 0) { throw 'Foundation deployment failed.' }

$outputs = az deployment group show --resource-group $group --name poineer-foundation --query properties.outputs -o json | ConvertFrom-Json
$registry = $outputs.registryName.value
$storage = $outputs.storageAccountName.value

# Use a fresh tag for each build, including builds of uncommitted changes.
$tag = "$(git rev-parse --short HEAD)-$(Get-Date -AsUTC -Format yyyyMMddHHmmss)"
az acr build --registry $registry --image "poineer-server:${tag}" --platform linux/amd64 --file Dockerfile .
if ($LASTEXITCODE -ne 0) { throw 'Image build failed.' }

az deployment group create --resource-group $group --name poineer-app --template-file $template --parameters "imageTag=$tag"
if ($LASTEXITCODE -ne 0) { throw 'API deployment failed.' }
$api = az deployment group show --resource-group $group --name poineer-app --query properties.outputs.apiUrl.value -o tsv
```

The first deployment creates the identity and its roles before the API attempts
its first pull. If role changes
have not propagated, wait a few minutes and rerun the relevant step. For
subsequent code updates, build a new tag and rerun the API deployment with it.
See [ACR pulls using managed identity](https://learn.microsoft.com/azure/container-apps/managed-identity-image-pull)
if the environment cannot pull the image.

## Publish a real Berlin release

Use the [renderer publication workflow](https://github.com/christian-hofmeister/poineer-render/issues/202)
with distinct roots:

- Metadata: the `metadata` container for `regions.json` and
  `geofabrik/europe/germany/berlin/manifest.json`.
- Artifacts: the `regions` container for the immutable SQLite and optional
  PMTiles objects at the exact `objectKey` values in the manifest.

Grant the publisher **Storage Blob Data Contributor** on each container
separately. For a renderer service principal, use its Entra **object ID**:

```powershell
$publisherObjectId = '<renderer-service-principal-object-id>'
foreach ($scope in @($outputs.metadataContainerId.value, $outputs.artifactContainerId.value)) {
    az role assignment create --assignee-object-id $publisherObjectId --assignee-principal-type ServicePrincipal `
        --role 'Storage Blob Data Contributor' --scope $scope
    if ($LASTEXITCODE -ne 0) { throw "Could not grant publication access to $scope." }
}
```

Use Entra authentication for publication.
If copying an existing release, copy and verify the real artifacts first, then
publish the complete region snapshot and the current Berlin manifest last.
Do not upload the synthetic fixtures in this repository.

## Smoke test

Run this with an account that has **Storage Blob Data Reader** or **Storage Blob
Data Contributor** on the metadata container. Compare the public response to the
real manifest you just published. A successful health response alone only proves
the API is running.

```powershell
$health = Invoke-RestMethod "$api/api/health"
if ($health.status -ne 'Healthy') { throw 'API health probe failed.' }

$regionId = 'geofabrik/europe/germany/berlin'
$manifestFile = New-TemporaryFile
try {
    az storage blob download --account-name $storage --container-name metadata --auth-mode login `
        --name "$regionId/manifest.json" --file $manifestFile.FullName --overwrite true --output none
    if ($LASTEXITCODE -ne 0) { throw 'Cannot read the published Berlin manifest.' }
    $expected = Get-Content -Raw $manifestFile.FullName | ConvertFrom-Json
    if ($expected.regionId -cne $regionId) { throw 'Published manifest is for another region.' }

    $catalog = Invoke-RestMethod "$api/api/datasets"
    $berlin = @($catalog | Where-Object { $_.id -ceq $regionId })
    if ($berlin.Count -ne 1 -or $berlin[0].dataset.version -cne $expected.releaseVersion) {
        throw 'The API does not advertise the published Berlin release.'
    }
    $artifacts = $berlin[0].dataset.artifacts
    if ($artifacts.Count -ne $expected.artifacts.Count) { throw 'Artifact count mismatch.' }
    foreach ($artifact in $expected.artifacts) {
        $actual = @($artifacts | Where-Object { $_.type -ceq $artifact.type })
        if ($actual.Count -ne 1 -or $actual[0].artifactVersion -cne $artifact.artifactVersion -or
            $actual[0].sizeBytes -ne $artifact.sizeBytes -or $actual[0].sha256Checksum -cne $artifact.sha256) {
            throw "Catalog metadata does not match the published $($artifact.type) artifact."
        }
    }
    "Verified Berlin release $($expected.releaseVersion)."
}
finally {
    Remove-Item -LiteralPath $manifestFile.FullName
}
```

A valid region snapshot with no current manifests returns `200 []`. A missing
or unreadable snapshot, or a failure to list or download metadata, returns
`503`. For a 503, check the active revision's logs and the API identity's
container-scoped read role. Download transport is tracked separately and is
not part of this smoke test.

## Optional cleanup when retiring the dev environment

Keep the resource group after deployment and smoke testing. The running dev
environment and future deployments use the same group.

Delete the group only when you want to retire the entire dev environment.
This removes the API, the registry and its images, and the storage account
and its published data. Save any data you want to keep before deleting the group.

```powershell
az group delete --name $group
```
