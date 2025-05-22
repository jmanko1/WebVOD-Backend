using WebVOD_Backend.Model;

namespace WebVOD_Backend.Repositories.Interfaces;

public interface IResetPasswordTokenRepository
{
    Task Add(ResetPasswordToken token);
    Task RemoveByUserId(string userId);
    Task<ResetPasswordToken> FindByToken(string token);
}
