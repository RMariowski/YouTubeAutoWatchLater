using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using YouTubeAutoWatchLater.Application.Handlers;

namespace YouTubeAutoWatchLater.Azure;

public sealed class YouTubeAutoWatchLater
{
    private readonly IMediator _mediator;

    public YouTubeAutoWatchLater(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Singleton]
    [FunctionName(nameof(Run))]
    public async Task Run(
        [TimerTrigger("%Cron%"
#if DEBUG
            , RunOnStartup = true
#endif
        )]
        TimerInfo timerInfo)
    {
        UpdateAutoWatchLater.Command command = new();
        await _mediator.Send(command);
    }

    [Singleton]
    [FunctionName(nameof(DeleteAutoAddedVideos))]
    public async Task DeleteAutoAddedVideos(
        [TimerTrigger("%DeleteAutoAddedVideos:Cron%"
#if DEBUG
            , RunOnStartup = true
#endif
        )]
        TimerInfo timerInfo)
    {
        DeleteAutoAddedVideos.Command command = new();
        await _mediator.Send(command);
    }
    
    [Singleton]
    [FunctionName(nameof(DeletePrivatePlaylistItems))]
    public async Task DeletePrivatePlaylistItems(
        [TimerTrigger("%DeletePrivatePlaylistItems:Cron%"
#if DEBUG
            , RunOnStartup = true
#endif
        )]
        TimerInfo timerInfo)
    {
        DeletePrivatePlaylistItems.Command command = new();
        await _mediator.Send(command);
    }

    [Singleton]
    [FunctionName(nameof(GetRefreshToken))]
    public async Task<IActionResult> GetRefreshToken(
        [HttpTrigger(AuthorizationLevel.Function, "get")]
        HttpRequest request)
    {
        GetRefreshToken.Query query = new();
        var refreshToken = await _mediator.Send(query);
        return new OkObjectResult(refreshToken);
    }
}
