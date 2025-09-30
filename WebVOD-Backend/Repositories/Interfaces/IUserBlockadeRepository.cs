using WebVOD_Backend.Model;

namespace WebVOD_Backend.Repositories.Interfaces;

public interface IUserBlockadeRepository
{
    Task Add(UserBlockade userBlockade);
    Task<bool> ExistsByUserId(string userId);
    Task<bool> ExistsByUserIdAndSourceIP(string userId, string sourceIP);

}
