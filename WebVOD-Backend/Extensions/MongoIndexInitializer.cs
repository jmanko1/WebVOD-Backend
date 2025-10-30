using MongoDB.Driver;
using WebVOD_Backend.Model;

namespace WebVOD_Backend.Extensions;

public class MongoIndexInitializer
{
    private readonly IMongoCollection<ResetPasswordToken> _resetPasswordTokens;
    private readonly IMongoCollection<BlacklistedToken> _blacklistedTokens;
    private readonly IMongoCollection<UserBlockade> _userBlockades;
    private readonly IMongoCollection<TagsProposition> _tagsPropositions;

    public MongoIndexInitializer(IMongoClient mongoClient, IConfiguration configuration)
    {
        var dbName = configuration.GetSection("MongoDB")["DbName"];
        var database = mongoClient.GetDatabase(dbName);

        _resetPasswordTokens = database.GetCollection<ResetPasswordToken>("ResetPasswordTokens");
        _blacklistedTokens = database.GetCollection<BlacklistedToken>("BlacklistedTokens");
        _userBlockades = database.GetCollection<UserBlockade>("UserBlockades");
        _tagsPropositions = database.GetCollection<TagsProposition>("TagsPropositions");
    }

    public async Task AddResetPasswordTokensIndexes()
    {
        var indexKeys = Builders<ResetPasswordToken>.IndexKeys.Ascending(t => t.ValidUntil);

        var indexOptions = new CreateIndexOptions
        {
            ExpireAfter = TimeSpan.Zero
        };

        var indexModel = new CreateIndexModel<ResetPasswordToken>(indexKeys, indexOptions);

        await _resetPasswordTokens.Indexes.CreateOneAsync(indexModel);
    }

    public async Task AddBlacklistedTokensIndexes()
    {
        var indexKeys = Builders<BlacklistedToken>.IndexKeys.Ascending(t => t.ExpiresAt);

        var indexOptions = new CreateIndexOptions
        {
            ExpireAfter = TimeSpan.Zero
        };

        var indexModel = new CreateIndexModel<BlacklistedToken>(indexKeys, indexOptions);

        await _blacklistedTokens.Indexes.CreateOneAsync(indexModel);
    }

    public async Task AddUserBlockadesIndexes()
    {
        var indexKeys = Builders<UserBlockade>.IndexKeys.Ascending(b => b.Until);

        var indexOptions = new CreateIndexOptions
        {
            ExpireAfter = TimeSpan.Zero
        };

        var indexModel = new CreateIndexModel<UserBlockade>(indexKeys, indexOptions);

        await _userBlockades.Indexes.CreateOneAsync(indexModel);
    }

    public async Task AddTagsPropositionsIndexes()
    {
        var indexKeys = Builders<TagsProposition>.IndexKeys.Ascending(p => p.ValidUntil);

        var indexOptions = new CreateIndexOptions
        {
            ExpireAfter = TimeSpan.Zero
        };

        var indexModel = new CreateIndexModel<TagsProposition>(indexKeys, indexOptions);

        await _tagsPropositions.Indexes.CreateOneAsync(indexModel);
    }
}
