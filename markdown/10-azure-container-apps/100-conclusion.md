---
theme: Azure Container Apps
title: Conclusion
visible: True
---

There are quite somethings to pay attention to when developing something using Container Apps, some come from its kubernetes background, some from docker, and some from Azure.

## Kubernetes

- Probing gRPC ASP.NET projects are a bit hard, as HTTP/2 probes are not supported by Kubernetes and setting up HTTP/1 next to HTTP/2 leads to SSL issues. I've solved it by implementing some custom TCP listeners running in a background service.
- Standardize on port numbering. It's easy to create a mess because ASP.NET defaults to 8080 while Function Apps to 80, its probably best to assign the same port number to the same stuff.
- Being able to connect with a container and inspect its contents via the console is very useful. But not possible if the container doesn't want to start.
- Inter-container communication can be done by configuring the FQDN of the target container as environment variable, but can also be done by configuring the target containers name and append the `CONTAINER_APP_ENV_DNS_SUFFIX` environment variable to it. 

## Docker

- Be mindful of how to use the Dockerfile. Its best to write the Dockerfile using paths from the root of your repository. It requires some deep paths sometimes, but it keeps everything working relatively (like csproj files linking to shared files).
- Default ASP.NET Core 8 linux docker containers are 200MB and default .NET 8 Out-Of-Process Azure Functions linux docker containers are 500MB. That is too much and must be reduced.

## Azure

- A log analytics workspace is required. You can skip it, but you'll miss the log retention and can only view the console of containers that are running.
- The UI is a bit clunky, and you're often redirected to the log stream of the wrong container.
- Depending on how big the containers are and how many dependencies they have, it might be good to deploy each Container App to its own resource groups.
- Azure Function containers still need a storage account. Since each function assumes its the only user of that storage account, you'll need a lot.
- Linux .NET is something different than Windows .NET, and some abstractions don't work. For example, `Directory.Move` doesn't work if the target folder is on another device, and devices are hard to spot on Linux. `Directory.Delete()` can fail on non-empty folders even though it's instructed to delete recursively. This has to do with SMB on Azure Files, which sometimes schedules files to be deleted, without actually deleting them, making it very annoying to work with it. It's important to use ReadOnly and ReadWrite file shares, as that can help mitigate the SMB issues.

## Performance

I've also performed the same load test on the new setup, and got these results:

- Random manual Postman requests: around 25ms, versus 50ms of the function app.
- Load test with 25 concurrent users: 225ms, versus 130ms of the function app. 

Please note that the consumption plan function app could (in theory) scale infinitely, while the container app was limited to 2 replicas of 0.25cpu. I do notice some disruption when the second replica was added, so I retested and got:

- Random manual Postman requests: still around 25ms.
- Load test with 25 concurrent users: 100ms, with a deviation of 2ms. 

The function app was talking directly to the storage account, while the internet facing http container first talked to another container over gRPC, which in turn talked to the storage account.
