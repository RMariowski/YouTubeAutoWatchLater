using System.Net;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using YouTubeAutoWatchLater.Application.Handlers;

namespace YouTubeAutoWatchLater.Azure;

public sealed class YouTubeAutoWatchLater
{
    private readonly IMediator _mediator;

    public YouTubeAutoWatchLater(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Function(nameof(Run))]
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

    [Function(nameof(DeleteAutoAddedVideos))]
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

    [Function(nameof(DeletePrivatePlaylistItems))]
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

    [Function(nameof(GetRefreshToken))]
    public async Task<HttpResponseData> GetRefreshToken(
        [HttpTrigger(AuthorizationLevel.Function, "get")]
        HttpRequestData request)
    {
        GetRefreshToken.Query query = new();
        var refreshToken = await _mediator.Send(query);

        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync(refreshToken);
        return response;
    }
}
