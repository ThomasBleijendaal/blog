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

resource ContainerEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: containerEnvironmentName
}

resource ContainerAppIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-07-31-preview' existing = {
  name: containerAppUidName
}

resource StorageContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  location: location
  name: '${namePrefix}-storage'
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
        external: false
        targetPort: 8080
        transport: 'http2'
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
          image: '${ContainerRegistry.name}.azurecr.io/storage-container:${containerTag}'
          name: revisionName
          env: [
            {
              name: 'Storage__FileRoot'
              value: '/share/static'
            }
          ]
          resources: {
            cpu: any('0.25')
            memory: '0.5Gi'
          }
          probes: [
            {
              type: 'Liveness'
              tcpSocket: {
                port: 8080
              }
              periodSeconds: 10
            }
            {
              type: 'Readiness'
              tcpSocket: {
                port: 8080
              }
              periodSeconds: 10
            }
            {
              type: 'Startup'
              tcpSocket: {
                port: 8080
              }
              periodSeconds: 10
            }
          ]
          volumeMounts: [
            {
              mountPath: '/share'
              volumeName: 'share'
              subPath: ''
            }
          ]
        }
      ]
      volumes: [
        {
          storageType: 'AzureFile'
          name: 'share'
          storageName: 'shareread'
        }
      ]
    }
  } 
}
