using Azure.Data.Tables;
using YouTubeAutoWatchLater.Core;
using YouTubeAutoWatchLater.Core.Models;
using YouTubeAutoWatchLater.Core.Repositories;

namespace YouTubeAutoWatchLater.Azure.Repositories;

public sealed class AutoAddedVideosTableStorageRepository : IAutoAddedVideosRepository
{
    private const string TableName = "AutoAddedVideos";

    private readonly TableClient _tableClient;

    public AutoAddedVideosTableStorageRepository()
    {
        var storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
        _tableClient = new TableClient(storageConnectionString, TableName);
        _tableClient.CreateIfNotExists();
    }

    public async Task Add(ChannelId channelId, Video[] videos)
    {
        foreach (var video in videos)
        {
            TableEntity entity = new(channelId.Value, video.Id.Value);
            await _tableClient.AddEntityAsync(entity);
        }
    }

    public async Task<IReadOnlyList<VideoId>> GetAutoAddedVideos(ChannelId channelId)
    {
        var query = _tableClient.QueryAsync<TableEntity>(
            $"PartitionKey eq '{channelId.Value}'", Consts.MaxResults);

        List<VideoId> videos = new();
        await foreach (var page in query.AsPages())
            videos.AddRange(page.Values.Select(entity => new VideoId(entity.RowKey)));

        return videos;
    }

    public async Task DeleteOlderThan(DateTimeOffset dateTime)
    {
        var query = _tableClient.QueryAsync<TableEntity>(
            $"Timestamp le datetime'{dateTime:yyyy-MM-ddTHH:mm}'", Consts.MaxResults);

        await foreach (var page in query.AsPages())
        {
            foreach (var entity in page.Values)
                await _tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
        }
    }
}
