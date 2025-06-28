using WebVOD_Backend.Dtos.User;
using WebVOD_Backend.Model;

namespace WebVOD_Backend.Services.Interfaces;

public interface IUserService
{
    Task<UserDto> GetUserByLogin(string login);
    Task<UserDto> GetMyProfile(string sub);
    Task<string> GetMyEmail(string sub);
    Task UpdateDescription(string sub, string description);
    Task<string> UpdateImage(string sub, IFormFile image);
    Task<List<User>> GetAll();
}
