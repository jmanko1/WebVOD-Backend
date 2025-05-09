using WebVOD_Backend.Model;

namespace WebVOD_Backend.Repositories.Interfaces;

public interface IUserRepository
{
    Task<List<User>> GetAll();
    Task Add(User user);
}
