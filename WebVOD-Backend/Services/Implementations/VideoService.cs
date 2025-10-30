using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MongoDB.Bson;
using WebVOD_Backend.Dtos.Comment;
using WebVOD_Backend.Dtos.TagsProposition;
using WebVOD_Backend.Dtos.Video;
using WebVOD_Backend.Exceptions;
using WebVOD_Backend.Model;
using WebVOD_Backend.Repositories.Interfaces;
using WebVOD_Backend.Services.Interfaces;

namespace WebVOD_Backend.Services.Implementations;

public class VideoService : IVideoService
{
    private readonly IVideoRepository _videoRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly ILikeRepository _likeRepository;
    private readonly IFilesService _filesService;
    private readonly IWatchingHistoryElementRepository _watchingHistoryElementRepository;
    private readonly ITagsPropositionRepository _tagsPropositionRepository;

    private const string recommendationsAPI = "http://localhost:5000";

    public VideoService(IVideoRepository videoRepository, IUserRepository userRepository, ICommentRepository commentRepository, ILikeRepository likeRepository, IFilesService filesService, IWatchingHistoryElementRepository watchingHistoryElementRepository, ITagsPropositionRepository tagsPropositionRepository)
    {
        _videoRepository = videoRepository;
        _userRepository = userRepository;
        _commentRepository = commentRepository;
        _likeRepository = likeRepository;
        _filesService = filesService;
        _watchingHistoryElementRepository = watchingHistoryElementRepository;
        _tagsPropositionRepository = tagsPropositionRepository;
    }

    public async Task CancelLikeVideo(string sub, string id)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401);
        }

        if (!ObjectId.TryParse(id, out _))
        {
            throw new RequestErrorException(404, "Film nie istnieje.");
        }

        var video = await _videoRepository.FindById(id);
        if (video == null || video.Status != VideoStatus.PUBLISHED)
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

    public async Task<string> CreateNewVideo(string sub, CreateVideoDto createVideoDto)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401);
        }

        if (string.IsNullOrWhiteSpace(createVideoDto.Title))
        {
            throw new RequestErrorException(400, "Podaj tytuł.");
        }

        var formatedTags = createVideoDto.Tags
            .Select(t => t.ToLower())
            .OrderBy(t => t)
            .ToList();

        if (formatedTags.Count() > 20)
        {
            throw new RequestErrorException(400, "Film może mieć maksymalnie 20 tagów.");
        }

        if (formatedTags.Any(t => t.Length > 25))
        {
            throw new RequestErrorException(400, "Każdy tag może mieć maksymalnie 25 znaków.");
        }

        if (formatedTags.Distinct().Count() != formatedTags.Count())
        {
            throw new RequestErrorException(400, "Tagi muszą być unikalne.");
        }

        if (formatedTags.Any(t => t.Any(c => !char.IsLetterOrDigit(c))))
        {
            throw new RequestErrorException(400, "Tagi mogą zawierać tylko litery i cyfry.");
        }

        if (createVideoDto.Duration <= 0)
        {
            throw new RequestErrorException(400, "Nieprawidłowa długość filmu.");
        }

        var video = new Video
        {
            Title = createVideoDto.Title,
            Description = createVideoDto.Description,
            Category = createVideoDto.Category,
            Tags = formatedTags,
            AuthorId = user.Id,
            Duration = createVideoDto.Duration,
            TagsProposalsEnabled = createVideoDto.TagsProposalsEnabled
        };

        await _videoRepository.Add(video);

        return video.Id;
    }

    public async Task DeleteVideoById(string sub, string id)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401);
        }

        if (!ObjectId.TryParse(id, out _))
        {
            throw new RequestErrorException(404, "Szukany film nie istnieje.");
        }

        var video = await _videoRepository.FindById(id);
        if (video == null)
        {
            throw new RequestErrorException(404, "Szukany film nie istnieje.");
        }

        var allowedStatuses = new List<VideoStatus> { VideoStatus.PUBLISHED, VideoStatus.FAILED };
        if (!allowedStatuses.Contains(video.Status))
        {
            throw new RequestErrorException(400, "Nie można usunąć filmu, który jest w trakcie przesyłania lub przetwarzania.");
        }

        if (video.AuthorId != user.Id)
        {
            throw new RequestErrorException(403, "Brak uprawnień do usunięcia tego filmu.");
        }

        if (video.Status == VideoStatus.PUBLISHED)
            await _userRepository.DecrementVideosCount(user.Id);

        await _likeRepository.DeleteByVideoId(id);
        await _commentRepository.DeleteByVideoId(id);
        await _watchingHistoryElementRepository.DeleteByVideoId(id);
        await _tagsPropositionRepository.DeleteByVideoId(id);

        if (video.ThumbnailPath != null)
        {
            var fileNameToDelete = Path.GetFileName(video.ThumbnailPath);
            _filesService.DeleteThumbnail(fileNameToDelete);
        }

        _filesService.DeleteVideo(id);

        if (video.Status == VideoStatus.PUBLISHED)
        {
            _ = Task.Run(async () =>
            {
                using var client = new HttpClient();
                await client.DeleteAsync($"{recommendationsAPI}/delete-video?id={id}");
            });

            return;
        }

        await _videoRepository.DeleteById(id);
    }

    public async Task<VideoDto> GetVideoById(string? sub, string id)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            throw new RequestErrorException(404, "Szukany film nie istnieje.");
        }

        var video = await _videoRepository.FindById(id);
        if (video == null || video.Status != VideoStatus.PUBLISHED)
        {
            throw new RequestErrorException(404, "Szukany film nie istnieje.");
        }

        var author = await _userRepository.FindById(video.AuthorId);
        if (author == null)
        {
            throw new RequestErrorException(500, "Nie znaleziono autora filmu.");
        }

        await _videoRepository.IncrementViewsCount(id);

        User? viewer = null;

        if (sub != null)
        {
            viewer = await _userRepository.FindByLogin(sub);
            if (viewer != null)
            {
                if (await _watchingHistoryElementRepository.ExistsByVideoIdAndViewerId(video.Id, viewer.Id))
                {
                    await _watchingHistoryElementRepository.UpdateViewedAt(video.Id, viewer.Id);
                }
                else
                {
                    var watchingHistoryElement = new WatchingHistoryElement
                    {
                        VideoId = video.Id,
                        ViewerId = viewer.Id
                    };

                    await _watchingHistoryElementRepository.Add(watchingHistoryElement);
                }
            }
        }

        var videoDto = new VideoDto
        {
            Id = video.Id,
            Title = video.Title,
            Description = video.Description ?? "Brak opisu.",
            Category = video.Category.ToString(),
            Tags = video.Tags,
            TagsProposalsEnabled = video.TagsProposalsEnabled,
            VideoPath = video.VideoPath,
            UploadDate = video.UploadDate,
            LikesCount = video.LikesCount,
            CommentsCount = video.CommentsCount,
            ViewsCount = video.ViewsCount + 1,
            TagsPropositionsCount = viewer != null && viewer.Id == video.AuthorId ? video.TagsPropositionsCount : null,
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

    public async Task<VideoToUpdateDto> GetVideoToUpdateById(string sub, string id)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401);
        }

        if (!ObjectId.TryParse(id, out _))
        {
            throw new RequestErrorException(404, "Szukany film nie istnieje.");
        }

        var video = await _videoRepository.FindById(id);
        if (video == null || video.Status != VideoStatus.PUBLISHED)
        {
            throw new RequestErrorException(404, "Szukany film nie istnieje.");
        }

        if (video.AuthorId != user.Id)
        {
            throw new RequestErrorException(403, "Brak uprawnień do aktualizacji tego filmu.");
        }

        var videoDto = new VideoToUpdateDto
        {
            Id = video.Id,
            Title = video.Title,
            Description = video.Description,
            Category = video.Category.ToString(),
            Tags = video.Tags,
            TagsProposalsEnabled = video.TagsProposalsEnabled,
            ThumbnailPath = video.ThumbnailPath
        };

        return videoDto;
    }

    public async Task<bool> IsVideoLiked(string sub, string id)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401);
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
            throw new RequestErrorException(401);
        }

        if (!ObjectId.TryParse(id, out _))
        {
            throw new RequestErrorException(404, "Film nie istnieje.");
        }

        var video = await _videoRepository.FindById(id);
        if (video == null || video.Status != VideoStatus.PUBLISHED)
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

    public async Task UpdateThumbnail(string sub, string id, IFormFile thumbnail)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401);
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

        var allowedStatuses = new List<VideoStatus> { VideoStatus.PUBLISHED, VideoStatus.UPLOADING };
        if (!allowedStatuses.Contains(video.Status))
        {
            throw new RequestErrorException(400, "Film musi być opublikowany lub w trakcie przesyłania.");
        }

        if (video.AuthorId != user.Id)
        {
            throw new RequestErrorException(403, "Brak uprawnień do zmiany miniatury filmu.");
        }

        if (thumbnail == null || thumbnail.Length == 0)
        {
            throw new RequestErrorException(400, "Brak miniatury.");
        }

        if (thumbnail.Length > 1048576)
        {
            throw new RequestErrorException(400, "Miniatura może mieć maksymalnie 1 MB.");
        }

        if (!thumbnail.ContentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
        {
            throw new RequestErrorException(400, "Miniatura musi być w formacie WebP.");
        }

        if (video.ThumbnailPath != null)
        {
            var fileNameToDelete = Path.GetFileName(video.ThumbnailPath);
            _filesService.DeleteThumbnail(fileNameToDelete);
        }

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var fileName = $"{video.Id}_{timestamp}.webp";
        await _filesService.SaveThumbnail(fileName, thumbnail);

        var thumbnailPath = $"/uploads/thumbnails/{fileName}";
        await _videoRepository.UpdateThumbnail(video.Id, thumbnailPath);
    }

    public async Task UpdateVideoById(string sub, string id, UpdateVideoDto updateVideoDto)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401);
        }

        if (!ObjectId.TryParse(id, out _))
        {
            throw new RequestErrorException(404, "Film nie istnieje.");
        }

        var video = await _videoRepository.FindById(id);
        if (video == null || video.Status != VideoStatus.PUBLISHED)
        {
            throw new RequestErrorException(404, "Film nie istnieje.");
        }

        if (video.AuthorId != user.Id)
        {
            throw new RequestErrorException(403, "Brak uprawnień do aktualizacji filmu.");
        }

        if (string.IsNullOrWhiteSpace(updateVideoDto.Title))
        {
            throw new RequestErrorException(400, "Podaj tytuł.");
        }

        var formatedTags = updateVideoDto.Tags
            .Select(t => t.ToLower())
            .OrderBy(t => t)
            .ToList();

        if (formatedTags.Count() > 20)
        {
            throw new RequestErrorException(400, "Film może mieć maksymalnie 20 tagów.");
        }

        if (formatedTags.Any(t => t.Length > 25))
        {
            throw new RequestErrorException(400, "Każdy tag może mieć maksymalnie 25 znaków.");
        }

        if (formatedTags.Distinct().Count() != formatedTags.Count())
        {
            throw new RequestErrorException(400, "Tagi muszą być unikalne.");
        }

        if (formatedTags.Any(t => t.Any(c => !char.IsLetterOrDigit(c))))
        {
            throw new RequestErrorException(400, "Tagi mogą zawierać tylko litery i cyfry.");
        }

        video.Title = updateVideoDto.Title;
        video.Description = updateVideoDto.Description;
        video.Category = updateVideoDto.Category;
        video.Tags = formatedTags;
        video.TagsProposalsEnabled = updateVideoDto.TagsProposalsEnabled;

        await _videoRepository.Replace(id, video);

        _ = Task.Run(async () =>
        {
                var videoData = new
                {
                    Title = video.Title,
                    Description = video.Description,
                    Category = video.Category.ToString(),
                    Tags = video.Tags,
                    AuthorLogin = sub
                };

                var json = JsonSerializer.Serialize(videoData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var client = new HttpClient();
                await client.PutAsync($"{recommendationsAPI}/update-video?id={id}", content);
        });
    }

    public async Task UploadChunk(string sub, IFormFile chunk, string videoId, string currentChunkIndex, string totalChunks)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401);
        }

        if (chunk == null || chunk.Length == 0)
        {
            throw new RequestErrorException(400, "Brak fragmentu filmu.");
        }

        if (chunk.Length > 5242880)
        {
            throw new RequestErrorException(400, "Fragment filmu może mieć maksymalnie 5 MB.");
        }

        if (!ObjectId.TryParse(videoId, out _))
        {
            throw new RequestErrorException(400, "Nieprawidłowe ID filmu.");
        }

        var video = await _videoRepository.FindById(videoId);
        if (video == null)
        {
            throw new RequestErrorException(404, "Film nie istnieje.");
        }

        if (video.Status != VideoStatus.UPLOADING)
        {
            throw new RequestErrorException(400, "Film nie jest w trakcie przesyłania.");
        }

        if (video.AuthorId != user.Id)
        {
            throw new RequestErrorException(403, "Brak uprawnień.");
        }

        if (!int.TryParse(currentChunkIndex, out int currentChunkIndexInt))
        {
            throw new RequestErrorException(400, "Nieprawidłowy numer fragmentu filmu.");
        }

        if (!int.TryParse(totalChunks, out int totalChunksInt))
        {
            throw new RequestErrorException(400, "Nieprawidłowa liczba fragmentów filmu.");
        }

        if (currentChunkIndexInt < 0)
        {
            throw new RequestErrorException(400, "Minimalna wartość numeru fragmentu filmu musi wynosić 0.");
        }

        if (currentChunkIndexInt >= totalChunksInt)
        {
            throw new RequestErrorException(400, "Wartość numeru fragmentu filmu musi być mniejsza niż liczba wszystkich fragmentów filmu.");
        }

        if (totalChunksInt > 103)
        {
            throw new RequestErrorException(400, "Liczba fragmentów filmu nie może przekraczać 103.");
        }

        await _filesService.SaveVideoChunk(videoId, currentChunkIndexInt, chunk);

        if (currentChunkIndexInt == totalChunksInt - 1)
        {
            _ = Task.Run(async () =>
            {
                _filesService.MergeVideoChunks(videoId);
                await _videoRepository.UpdateStatus(videoId, VideoStatus.PROCESSED);

                var batPath = @"C:\Users\Kuba\source\repos\WebVOD-Backend\WebVOD-Backend\ffmpeg\bin\convert.bat";

                var processInfo = new ProcessStartInfo
                {
                    FileName = batPath,
                    Arguments = videoId,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = @"C:\Users\Kuba\source\repos\WebVOD-Backend\WebVOD-Backend\ffmpeg\bin"
                };

                using var process = new Process { StartInfo = processInfo };

                process.OutputDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) Console.WriteLine(e.Data); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    await _videoRepository.UpdateStatus(videoId, VideoStatus.FAILED);
                }
                else
                {
                    await _videoRepository.UpdateStatus(videoId, VideoStatus.PUBLISHED);
                    await _userRepository.IncrementVideosCount(video.AuthorId);
                    await _videoRepository.UpdateVideoPath(videoId, $"/uploads/videos/{videoId}/master.m3u8");
                    _filesService.DeleteVideoMP4(videoId);

                    var videoData = new
                    {
                        Id = videoId,
                        Title = video.Title,
                        Description = video.Description,
                        Category = video.Category.ToString(),
                        Tags = video.Tags,
                        AuthorLogin = sub
                    };

                    var json = JsonSerializer.Serialize(videoData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    using var client = new HttpClient();
                    await client.PostAsync($"{recommendationsAPI}/add-video", content);
                }
            });
        }
    }

    public async Task CancelUpload(string sub, string id)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401);
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

        if (video.Status != VideoStatus.UPLOADING)
        {
            throw new RequestErrorException(400, "Film nie jest w trakcie przesyłania.");
        }

        if (user.Id != video.AuthorId)
        {
            throw new RequestErrorException(403, "Brak uprawnień.");
        }

        await _videoRepository.UpdateStatus(id, VideoStatus.FAILED);
    }

    public async Task ProposeTags(string sub, string videoId,NewTagsPropositionDto newTagsPropositionDto)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401);
        }

        if (!ObjectId.TryParse(videoId, out _))
        {
            throw new RequestErrorException(404, "Nie znaleziono filmu.");
        }

        var video = await _videoRepository.FindById(videoId);
        if (video == null || video.Status != VideoStatus.PUBLISHED)
        {
            throw new RequestErrorException(404, "Nie znaleziono filmu.");
        }

        if (video.AuthorId == user.Id)
        {
            throw new RequestErrorException(403, "Propozycji tagów nie może wysłać autor filmu.");
        }

        if (!video.TagsProposalsEnabled)
        {
            throw new RequestErrorException(403, "Autor filmu nie pozwala na proponowanie tagów.");
        }

        if (await _tagsPropositionRepository.ExistsByVideoIdAndUserId(video.Id, user.Id))
        {
            throw new RequestErrorException(400, "Propozycja tagów już została wysłana.");
        }

        if (newTagsPropositionDto.Tags == null || newTagsPropositionDto.Tags.Count == 0)
        {
            throw new RequestErrorException(400, "Podaj listę tagów.");
        }

        var formatedTags = newTagsPropositionDto.Tags
            .Select(t => t.ToLower())
            .ToList();

        if (formatedTags.Count() > 5)
        {
            throw new RequestErrorException(400, "Można zaproponować maksymalnie 5 tagów.");
        }

        if (formatedTags.Any(t => t.Length > 25))
        {
            throw new RequestErrorException(400, "Każdy tag może mieć maksymalnie 25 znaków.");
        }

        if (formatedTags.Distinct().Count() != formatedTags.Count())
        {
            throw new RequestErrorException(400, "Tagi muszą być unikalne.");
        }

        if (formatedTags.Any(t => t.Any(c => !char.IsLetterOrDigit(c))))
        {
            throw new RequestErrorException(400, "Tagi mogą zawierać tylko litery i cyfry.");
        }

        var result = video.Tags.Concat(formatedTags).ToList();
        var duplicates = result
            .GroupBy(t => t)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count() > 0)
        {
            throw new RequestErrorException(400, $"Następujące tagi są już zapisane w filmie: {string.Join(", ", duplicates)}.");
        }

        if (formatedTags.Count() + video.Tags.Count() > 20)
        {
            throw new RequestErrorException(400, "Liczba proponowanych tagów + liczba już zapisanych tagów nie może przekraczać 20.");
        }

        var proposition = new TagsProposition
        {
            VideoId = video.Id,
            UserId = user.Id,
            Tags = formatedTags,
            Comment = newTagsPropositionDto.Comment
        };

        await _tagsPropositionRepository.Add(proposition);
        await _videoRepository.IncrementTagsPropositionsCount(video.Id);
    }

    public async Task<List<TagsPropositionDto>> GetProposedTags(string sub, string videoId, int page, int size)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401);
        }

        if (!ObjectId.TryParse(videoId, out _))
        {
            throw new RequestErrorException(404, "Nie znaleziono filmu.");
        }

        var video = await _videoRepository.FindById(videoId);
        if (video == null || video.Status != VideoStatus.PUBLISHED)
        {
            throw new RequestErrorException(404, "Nie znaleziono filmu.");
        }

        if (video.AuthorId != user.Id)
        {
            throw new RequestErrorException(403, "Brak uprawnień");
        }

        var propositions = await _tagsPropositionRepository.FindByVideoId(videoId, page, size);
        var authorIds = propositions.Select(p => p.UserId).Distinct().ToList();
        var authors = await _userRepository.FindById(authorIds);
        var authorsDict = authors.ToDictionary(a => a.Id);

        var propositionDtos = propositions.Select(p => new TagsPropositionDto
        {
            Id = p.Id,
            VideoId = p.VideoId,
            Author = new AuthorDto
            {
                Id = p.UserId,
                Login = authorsDict[p.UserId].Login,
                ImageUrl = authorsDict[p.UserId].ImageUrl
            },
            Tags = p.Tags,
            Comment = p.Comment,
            CreatedAt = p.CreatedAt,
            ValidUntil = p.ValidUntil
        }).ToList();

        return propositionDtos;
    }

    public async Task RejectProposedTags(string sub, string tagsPropositionId)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401);
        }

        if (!ObjectId.TryParse(tagsPropositionId, out _))
        {
            throw new RequestErrorException(404, "Nie znaleziono propozycji.");
        }

        var tagsProposition = await _tagsPropositionRepository.FindById(tagsPropositionId);
        if (tagsProposition == null)
        {
            throw new RequestErrorException(404, "Nie znaleziono propozycji.");
        }

        var video = await _videoRepository.FindById(tagsProposition.VideoId);
        if (video == null || video.Status != VideoStatus.PUBLISHED)
        {
            throw new RequestErrorException(500, "Nie znaleziono filmu.");
        }

        if (video.AuthorId != user.Id)
        {
            throw new RequestErrorException(403, "Brak uprawnień.");
        }

        await _tagsPropositionRepository.DeleteById(tagsPropositionId);
        await _videoRepository.DecrementTagsPropositionsCount(video.Id);
    }

    public async Task AcceptProposedTags(string sub, string tagsPropositionId)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401);
        }

        if (!ObjectId.TryParse(tagsPropositionId, out _))
        {
            throw new RequestErrorException(404, "Nie znaleziono propozycji.");
        }

        var tagsProposition = await _tagsPropositionRepository.FindById(tagsPropositionId);
        if (tagsProposition == null)
        {
            throw new RequestErrorException(404, "Nie znaleziono propozycji.");
        }

        if (tagsProposition.ValidUntil <  DateTime.UtcNow)
        {
            throw new RequestErrorException(400, "Propozycja wygasła.");
        }

        var video = await _videoRepository.FindById(tagsProposition.VideoId);
        if (video == null || video.Status != VideoStatus.PUBLISHED)
        {
            throw new RequestErrorException(500, "Nie znaleziono filmu.");
        }

        if (video.AuthorId != user.Id)
        {
            throw new RequestErrorException(403, "Brak uprawnień.");
        }

        var result = video.Tags.Concat(tagsProposition.Tags).OrderBy(t => t).ToList();
        var duplicates = result
            .GroupBy(t => t)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count() > 0)
        {
            throw new RequestErrorException(400, $"Następujące tagi są już zapisane w filmie: {string.Join(", ", duplicates)}.");
        }

        if (result.Count() > 20)
        {
            throw new RequestErrorException(400, "Liczba proponowanych tagów + liczba już zapisanych tagów nie może przekraczać 20.");
        }

        video.Tags = result;
        video.TagsPropositionsCount -= 1;
        await _videoRepository.Replace(video.Id, video);

        await _tagsPropositionRepository.DeleteById(tagsPropositionId);


        _ = Task.Run(async () =>
        {
            var videoData = new
            {
                Title = video.Title,
                Description = video.Description,
                Category = video.Category.ToString(),
                Tags = video.Tags,
                AuthorLogin = sub
            };

            var json = JsonSerializer.Serialize(videoData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var client = new HttpClient();
            await client.PutAsync($"{recommendationsAPI}/update-video?id={video.Id}", content);
        });
    }

    public async Task<bool> HasUserProposedTags(string sub, string videoId)
    {
        var user = await _userRepository.FindByLogin(sub);
        if (user == null)
        {
            throw new RequestErrorException(401);
        }

        if (!ObjectId.TryParse(videoId, out _))
        {
            throw new RequestErrorException(404, "Nie znaleziono filmu.");
        }

        var video = await _videoRepository.FindById(videoId);
        if (video == null || video.Status != VideoStatus.PUBLISHED)
        {
            throw new RequestErrorException(404, "Nie znaleziono filmu.");
        }

        return await _tagsPropositionRepository.ExistsByVideoIdAndUserId(video.Id, user.Id);
    }
}
