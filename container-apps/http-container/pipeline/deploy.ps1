$now = Get-Date
$revisionName = $now.ToString("yyyyMMdd-HHmmss")
$containerTag = $revisionName

$localContainerName = "blog/http-container:$containerTag"
$remoteContainerName = "containerblogcr.azurecr.io/http-container:$containerTag"

docker build -f ../src/Dockerfile ../src/ -t $localContainerName

az acr login --name containerblogcr

docker tag $localContainerName $remoteContainerName

docker push $remoteContainerName

az deployment group create -f ../bicep/httpContainer.bicep -g blog -p revisionName=$revisionName containerTag=$containerTag

docker rmi $remoteContainerName
docker rmi $localContainerName
