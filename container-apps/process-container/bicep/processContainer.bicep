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
}

resource ContainerAppIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-07-31-preview' existing = {
  name: containerAppUidName
}

resource ProcessContainerApp 'Microsoft.App/jobs@2024-03-01' = {
  location: location
  name: '${namePrefix}-process'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${ContainerAppIdentity.id}': {}
    }
  }
  properties: {
    environmentId: ContainerEnvironment.id
    configuration: {
      triggerType: 'Schedule'
      replicaTimeout: 60
      registries: [
        {
          server: '${ContainerRegistry.name}.azurecr.io'
          identity: ContainerAppIdentity.id
        }
      ]
      scheduleTriggerConfig: {
        cronExpression: '0 */1 * * *'
      }
    }
    template: {
      containers: [
        {
          image: '${ContainerRegistry.name}.azurecr.io/process-container:${containerTag}'
          name: revisionName
          env: [
            {
              name: 'InputFile'
              value: 'https://github.com/ThomasBleijendaal/blog/archive/refs/heads/main.zip'
            }
            {
              name: 'PublishFolder'
              value: '/share/static'
            }
          ]
          resources: {
            cpu: any('0.25')
            memory: '0.5Gi'
          }
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
          storageName: 'sharereadwrite'
        }
      ]
    }
  } 
}
