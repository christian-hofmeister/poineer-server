# Azure development deployment

[Issue #67](https://github.com/christian-hofmeister/poineer-server/issues/67)
shares the existing `rg-poineer-dev` resource group in `westeurope` with the
renderer. It uses the existing `poineer-dev-env` Container Apps environment on the
[Consumption profile](https://learn.microsoft.com/azure/container-apps/workload-profiles-overview),
Basic container registry `poineerdevacr`, and storage account `poineerstoragedev`.
The server template creates a dedicated API managed identity and its roles;
supplying an image tag also deploys the API.
The API accepts HTTPS and can scale to zero. Storage and the registry remain
billable when no API replica is running. Both Blob containers are private;
the API identity can read only `metadata` and pull images from the registry.
The storage account rejects Shared Key authentication.

Run the following Azure CLI commands in PowerShell 7 from the repository root.
Select the intended development subscription with `az account set` first.
The shared storage account must already contain private `metadata` and `regions`
containers. Shared Key access must be disabled. The registry must allow ARM-token
authentication with legacy registry permissions, and the environment must have a
Consumption workload profile. These shared resources are referenced with Bicep
`existing` declarations and are managed outside this server template.
Their names can be overridden with `existingStorageAccountName`,
`existingRegistryName`, and `existingEnvironmentName`. Keep them in the same
resource group and Azure region. The `location` parameter defaults to the
resource group's region; override it if the shared resources use another region.

The deploying account needs permission to create
resources and role assignments and to queue ACR builds. The subscription must
have the `Microsoft.App`, `Microsoft.Storage`, `Microsoft.ContainerRegistry` and
`Microsoft.ManagedIdentity` providers registered.

## Verified foundation deployment

On 2026-11-20, `poineer-foundation` succeeded in `rg-poineer-dev` in `westeurope`.
All four required providers were registered and management permissions were
verified. The deployment added only the `poineer-api-dev` user-assigned identity,
`Storage Blob Data Reader` on the existing `metadata` container, and `AcrPull`
on the existing registry. Both role assignments were verified after deployment.
The shared renderer resources were preserved.

## Verified API deployment

On 2026-11-20, ACR build `cb1` built and pushed
`poineerdevacr.azurecr.io/poineer-server:3830b50-20261120141116` with digest
`sha256:811c1a1719fec912e001ecdf68f7110a9ff28eb86ab5b5d19ece6b1552276ee9`.
Deployment `poineer-app` succeeded, and revision `poineer-api-dev--9f02xw7`
became ready in the shared Consumption environment. The app uses managed
identity for registry pulls and catalog access, rejects insecure ingress,
and has a replica range of zero to two.

API URL: <https://poineer-api-dev.icyocean-acecb4f2.westeurope.azurecontainerapps.io>

Live checks returned `200` with `Healthy` from `/api/health` and `200` from
`/api/datasets`. The catalog advertised Berlin release `2-290fe03527ac0d03`
with SQLite and PMTiles artifacts. The release version, artifact versions,
sizes, and SHA-256 checksums matched the current manifest downloaded from the
private `metadata` container with Entra authentication. This also verified
catalog access through the API's managed identity. Artifact contents and download
transport were not tested by this metadata smoke test.

## Verified published artifacts

On 2026-11-21, the existing Berlin release `2-290fe03527ac0d03`, published on
2026-11-15, was verified without republishing data. `regions.json` contained
Berlin and the current manifest referenced both artifacts in the private
`regions` container. Each artifact was downloaded with Entra authentication;
its actual file size and locally computed SHA-256 matched the manifest:

| Artifact | Size in bytes | SHA-256 |
| --- | ---: | --- |
| SQLite | 13881344 | `ee6b495c06c20b64a65b6beaff6dee0c7ec8b5045ecb83b6df3159e3192dd17f` |
| PMTiles | 41191441 | `7b532985238ab16ce94b08b8a18ababc03dd524d35524499b890e62521c88dce` |

The renderer identity `poineer-render-berlin-dev-identity` had
`Storage Blob Data Contributor` on each container separately. The live health
endpoint remained healthy, and the dataset endpoint matched the published
release and both artifacts' versions, sizes, and checksums. This verifies the
existing release for the dev catalog; it does not test the pending API download
transport or validate the semantic contents of SQLite and PMTiles.

## Provision and deploy

```powershell
$group = 'rg-poineer-dev'
$template = 'infra/dev/main.bicep'

az group show --name $group --output table
if ($LASTEXITCODE -ne 0) { throw 'Shared development resource group is missing.' }
az deployment group what-if --resource-group $group --template-file $template --mode Incremental
if ($LASTEXITCODE -ne 0) { throw 'Foundation preview failed.' }
az deployment group create --resource-group $group --name poineer-foundation --template-file $template --mode Incremental
if ($LASTEXITCODE -ne 0) { throw 'Foundation deployment failed.' }

$outputs = az deployment group show --resource-group $group --name poineer-foundation --query properties.outputs -o json | ConvertFrom-Json
$registry = $outputs.registryName.value
$storage = $outputs.storageAccountName.value

# Use a fresh tag for each build, including builds of uncommitted changes.
$tag = "$(git rev-parse --short HEAD)-$(Get-Date -AsUTC -Format yyyyMMddHHmmss)"
az acr build --registry $registry --image "poineer-server:${tag}" --platform linux/amd64 --file Dockerfile .
if ($LASTEXITCODE -ne 0) { throw 'Image build failed.' }

az deployment group create --resource-group $group --name poineer-app --template-file $template --mode Incremental --parameters "imageTag=$tag"
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

## Optional cleanup when retiring the API

Keep the shared resource group, storage account, registry, and Container Apps
environment after deployment and smoke testing. The renderer also uses them.
Never delete the resource group as part of server cleanup.

To retire only the API, remove its Container App, its two role assignments, and
then its managed identity. Resolve the role assignments before deleting the
identity:

```powershell
$identityId = az identity show --resource-group $group --name poineer-api-dev --query principalId -o tsv
if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve the API identity.' }
az containerapp delete --resource-group $group --name poineer-api-dev
if ($LASTEXITCODE -ne 0) { throw 'API deletion failed.' }
az role assignment delete --assignee $identityId --role 'Storage Blob Data Reader' --scope $outputs.metadataContainerId.value
if ($LASTEXITCODE -ne 0) { throw 'Metadata role deletion failed.' }
$registryId = az acr show --name $registry --query id -o tsv
if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve the shared registry.' }
az role assignment delete --assignee $identityId --role 'AcrPull' --scope $registryId
if ($LASTEXITCODE -ne 0) { throw 'Registry role deletion failed.' }
az identity delete --resource-group $group --name poineer-api-dev
```
