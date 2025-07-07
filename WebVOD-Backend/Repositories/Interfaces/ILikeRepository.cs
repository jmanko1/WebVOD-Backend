using WebVOD_Backend.Model;

namespace WebVOD_Backend.Repositories.Interfaces;

public interface ILikeRepository
{
    Task DeleteByVideoIdAndUserId(string videoId, string userId);
    Task<bool> ExistsByVideoIdAndUserId(string videoId, string userId);
    Task Add(Like like);
}
