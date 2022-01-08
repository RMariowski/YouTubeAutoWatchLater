using System.IO;
using System.Net.Http;
using System.Reflection;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using YouTubeAutoWatchLater.Settings;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace YouTubeAutoWatchLater.GoogleApis;

public class GoogleApis : IGoogleApis
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISettings _settings;
    private readonly ILogger<GoogleApis> _logger;
    private readonly Lazy<GoogleClientSecrets> _clientSecrets;

    private GoogleClientSecrets ClientSecrets => _clientSecrets.Value;

    public GoogleApis(IHttpClientFactory httpClientFactory, ISettings settings, ILogger<GoogleApis> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings;
        _logger = logger;
        _clientSecrets = new Lazy<GoogleClientSecrets>(() => GetClientSecrets(_logger));
    }

    public async Task<string> GetAccessToken()
    {
        var secrets = ClientSecrets.Secrets;
        var refreshMessage = new HttpRequestMessage(HttpMethod.Post, "https://www.googleapis.com/oauth2/v4/token")
        {
            Content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
            {
                new("client_id", secrets.ClientId),
                new("client_secret", secrets.ClientSecret),
                new("refresh_token", _settings.RefreshToken),
                new("grant_type", "refresh_token")
            })
        };

        _logger.LogInformation("Sending request for getting access token...");
        var response = await _httpClientFactory.CreateClient().SendAsync(refreshMessage);
        _logger.LogInformation("Finished sending request for getting access token...");

        if (response.IsSuccessStatusCode is false)
        {
            string reason = await response.Content.ReadAsStringAsync();
            throw new ApplicationException($"Failed to get access token. Reason: {reason}");
        }

        var contentStream = await response.Content.ReadAsStreamAsync();
        using StreamReader streamReader = new(contentStream);
        using JsonTextReader jsonReader = new(streamReader);
        var tokenResponse = new JsonSerializer().Deserialize<TokenResponse>(jsonReader);
        if (tokenResponse is null)
            throw new ApplicationException("Something went wrong with deserialization of token response");

        return tokenResponse.AccessToken;
    }

    public YouTubeService CreateYouTubeService(string accessToken)
    {
        var initializer = new BaseClientService.Initializer
        {
            HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
            ApplicationName = nameof(YouTubeAutoWatchLater)
        };
        var youtubeService = new YouTubeService(initializer);
        return youtubeService;
    }

    private static GoogleClientSecrets GetClientSecrets(ILogger logger)
    {
        const string clientSecretsFileName = "client_secrets.json";

        logger.LogInformation($"Creating path of {clientSecretsFileName}");
        string binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        string rootDirectory = Path.GetFullPath(Path.Combine(binDirectory, ".."))!;
        string clientSecretsFilePath = Path.Combine(rootDirectory, clientSecretsFileName);
        logger.LogInformation($"Created path: {clientSecretsFilePath}");

        logger.LogInformation($"Reading {clientSecretsFilePath} file...");
        var googleClientSecrets = GoogleClientSecrets.FromFile(clientSecretsFilePath);
        if (googleClientSecrets is null)
            throw new ApplicationException($"{clientSecretsFilePath} file not found");
        logger.LogInformation($"Finished reading {clientSecretsFilePath} file");

        return googleClientSecrets;
    }
}
