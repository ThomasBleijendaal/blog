param location string = resourceGroup().location

param namePrefix string = 'containerblog'

param containerRegistryName string = '${namePrefix}cr'
param containerEnvironmentName string = '${namePrefix}-ace'
param containerAppUidName string = '${namePrefix}-uid'
param logAnalyticsWorkspaceName string = '${namePrefix}-law'
param serviceBusNamespaceName string = '${namePrefix}-asb'

param arcPullRoleDefinitionId string = resourceId(
  'Microsoft.Authorization/roleDefinitions',
  '7f951dda-4ed3-4680-a7ca-43fe172d538d'
)

param asbDataOwnerRoleDefinitionId string = resourceId(
  'Microsoft.Authorization/roleDefinitions',
  '090c5cfd-751d-490a-894a-3ce6f1109419'
)

resource ContainerStorage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  location: location
  name: '${namePrefix}sta'
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
     minimumTlsVersion: 'TLS1_2'
  }

  resource FileService 'fileServices' = {
    name: 'default'

    resource Share 'shares' = {
      name: 'share'
    }
  }
}

resource ContainerRegistry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  location: location
  name: containerRegistryName
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
    policies: {
      azureADAuthenticationAsArmPolicy: {
        status: 'enabled'
      }
      exportPolicy: {
        status: 'enabled'
      }
    }
    publicNetworkAccess: 'Enabled'
    networkRuleBypassOptions: 'AzureServices'
  }
}

resource ContainerEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  location: location
  name: containerEnvironmentName
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: LogAnalyticsWorkspace.properties.customerId
        sharedKey: LogAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
  }

  resource ReadWriteFiles 'storages' = {
    name: 'sharereadwrite'
    properties: {
      azureFile: {
        accountName: ContainerStorage.name
        accessMode: 'ReadWrite'
        accountKey: ContainerStorage.listKeys().keys[0].value
        shareName: ContainerStorage::FileService::Share.name
      }
    }
  }

  resource ReadFiles 'storages' = {
    name: 'shareread'
    properties: {
      azureFile: {
        accountName: ContainerStorage.name
        accessMode: 'ReadOnly'
        accountKey: ContainerStorage.listKeys().keys[0].value
        shareName: ContainerStorage::FileService::Share.name
      }
    }
  }

  resource Certificate 'managedCertificates' = {
    location: location
    name: 'blog.bleij.pro-containe-240516090932'
    properties: {
      domainControlValidation: 'CNAME'
      subjectName: 'blog.bleij.pro'
    }
  }
}

resource ServiceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  location: location
  name: serviceBusNamespaceName
  properties: {
    minimumTlsVersion: '1.2'
  }
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
}

resource ContainerAppIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-07-31-preview' = {
  location: location
  name: containerAppUidName
}

resource ContainerAppIdentityArcPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, ContainerAppIdentity.name, ContainerRegistry.name)
  scope: ContainerRegistry
  properties: {
    principalId: ContainerAppIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: arcPullRoleDefinitionId
  }
}

resource ContainerAppIdentityAsbDataOwnerRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, ContainerAppIdentity.name, ContainerRegistry.name, asbDataOwnerRoleDefinitionId)
  scope: ServiceBus
  properties: {
    principalId: ContainerAppIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: asbDataOwnerRoleDefinitionId
  }
}

resource LogAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  location: location
  name: logAnalyticsWorkspaceName
  properties: {
    retentionInDays: 30
    sku: {
      name: 'PerGB2018'
    }
  }
}
