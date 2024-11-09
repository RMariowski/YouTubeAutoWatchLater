using Microsoft.Azure.Functions.Worker;
using YouTubeAutoWatchLater.Core.Handlers;

namespace YouTubeAutoWatchLater.Azure.Functions;

public sealed class DeleteAutoAddedVideosFunction
{
    private readonly IDeleteAutoAddedVideosHandler _handler;

    public DeleteAutoAddedVideosFunction(IDeleteAutoAddedVideosHandler handler)
    {
        _handler = handler;
    }

    [Function(nameof(DeleteAutoAddedVideos))]
    public async Task DeleteAutoAddedVideos(
        [TimerTrigger("%DeleteAutoAddedVideos:Cron%", RunOnStartup = RunOnStartup.WhenDebug)]
        TimerInfo timerInfo)
    {
        await _handler.HandleAsync();
    }
}
