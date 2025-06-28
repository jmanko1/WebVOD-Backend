using WebVOD_Backend.Services.Implementations;
using WebVOD_Backend.Services.Interfaces;

namespace WebVOD_Backend.Extensions;

public static class ServicesLayerExtension
{
    public static IServiceCollection ConfigureServicesLayer(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICryptoService, CryptoService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IFilesService, FilesService>();

        return services;
    }
}
