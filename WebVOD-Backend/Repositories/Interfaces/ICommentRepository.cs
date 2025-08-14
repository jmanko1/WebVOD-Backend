using WebVOD_Backend.Model;

namespace WebVOD_Backend.Repositories.Interfaces;

public interface ICommentRepository
{
    Task<Comment> FindById(string id);
    Task<List<Comment>> FindByVideoId(string videoId, int page, int size);
    Task<string> Add(Comment comment);
    Task DeleteById(string id);
    Task DeleteByVideoId(string id);
}
