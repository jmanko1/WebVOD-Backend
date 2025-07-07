using System.Text.RegularExpressions;
using OtpNet;
using WebVOD_Backend.Dtos.User;
using WebVOD_Backend.Exceptions;
using WebVOD_Backend.Model;
using WebVOD_Backend.Repositories.Interfaces;
using WebVOD_Backend.Services.Interfaces;

namespace WebVOD_Backend.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IVideoRepository _videoRepository;
    private readonly IFilesService _filesService;
    private readonly ICryptoService _cryptoService;

    public UserService(IUserRepository userRepository, IFilesService filesService, ICryptoService cryptoService, IVideoRepository videoRepository)
    {
        _userRepository = userRepository;
        _filesService = filesService;
        _cryptoService = cryptoService;
        _videoRepository = videoRepository;
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
            SignupDate = user.SignupDate,
            VideosCount = user.VideosCount,
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
        if(string.IsNullOrWhiteSpace(description))
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

    public async Task<bool> IsTFARequired(string sub)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401, "Użytkownik nie istnieje.");
        }

        return user.IsTFAEnabled;
    }

    public async Task ChangePassword(string sub, ChangePasswordDto changePasswordDto)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401, "Użytkownik nie istnieje.");
        }

        if (!_cryptoService.VerifyPassword(changePasswordDto.OldPassword, user.Password))
        {
            throw new RequestErrorException(401, "Nieprawidłowe stare hasło.");
        }

        if (user.IsTFAEnabled)
        {
            if (changePasswordDto.Code == null)
            {
                throw new RequestErrorException(400, "Podaj kod.");
            }

            var regex = "^[0-9]{6}$";
            if (!Regex.IsMatch(changePasswordDto.Code, regex))
            {
                throw new RequestErrorException(400, "Podaj prawidłowy 6-cyfrowy kod.");
            }

            var secretKey = _cryptoService.Decrypt(user.TOTPKey);
            if (!ValidateTotp(secretKey, changePasswordDto.Code))
            {
                throw new RequestErrorException(401, "Nieprawidłowy kod.");
            }

            await _userRepository.ChangePassword(user.Id, _cryptoService.HashPassword(changePasswordDto.NewPassword));
            return;
        }

        await _userRepository.ChangePassword(user.Id, _cryptoService.HashPassword(changePasswordDto.NewPassword));
    }
    private bool ValidateTotp(string secret, string userInput)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(userInput, out _, new VerificationWindow(previous: 1, future: 0));
    }

    public async Task<string?> GetTFAQrCode(string sub)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401, "Użytkownik nie istnieje.");
        }

        if (user.IsTFAEnabled)
            return null;

        var secret = _cryptoService.Decrypt(user.TOTPKey);

        return $"otpauth://totp/webvod:{user.Login}?secret={secret}&issuer=webvod&algorithm=SHA1&digits=6&period=30";
    }

    public async Task ToggleTFA(string sub, ToggleTFADto toggleTFADto)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401, "Użytkownik nie istnieje.");
        }

        if (!_cryptoService.VerifyPassword(toggleTFADto.Password, user.Password))
        {
            throw new RequestErrorException(401, "Nieprawidłowe hasło.");
        }

        var secretKey = _cryptoService.Decrypt(user.TOTPKey);
        if (!ValidateTotp(secretKey, toggleTFADto.Code))
        {
            throw new RequestErrorException(401, "Nieprawidłowy kod.");
        }

        await _userRepository.SetTFA(user.Id, !user.IsTFAEnabled);
    }

    public async Task<List<UserVideoDto>> GetUserVideos(string login, int page, int size)
    {
        var user = await _userRepository.FindByLogin(login);
        if (user == null)
        {
            throw new RequestErrorException(404, "Użytkownik nie istnieje.");
        }

        var videos = await _videoRepository.FindByUserId(user.Id, page, size);
        var videoDtos = videos.Select(v => new UserVideoDto
        {
            Id = v.Id,
            Title = v.Title,
            ThumbnailPath = v.ThumbnailPath,
            UploadDate = v.UploadDate,
            ViewsCount = v.ViewsCount,
            Duration = v.Duration
        }).ToList();

        return videoDtos;
    }
}
