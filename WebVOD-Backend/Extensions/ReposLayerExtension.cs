using WebVOD_Backend.Repositories.Implementations;
using WebVOD_Backend.Repositories.Interfaces;

namespace WebVOD_Backend.Extensions;

public static class ReposLayerExtension
{
    public static IServiceCollection ConfigureReposLayer(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IFailedLoginLogRepository, FailedLoginLogRepository>();
        services.AddScoped<IUserBlockadeRepository, UserBlockadeRepository>();

        return services;
    }
}
