---
theme: Azure Container Apps
title: Process Container
visible: True
---

The process container will be a simple F# console application that listens to a Service Bus queue. Using the `queue-based-autoscaling` feature of the scaling rules it is easy to setup autoscaling based on the amount of service bus messages. Getting it to work is a bit difficult, there are a lot of tutorials that skip over lots of stuff, or show incomplete bicep files.

The complete bicep I needed to create is listed below:

{!container-apps/process-container/bicep/processContainer.bicep!}

Few highlights:

- KEDA needs a connection string to connect with the service bus queue, no MSI support yet.
- The `listKeysEndpoint` is uses the built-in `RootManageSharedAccessKey`, which is a bit much for just reading the message count. That can probably be tuned down.
- A secret needs to be configured which the autoscaling rule can use. The name of that secret must be lowercase.
- The message count (a `string`) is the amount of messages to trigger on.
- The config is probably forwarded to [the KEDA scaler](https://keda.sh/docs/2.0/scalers/azure-service-bus/) without much sanitization or validation, the message count is even a `string` while an `int` would have been a better option.
