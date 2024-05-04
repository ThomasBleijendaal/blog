---
theme: Azure Container Apps
title: Storage Container
visible: True
---

The storage container will be a gRPC based ASP.NET application serving files from an Azure Files file share. First, we have to add an Azure Files share to the Container Environment:

```bicep
resource ContainerStorage 'Microsoft.Storage/storageAccounts@2023-01-01'= {
  location: location
  name: '${namePrefix}sta'
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  
  resource FileService 'fileServices' = {
    name: 'default'
    
    resource Share 'shares' = {
      name: 'share'
    }
  }
}

resource ContainerEnvironment 'Microsoft.App/managedEnvironments@2023-11-02-preview' = {
  location: location
  name: containerEnvironmentName
  properties: {}

  resource Files 'storages' = {
    name: 'share'
    properties: {
      azureFile: {
        accountName: ContainerStorage.name
        accessMode: 'ReadWrite'
        accountKey: ContainerStorage.listKeys().keys[0].value
        shareName: ContainerStorage::FileService::Share.name
      }
    }
  }
}
```

This will add a file share that we use in a container. A container needs the volume added, which can be mount to a specific image in the Container App.

```bicep
resource StorageContainerApp 'Microsoft.App/containerApps@2023-11-02-preview' = {
  // ..
  properties: {
    // ..
    template: {
      containers: [
        {
          // ..
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
          storageName: 'share'
        }
      ]
    }
  } 
}
```

The rest or the container is pretty mundane, although setting up the probes is a bit more tricky. This is due to the behavior of ASP.NET, and it's pretty annoying. gRPC requires HTTP/2, which can be setup by adding a small instruction to appsettings.json:

```json
{
  "Kestrel": {
    "EndpointDefaults": {
      "Protocols": "Http2"
    }
  }
}
```

Probing does not support HTTP/2, so one would think that changing `Http2` into `Http1AndHttp2` would fix that. But with that setting ASP.NET suddenly requires TLS configuration on HTTP/2, instead of assuming "h2c", HTTP/2 over clear text. There is no easy way to get around that, and I didn't want to mess with certificates. Instead of using http probes, I converted to tcp socket probes on the same port as HTTP/2 and didn't bother implementing much. In the real world you might want to implement something on a separate tcp port and have it reflect the real status of the container. I find it surprising that probes do not support HTTP/2, which would have made everything pretty easy.

```bicep
resource StorageContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  // ..
  properties: {
    // ..
    template: {
      // ..
      containers: [
        {
          // ..
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
        }
      ]
    }
  } 
}
```

The build and deploy pipeline for this container is identical to the http container:

{!container-apps/storage-container/pipeline/deploy.ps1!}
