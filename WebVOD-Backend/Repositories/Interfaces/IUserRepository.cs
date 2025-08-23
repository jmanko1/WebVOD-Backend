using WebVOD_Backend.Model;

namespace WebVOD_Backend.Repositories.Interfaces;

public interface IUserRepository
{
    Task<List<User>> GetAll();
    Task Add(User user);
    Task<bool> ExistsByLogin(string login);
    Task<bool> ExistsByEmail(string email);
    Task<bool> ExistsById(string id);
    Task<User> FindByLogin(string login);
    Task<User> FindById(string id);
    Task<List<User>> FindById(List<string> ids);
    Task<User> FindByEmail(string email);
    Task ChangePassword(string userId, string newPassword);
    Task UpdateDescription(string userId, string description);
    Task UpdateImageUrl(string userId, string imageUrl);
    Task SetTFA(string userId, bool isTFAEnabled);
    Task IncrementVideosCount(string userId);
    Task DecrementVideosCount(string userId);
    Task<List<User>> GetUsersByLoginRegex(string loginRegex, int page, int size);
}
