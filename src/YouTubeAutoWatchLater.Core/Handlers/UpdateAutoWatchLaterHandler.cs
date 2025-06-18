using System.Collections.Concurrent;
using System.Net;
using Google;
using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.Core.YouTube.Services;
using YouTubeAutoWatchLater.Core.Models;
using YouTubeAutoWatchLater.Core.Repositories;

namespace YouTubeAutoWatchLater.Core.Handlers;

public interface IUpdateAutoWatchLaterHandler
{
    Task HandleAsync();
}

internal sealed class UpdateAutoWatchLaterHandler : IUpdateAutoWatchLaterHandler
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IChannelRepository _channelRepository;
    private readonly IPlaylistItemRepository _playlistItemRepository;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IAutoAddedVideosRepository _autoAddedVideosRepository;
    private readonly IPlaylistRuleResolver _playlistRuleResolver;
    private readonly ILogger<UpdateAutoWatchLaterHandler> _logger;

    public UpdateAutoWatchLaterHandler(
        ISubscriptionRepository subscriptionRepository,
        IChannelRepository channelRepository,
        IPlaylistItemRepository playlistItemRepository,
        IConfigurationRepository configurationRepository,
        IAutoAddedVideosRepository autoAddedVideosRepository,
        IPlaylistRuleResolver playlistRuleResolver,
        ILogger<UpdateAutoWatchLaterHandler> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _channelRepository = channelRepository;
        _playlistItemRepository = playlistItemRepository;
        _configurationRepository = configurationRepository;
        _autoAddedVideosRepository = autoAddedVideosRepository;
        _playlistRuleResolver = playlistRuleResolver;
        _logger = logger;
    }

    public async Task HandleAsync()
    {
        var subscriptions = await GetSubscriptionsAsync();

        var dateTime = DateTimeOffset.UtcNow.AddDays(-20);
        var videosToAdd = await GetNewVideosOfSubscriptionsAsync(subscriptions, dateTime);
        await AddNewVideosToSubscriptionsPlaylistsAsync(videosToAdd);

        await SetLastSuccessfulExecutionDateTimeToNowAsync();
    }

    private async Task<Subscriptions> GetSubscriptionsAsync()
    {
        _logger.LogInformation("Getting subscriptions");
        var subscriptions = await _subscriptionRepository.GetMySubscriptionsAsync();
        _logger.LogDebug("Finished getting subscriptions");

        _logger.LogInformation("Setting uploads playlist for subscriptions");

        var channelIds = subscriptions.Select(subscription => subscription.Key).ToArray();
        var uploadsPlaylists = await _channelRepository.GetUploadsPlaylistsAsync(channelIds);

        foreach (var uploadsPlaylist in uploadsPlaylists)
        {
            subscriptions[uploadsPlaylist.Key].UploadsPlaylist = uploadsPlaylist.Value;
        }

        _logger.LogDebug("Finished setting uploads playlist for subscriptions");

        return subscriptions;
    }

    private async Task<ConcurrentDictionary<ChannelId, Video[]>> GetNewVideosOfSubscriptionsAsync(
        Subscriptions subscriptions, DateTimeOffset dateTime)
    {
        _logger.LogInformation("Setting recent videos of subscriptions");

        ConcurrentDictionary<ChannelId, Video[]> newVideos = new();
        ParallelOptions options = new() { MaxDegreeOfParallelism = 10 };
        await Parallel.ForEachAsync(subscriptions.Values, options, async (channel, _) =>
        {
            _logger.LogDebug("Getting videos auto added for last month of {Channel}", channel);
            var videosAutoAdded = await _autoAddedVideosRepository.GetAutoAddedVideosAsync(channel.Id);
            _logger.LogDebug("Finished getting videos auto added for last month of {Channel}", channel);

            _logger.LogDebug("Getting uploads playlist items of {Channel}", channel);
            var videosSinceDateTime = await _playlistItemRepository.GetVideosAsync(channel.UploadsPlaylist!, dateTime);
            _logger.LogDebug("Finished getting uploads playlist items of {Channel}", channel);

            var videosToAdd = videosSinceDateTime
                .Where(video => videosAutoAdded.Contains(video.Id) is false)
                .ToArray();
            newVideos.TryAdd(channel.Id, videosToAdd);
        });

        _logger.LogDebug("Finished setting recent videos of subscriptions");

        return newVideos;
    }

    private async Task AddNewVideosToSubscriptionsPlaylistsAsync(ConcurrentDictionary<ChannelId, Video[]> videosToAdd)
    {
        if (videosToAdd.IsEmpty)
        {
            _logger.LogInformation("No videos to add");
            return;
        }

        _logger.LogInformation("Adding recent videos");

        foreach (var (channelId, videos) in videosToAdd)
        {
            foreach (var video in videos)
            {
                var playlistId = _playlistRuleResolver.Resolve(video);

                try
                {
                    _logger.LogDebug("Adding {Video} to playlist {PlaylistId}", video, playlistId);
                    await _playlistItemRepository.AddToPlaylistAsync(playlistId, video);
                    _logger.LogDebug("Finished adding video {Video} to playlist {PlaylistId}",
                        video, playlistId);

                    _logger.LogDebug("Adding auto added videos of channel {ChannelId}", channelId);
                    await _autoAddedVideosRepository.AddAsync(channelId, video);
                    _logger.LogDebug("Finished adding auto added videos of channel {ChannelId}", channelId);
                }
                catch (GoogleApiException exception)
                    when (exception.HttpStatusCode == HttpStatusCode.Conflict ||
                          exception.Message.Contains("Parameter validation failed") ||
                          exception.HttpStatusCode == HttpStatusCode.InternalServerError)
                {
                    _logger.LogError("Unable to add video {Video} to playlist {PlaylistId}", video, playlistId);
                }
            }
        }

        _logger.LogDebug("Finished adding recent videos");
    }

    private async Task SetLastSuccessfulExecutionDateTimeToNowAsync()
    {
        _logger.LogInformation("Setting last successful execution date time");
        await _configurationRepository.SetLastSuccessfulExecutionDateTimeToNowAsync();
        _logger.LogDebug("Finished setting last successful execution date time");
    }
}
