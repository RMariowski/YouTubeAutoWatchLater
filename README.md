# YouTubeAutoWatchLater

## YouTube API
1. Create a new project in Google Cloud Platform
2. Enable `YouTube Data API v3` API
3. Configure OAuth consent screen: set user type as `External` and publish it
4. Create a new OAuth 2.0 client ID. Set application type as `Web application` and add authorized redirect URIs: `https://developers.google.com/oauthplayground`, `http://localhost`, `http://127.0.0.1/authorize/`
5. Download client_secrets.json and add to project
6. Send GET `api/GetRefreshToken` to get refresh token

## How to deploy infrastructure
```
az login -t TENANT_ID
```
```
az deployment group create --name Deployment --resource-group RESOURCE_GROUP_NAME --template-file infrastructure.bicep
```