using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.Core.Google;
using YouTubeAutoWatchLater.Core.Handlers;
using YouTubeAutoWatchLater.Core.YouTube.Repositories;
using YouTubeAutoWatchLater.Core.YouTube.Services;
using YouTubeAutoWatchLater.Core.Repositories;

namespace YouTubeAutoWatchLater.Core.YouTube;

internal static class Extensions
{
    internal static IServiceCollection AddYouTube(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<YouTubeOptions>().Bind(configuration.GetSection("YouTube"));

        return services
            .AddScoped(CreateYouTubeService)
            .AddScoped<ISubscriptionRepository, YouTubeSubscriptionRepository>()
            .AddScoped<IChannelRepository, YouTubeChannelRepository>()
            .AddScoped<IPlaylistItemRepository, YouTubePlaylistItemRepository>()
            .AddScoped<IPlaylistRuleResolver, PlaylistRuleResolver>()
            .AddTransient<IUpdateAutoWatchLaterHandler, UpdateAutoWatchLaterHandler>()
            .AddTransient<IDeleteAutoAddedVideosHandler, DeleteAutoAddedVideosHandler>()
            .AddTransient<IDeletePrivatePlaylistItemsHandler, DeletePrivatePlaylistItemsHandler>()
            .AddTransient<IGetRefreshTokenHandler, GetRefreshTokenHandler>();
    }

    // TODO: Would be good to make it somehow asynchronous
    private static YouTubeService CreateYouTubeService(IServiceProvider serviceProvider)
    {
        var googleApi = serviceProvider.GetRequiredService<IGoogleApi>();
        var logger = serviceProvider.GetRequiredService<ILogger<YouTubeService>>();

        logger.LogInformation("Getting access token");
        var accessToken = googleApi.GetAccessToken().GetAwaiter().GetResult();
        logger.LogInformation("Finished getting access token");

        logger.LogInformation("Creating YouTube Service");
        var youTubeService = googleApi.CreateYouTubeService(accessToken);
        logger.LogInformation("Finished creating YouTube API");

        return youTubeService;
    }
}
