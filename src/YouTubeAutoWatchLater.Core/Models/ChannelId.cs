namespace YouTubeAutoWatchLater.Core.Models;

public sealed record ChannelId(string Value)
{
    public string Value { get; } = IsValid(Value) ? Value : throw new ApplicationException("Invalid channel id");

    public static bool IsValid(string value)
        => !string.IsNullOrWhiteSpace(value);

    public override string ToString() => Value;
}
