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

    public async Task<UserCredential> AuthorizeAsync(int refreshTokenIdx)
    {
        var secrets = GetClientSecrets(refreshTokenIdx);
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets.Secrets, [YouTubeService.Scope.Youtube],
            "user", CancellationToken.None, new FileDataStore($"{GetType()}_{refreshTokenIdx}"));
        return credential;
    }

    public YouTubeService[] CreateYouTubeServices()
    {
        List<YouTubeService> services = new(_options.RefreshTokens.Length);
        for (var idx = 0; idx < _options.RefreshTokens.Length; idx++)
        {
            _logger.LogInformation("Getting access token");
            var accessToken = GetAccessTokenAsync(idx).GetAwaiter().GetResult();
            _logger.LogInformation("Finished getting access token");

            BaseClientService.Initializer initializer = new()
            {
                HttpClientInitializer = GoogleCredential.FromAccessToken(accessToken),
                ApplicationName = nameof(YouTubeAutoWatchLater)
            };

            services.Add(new YouTubeService(initializer));
        }

        return services.ToArray();
    }

    private async Task<string> GetAccessTokenAsync(int refreshTokenIdx)
    {
        var secrets = GetClientSecrets(refreshTokenIdx).Secrets;
        HttpRequestMessage refreshMessage = new(HttpMethod.Post, "https://www.googleapis.com/oauth2/v4/token")
        {
            Content = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("client_id", secrets.ClientId),
                new KeyValuePair<string, string>("client_secret", secrets.ClientSecret),
                new KeyValuePair<string, string>("refresh_token", _options.RefreshTokens[refreshTokenIdx]),
                new KeyValuePair<string, string>("grant_type", "refresh_token")
            ])
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
        await using JsonTextReader jsonReader = new(streamReader);
        var tokenResponse = new JsonSerializer().Deserialize<TokenResponse>(jsonReader);
        if (tokenResponse is null)
            throw new ApplicationException("Something went wrong with deserialization of token response");

        return tokenResponse.AccessToken;
    }

    private GoogleClientSecrets GetClientSecrets(int refreshTokenIdx)
    {
        var clientSecretsFileName = $"client_secrets_{refreshTokenIdx}.json";
        var binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var clientSecretsFilePath = Path.Combine(binDirectory, clientSecretsFileName);
        _logger.LogInformation("Created path: {ClientSecretsFilePath}", clientSecretsFilePath);

        _logger.LogInformation("Reading {ClientSecretsFilePath} file", clientSecretsFilePath);
        var googleClientSecrets = GoogleClientSecrets.FromFile(clientSecretsFilePath);
        if (googleClientSecrets is null)
            throw new ApplicationException($"{clientSecretsFilePath} file not found");
        _logger.LogInformation("Finished reading {ClientSecretsFilePath} file", clientSecretsFilePath);

        return googleClientSecrets;
    }
}
