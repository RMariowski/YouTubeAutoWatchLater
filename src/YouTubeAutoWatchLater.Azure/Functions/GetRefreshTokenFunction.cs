﻿using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using YouTubeAutoWatchLater.Core.Handlers;

namespace YouTubeAutoWatchLater.Azure.Functions;

public sealed class GetRefreshTokenFunction
{
    private readonly IGetRefreshTokenHandler _handler;

    public GetRefreshTokenFunction(IGetRefreshTokenHandler handler)
    {
        _handler = handler;
    }

    [Function(nameof(GetRefreshToken))]
    public async Task<HttpResponseData> GetRefreshToken(
        [HttpTrigger(AuthorizationLevel.Function, "get")]
        HttpRequestData request)
    {
        var parsed = int.TryParse(request.Query.Get("index"), out var refreshTokenIdx);
        var refreshToken = await _handler.HandleAsync(parsed ? refreshTokenIdx : 0);

        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync(refreshToken);
        return response;
    }
}
