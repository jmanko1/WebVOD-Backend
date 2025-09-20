using WebVOD_Backend.Dtos.Comment;
using WebVOD_Backend.Dtos.Video;

namespace WebVOD_Backend.Services.Interfaces;

public interface IVideoService
{
    Task<VideoDto> GetVideoById(string? sub, string id);
    Task<List<CommentDto>> GetVideoComments(string id, int page, int size);
    Task<bool> IsVideoLiked(string sub, string id);
    Task LikeVideo(string sub, string id);
    Task CancelLikeVideo(string sub, string id);
    Task UploadChunk(string sub, IFormFile chunk, string videoId, string currentChunkIndex, string totalChunks);
    Task CancelUpload(string sub, string id);
    Task<string> CreateNewVideo(string sub, CreateVideoDto createVideoDto);
    Task UpdateThumbnail(string sub, string id, IFormFile thumbnail);
    Task<VideoToUpdateDto> GetVideoToUpdateById(string sub, string id);
    Task UpdateVideoById(string sub, string id, UpdateVideoDto updateVideoDto);
    Task DeleteVideoById(string sub, string id);
}
