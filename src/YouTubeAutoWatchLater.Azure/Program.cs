using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YouTubeAutoWatchLater.Application;
using YouTubeAutoWatchLater.Application.Repositories;
using YouTubeAutoWatchLater.Azure.Repositories;

namespace YouTubeAutoWatchLater.Azure;

public class Program
{
    private static async Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults()
            .ConfigureServices((hostContext, services) =>
            {
                var configuration = hostContext.Configuration;

                services
                    .AddApplication(configuration)
                    .AddScoped<IConfigurationRepository, ConfigurationTableStorageRepository>()
                    .AddScoped<IAutoAddedVideosRepository, AutoAddedVideosTableStorageRepository>();
            })
            .Build();
        await host.RunAsync();
    }
}
