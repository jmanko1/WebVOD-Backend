using WebVOD_Backend.Dtos.Comment;

namespace WebVOD_Backend.Services.Interfaces;

public interface ICommentService
{
    Task<string> AddComment(string sub, NewCommentDto newCommentDto);
    Task DeleteComment(string sub, string commentId);
}
