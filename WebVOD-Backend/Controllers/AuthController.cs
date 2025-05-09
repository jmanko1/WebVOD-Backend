using Microsoft.AspNetCore.Mvc;
using WebVOD_Backend.Dtos.Auth;
using WebVOD_Backend.Exceptions;
using WebVOD_Backend.Services.Interfaces;

namespace WebVOD_Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("Login")]
    public async Task<ActionResult<LoginResponseDto>> Authenticate([FromBody] LoginDto loginDto)
    {
        try
        {
            var response = await _authService.Authenticate(loginDto, HttpContext, Request);
            
            if(response.StatusCode == 200)
            {
                return Ok(response);
            }

            return Redirect(response.RedirectUrl);
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [HttpPost("Register")]
    public async Task<ActionResult> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            await _authService.Register(registerDto);
            return StatusCode(201);
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }
}
