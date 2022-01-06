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
        var refreshMessage = new HttpRequestMessage(HttpMethod.Post, "https://www.googleapis.com/oauth2/v4/token")
        {
            Content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
            {
                new("client_id", Environment.GetEnvironmentVariable("YouTube:ClientId")!),
                new("client_secret", Environment.GetEnvironmentVariable("YouTube:ClientSecret")!),
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
