using System.Net;
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
        var refreshToken = await _handler.HandleAsync();

        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync(refreshToken);
        return response;
    }
}
