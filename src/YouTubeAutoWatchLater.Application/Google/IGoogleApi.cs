using Google.Apis.Auth.OAuth2;
using Google.Apis.YouTube.v3;

namespace YouTubeAutoWatchLater.Application.Google;

public interface IGoogleApi
{
    Task<UserCredential> Authorize();

    Task<string> GetAccessToken();

    YouTubeService CreateYouTubeService(string accessToken);
}
