using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WebVOD_Backend.Config;
using WebVOD_Backend.Model;
using WebVOD_Backend.Repositories.Interfaces;

namespace WebVOD_Backend.Repositories.Implementations;

public class UserDeviceRepository : IUserDeviceRepository
{
    private readonly IMongoCollection<UserDevice> _userDevices;

    public UserDeviceRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DbName);
        _userDevices = database.GetCollection<UserDevice>("UserDevices");
    }

    public async Task Add(UserDevice device)
    {
        await _userDevices.InsertOneAsync(device);
    }

    public async Task<UserDevice> FindByNameAndUserId(string name, string userId)
    {
        var builder = Builders<UserDevice>.Filter;
        var filter = builder.Eq(d => d.Name, name) &
                     builder.Eq(d => d.UserId, userId);

        return await _userDevices.Find(filter).FirstOrDefaultAsync();
    }

    public async Task UpdateDates(string id, DateTime registeredAt, DateTime validUntil)
    {
        var filter = Builders<UserDevice>.Filter.Eq(d => d.Id, id);
        var update = Builders<UserDevice>.Update
            .Set(d => d.RegisteredAt, registeredAt)
            .Set(d => d.ValidUntil, validUntil);

        await _userDevices.UpdateOneAsync(filter, update);
    }

    public async Task UpdateLastLoginAt(string id, DateTime lastLoginAt)
    {
        var filter = Builders<UserDevice>.Filter.Eq(d => d.Id, id);
        var update = Builders<UserDevice>.Update
            .Set(d => d.LastLoginAt, lastLoginAt);

        await _userDevices.UpdateOneAsync(filter, update);
    }
}
