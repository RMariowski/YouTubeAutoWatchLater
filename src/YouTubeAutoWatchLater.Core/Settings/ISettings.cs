namespace YouTubeAutoWatchLater.Core.Settings;

public interface ISettings
{
    string RefreshToken { get; }
    string PlaylistId { get; }
}
