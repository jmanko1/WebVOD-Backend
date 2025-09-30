using WebVOD_Backend.Dtos.User;
using WebVOD_Backend.Dtos.Video;
using WebVOD_Backend.Model;

namespace WebVOD_Backend.Services.Interfaces;

public interface IUserService
{
    Task<UserDto> GetUserByLogin(string login);
    Task<UserDto> GetMyProfile(string sub);
    Task<string> GetMyEmail(string sub);
    Task UpdateDescription(string sub, string description);
    Task<string> UpdateImage(string sub, IFormFile image);
    Task<bool> IsTFARequired(string sub);
    Task ChangePassword(string sub, ChangePasswordDto changePasswordDto);
    Task<string?> GetTFAQrCode(string sub);
    Task ToggleTFA(string sub, ToggleTFADto toggleTFADto);
    Task<List<UserVideoDto>> GetUserVideos(string login, int page, int size);
    Task<List<UserVideoDto>> GetMyVideos(string sub, int page, int size, string? titlePattern);
    Task<List<UserVideoActivityDto>> GetLikedVideos(string sub, int page, int size);
    Task<List<UserVideoActivityDto>> GetViewedVideos(string sub, int page, int size);
    Task ClearHistory(string sub);
    Task<List<SearchUserDto>> SearchUsers(string query, int page, int size);
    Task DeleteDescription(string sub);
    Task DeleteImage(string sub);
    Task<List<User>> GetAll();
}
