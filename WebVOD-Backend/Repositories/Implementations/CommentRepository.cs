using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WebVOD_Backend.Config;
using WebVOD_Backend.Model;
using WebVOD_Backend.Repositories.Interfaces;

namespace WebVOD_Backend.Repositories.Implementations;

public class CommentRepository : ICommentRepository
{
    private readonly IMongoCollection<Comment> _comments;

    public CommentRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DbName);
        _comments = database.GetCollection<Comment>("Comments");
    }

    public async Task<string> Add(Comment comment)
    {
        await _comments.InsertOneAsync(comment);
        return comment.Id;
    }

    public async Task DeleteById(string id)
    {
        var filter = Builders<Comment>.Filter.Eq(c => c.Id, id);
        await _comments.DeleteOneAsync(filter);
    }

    public async Task<Comment> FindById(string id)
    {
        var filter = Builders<Comment>.Filter.Eq(c => c.Id, id);
        var comment = await _comments.Find(filter).FirstOrDefaultAsync();

        return comment;
    }

    public async Task<List<Comment>> FindByVideoId(string videoId, int page, int size)
    {
        var filter = Builders<Comment>.Filter.Eq(c => c.VideoId, videoId);

        var comments = await _comments.Find(filter)
            .SortByDescending(c => c.UploadDate)
            .Skip((page - 1) * size)
            .Limit(size)
            .ToListAsync();

        return comments;
    }
}
