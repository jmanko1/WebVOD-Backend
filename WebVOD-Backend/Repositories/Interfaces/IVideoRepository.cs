using WebVOD_Backend.Model;

namespace WebVOD_Backend.Repositories.Interfaces;

public interface IVideoRepository
{
    Task<Video> FindById(string id);
    Task<List<Video>> FindById(List<string> ids);
    Task<List<Video>> FindByUserId(string userId, int page, int size, string? titlePattern = null, bool onlyPublished = true);
    Task IncrementViewsCount(string id);
    Task IncrementCommentsCount(string id);
    Task DecrementCommentsCount(string id);
    Task IncrementLikesCount(string id);
    Task DecrementLikesCount(string id);
    Task<bool> ExistsById(string id);
    Task Add(Video video);
    Task Replace(string id, Video video);
    Task UpdateThumbnail(string id, string thumbnailPath);
    Task UpdateStatus(string id, VideoStatus status);
    Task DeleteById(string id);
}
