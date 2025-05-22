using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WebVOD_Backend.Config;
using WebVOD_Backend.Model;
using WebVOD_Backend.Repositories.Interfaces;

namespace WebVOD_Backend.Repositories.Implementations;

public class ResetPasswordTokenRepository : IResetPasswordTokenRepository
{
    private readonly IMongoCollection<ResetPasswordToken> _resetPasswordTokens;

    public ResetPasswordTokenRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DbName);
        _resetPasswordTokens = database.GetCollection<ResetPasswordToken>("ResetPasswordTokens");
    }

    public async Task Add(ResetPasswordToken token)
    {
        await _resetPasswordTokens.InsertOneAsync(token);
    }

    public async Task<ResetPasswordToken> FindByToken(string token)
    {
        var filter = Builders<ResetPasswordToken>.Filter.Eq(t => t.Token, token);
        return await _resetPasswordTokens.Find(filter).FirstOrDefaultAsync();
    }

    public async Task RemoveByUserId(string userId)
    {
        var filter = Builders<ResetPasswordToken>.Filter.Eq(t => t.UserId, userId);
        await _resetPasswordTokens.DeleteManyAsync(filter);
    }
}
