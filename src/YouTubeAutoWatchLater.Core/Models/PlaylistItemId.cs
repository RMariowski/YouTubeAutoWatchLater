namespace YouTubeAutoWatchLater.Core.Models;

public record PlaylistItemId(string Value)
{
    public string Value { get; } = IsValid(Value) ? Value : throw new ApplicationException("Invalid playlist item id");

    public static bool IsValid(string value)
        => !string.IsNullOrWhiteSpace(value);
}
