using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WebVOD_Backend.Config;
using WebVOD_Backend.Model;
using WebVOD_Backend.Repositories.Interfaces;

namespace WebVOD_Backend.Repositories.Implementations;

public class FailedLoginLogRepository : IFailedLoginLogRepository
{
    private readonly IMongoCollection<FailedLoginLog> _failedLoginLogs;

    public FailedLoginLogRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DbName);
        _failedLoginLogs = database.GetCollection<FailedLoginLog>("FailedLoginLogs");
    }
    public async Task Add(FailedLoginLog log)
    {
        await _failedLoginLogs.InsertOneAsync(log);
    }

    public async Task<int> CountFailedAttempts(string sourceIP, string userId, TimeSpan timeSpan)
    {
        var cutoffTime = DateTime.UtcNow - timeSpan;
        var builder = Builders<FailedLoginLog>.Filter;

        var filter = builder.Eq(l => l.SourceIP, sourceIP) &
                     builder.Eq(l => l.UserId, userId) &
                     builder.Gte(l => l.LogAt, cutoffTime);

        return (int)await _failedLoginLogs.CountDocumentsAsync(filter);
    }
}
