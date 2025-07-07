using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WebVOD_Backend.Config;
using WebVOD_Backend.Model;
using WebVOD_Backend.Repositories.Interfaces;

namespace WebVOD_Backend.Repositories.Implementations;

public class LikeRepository : ILikeRepository
{
    private readonly IMongoCollection<Like> _likes;

    public LikeRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DbName);
        _likes = database.GetCollection<Like>("Likes");
    }

    public async Task Add(Like like)
    {
        await _likes.InsertOneAsync(like);
    }

    public async Task<bool> ExistsByVideoIdAndUserId(string videoId, string userId)
    {
        var builder = Builders<Like>.Filter;
        var filter = builder.Eq(l => l.VideoId, videoId) &
                     builder.Eq(l => l.UserId, userId);

        return await _likes.Find(filter).AnyAsync();
    }

    public async Task DeleteByVideoIdAndUserId(string videoId, string userId)
    {
        var builder = Builders<Like>.Filter;
        var filter = builder.Eq(l => l.VideoId, videoId) &
                     builder.Eq(l => l.UserId, userId);

        await _likes.DeleteOneAsync(filter);
    }
}
