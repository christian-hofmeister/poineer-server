@description('Deploy all resources in this Azure region.')
param location string = resourceGroup().location

@description('Leave empty to provision storage and registry before building the image.')
param imageTag string = ''

var suffix = uniqueString(resourceGroup().id)
var repository = 'poineer-server'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: 'poineer${suffix}'
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
  }
}

resource blobs 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
}

resource metadata 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobs
  name: 'metadata'
  properties: {
    publicAccess: 'None'
  }
}

resource artifacts 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobs
  name: 'regions'
  properties: {
    publicAccess: 'None'
  }
}

resource registry 'Microsoft.ContainerRegistry/registries@2025-11-01' = {
  name: 'poineer${suffix}'
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
    roleAssignmentMode: 'LegacyRegistryPermissions'
    policies: {
      azureADAuthenticationAsArmPolicy: {
        status: 'enabled'
      }
    }
  }
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

resource environment 'Microsoft.App/managedEnvironments@2025-01-01' = if (!empty(imageTag)) {
  name: 'poineer-dev'
  location: location
  properties: {
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
  }
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
    environmentId: environment!.id
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
