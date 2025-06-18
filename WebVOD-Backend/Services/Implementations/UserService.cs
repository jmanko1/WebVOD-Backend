using WebVOD_Backend.Dtos.User;
using WebVOD_Backend.Exceptions;
using WebVOD_Backend.Model;
using WebVOD_Backend.Repositories.Interfaces;
using WebVOD_Backend.Services.Interfaces;

namespace WebVOD_Backend.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto> GetMyProfile(string sub)
    {
        var user = await _userRepository.FindByLogin(sub);
        if(user == null)
        {
            throw new RequestErrorException(401, "Użytkownik nie istnieje.");
        }

        return mapUserDtoFromUser(user);
    }

    public async Task<List<User>> GetAll()
    {
        return await _userRepository.GetAll();
    }

    public async Task<UserDto> GetUserByLogin(string login)
    {
        var user = await _userRepository.FindByLogin(login);
        if(user == null)
        {
            throw new RequestErrorException(404, "Szukany użytkownik nie istnieje.");
        }

        return mapUserDtoFromUser(user);
    }

    private UserDto mapUserDtoFromUser(User user)
    {
        var userDto = new UserDto
        {
            Id = user.Id,
            Login = user.Login,
            Description = user.Description,
            ImageUrl = user.ImageUrl,
            SignupDate = DateOnly.FromDateTime(user.SignupDate)
        };

        return userDto;
    }

    public async Task<string> GetMyEmail(string sub)
    {
        var user = await _userRepository.FindByLogin(sub);
        if(user == null)
        {
            throw new RequestErrorException(401, "Użytkownik nie istnieje.");
        }

        return user.Email;
    }
}
