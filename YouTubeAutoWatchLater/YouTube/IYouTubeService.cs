namespace YouTubeAutoWatchLater.YouTube;

public interface IYouTubeService
{
    Task Init();

    Task<Subscriptions> GetMySubscriptions();

    Task SetUploadsPlaylistForSubscriptions(Subscriptions subscriptions);

    Task SetRecentVideosForSubscriptions(Subscriptions subscriptions, DateTimeOffset dateTime);

    Task AddRecentVideosToPlaylist(Subscriptions subscriptions);
}
