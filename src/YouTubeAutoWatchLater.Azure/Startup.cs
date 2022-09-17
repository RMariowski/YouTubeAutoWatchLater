using MediatR;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using YouTubeAutoWatchLater.Application.Google;
using YouTubeAutoWatchLater.Application.Handlers;
using YouTubeAutoWatchLater.Application.Repositories;
using YouTubeAutoWatchLater.Application.YouTube;
using YouTubeAutoWatchLater.Azure;
using YouTubeAutoWatchLater.Azure.Repositories;

[assembly: FunctionsStartup(typeof(Startup))]

namespace YouTubeAutoWatchLater.Azure;

public sealed  class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        var configuration = builder.GetContext().Configuration;

        builder.Services
            .AddLogging()
            .AddHttpClient()
            .AddGoogle(configuration)
            .AddYouTube(configuration)
            .AddMediatR(typeof(UpdateAutoWatchLater.Handler).Assembly)
            .AddSingleton<IConfigurationRepository, ConfigurationTableStorageRepository>();
    }
}
