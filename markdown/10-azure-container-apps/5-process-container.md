---
theme: Azure Container Apps
title: Process Container
visible: True
---

The process container will be a Container App Job type of container. This offering feels like a sort of compatibility offering that allows you to containerize and use old stuff and run it using a simple cron schedule.

The namespace that has to be used for this resource type is `Microsoft.App/jobs`, compared to the Container Apps `Microsoft.App/containerApps`. Next to some expected differences, like no `ingress` or `probe` configuration, a schedule can be configured quite easily with `scheduleTriggerConfig::cronExpression`.

