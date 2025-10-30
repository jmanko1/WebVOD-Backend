using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WebVOD_Backend.Config;
using WebVOD_Backend.Model;
using WebVOD_Backend.Repositories.Interfaces;

namespace WebVOD_Backend.Repositories.Implementations;

public class TagsPropositionRepository : ITagsPropositionRepository
{
    private readonly IMongoCollection<TagsProposition> _tagsPropositions;

    public TagsPropositionRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DbName);
        _tagsPropositions = database.GetCollection<TagsProposition>("TagsPropositions");
    }

    public async Task Add(TagsProposition tagsProposition)
    {
        await _tagsPropositions.InsertOneAsync(tagsProposition);
    }

    public async Task DeleteById(string id)
    {
        var filter = Builders<TagsProposition>.Filter.Eq(p => p.Id, id);

        await _tagsPropositions.DeleteOneAsync(filter);
    }

    public async Task DeleteByVideoId(string videoId)
    {
        var filter = Builders<TagsProposition>.Filter.Eq(p => p.VideoId, videoId);

        await _tagsPropositions.DeleteManyAsync(filter);
    }

    public async Task<bool> ExistsById(string id)
    {
        var filter = Builders<TagsProposition>.Filter.Eq(p => p.Id, id);

        return await _tagsPropositions.Find(filter).AnyAsync();
    }

    public async Task<bool> ExistsByVideoIdAndUserId(string videoId, string userId)
    {
        var builder = Builders<TagsProposition>.Filter;
        var filter = builder.Eq(t => t.VideoId, videoId) &
                     builder.Eq(l => l.UserId, userId);

        return await _tagsPropositions.Find(filter).AnyAsync();
    }

    public async Task<TagsProposition> FindById(string id)
    {
        var filter = Builders<TagsProposition>.Filter.Eq(p => p.Id, id);

        return await _tagsPropositions.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<List<TagsProposition>> FindByVideoId(string videoId, int page, int size)
    {
        var filter = Builders<TagsProposition>.Filter.Eq(p => p.VideoId, videoId);

        var propositions = await _tagsPropositions.Find(filter)
            .SortByDescending(p => p.CreatedAt)
            .Skip((page - 1) * size)
            .Limit(size)
            .ToListAsync();

        return propositions;
    }
}
