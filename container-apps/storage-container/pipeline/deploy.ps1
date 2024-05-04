$now = Get-Date
$revisionName = $now.ToString("yyyyMMdd-HHmmss")
$containerTag = $revisionName

$localContainerName = "blog/storage-container:$containerTag"
$remoteContainerName = "containerblogcr.azurecr.io/storage-container:$containerTag"

docker build -f ../src/Dockerfile ../src/ -t $localContainerName

az acr login --name containerblogcr

docker tag $localContainerName $remoteContainerName

docker push $remoteContainerName

az deployment group create -f ../bicep/storageContainer.bicep -g blog -p revisionName=$revisionName containerTag=$containerTag

docker rmi $remoteContainerName
docker rmi $localContainerName
