﻿using System.Xml.Linq;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WebVOD_Backend.Config;
using WebVOD_Backend.Model;
using WebVOD_Backend.Repositories.Interfaces;

namespace WebVOD_Backend.Repositories.Implementations;

public class VideoRepository : IVideoRepository
{
    private readonly IMongoCollection<Video> _videos;

    public VideoRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DbName);
        _videos = database.GetCollection<Video>("Videos");
    }

    public async Task DecrementCommentsCount(string id)
    {
        var filter = Builders<Video>.Filter.Eq(v => v.Id, id);
        var update = Builders<Video>.Update.Inc(v => v.CommentsCount, -1);

        var result = await _videos.UpdateOneAsync(filter, update);
    }

    public async Task DecrementLikesCount(string id)
    {
        var filter = Builders<Video>.Filter.Eq(v => v.Id, id);
        var update = Builders<Video>.Update.Inc(v => v.LikesCount, -1);

        var result = await _videos.UpdateOneAsync(filter, update);
    }

    public async Task<Video> FindById(string id)
    {
        var filter = Builders<Video>.Filter.Eq(v => v.Id, id);
        return await _videos.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<List<Video>> FindByUserId(string userId, int page, int size)
    {
        var builder = Builders<Video>.Filter;
        var filter = builder.Eq(v => v.AuthorId, userId) &
                     builder.Eq(v => v.Status, VideoStatus.PUBLISHED);

        var videos = await _videos.Find(filter)
            .SortByDescending(v => v.UploadDate)
            .Skip((page - 1) * size)
            .Limit(size)
            .ToListAsync();

        return videos;
    }

    public async Task IncrementCommentsCount(string id)
    {
        var filter = Builders<Video>.Filter.Eq(v => v.Id, id);
        var update = Builders<Video>.Update.Inc(v => v.CommentsCount, 1);

        var result = await _videos.UpdateOneAsync(filter, update);
    }

    public async Task IncrementLikesCount(string id)
    {
        var filter = Builders<Video>.Filter.Eq(v => v.Id, id);
        var update = Builders<Video>.Update.Inc(v => v.LikesCount, 1);

        var result = await _videos.UpdateOneAsync(filter, update);
    }

    public async Task IncrementViewsCount(string id)
    {
        var filter = Builders<Video>.Filter.Eq(v => v.Id, id);
        var update = Builders<Video>.Update.Inc(v => v.ViewsCount, 1);

        var result = await _videos.UpdateOneAsync(filter, update);
    }

}
