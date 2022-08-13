namespace YouTubeAutoWatchLater.Core.YouTube;

public interface IYouTubeService
{
    Task<string> GetRefreshToken();

    Task Init();

    Task<Subscriptions> GetMySubscriptions();

    Task SetUploadsPlaylistForSubscriptions(Subscriptions subscriptions);

    Task SetRecentVideosForSubscriptions(Subscriptions subscriptions, DateTimeOffset dateTime);

    Task AddRecentVideosToPlaylist(Subscriptions subscriptions);

    Task DeletePrivatePlaylistItems();
}
