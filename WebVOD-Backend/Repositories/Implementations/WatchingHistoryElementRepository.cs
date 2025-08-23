using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WebVOD_Backend.Config;
using WebVOD_Backend.Model;
using WebVOD_Backend.Repositories.Interfaces;

namespace WebVOD_Backend.Repositories.Implementations;

public class WatchingHistoryElementRepository : IWatchingHistoryElementRepository
{
    private readonly IMongoCollection<WatchingHistoryElement> _watchingHistoryElements;

    public WatchingHistoryElementRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DbName);
        _watchingHistoryElements = database.GetCollection<WatchingHistoryElement>("WatchingHistoryElements");
    }

    public async Task Add(WatchingHistoryElement watchingHistoryElement)
    {
        await _watchingHistoryElements.InsertOneAsync(watchingHistoryElement);
    }

    public async Task DeleteByVideoId(string videoId)
    {
        var filter = Builders<WatchingHistoryElement>.Filter.Eq(el => el.VideoId, videoId);
        await _watchingHistoryElements.DeleteManyAsync(filter);
    }

    public async Task DeleteByViewerId(string viewerId)
    {
        var filter = Builders<WatchingHistoryElement>.Filter.Eq(el => el.ViewerId, viewerId);
        await _watchingHistoryElements.DeleteManyAsync(filter);
    }

    public async Task<bool> ExistsByVideoIdAndViewerId(string videoId, string viewerId)
    {
        var builder = Builders<WatchingHistoryElement>.Filter;
        var filter = builder.Eq(el => el.VideoId, videoId) &
                     builder.Eq(el => el.ViewerId, viewerId);

        return await _watchingHistoryElements.Find(filter).AnyAsync();
    }

    public async Task<List<WatchingHistoryElement>> FindByViewerId(string viewerId, int page, int size)
    {
        var filter = Builders<WatchingHistoryElement>.Filter.Eq(el => el.ViewerId, viewerId);
        var elements = await _watchingHistoryElements.Find(filter)
            .SortByDescending(el => el.ViewedAt)
            .Skip((page - 1) * size)
            .Limit(size)
            .ToListAsync();

        return elements;
    }

    public async Task UpdateViewedAt(string videoId, string viewerId)
    {
        var builder = Builders<WatchingHistoryElement>.Filter;
        var filter = builder.Eq(el => el.VideoId, videoId) &
                     builder.Eq(el => el.ViewerId, viewerId);

        var update = Builders<WatchingHistoryElement>.Update
            .Set(el => el.ViewedAt, DateTime.UtcNow);

        await _watchingHistoryElements.UpdateOneAsync(filter, update);
    }
}
