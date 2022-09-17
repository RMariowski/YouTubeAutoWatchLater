namespace YouTubeAutoWatchLater.Application.YouTube.Options;

public sealed class YouTubeOptions
{
    public string PlaylistId { get; set; } = string.Empty;
    public PlaylistRuleOptions[] PlaylistRules { get; set; } = Array.Empty<PlaylistRuleOptions>();
}

public sealed class PlaylistRuleOptions
{
    public string PlaylistId { get; set; } = string.Empty;
    public string Channels { get; set; } = string.Empty;
    public string TitleKeywords { get; set; } = string.Empty;
}
