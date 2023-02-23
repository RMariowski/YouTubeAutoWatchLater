using System.Reflection;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace YouTubeAutoWatchLater.Core.Google;

internal sealed class GoogleApi : IGoogleApi
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GoogleOptions _options;
    private readonly ILogger<GoogleApi> _logger;

    public GoogleApi(IHttpClientFactory httpClientFactory, IOptions<GoogleOptions> options, ILogger<GoogleApi> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<UserCredential> Authorize()
    {
        var googleClientSecrets = await GoogleClientSecrets.FromFileAsync("client_secrets.json");
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            googleClientSecrets.Secrets, new[] { YouTubeService.Scope.Youtube },
            "user", CancellationToken.None, new FileDataStore(GetType().ToString()));
        return credential;
    }

    public async Task<string> GetAccessToken()
    {
        var secrets = GetClientSecrets().Secrets;
        HttpRequestMessage refreshMessage = new(HttpMethod.Post, "https://www.googleapis.com/oauth2/v4/token")
        {
            Content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
            {
                new("client_id", secrets.ClientId),
                new("client_secret", secrets.ClientSecret),
                new("refresh_token", _options.RefreshToken),
                new("grant_type", "refresh_token")
            })
        };

        _logger.LogInformation("Sending request for getting access token");
        var response = await _httpClientFactory.CreateClient().SendAsync(refreshMessage);
        _logger.LogInformation("Finished sending request for getting access token");

        if (response.IsSuccessStatusCode is false)
        {
            var reason = await response.Content.ReadAsStringAsync();
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
        BaseClientService.Initializer initializer = new()
        {
            HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
            ApplicationName = nameof(YouTubeAutoWatchLater)
        };
        return new YouTubeService(initializer);
    }

    private GoogleClientSecrets GetClientSecrets()
    {
        const string clientSecretsFileName = "client_secrets.json";

        _logger.LogInformation($"Creating path of {clientSecretsFileName}");
        var binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var clientSecretsFilePath = Path.Combine(binDirectory, clientSecretsFileName);
        _logger.LogInformation($"Created path: {clientSecretsFilePath}");

        _logger.LogInformation($"Reading {clientSecretsFilePath} file");
        var googleClientSecrets = GoogleClientSecrets.FromFile(clientSecretsFilePath);
        if (googleClientSecrets is null)
            throw new ApplicationException($"{clientSecretsFilePath} file not found");
        _logger.LogInformation($"Finished reading {clientSecretsFilePath} file");

        return googleClientSecrets;
    }
}
