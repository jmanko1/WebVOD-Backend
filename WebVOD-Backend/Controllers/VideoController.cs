using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebVOD_Backend.Dtos.Comment;
using WebVOD_Backend.Dtos.Video;
using WebVOD_Backend.Exceptions;
using WebVOD_Backend.Model;
using WebVOD_Backend.Services.Interfaces;

namespace WebVOD_Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class VideoController : ControllerBase
{
    private readonly IVideoService _videoService;

    public VideoController(IVideoService videoService)
    {
        _videoService = videoService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<VideoDto>> GetVideoById(string id)
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        try
        {
            var video = await _videoService.GetVideoById(sub, id);
            return Ok(video);
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [HttpGet("{id}/comments")]
    public async Task<ActionResult<List<CommentDto>>> GetVideoComments(string id, int page = 1, int size = 10)
    {
        try
        {
            var comments = await _videoService.GetVideoComments(id, page, size);
            return Ok(comments);
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [Authorize]
    [HttpGet("{id}/like")]
    public async Task<ActionResult<bool>> IsVideoLiked(string id)
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (sub == null)
        {
            return Unauthorized();
        }

        try
        {
            var isVideoLiked = await _videoService.IsVideoLiked(sub, id);
            return Ok(isVideoLiked);
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [Authorize]
    [HttpPost("{id}/like")]
    public async Task<ActionResult> LikeVideo(string id)
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (sub == null)
        {
            return Unauthorized();
        }

        try
        {
            await _videoService.LikeVideo(sub, id);
            return StatusCode(201);
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [Authorize]
    [HttpDelete("{id}/like")]
    public async Task<ActionResult> CancelLikeVideo(string id)
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (sub == null)
        {
            return Unauthorized();
        }

        try
        {
            await _videoService.CancelLikeVideo(sub, id);
            return Ok("Anulowano polubienie filmu.");
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [Authorize]
    [HttpPost("new")]
    public async Task<ActionResult<string>> CreateNewVideo([FromBody] CreateVideoDto createVideoDto)
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (sub == null)
        {
            return Unauthorized();
        }

        try
        {
            var videoId = await _videoService.CreateNewVideo(sub, createVideoDto);
            HttpContext.Session.SetString("UploadingSub", sub);

            return Ok(videoId);
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [HttpPost("chunk")]
    [RequestSizeLimit(6291456)] // 6 MB
    public async Task<ActionResult<string>> UploadChunk([FromForm] IFormFile videoChunk)
    {
        var sub = HttpContext.Session.GetString("UploadingSub");
        if (sub == null)
        {
            return Unauthorized();    
        }

        var videoId = Request.Headers["Video-Id"].ToString();
        var currentChunkIndex = Request.Headers["Chunk-Index"].ToString();
        var totalChunks = Request.Headers["Total-Chunks"].ToString();

        try
        {
            await _videoService.UploadChunk(sub, videoChunk, videoId, currentChunkIndex, totalChunks);
            HttpContext.Session.SetString("UploadingSub", sub);

            return Ok("Fragment filmu został pomyślnie przesłany.");
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [Authorize]
    [HttpPut("{id}/thumbnail")]
    [RequestSizeLimit(2097152)] // 2 MB
    public async Task<ActionResult<string>> SetThumbnail(string id, [FromForm] IFormFile thumbnail)
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (sub == null)
        {
            return Unauthorized();
        }

        try
        {
            await _videoService.UpdateThumbnail(sub, id, thumbnail);

            return Ok("Miniatura została ustawiona.");
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories()
    {
        var categories = Enum.GetNames(typeof(VideoCategory)).ToList();
        return Ok(categories);
    }

    [Authorize]
    [HttpGet("{id}/update")]
    public async Task<ActionResult<VideoToUpdateDto>> GetVideoToUpdateById(string id)
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (sub == null)
        {
            return Unauthorized();
        }

        try
        {
            var video = await _videoService.GetVideoToUpdateById(sub, id);

            return Ok(video);
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [Authorize]
    [HttpPut("{id}/update")]
    public async Task<ActionResult> UpdateVideoById(string id, [FromBody] UpdateVideoDto updateVideoDto)
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (sub == null)
        {
            return Unauthorized();
        }

        try
        {
            await _videoService.UpdateVideoById(sub, id, updateVideoDto);

            return Ok("Film został zaktualizowany.");
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteVideoById(string id)
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (sub == null)
        {
            return Unauthorized();
        }

        try
        {
            await _videoService.DeleteVideoById(sub, id);

            return Ok("Film został usunięty.");
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }
}
