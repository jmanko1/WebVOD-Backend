using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebVOD_Backend.Services.Interfaces;

namespace WebVOD_Backend.Services.Implementations;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private IConfigurationSection JwtSettings => _configuration.GetSection("JwtSettings");


    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateJwtToken(string login)
    {
        var secretKey = Encoding.UTF8.GetBytes(JwtSettings["SecretKey"]);
        var lifetime = JwtSettings.GetValue<int>("Lifetime");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, login),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var credentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: JwtSettings["Issuer"],
            audience: JwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(lifetime),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public int GetExpiresIn()
    {
        var lifetime = JwtSettings.GetValue<int>("Lifetime");

        return lifetime;
    }
}
