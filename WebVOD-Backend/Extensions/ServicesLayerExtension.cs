using WebVOD_Backend.Services.Implementations;
using WebVOD_Backend.Services.Interfaces;

namespace WebVOD_Backend.Extensions;

public static class ServicesLayerExtension
{
    public static IServiceCollection ConfigureServicesLayer(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
