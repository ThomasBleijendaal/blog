---
theme: Azure Container Apps
title: HTTP Container
visible: True
---

The first container app will be the HTTP container facing the internet. Since I have a storage account with my hand-written HTML files, I can easily output those files and replace the temporary Azure Function. I have to deploy quite a lot of resource to get the first container app working so lets begin.

To host all the containers, I need a bog-standard Container Registry:
        
```bicep
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
        test: 1.2
        test2: 1
        test3: true
      }
    }
    publicNetworkAccess: 'Enabled'
    networkRuleBypassOptions: 'AzureServices'
  }
}
```

Next to that I also need as Container Environment, which, in my mind, is similar to an App Service Plan when using Web Apps. There is not much to it:

```bicep
resource ContainerEnvironment 'Microsoft.App/managedEnvironments@2023-11-02-preview' = {
  location: location
  name: containerEnvironmentName

  properties: {
    
  }
}
```



To give Container Apps access to the Container Registry, a User Assigned Identity needs to be created so I have something to assign the ARC Pull Role to:

```bicep
resource ContainerAppIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-07-31-preview' = {
  location: location
  name: containerAppUidName
}

resource ContainerAppIdentityRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, ContainerAppIdentity.name, ContainerRegistry.name)
  scope: ContainerRegistry
  properties: {
    principalId: ContainerAppIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: arcPullRoleDefinitionId
  }
}
```

If we want to deploy the Container App into the environment, we need to can add the following:

```bicep
resource HttpContainerApp 'Microsoft.App/containerApps@2023-11-02-preview' = {
  location: location
  name: httpContainerName
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
      activeRevisionsMode: 'Multiple'
      ingress: {
        external: true
        targetPort: 7071
        transport: 'auto'
      }
      registries: [
        {
          server: '${ContainerRegistry.name}.azurecr.io'
          identity: 'system'
        }
      ]
    }
  }
}
```

This add an app to the environment and give it access the container registry to pull any container. Note that this example _does not work_. It is missing the template part for the container app. This part needs the container name, the revision name, resources the container needs, and any environment variables that might be required to run the container.

That template part might look something like:

```bicep
resource HttpContainerApp 'Microsoft.App/containerApps@2023-11-02-preview' = {
    // [..]
    template: {
      revisionSuffix: '2024-04-25-001'
      terminationGracePeriodSeconds: 30
      scale: {
        minReplicas: 0
        maxReplicas: 2
      }
      containers: [
        {
          image: '${ContainerRegistry.name}.azurecr.io/http-container:20240425'
          name: '2024-04-25-001'
          env: [
            {
              name: 'VERSION'
              value: '2024-04-25-001'
            }
          ]
          resources: {
            cpu: any('0.25')
            memory: '0.5Gi'
          }
        }
      ]
    }
}
```

The problem that I have with this is that it requires an ever changing revision suffix and container tag, which should align with the uploaded containers in the container registry. You could move this part of the configuration to some command line solution, but the environment variables and resource specification are inherently infra. I find best to put those in bicep templates, instead of hiding it somewhere in some yaml pipeline.

I came up with somewhat of a middle ground solution:

- Put each Container App in a separate bicep from the Container Environment and Registry. This probably scales the best and isolates the apps.
- Only supply infra-related environment variables, no configuration. So things like the name of the KeyVault or App Configuration instances the container can access with its MSI, and not actual config.
- Keep resource spec in the bicep file.
- Keep probes in the bicep file.
- Accept the revision name and container tag via parameters. These should be date based, and provided by the earlier build step.

Capturing this in a powershell script will result in:

{!container-apps/http-container/pipeline/deploy.ps1!}
