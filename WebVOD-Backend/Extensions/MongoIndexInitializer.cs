using MongoDB.Driver;
using WebVOD_Backend.Model;

namespace WebVOD_Backend.Extensions;

public class MongoIndexInitializer
{
    private readonly IMongoCollection<ResetPasswordToken> _resetPasswordTokens;
    private readonly IMongoCollection<BlacklistedToken> _blacklistedTokens;
    private readonly IMongoCollection<UserBlockade> _userBlockades;

    public MongoIndexInitializer(IMongoClient mongoClient, IConfiguration configuration)
    {
        var dbName = configuration.GetSection("MongoDB")["DbName"];
        var database = mongoClient.GetDatabase(dbName);

        _resetPasswordTokens = database.GetCollection<ResetPasswordToken>("ResetPasswordTokens");
        _blacklistedTokens = database.GetCollection<BlacklistedToken>("BlacklistedTokens");
        _userBlockades = database.GetCollection<UserBlockade>("UserBlockades");
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
}
