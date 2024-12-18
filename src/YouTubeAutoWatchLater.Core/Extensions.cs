﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YouTubeAutoWatchLater.Core.Google;
using YouTubeAutoWatchLater.Core.YouTube;

namespace YouTubeAutoWatchLater.Core;

public static class Extensions
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddLogging()
            .AddHttpClient()
            .AddGoogle(configuration)
            .AddYouTube(configuration);
    }
}
