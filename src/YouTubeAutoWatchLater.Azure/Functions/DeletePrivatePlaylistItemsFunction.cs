using Microsoft.Azure.Functions.Worker;
using YouTubeAutoWatchLater.Core.Handlers;

namespace YouTubeAutoWatchLater.Azure.Functions;

public sealed class DeletePrivatePlaylistItemsFunction
{
    private readonly IDeletePrivatePlaylistItemsHandler _handler;

    public DeletePrivatePlaylistItemsFunction(IDeletePrivatePlaylistItemsHandler handler)
    {
        _handler = handler;
    } 
    
    [Function(nameof(DeletePrivatePlaylistItems))]
    public async Task DeletePrivatePlaylistItems(
        [TimerTrigger("%DeletePrivatePlaylistItems:Cron%", RunOnStartup = RunOnStartup.WhenDebug)]
        TimerInfo timerInfo)
    {
        await _handler.HandleAsync();
    }
}
