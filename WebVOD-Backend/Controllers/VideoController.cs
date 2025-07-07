using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebVOD_Backend.Dtos.Comment;
using WebVOD_Backend.Dtos.Video;
using WebVOD_Backend.Exceptions;
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
        try
        {
            var video = await _videoService.GetVideoById(id);
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
}
