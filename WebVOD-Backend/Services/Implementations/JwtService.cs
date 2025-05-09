using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebVOD_Backend.Services.Interfaces;

namespace WebVOD_Backend.Services.Implementations;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateJwtToken(string userId)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]);
        var lifetime = jwtSettings.GetValue<int>("Lifetime");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.Now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var credentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(lifetime),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public int GetExpiresIn()
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var lifetime = jwtSettings.GetValue<int>("Lifetime");

        return lifetime;
    }
}
