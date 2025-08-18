using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebVOD_Backend.Dtos.User;
using WebVOD_Backend.Dtos.Video;
using WebVOD_Backend.Exceptions;
using WebVOD_Backend.Model;
using WebVOD_Backend.Services.Interfaces;

namespace WebVOD_Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{login}")]
    public async Task<ActionResult<UserDto>> GetUserByLogin(string login)
    {
        try
        {
            var user = await _userService.GetUserByLogin(login);
            return Ok(user);
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [Authorize]
    [HttpGet("my-profile")]
    public async Task<ActionResult<UserDto>> GetMyProfile()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (sub == null)
        {
            return Unauthorized();
        }

        try
        {
            var user = await _userService.GetMyProfile(sub);
            return Ok(user);
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [Authorize]
    [HttpGet("my-profile/email")]
    public async Task<ActionResult<string>> GetMyEmail()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (sub == null)
        {
            return Unauthorized();
        }

        try
        {
            var email = await _userService.GetMyEmail(sub);
            return Ok(email);
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [Authorize]
    [HttpPut("my-profile/description")]
    public async Task<ActionResult<string>> UpdateDescription(
        [FromBody]
        [Required(ErrorMessage = "Podaj opis.")]
        [MinLength(1, ErrorMessage = "Podaj opis.")]
        [MaxLength(1000, ErrorMessage = "Opis kanału może mieć maksymalnie 1000 znaków.")]
        string description
    )
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (sub == null)
        {
            return Unauthorized();
        }

        try
        {
            await _userService.UpdateDescription(sub, description);
            return Ok("Opis kanału został zaktualizowany.");
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [Authorize]
    [HttpPut("my-profile/image")]
    [RequestSizeLimit(1048576)] // 1 MB
    public async Task<ActionResult<string>> UpdateImage([FromForm] IFormFile image)
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (sub == null)
        {
            return Unauthorized();
        }

        try
        {
            var imageUrl = await _userService.UpdateImage(sub, image);
            return Ok(imageUrl);
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [Authorize]
    [HttpGet("my-profile/is-tfa-required")]
    public async Task<ActionResult<bool>> IsTFARequired()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (sub == null)
        {
            return Unauthorized();
        }

        try
        {
            var isTFARequired = await _userService.IsTFARequired(sub);
            return Ok(isTFARequired);
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [Authorize]
    [HttpPut("my-profile/change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (sub == null)
        {
            return Unauthorized();
        }

        try
        {
            await _userService.ChangePassword(sub, changePasswordDto);
            return Ok("Hasło zostało zmienione.");
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [Authorize]
    [HttpGet("my-profile/tfa-qr-code")]
    public async Task<ActionResult> GetTFAQrCode()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (sub == null)
        {
            return Unauthorized();
        }

        var result = await _userService.GetTFAQrCode(sub);
        if(result == null)
        {
            return NoContent();
        }

        return Ok(result);
    }

    [Authorize]
    [HttpPut("my-profile/toggle-tfa")]
    public async Task<ActionResult> ToggleTFA([FromBody] ToggleTFADto toggleTFADto)
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (sub == null)
        {
            return Unauthorized();
        }

        try
        {
            await _userService.ToggleTFA(sub, toggleTFADto);
            return Ok("Uwierzytelnianie dwuskładnikowe zostało skonfigurowane.");
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [HttpGet("{login}/videos")]
    public async Task<ActionResult<List<UserVideoDto>>> GetUserVideos(string login, int page = 1, int size = 10)
    {
        try
        {
            var videos = await _userService.GetUserVideos(login, page, size);
            return Ok(videos);
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [Authorize]
    [HttpGet("my-profile/videos")]
    public async Task<ActionResult<List<UserVideoDto>>> GetMyVideos(int page = 1, int size = 10, string? search = null)
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (sub == null)
        {
            return Unauthorized();
        }

        try
        {
            var videos = await _userService.GetMyVideos(sub, page, size, search);
            return Ok(videos);
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [Authorize]
    [HttpGet("my-profile/liked-videos")]
    public async Task<ActionResult<List<UserVideoActivityDto>>> GetLikedVideos(int page = 1, int size = 10)
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (sub == null)
        {
            return Unauthorized();
        }

        try
        {
            var videos = await _userService.GetLikedVideos(sub, page, size);
            return Ok(videos);
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [Authorize]
    [HttpGet("my-profile/watched-videos")]
    public async Task<ActionResult<List<UserVideoActivityDto>>> GetViewedVideos(int page = 1, int size = 10)
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (sub == null)
        {
            return Unauthorized();
        }

        try
        {
            var videos = await _userService.GetViewedVideos(sub, page, size);
            return Ok(videos);
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<User>>> GetAll()
    {
        var users = await _userService.GetAll();
        return Ok(users);
    }
}
