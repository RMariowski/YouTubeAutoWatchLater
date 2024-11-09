using Microsoft.Azure.Functions.Worker;
using YouTubeAutoWatchLater.Core.Handlers;

namespace YouTubeAutoWatchLater.Azure.Functions;

public sealed class UpdateAutoWatchLaterFunction
{
    private readonly IUpdateAutoWatchLaterHandler _handler;

    public UpdateAutoWatchLaterFunction(IUpdateAutoWatchLaterHandler handler)
    {
        _handler = handler;
    }

    [Function(nameof(UpdateAutoWatchLater))]
    public async Task UpdateAutoWatchLater(
        [TimerTrigger("%Cron%", RunOnStartup = RunOnStartup.WhenDebug)]
        TimerInfo timerInfo)
    {
        await _handler.HandleAsync();
    }
}
