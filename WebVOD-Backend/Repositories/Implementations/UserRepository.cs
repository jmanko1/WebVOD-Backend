using DnsClient;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WebVOD_Backend.Config;
using WebVOD_Backend.Model;
using WebVOD_Backend.Repositories.Interfaces;

namespace WebVOD_Backend.Repositories.Implementations;

public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;

    public UserRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DbName);
        _users = database.GetCollection<User>("Users");
    }

    public async Task Add(User user)
    {
        await _users.InsertOneAsync(user);
    }

    public async Task<bool> ExistsByEmail(string email)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Email, email);
        return await _users.Find(filter).AnyAsync();
    }

    public async Task<bool> ExistsByLogin(string login)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Login, login);
        return await _users.Find(filter).AnyAsync();
    }

    public async Task<User> FindByLogin(string login)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Login, login);
        return await _users.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<List<User>> GetAll()
    {
        return await _users.Find(_ => true).ToListAsync();
    }
}
