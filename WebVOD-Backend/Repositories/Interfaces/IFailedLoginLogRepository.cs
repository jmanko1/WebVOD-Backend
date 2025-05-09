using WebVOD_Backend.Model;

namespace WebVOD_Backend.Repositories.Interfaces;

public interface IFailedLoginLogRepository
{
    Task Add(FailedLoginLog log);
    Task<int> CountFailedAttempts(string sourceIP, string userId, TimeSpan timeSpan);
}
