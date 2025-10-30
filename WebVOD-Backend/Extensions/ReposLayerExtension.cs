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
        //services.AddScoped<IUserDeviceRepository, UserDeviceRepository>();
        services.AddScoped<IResetPasswordTokenRepository, ResetPasswordTokenRepository>();
        services.AddScoped<IVideoRepository, VideoRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ILikeRepository, LikeRepository>();
        services.AddScoped<IBlacklistedTokenRepository, BlacklistedTokenRepository>();
        services.AddScoped<IWatchingHistoryElementRepository, WatchingHistoryElementRepository>();
        services.AddScoped<ITagsPropositionRepository, TagsPropositionRepository>();        

        return services;
    }
}
