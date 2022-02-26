using YouTubeAutoWatchLater.YouTube.Models;

namespace YouTubeAutoWatchLater.YouTube;

public interface IYouTubeService
{
    Task<string> GetRefreshToken();

    YouTubeApi Init();

    Task<Subscriptions> GetMySubscriptions();

    Task SetUploadsPlaylistForSubscriptions(Subscriptions subscriptions);

    Task<IList<YouTubeVideo>> GetRecentVideosOfChannel(YouTubeChannel channel, DateTimeOffset dateTime);

    Task AddVideosToPlaylist(YouTubeChannel subscription, IList<YouTubeVideo> videos);
}
