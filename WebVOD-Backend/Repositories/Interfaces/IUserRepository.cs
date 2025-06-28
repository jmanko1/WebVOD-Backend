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
    Task<User> FindByEmail(string email);
    Task ChangePassword(string userId, string newPassword);
    Task UpdateDescription(string userId, string description);
    Task UpdateImageUrl(string userId, string imageUrl);
}
