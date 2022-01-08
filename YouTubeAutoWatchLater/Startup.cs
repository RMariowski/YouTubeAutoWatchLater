﻿using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using YouTubeAutoWatchLater.GoogleApis;
using YouTubeAutoWatchLater.Settings;

[assembly: FunctionsStartup(typeof(YouTubeAutoWatchLater.Startup))]

namespace YouTubeAutoWatchLater;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services
            .AddLogging()
            .AddHttpClient()
            .AddSingleton<ISettings, Settings.Settings>()
            .AddSingleton<IGoogleApis, GoogleApis.GoogleApis>();
    }
}
