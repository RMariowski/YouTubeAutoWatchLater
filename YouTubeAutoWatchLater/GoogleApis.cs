using System.IO;
using System.Net.Http;
using System.Reflection;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Newtonsoft.Json;

namespace YouTubeAutoWatchLater;

public class GoogleApis
{
    public static async Task<string> GetAccessToken()
    {
        var clientSecrets = await GetClientSecrets();
        var refreshMessage = new HttpRequestMessage(HttpMethod.Post, "https://www.googleapis.com/oauth2/v4/token")
        {
            Content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
            {
                new("client_id", clientSecrets.Secrets.ClientId),
                new("client_secret", clientSecrets.Secrets.ClientSecret),
                new("refresh_token", Environment.GetEnvironmentVariable("YouTube:RefreshToken")!),
                new("grant_type", "refresh_token")
            })
        };

        using HttpClient httpClient = new();
        var response = await httpClient.SendAsync(refreshMessage);
        if (response.IsSuccessStatusCode is false)
        {
            string reason = await response.Content.ReadAsStringAsync();
            throw new ApplicationException($"Failed to get access token. Reason: {reason}");
        }

        var contentStream = await response.Content.ReadAsStreamAsync();
        using StreamReader streamReader = new(contentStream);
        using JsonTextReader jsonReader = new(streamReader);
        JsonSerializer serializer = new();

        var tokenResponse = serializer.Deserialize<TokenResponse>(jsonReader);
        return tokenResponse!.AccessToken;
    }

    private static async Task<GoogleClientSecrets> GetClientSecrets()
    {
        const string clientSecretsFilePath = "client_secrets.json";
        var googleClientSecrets = await GoogleClientSecrets.FromFileAsync(clientSecretsFilePath);
        if (googleClientSecrets is null)
            throw new ApplicationException($"{clientSecretsFilePath} file not found");
        return googleClientSecrets;
    }

    public static YouTubeService CreateYouTubeService(string accessToken)
    {
        var youtubeService = new YouTubeService(new BaseClientService.Initializer
        {
            HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
            ApplicationName = Assembly.GetExecutingAssembly().GetName().Name
        });
        return youtubeService;
    }
}
