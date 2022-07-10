using Google.Apis.Auth.OAuth2;

namespace YouTubeAutoWatchLater.GoogleApis;

public interface IGoogleApis
{
    Task<UserCredential> Authorize();

    Task<string> GetAccessToken();

    YouTubeService CreateYouTubeService(string accessToken);
}
