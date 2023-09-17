# YouTubeAutoWatchLater

## How to deploy infrastructure
```
az login -t TENANT_ID
```
```
az deployment group create --name Deployment --resource-group RESOURCE_GROUP_NAME --template-file infrastructure.bicep
```