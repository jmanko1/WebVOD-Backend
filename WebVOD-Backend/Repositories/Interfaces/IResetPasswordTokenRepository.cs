using WebVOD_Backend.Model;

namespace WebVOD_Backend.Repositories.Interfaces;

public interface IResetPasswordTokenRepository
{
    Task Add(ResetPasswordToken token);
    Task RemoveByUserId(string userId);
    Task RemoveById(string id);
    Task<ResetPasswordToken> FindByToken(string token);
}
