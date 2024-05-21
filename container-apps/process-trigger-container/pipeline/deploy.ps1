$now = Get-Date
$revisionName = $now.ToString("yyyyMMdd-HHmmss")
$containerTag = $revisionName

$localContainerName = "blog/process-trigger-container:$containerTag"
$remoteContainerName = "containerblogcr.azurecr.io/process-trigger-container:$containerTag"

docker build -f ../src/Dockerfile ../../../ -t $localContainerName

az acr login --name containerblogcr

docker tag $localContainerName $remoteContainerName

docker push $remoteContainerName

az deployment group create -f ../bicep/processTriggerContainer.bicep -g blog -p revisionName=$revisionName containerTag=$containerTag

docker rmi $remoteContainerName
docker rmi $localContainerName
