using MongoDB.Driver;
using WebVOD_Backend.Services.Implementations;
using WebVOD_Backend.Services.Interfaces;

namespace WebVOD_Backend.Extensions;

public static class ServicesLayerExtension
{
    public static IServiceCollection ConfigureServicesLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICryptoService, CryptoService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IFilesService, FilesService>();
        services.AddScoped<IVideoService, VideoService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ICaptchaService, CaptchaService>();

        var mongoSettings = configuration.GetSection("MongoDB");
        var connectionString = mongoSettings["ConnectionString"];
        services.AddSingleton<IMongoClient>(sp => new MongoClient(connectionString));

        services.AddSingleton<MongoIndexInitializer>();

        return services;
    }
}
