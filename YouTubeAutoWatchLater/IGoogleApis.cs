namespace YouTubeAutoWatchLater;

public interface IGoogleApis
{
    Task<string> GetAccessToken();

    YouTubeService CreateYouTubeService(string accessToken);
}
