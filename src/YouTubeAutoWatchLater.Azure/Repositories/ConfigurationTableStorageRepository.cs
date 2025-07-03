using System.Net;
using Azure;
using Azure.Data.Tables;
using YouTubeAutoWatchLater.Core.Repositories;

namespace YouTubeAutoWatchLater.Azure.Repositories;

internal sealed class ConfigurationTableStorageRepository : IConfigurationRepository
{
    private const string TableName = "Configurations";
    private const string ValuePropKey = "Value";
    private const string GeneralPartitionKey = "General";
    private const string LastSuccessfulRun = nameof(LastSuccessfulRun);
    private const string TakeSubscriptionVideosFromLastXDays = nameof(TakeSubscriptionVideosFromLastXDays);

    private readonly TableClient _tableClient;

    public ConfigurationTableStorageRepository()
    {
        var storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
        _tableClient = new TableClient(storageConnectionString, TableName);
        _tableClient.CreateIfNotExists();
    }

    public async Task<DateTime> GetLastSuccessfulExecutionDateTimeAsync()
    {
        var entity = await GetEntity(LastSuccessfulRun);
        var lastSuccessfulExecutionAsString = entity.GetString(ValuePropKey);
        return string.IsNullOrWhiteSpace(lastSuccessfulExecutionAsString)
            ? DateTime.UtcNow
            : DateTimeOffset.Parse(lastSuccessfulExecutionAsString).UtcDateTime;
    }

    public async Task SetLastSuccessfulExecutionDateTimeToNowAsync()
    {
        TableEntity entity = new(GeneralPartitionKey, LastSuccessfulRun)
        {
            [ValuePropKey] = DateTimeOffset.UtcNow.ToString("o")
        };
        await _tableClient.UpsertEntityAsync(entity);
    }

    public async Task<DateTimeOffset> GetSubscriptionVideosFrom()
    {
        var entity = await GetEntity(TakeSubscriptionVideosFromLastXDays);
        var days = int.Parse(entity.GetString(ValuePropKey));
        return DateTimeOffset.UtcNow.AddDays(-days);
    }

    private async Task<TableEntity> GetEntity(string rowKey)
    {
        Response<TableEntity>? entityResponse = null;

        try
        {
            entityResponse = await _tableClient.GetEntityAsync<TableEntity>(GeneralPartitionKey, rowKey);
        }
        catch (RequestFailedException e)
        {
            if (e.Status != (int)HttpStatusCode.NotFound)
                throw;
        }

        return entityResponse!.Value;
    }
}
