param location string = resourceGroup().location

param namePrefix string = 'containerblog'

param containerRegistryName string = '${namePrefix}cr'
param containerEnvironmentName string = '${namePrefix}-ace'
param containerAppUidName string = '${namePrefix}-uid'

param revisionName string
param containerTag string

resource ContainerRegistry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' existing = {
  name: containerRegistryName 
}

resource ContainerEnvironment 'Microsoft.App/managedEnvironments@2023-11-02-preview' existing = {
  name: containerEnvironmentName

  resource Certificate 'managedCertificates' existing = {
    name: 'blog.bleij.pro-containe-240516090932'
  }
}

resource ContainerAppIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-07-31-preview' existing = {
  name: containerAppUidName
}

resource ContainerStorageApp 'Microsoft.App/containerApps@2024-03-01' existing = {
  name: '${namePrefix}-storage'
}

resource ContainerStorageAccount 'Microsoft.Storage/storageAccounts@2023-01-01'= {
  location: location
  name: '${namePrefix}httpsta'
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
     minimumTlsVersion: 'TLS1_2'
  }
}

resource HttpContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  location: location
  name: '${namePrefix}-http'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${ContainerAppIdentity.id}': {}
    }
  }
  properties: {
    environmentId: ContainerEnvironment.id
    managedEnvironmentId: ContainerEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 80
        transport: 'auto'
        customDomains: [
          {
            certificateId: ContainerEnvironment::Certificate.id
            bindingType: 'SniEnabled'
            name: 'blog.bleij.pro'
          }
        ]
      }
      registries: [
        {
          server: '${ContainerRegistry.name}.azurecr.io'
          identity: ContainerAppIdentity.id
        }
      ]
      maxInactiveRevisions: 3
    }
    template: {
      revisionSuffix: revisionName
      terminationGracePeriodSeconds: 30
      scale: {
        minReplicas: 1
        maxReplicas: 2
      }
      containers: [
        {
          image: '${ContainerRegistry.name}.azurecr.io/http-container:${containerTag}'
          name: revisionName
          env: [
            {
              name: 'FUNCTIONS_WORKER_RUNTIME'
              value: 'dotnet-isolated'
            }
            {
              name: 'AzureWebJobsStorage'
              value: 'DefaultEndpointsProtocol=https;AccountName=${ContainerStorageAccount.name};AccountKey=${ContainerStorageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
            }
            {
              name: 'StorageContainer__Address'
              value: 'https://${ContainerStorageApp.properties.configuration.ingress.fqdn}'
            }
          ]
          resources: {
            cpu: any('0.25')
            memory: '0.5Gi'
          }
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                port: 80
                path: 'container/live'
                scheme: 'HTTP'
              }
              periodSeconds: 10
            }
            {
              type: 'Readiness'
              httpGet: {
                port: 80
                path: 'container/ready'
                scheme: 'HTTP'
              }
              periodSeconds: 10
            }
            {
              type: 'Startup'
              httpGet: {
                port: 80
                path: 'container/startup'
                scheme: 'HTTP'
              }
              periodSeconds: 10
            }
          ]
        }
      ]
    }
  }
}
