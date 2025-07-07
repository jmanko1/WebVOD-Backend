using WebVOD_Backend.Model;

namespace WebVOD_Backend.Repositories.Interfaces;

public interface IBlacklistedTokenRepository
{
    Task Add(BlacklistedToken jwt);
    Task<bool> ExistsByJti(string jti);
}
