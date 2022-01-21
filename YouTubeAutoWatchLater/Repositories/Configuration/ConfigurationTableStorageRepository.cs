﻿using System.Net;
using Azure;
using Azure.Data.Tables;

namespace YouTubeAutoWatchLater.Repositories.Configuration;

public class ConfigurationTableStorageRepository : IConfigurationRepository
{
    private const string TableName = "Configurations";
    private const string LastSuccessfulExecutionPropKey = "LastSuccessfulExecution";
    private readonly (string PartitionKey, string RowKey) _lastSuccessfulRun = ("General", "LastSuccessfulRun");

    private readonly TableClient _tableClient;

    public ConfigurationTableStorageRepository()
    {
        string storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
        _tableClient = new TableClient(storageConnectionString, TableName);
        _tableClient.CreateIfNotExists();
    }

    public async Task<DateTime> GetLastSuccessfulExecutionDateTime()
    {
        Response<TableEntity>? entityResponse = null;

        try
        {
            entityResponse = await _tableClient.GetEntityAsync<TableEntity>(
                _lastSuccessfulRun.PartitionKey, _lastSuccessfulRun.RowKey);
        }
        catch (RequestFailedException e)
        {
            if (e.Status != (int)HttpStatusCode.NotFound)
                throw;
        }

        var lastSuccessfulExecution = entityResponse?.Value
            .GetDateTime(LastSuccessfulExecutionPropKey) ?? DateTime.UtcNow;
        return lastSuccessfulExecution;
    }

    public async Task SetLastSuccessfulExecutionDateTimeToNow()
    {
        var entity = new TableEntity(_lastSuccessfulRun.PartitionKey, _lastSuccessfulRun.RowKey)
        {
            [LastSuccessfulExecutionPropKey] = DateTime.UtcNow
        };
        await _tableClient.UpsertEntityAsync(entity);
    }
}