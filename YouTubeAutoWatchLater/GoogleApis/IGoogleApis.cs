namespace YouTubeAutoWatchLater.GoogleApis;

public interface IGoogleApis
{
    Task<string> GetAccessToken();

    YouTubeService CreateYouTubeService(string accessToken);
}
