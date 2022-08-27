using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using YouTubeAutoWatchLater.Application.Repositories;
using YouTubeAutoWatchLater.Application.Settings;
using YouTubeAutoWatchLater.Application.YouTube.Extensions;
using YouTubeAutoWatchLater.Azure;
using YouTubeAutoWatchLater.Azure.Repositories;

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
            .AddYouTube()
            .AddSingleton<IConfigurationRepository, ConfigurationTableStorageRepository>();
    }
}
