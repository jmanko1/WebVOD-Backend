using Microsoft.IdentityModel.Tokens;
using WebVOD_Backend.Dtos.User;
using WebVOD_Backend.Exceptions;
using WebVOD_Backend.Model;
using WebVOD_Backend.Repositories.Interfaces;
using WebVOD_Backend.Services.Interfaces;

namespace WebVOD_Backend.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IFilesService _filesService;

    public UserService(IUserRepository userRepository, IFilesService filesService)
    {
        _userRepository = userRepository;
        _filesService = filesService;
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

    public async Task UpdateDescription(string sub, string description)
    {
        if(string.IsNullOrEmpty(description))
        {
            throw new RequestErrorException(400, "Podaj opis.");
        }

        var user = await _userRepository.FindByLogin(sub);
        if(user == null)
        {
            throw new RequestErrorException(401, "Użytkownik nie istnieje.");
        }

        await _userRepository.UpdateDescription(user.Id, description);
    }

    public async Task<string> UpdateImage(string sub, IFormFile image)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401, "Użytkownik nie istnieje.");
        }

        if (image == null || image.Length == 0)
        {
            throw new RequestErrorException(400, "Wybierz zdjęcie do przesłania.");
        }

        if (image.Length > 1048576)
        {
            throw new RequestErrorException(400, "Zdjęcie profilowe może mieć maksymalnie 1 MB.");
        }

        if (!image.ContentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
        {
            throw new RequestErrorException(400, "Zdjęcie profilowe musi być w formacie WebP.");
        }

        if (user.ImageUrl != null)
        {
            var fileNameToDelete = Path.GetFileName(user.ImageUrl);
            _filesService.DeleteProfileImage(fileNameToDelete);
        }

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var fileName = $"{sub}_{timestamp}.webp";
        await _filesService.SaveProfileImage(fileName, image);

        var imageUrl = $"/uploads/user_images/{fileName}";
        await _userRepository.UpdateImageUrl(user.Id, imageUrl);

        return imageUrl;
    }
}
