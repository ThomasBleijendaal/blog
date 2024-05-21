---
theme: Azure Container Apps
title: Process Container (temporary trigger)
visible: True
---

To trigger the process container using a simple service bus message generator I'll employ a Container App Job. 

This offering feels like a sort of compatibility offering that allows you to containerize and use old stuff and run it using a simple cron schedule. The namespace that has to be used for this resource type is `Microsoft.App/jobs`, compared to the Container Apps `Microsoft.App/containerApps`. Next to some expected differences, like no `ingress` or `probe` configuration, a schedule can be configured quite easily with `scheduleTriggerConfig::cronExpression`. The app job disappears often from the resource group, making it easy to lose track of the running app jobs.

This container will publish service bus messages in the Azure Service Bus Namespace which will trigger the process container to start processing. Eventually, I can hook up my GitHub to trigger these messages so the process container triggers when I push something - but for now a timer trigger will do. 
