using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using YouTubeAutoWatchLater.Extensions;
using YouTubeAutoWatchLater.Repositories.Configuration;
using YouTubeAutoWatchLater.YouTube;
using YouTubeAutoWatchLater.YouTube.Models;

namespace YouTubeAutoWatchLater;

public class YouTubeAutoWatchLater
{
    private readonly IYouTubeService _youTubeService;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly ILogger<YouTubeAutoWatchLater> _logger;

    public YouTubeAutoWatchLater(IYouTubeService youTubeService, IConfigurationRepository configurationRepository,
        ILogger<YouTubeAutoWatchLater> logger)
    {
        _youTubeService = youTubeService;
        _configurationRepository = configurationRepository;
        _logger = logger;
    }

    [FunctionName(nameof(Run))]
    public async Task Run(
        [TimerTrigger("%Cron%"
#if DEBUG
            , RunOnStartup = true
#endif
        )]
        TimerInfo timerInfo,
        [DurableClient] IDurableOrchestrationClient client)
    {
        await client.StartNewAsync(nameof(Orchestrator));
    }

    [FunctionName(nameof(Orchestrator))]
    public async Task Orchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var lastSuccessfulExecutionDateTime = await context.CallActivityAsync<DateTime>(
            nameof(GetLastSuccessfulExecutionActivity), null);

        await context.CallSubOrchestratorAsync(nameof(SubscriptionsOrchestrator), lastSuccessfulExecutionDateTime);

        await context.CallActivityAsync(nameof(SetLastSuccessfulExecutionActivity), null);
    }

    [FunctionName(nameof(GetLastSuccessfulExecutionActivity))]
    public async Task<DateTime> GetLastSuccessfulExecutionActivity([ActivityTrigger] IDurableActivityContext context)
    {
        _logger.LogInformation("Getting last successful execution date time...");
        var lastSuccessfulExecutionDateTime = await _configurationRepository.GetLastSuccessfulExecutionDateTime();
        _logger.LogInformation(
            $"Finished getting last successful execution date time: {lastSuccessfulExecutionDateTime:o} UTC");

        return lastSuccessfulExecutionDateTime;
    }

    [FunctionName(nameof(SubscriptionsOrchestrator))]
    public async Task SubscriptionsOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var lastSuccessfulExecutionDateTime = context.GetInput<DateTime>();

        var subscriptions = await context.CallActivityAsync<Subscriptions>(nameof(GetSubscriptionsActivity), null);

        await context.CallInBatches(
            subscription => context.CallSubOrchestratorAsync(nameof(SubscriptionOrchestrator),
                (subscription, lastSuccessfulExecutionDateTime)),
            subscriptions.Values.ToArray());
    }

    [FunctionName(nameof(GetSubscriptionsActivity))]
    public async Task<Subscriptions> GetSubscriptionsActivity([ActivityTrigger] IDurableActivityContext context)
    {
        _logger.LogInformation("Getting subscriptions...");
        var subscriptions = await _youTubeService.GetMySubscriptions();
        _logger.LogInformation($"Finished getting {subscriptions.Count} subscriptions");

        _logger.LogInformation("Setting uploads playlist for subscriptions");
        await _youTubeService.SetUploadsPlaylistForSubscriptions(subscriptions);
        _logger.LogInformation("Finished setting uploads playlist of subscriptions");

        return subscriptions;
    }

    [FunctionName(nameof(SubscriptionOrchestrator))]
    public async Task SubscriptionOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var (subscription, lastSuccessfulExecutionDateTime) = context.GetInput<(YouTubeChannel, DateTime)>();

        _logger.LogInformation($"Getting recent videos of {subscription}...");
        var recentVideos = await _youTubeService.GetRecentVideosOfChannel(
            subscription, lastSuccessfulExecutionDateTime);
        _logger.LogInformation($"Finished getting recent videos of {subscription}");

        await _youTubeService.AddVideosToPlaylist(subscription, recentVideos);
    }

    [FunctionName(nameof(SetLastSuccessfulExecutionActivity))]
    public async Task SetLastSuccessfulExecutionActivity([ActivityTrigger] IDurableActivityContext context)
    {
        _logger.LogInformation("Setting last successful execution date time...");
        await _configurationRepository.SetLastSuccessfulExecutionDateTimeToNow();
        _logger.LogInformation("Finished setting last successful execution date time");
    }

    [Singleton]
    [FunctionName(nameof(GetRefreshToken))]
    public async Task<IActionResult> GetRefreshToken([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest request)
    {
        string refreshToken = await _youTubeService.GetRefreshToken();
        return new OkObjectResult(refreshToken);
    }
}
