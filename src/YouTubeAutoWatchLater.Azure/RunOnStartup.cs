namespace YouTubeAutoWatchLater.Azure;

internal static class RunOnStartup
{
    internal const bool WhenDebug =
#if DEBUG
        true;
#else
        false;
#endif
}