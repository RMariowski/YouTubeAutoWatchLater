namespace YouTubeAutoWatchLater.Core.Models;

public sealed record VideoId(string Value)
{
    public string Value { get; } = IsValid(Value) ? Value : throw new ApplicationException("Invalid video id");

    public static bool IsValid(string value)
        => !string.IsNullOrWhiteSpace(value);

    public override string ToString() => Value;
}
