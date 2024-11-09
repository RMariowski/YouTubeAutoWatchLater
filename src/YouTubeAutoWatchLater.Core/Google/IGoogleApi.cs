using Google.Apis.Auth.OAuth2;
using Google.Apis.YouTube.v3;

namespace YouTubeAutoWatchLater.Core.Google;

public interface IGoogleApi
{
    Task<UserCredential> AuthorizeAsync();

    Task<string> GetAccessTokenAsync();

    YouTubeService CreateYouTubeService(string accessToken);
}
