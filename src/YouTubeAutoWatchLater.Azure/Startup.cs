using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using YouTubeAutoWatchLater.Azure;
using YouTubeAutoWatchLater.Azure.Repositories;
using YouTubeAutoWatchLater.Core.GoogleApis;
using YouTubeAutoWatchLater.Core.Repositories;
using YouTubeAutoWatchLater.Core.Settings;
using YouTubeAutoWatchLater.Core.YouTube;
using YouTubeService = YouTubeAutoWatchLater.Core.YouTube.YouTubeService;

[assembly: FunctionsStartup(typeof(Startup))]

namespace YouTubeAutoWatchLater.Azure;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services
            .AddLogging()
            .AddHttpClient()
            .AddSingleton<ISettings, Settings.Settings>()
            .AddSingleton<IGoogleApis, GoogleApis>()
            .AddSingleton<IYouTubeService, YouTubeService>()
            .AddSingleton<IConfigurationRepository, ConfigurationTableStorageRepository>();
    }
}
