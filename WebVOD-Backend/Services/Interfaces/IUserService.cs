using WebVOD_Backend.Model;

namespace WebVOD_Backend.Services.Interfaces;

public interface IUserService
{
    Task<List<User>> GetAll();
}
