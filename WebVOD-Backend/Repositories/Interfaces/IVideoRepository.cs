using WebVOD_Backend.Model;

namespace WebVOD_Backend.Repositories.Interfaces;

public interface IVideoRepository
{
    Task<Video> FindById(string id);
    Task<List<Video>> FindByUserId(string userId, int page, int size);
    Task IncrementViewsCount(string id);
    Task IncrementCommentsCount(string id);
    Task DecrementCommentsCount(string id);
    Task IncrementLikesCount(string id);
    Task DecrementLikesCount(string id);
}
