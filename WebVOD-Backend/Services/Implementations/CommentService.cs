using MongoDB.Bson;
using WebVOD_Backend.Dtos.Comment;
using WebVOD_Backend.Exceptions;
using WebVOD_Backend.Model;
using WebVOD_Backend.Repositories.Interfaces;
using WebVOD_Backend.Services.Interfaces;

namespace WebVOD_Backend.Services.Implementations;

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IVideoRepository _videoRepository;

    public CommentService(ICommentRepository commentRepository, IUserRepository userRepository, IVideoRepository videoRepository)
    {
        _commentRepository = commentRepository;
        _userRepository = userRepository;
        _videoRepository = videoRepository;
    }

    public async Task<string> AddComment(string sub, NewCommentDto newCommentDto)
    {
        if (string.IsNullOrWhiteSpace(newCommentDto.Content))
        {
            throw new RequestErrorException(400, "Podaj treść komentarza");
        }

        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401);
        }

        if (!ObjectId.TryParse(newCommentDto.VideoId, out _))
        {
            throw new RequestErrorException(404, "Film nie istnieje.");
        }

        var video = await _videoRepository.FindById(newCommentDto.VideoId);
        if (video == null || video.Status != VideoStatus.PUBLISHED)
        {
            throw new RequestErrorException(404, "Film nie istnieje.");
        }

        var comment = new Comment
        {
            Content = newCommentDto.Content,
            AuthorId = user.Id,
            VideoId = video.Id
        };

        var newCommentId = await _commentRepository.Add(comment);
        await _videoRepository.IncrementCommentsCount(newCommentDto.VideoId);

        return newCommentId;
    }

    public async Task DeleteComment(string sub, string commentId)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401);
        }

        if (!ObjectId.TryParse(commentId, out _))
        {
            throw new RequestErrorException(404, "Komentarz nie istnieje.");
        }

        var comment = await _commentRepository.FindById(commentId);
        if (comment == null)
        {
            throw new RequestErrorException(404, "Komentarz nie istnieje.");
        }

        if (comment.AuthorId != user.Id)
        {
            throw new RequestErrorException(403, "Brak uprawnień.");
        }

        var video = await _videoRepository.FindById(comment.VideoId);
        if (video == null)
        {
            throw new RequestErrorException(500, "Film, do którego jest komentarz, nie istnieje.");
        }

        if (video.Status != VideoStatus.PUBLISHED)
        {
            throw new RequestErrorException(400, "Nie można usuwać komentarza do nieopublikowanego filmu.");
        }

        await _commentRepository.DeleteById(commentId);
        await _videoRepository.DecrementCommentsCount(comment.VideoId);
    }
}
