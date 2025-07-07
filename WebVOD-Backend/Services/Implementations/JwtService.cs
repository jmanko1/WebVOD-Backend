using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using WebVOD_Backend.Repositories.Interfaces;
using WebVOD_Backend.Services.Interfaces;

namespace WebVOD_Backend.Services.Implementations;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private IConfigurationSection JwtSettings => _configuration.GetSection("JwtSettings");
    private readonly IBlacklistedTokenRepository _blacklistedTokenRepository;

    public JwtService(IConfiguration configuration, IBlacklistedTokenRepository blacklistedTokenRepository)
    {
        _configuration = configuration;
        _blacklistedTokenRepository = blacklistedTokenRepository;
    }

    public string GenerateAccessToken(string login)
    {
        var secretKey = Encoding.UTF8.GetBytes(JwtSettings["SecretKey"]);
        var lifetime = JwtSettings.GetValue<int>("AccessTokenLifetime");

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

    public int GetAccessTokenLifetime()
    {
        var lifetime = JwtSettings.GetValue<int>("AccessTokenLifetime");

        return lifetime;
    }

    public string GenerateRefreshToken(string login)
    {
        var secretKey = Encoding.UTF8.GetBytes(JwtSettings["SecretKey"]);
        var lifetime = JwtSettings.GetValue<int>("RefreshTokenLifetime");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, login),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim("type", "refresh")
        };

        var credentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: JwtSettings["Issuer"],
            audience: JwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(lifetime),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public int GetRefreshTokenLifetime()
    {
        var lifetime = JwtSettings.GetValue<int>("RefreshTokenLifetime");

        return lifetime;
    }

    public async Task<ClaimsPrincipal?> ValidateRefreshToken(string refreshToken)
    {
        var secretKey = Encoding.UTF8.GetBytes(JwtSettings["SecretKey"]);

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = JwtSettings["Issuer"],
            ValidAudience = JwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(secretKey)
        };

        try
        {
            var principal = tokenHandler.ValidateToken(refreshToken, validationParameters, out SecurityToken validatedToken);

            var tokenType = principal.FindFirst("type")?.Value;
            if (tokenType != null && tokenType != "refresh")
            {
                return null;
            }

            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (string.IsNullOrEmpty(jti))
            {
                return null;
            }

            if (await _blacklistedTokenRepository.ExistsByJti(jti))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    public string? GetJti(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(jwt);

        var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

        return jti;
    }

    public DateTime GetExpiresAt(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(jwt);
        var expires = jwtToken.ValidTo;

        return expires;
    }
}
