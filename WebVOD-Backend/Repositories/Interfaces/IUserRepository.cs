using WebVOD_Backend.Model;

namespace WebVOD_Backend.Repositories.Interfaces;

public interface IUserRepository
{
    Task<List<User>> GetAll();
    Task Add(User user);
    Task<bool> ExistsByLogin(string login);
    Task<bool> ExistsByEmail(string email);
    Task<User> FindByLogin(string login);
    Task<User> FindById(string id);
}
