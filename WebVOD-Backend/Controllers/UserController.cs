using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebVOD_Backend.Dtos.User;
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

    [HttpGet]
    public async Task<ActionResult<List<User>>> GetAll()
    {
        var users = await _userService.GetAll();
        return Ok(users);
    }
}
