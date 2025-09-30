using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
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
            var response = await _authService.Authenticate(loginDto, HttpContext, Request, Response);

            return Ok(response);
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [HttpPost("Login/Code")]
    public async Task<ActionResult<LoginResponseDto>> Code(
        [FromBody]
        [Required(ErrorMessage = "Podaj kod.")]
        [RegularExpression("^[0-9]{6}$", ErrorMessage = "Podaj prawidłowy 6-cyfrowy kod.")]
        string code
    )
    {
        try
        {
            var response = await _authService.Code(code, HttpContext, Request, Response);
            return Ok(response);
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message});
        }
    }

    [HttpPost("Refresh")]
    public async Task<ActionResult<LoginResponseDto>> Refresh()
    {
        var refreshToken = Request.Cookies["refreshToken"];

        try
        {
            var response = await _authService.Refresh(refreshToken);
            return Ok(response);
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

    [HttpPost("Reset-Password")]
    public async Task<ActionResult> InitiateResetPassword([FromBody] InitiateResetPasswordDto initiateResetPasswordDto)
    {
        try
        {
            await _authService.InitiateResetPassword(initiateResetPasswordDto);
            return Ok("Wysłano maila z linkiem resetującym hasło na adres " + initiateResetPasswordDto.Email + ".");
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [HttpPut("Reset-Password/Complete")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
    {
        try
        {
            await _authService.ResetPassword(resetPasswordDto);
            return Ok("Hasło zostało zresetowane.");
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }

    [Authorize]
    [HttpPost("Logout")]
    public async Task<ActionResult> Logout()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return BadRequest("Brak lub nieprawidłowy nagłówek Authorization.");
        }

        var accessToken = authHeader.Substring("Bearer ".Length);
        var refreshToken = Request.Cookies["refreshToken"];

        await _authService.Logout(accessToken, refreshToken);

        Response.Cookies.Delete("refreshToken");
        return Ok("Wylogowano.");
    }
}
