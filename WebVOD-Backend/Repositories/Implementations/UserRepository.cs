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

    public async Task<User> FindById(string id)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, id);
        return await _users.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<User> FindByLogin(string login)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Login, login);
        return await _users.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<User> FindByEmail(string email)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Email, email);
        return await _users.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<List<User>> GetAll()
    {
        return await _users.Find(_ => true).ToListAsync();
    }

    public async Task<bool> ExistsById(string id)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, id);
        return await _users.Find(filter).AnyAsync();
    }

    public async Task ChangePassword(string userId, string newPassword)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update
            .Set(u => u.Password, newPassword);

        await _users.UpdateOneAsync(filter, update);
    }

    public async Task UpdateDescription(string userId, string description)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update
            .Set(u => u.Description, description);

        await _users.UpdateOneAsync(filter, update);
    }

    public async Task UpdateImageUrl(string userId, string imageUrl)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update
            .Set(u => u.ImageUrl, imageUrl);

        await _users.UpdateOneAsync(filter, update);
    }

    public async Task SetTFA(string userId, bool isTFAEnabled)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update
            .Set(u => u.IsTFAEnabled, isTFAEnabled);

        await _users.UpdateOneAsync(filter, update);
    }

    public async Task<List<User>> FindById(List<string> ids)
    {
        var filter = Builders<User>.Filter.In(u => u.Id, ids);
        return await _users.Find(filter).ToListAsync();
    }

    public async Task IncrementVideosCount(string userId)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Inc(u => u.VideosCount, 1);

        var result = await _users.UpdateOneAsync(filter, update);
    }

    public async Task DecrementVideosCount(string userId)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Inc(u => u.VideosCount, -1);

        var result = await _users.UpdateOneAsync(filter, update);
    }
}
