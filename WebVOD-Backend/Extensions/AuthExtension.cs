using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using WebVOD_Backend.Model;

namespace WebVOD_Backend.Extensions;

public static class AuthExtension
{
    public static IServiceCollection ConfigureAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(secretKey)
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var jti = context.Principal?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

                    if (string.IsNullOrEmpty(jti))
                    {
                        context.Fail("Token nie zawiera JTI.");
                        return;
                    }

                    var mongoClient = context.HttpContext.RequestServices.GetRequiredService<IMongoClient>();
                    var database = mongoClient.GetDatabase("WebVOD");
                    var collection = database.GetCollection<BlacklistedToken>("BlacklistedTokens");

                    var isRevoked = await collection.Find(x => x.Jti == jti).AnyAsync();
                    if (isRevoked)
                    {
                        context.Fail("Token został unieważniony.");
                    }
                }
            };
        });

        return services;
    }
}
