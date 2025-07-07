using WebVOD_Backend.Dtos.Comment;
using WebVOD_Backend.Dtos.Video;

namespace WebVOD_Backend.Services.Interfaces;

public interface IVideoService
{
    Task<VideoDto> GetVideoById(string id);
    Task<List<CommentDto>> GetVideoComments(string id, int page, int size);
    Task<bool> IsVideoLiked(string sub, string id);
    Task LikeVideo(string sub, string id);
    Task CancelLikeVideo(string sub, string id);
}
