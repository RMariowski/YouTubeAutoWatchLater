namespace YouTubeAutoWatchLater.Core.Models;

public record VideoId(string Value)
{
    public string Value { get; } = IsValid(Value) ? Value : throw new ApplicationException("Invalid video id");

    public static bool IsValid(string value)
        => !string.IsNullOrWhiteSpace(value);
}
