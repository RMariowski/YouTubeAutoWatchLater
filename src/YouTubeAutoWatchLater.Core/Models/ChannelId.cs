namespace YouTubeAutoWatchLater.Core.Models;

public record ChannelId(string Value)
{
    public string Value { get; } = IsValid(Value) ? Value : throw new ApplicationException("Invalid channel id");

    public static bool IsValid(string value)
        => !string.IsNullOrWhiteSpace(value);
}