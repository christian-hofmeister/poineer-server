@description('Azure region of the shared storage account and Container Apps environment.')
param location string = resourceGroup().location

@description('Existing shared development storage account in this resource group.')
param existingStorageAccountName string = 'poineerstoragedev'

@description('Existing shared development container registry in this resource group.')
param existingRegistryName string = 'poineerdevacr'

@description('Existing shared Consumption environment in this resource group.')
param existingEnvironmentName string = 'poineer-dev-env'

@description('Leave empty to provision only the API identity and its roles before building the image.')
param imageTag string = ''

var repository = 'poineer-server'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: existingStorageAccountName
}

resource blobs 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' existing = {
  parent: storage
  name: 'default'
}

resource metadata 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' existing = {
  parent: blobs
  name: 'metadata'
}

resource artifacts 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' existing = {
  parent: blobs
  name: 'regions'
}

resource registry 'Microsoft.ContainerRegistry/registries@2025-11-01' existing = {
  name: existingRegistryName
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'poineer-api-dev'
  location: location
}

var blobReader = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1')
resource metadataReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(metadata.id, identity.id, blobReader)
  scope: metadata
  properties: {
    roleDefinitionId: blobReader
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

var acrPull = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
resource imagePull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, identity.id, acrPull)
  scope: registry
  properties: {
    roleDefinitionId: acrPull
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource environment 'Microsoft.App/managedEnvironments@2025-01-01' existing = {
  name: existingEnvironmentName
}

resource app 'Microsoft.App/containerApps@2025-01-01' = if (!empty(imageTag)) {
  name: 'poineer-api-dev'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
  properties: {
    environmentId: environment.id
    workloadProfileName: 'Consumption'
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }
      registries: [
        {
          server: registry.properties.loginServer
          identity: identity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: '${registry.properties.loginServer}/${repository}:${imageTag}'
          env: [
            {
              name: 'Catalog__MetadataRoot'
              value: '${storage.properties.primaryEndpoints.blob}${metadata.name}'
            }
            {
              name: 'Catalog__ArtifactRoot'
              value: '${storage.properties.primaryEndpoints.blob}${artifacts.name}'
            }
            {
              name: 'Catalog__ManagedIdentityClientId'
              value: identity.properties.clientId
            }
          ]
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          probes: [
            {
              type: 'Readiness'
              httpGet: {
                path: '/api/health'
                port: 8080
              }
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 2
      }
    }
  }
  dependsOn: [
    metadataReader
    imagePull
  ]
}

output registryName string = registry.name
output storageAccountName string = storage.name
output metadataContainerId string = metadata.id
output artifactContainerId string = artifacts.id
output apiUrl string = empty(imageTag) ? '' : 'https://${app!.properties.configuration.ingress.fqdn}'
