using WebVOD_Backend.Dtos.User;
using WebVOD_Backend.Model;

namespace WebVOD_Backend.Services.Interfaces;

public interface IUserService
{
    Task<UserDto> GetUserByLogin(string login);
    Task<UserDto> GetMyProfile(string sub);
    Task<string> GetMyEmail(string sub);
    Task<List<User>> GetAll();
}
