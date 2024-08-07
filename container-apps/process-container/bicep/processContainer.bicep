param location string = resourceGroup().location

param namePrefix string = 'containerblog'

param containerRegistryName string = '${namePrefix}cr'
param containerEnvironmentName string = '${namePrefix}-ace'
param containerAppUidName string = '${namePrefix}-uid'
param serviceBusNamespaceName string = '${namePrefix}-asb'

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

resource ServiceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' existing = {
  name: serviceBusNamespaceName

  resource ServiceBusQueue 'queues' existing = {
    name: 'process'
  }
}

var listKeysEndpoint = '${ServiceBus.id}/AuthorizationRules/RootManageSharedAccessKey'

resource ProcessContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
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
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: '${ContainerRegistry.name}.azurecr.io'
          identity: ContainerAppIdentity.id
        }
      ]
      secrets: [
        {
          name: 'servicebusauth'
          value: 'Endpoint=sb://${ServiceBus.name}.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=${listKeys(listKeysEndpoint, ServiceBus.apiVersion).primaryKey}'
        }
      ]
      maxInactiveRevisions: 3
    }
    template: {
      revisionSuffix: revisionName
      terminationGracePeriodSeconds: 30
      scale: {
        minReplicas: 0
        maxReplicas: 1
        rules: [
          {
            name: 'queue-based-autoscaling'
            custom: {
              type: 'azure-servicebus'
              metadata: {
                queueName: ServiceBus::ServiceBusQueue.name
                messageCount: '1'
              }
              auth: [
                {
                  secretRef: 'servicebusauth'
                  triggerParameter: 'connection'
                }
              ]
            }
          }
        ]
      }
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
            {
              name: 'ServiceBusFqns'
              value: '${ServiceBus.name}.servicebus.windows.net'
            }
            {
              name: 'TenantId'
              value: subscription().tenantId
            }
            {
              name: 'ClientId'
              value: ContainerAppIdentity.properties.clientId
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
