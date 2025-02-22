using System.Net;
using Google;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using YouTubeAutoWatchLater.Core.Google;
using YouTubeAutoWatchLater.Core.Models;
using YouTubePlaylistItem = Google.Apis.YouTube.v3.Data.PlaylistItem;

namespace YouTubeAutoWatchLater.Core.YouTube.Services;

public interface IYouTubeApi
{
    Task<ChannelListResponse> GetChannelsAsync(ChannelId[] channelIds);

    Task<SubscriptionListResponse> GetSubscriptionsAsync(string pageToken);

    Task<PlaylistItemListResponse> GetPlaylistItemsAsync(PlaylistId playlistId, string pageToken, string part);

    Task InsertPlaylistItemAsync(YouTubePlaylistItem playlistItem);

    Task DeletePlaylistItemAsync(PlaylistItemId playlistId);
}

internal sealed class YouTubeApi : IYouTubeApi
{
    private readonly IGoogleApi _googleApi;
    private readonly ILogger<YouTubeApi> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly YouTubeService[] _youTubeServices;
    private int _currentServiceIndex;

    public YouTubeApi(IGoogleApi googleApi, ILogger<YouTubeApi> logger)
    {
        _googleApi = googleApi;
        _logger = logger;
        _youTubeServices = CreateYouTubeService();
        _currentServiceIndex = 0;

        _retryPolicy = Policy
            .Handle<GoogleApiException>(ex => ex.HttpStatusCode == HttpStatusCode.Forbidden && ex.Error.Code == 403)
            .RetryAsync(_youTubeServices.Length - 1, onRetry: (_, _) =>
            {
                _logger.LogWarning("Quota limit reached for current service, switching to next service.");
                SwitchService();
            });
    }

    public async Task<ChannelListResponse> GetChannelsAsync(ChannelId[] channelIds)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var channelsListRequest = GetService().Channels.List("contentDetails");
            channelsListRequest.Id = channelIds.Select(id => id.Value).ToArray();
            channelsListRequest.MaxResults = channelIds.Length;
            var channelListResponse = await channelsListRequest.ExecuteAsync();
            return channelListResponse;
        });
    }

    public async Task<SubscriptionListResponse> GetSubscriptionsAsync(string pageToken)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var subscriptionsListRequest = GetService().Subscriptions.List("snippet");
            subscriptionsListRequest.MaxResults = Consts.MaxResults;
            subscriptionsListRequest.Mine = true;
            subscriptionsListRequest.PageToken = pageToken;
            var subscriptionsListResponse = await subscriptionsListRequest.ExecuteAsync();
            return subscriptionsListResponse;
        });
    }

    public async Task<PlaylistItemListResponse> GetPlaylistItemsAsync(
        PlaylistId playlistId, string pageToken, string part)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var playlistItemsListRequest = GetService().PlaylistItems.List(part);
            playlistItemsListRequest.PlaylistId = playlistId.Value;
            playlistItemsListRequest.MaxResults = Consts.MaxResults;
            playlistItemsListRequest.PageToken = pageToken;
            var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync();
            return playlistItemsListResponse;
        });
    }

    public async Task InsertPlaylistItemAsync(YouTubePlaylistItem playlistItem)
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            _ = await GetService().PlaylistItems.Insert(playlistItem, "snippet").ExecuteAsync();
        });
    }

    public async Task DeletePlaylistItemAsync(PlaylistItemId playlistId)
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            var playlistItemsDeleteRequest = GetService().PlaylistItems.Delete(playlistId.Value);
            _ = await playlistItemsDeleteRequest.ExecuteAsync();
        });
    }

    private YouTubeService GetService()
        => _youTubeServices[_currentServiceIndex];

    private void SwitchService()
        => _currentServiceIndex = (_currentServiceIndex + 1) % _youTubeServices.Length;

    private YouTubeService[] CreateYouTubeService()
    {
        _logger.LogInformation("Creating YouTube Services");
        var youTubeServices = _googleApi.CreateYouTubeServices();
        _logger.LogInformation("Finished creating YouTube Services");

        return youTubeServices;
    }
}
