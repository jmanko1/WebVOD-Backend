using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WebVOD_Backend.Config;
using WebVOD_Backend.Model;
using WebVOD_Backend.Repositories.Interfaces;

namespace WebVOD_Backend.Repositories.Implementations;

public class BlacklistedTokenRepository : IBlacklistedTokenRepository
{
    private readonly IMongoCollection<BlacklistedToken> _revokedJwts;

    public BlacklistedTokenRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DbName);
        _revokedJwts = database.GetCollection<BlacklistedToken>("BlacklistedTokens");
    }

    public async Task Add(BlacklistedToken jwt)
    {
        await _revokedJwts.InsertOneAsync(jwt);
    }

    public async Task<bool> ExistsByJti(string jti)
    {
        var filter = Builders<BlacklistedToken>.Filter.Eq(t => t.Jti, jti);

        return await _revokedJwts.Find(filter).AnyAsync();
    }
}
