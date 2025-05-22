using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WebVOD_Backend.Config;
using WebVOD_Backend.Model;
using WebVOD_Backend.Repositories.Interfaces;

namespace WebVOD_Backend.Repositories.Implementations;

public class UserBlockadeRepository : IUserBlockadeRepository
{
    private readonly IMongoCollection<UserBlockade> _userBlockades;

    public UserBlockadeRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DbName);
        _userBlockades = database.GetCollection<UserBlockade>("UserBlockades");
    }

    public async Task Add(UserBlockade userBlockade)
    {
        await _userBlockades.InsertOneAsync(userBlockade);
    }

    public async Task<bool> ExistsByUserId(string userId)
    {
        var builder = Builders<UserBlockade>.Filter;
        var filter = builder.Eq(b => b.UserId, userId) &
                     builder.Gt(b => b.Until, DateTime.Now);

        return await _userBlockades.Find(filter).AnyAsync();
    }
}
