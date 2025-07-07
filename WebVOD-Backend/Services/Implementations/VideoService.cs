using MongoDB.Bson;
using WebVOD_Backend.Dtos.Video;
using WebVOD_Backend.Exceptions;
using WebVOD_Backend.Repositories.Interfaces;
using WebVOD_Backend.Services.Interfaces;
using WebVOD_Backend.Model;
using WebVOD_Backend.Dtos.Comment;

namespace WebVOD_Backend.Services.Implementations;

public class VideoService : IVideoService
{
    private readonly IVideoRepository _videoRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly ILikeRepository _likeRepository;

    public VideoService(IVideoRepository videoRepository, IUserRepository userRepository, ICommentRepository commentRepository, ILikeRepository likeRepository)
    {
        _videoRepository = videoRepository;
        _userRepository = userRepository;
        _commentRepository = commentRepository;
        _likeRepository = likeRepository;
    }

    public async Task CancelLikeVideo(string sub, string id)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401, "Użytkownik nie istnieje.");
        }

        if (!ObjectId.TryParse(id, out _))
        {
            throw new RequestErrorException(404, "Film nie istnieje.");
        }

        var video = await _videoRepository.FindById(id);
        if (video == null)
        {
            throw new RequestErrorException(404, "Film nie istnieje.");
        }

        var alreadyLiked = await _likeRepository.ExistsByVideoIdAndUserId(video.Id, user.Id);
        if (!alreadyLiked)
        {
            throw new RequestErrorException(400, "Film nie został polubiony.");
        }

        await _likeRepository.DeleteByVideoIdAndUserId(video.Id, user.Id);
        await _videoRepository.DecrementLikesCount(video.Id);
    }

    public async Task<VideoDto> GetVideoById(string id)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            throw new RequestErrorException(404, "Szukany film nie istnieje.");
        }

        var video = await _videoRepository.FindById(id);
        if (video == null || video.Status == VideoStatus.UPLOADING)
        {
            throw new RequestErrorException(404, "Szukany film nie istnieje.");
        }

        var author = await _userRepository.FindById(video.AuthorId);
        if (author == null)
        {
            throw new RequestErrorException(500, "Nie znaleziono autora filmu.");
        }

        await _videoRepository.IncrementViewsCount(id);

        var videoDto = new VideoDto
        {
            Id = video.Id,
            Title = video.Title,
            Description = video.Description ?? "Brak opisu.",
            Category = video.Category.ToString(),
            Tags = video.Tags,
            VideoPath = video.VideoPath,
            UploadDate = video.UploadDate,
            LikesCount = video.LikesCount,
            CommentsCount = video.CommentsCount,
            ViewsCount = video.ViewsCount + 1,
            Author = new AuthorDto
            {
                Id = author.Id,
                Login = author.Login,
                ImageUrl = author.ImageUrl
            }
        };

        return videoDto;
    }

    public async Task<List<CommentDto>> GetVideoComments(string id, int page, int size)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            throw new RequestErrorException(404, "Film nie istnieje.");
        }

        var comments = await _commentRepository.FindByVideoId(id, page, size);
        var authorIds = comments.Select(c => c.AuthorId).Distinct().ToList();
        var authors = await _userRepository.FindById(authorIds);
        var authorsDict = authors.ToDictionary(a => a.Id);

        var commentDtos = comments.Select(c => new CommentDto
        {
            Id = c.Id,
            Content = c.Content,
            UploadDate = c.UploadDate,
            Author = new AuthorDto
            {
                Id = c.AuthorId,
                Login = authorsDict[c.AuthorId].Login,
                ImageUrl = authorsDict[c.AuthorId].ImageUrl
            }
        }).ToList();

        return commentDtos;
    }

    public async Task<bool> IsVideoLiked(string sub, string id)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401, "Użytkownik nie istnieje.");
        }

        if (!ObjectId.TryParse(id, out _))
        {
            throw new RequestErrorException(404, "Film nie istnieje.");
        }

        var video = await _videoRepository.FindById(id);
        if (video == null)
        {
            throw new RequestErrorException(404, "Film nie istnieje.");
        }

        var isVideoLiked = await _likeRepository.ExistsByVideoIdAndUserId(video.Id, user.Id);
        return isVideoLiked;
    }

    public async Task LikeVideo(string sub, string id)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401, "Użytkownik nie istnieje.");
        }

        if (!ObjectId.TryParse(id, out _))
        {
            throw new RequestErrorException(404, "Film nie istnieje.");
        }

        var video = await _videoRepository.FindById(id);
        if (video == null)
        {
            throw new RequestErrorException(404, "Film nie istnieje.");
        }

        var alreadyLiked = await _likeRepository.ExistsByVideoIdAndUserId(video.Id, user.Id);
        if (alreadyLiked)
        {
            throw new RequestErrorException(400, "Film został już polubiony.");
        }

        var like = new Like
        {
            UserId = user.Id,
            VideoId = video.Id,
        };

        await _likeRepository.Add(like);
        await _videoRepository.IncrementLikesCount(video.Id);
    }
}
