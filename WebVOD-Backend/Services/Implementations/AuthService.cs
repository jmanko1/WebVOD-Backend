using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using OtpNet;
using WebVOD_Backend.Dtos.Auth;
using WebVOD_Backend.Exceptions;
using WebVOD_Backend.Model;
using WebVOD_Backend.Repositories.Interfaces;
using WebVOD_Backend.Services.Interfaces;

namespace WebVOD_Backend.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IFailedLoginLogRepository _failedLoginLogRepository;
    private readonly IUserBlockadeRepository _blockadeRepository;
    private readonly ICryptoService _cryptoService;
    private readonly IJwtService _jwtService;
    private readonly IResetPasswordTokenRepository _resetPasswordTokenRepository;

    private const int MaxTimeSpanMinutes = 1;
    private const int MaxFailedAttempts = 5;
    private const int TFASessionLifetime = 5;
    private const int MaxTFACodeAttempts = 2;


    public AuthService(IUserRepository userRepository, IFailedLoginLogRepository failedLoginLogRepository, IUserBlockadeRepository blockadeRepository, ICryptoService cryptoService, IJwtService jwtService, IResetPasswordTokenRepository resetPasswordTokenRepository)
    {
        _userRepository = userRepository;
        _failedLoginLogRepository = failedLoginLogRepository;
        _blockadeRepository = blockadeRepository;
        _cryptoService = cryptoService;
        _jwtService = jwtService;
        _resetPasswordTokenRepository = resetPasswordTokenRepository;
    }

    public async Task<LoginResponseDto> Authenticate(LoginDto loginDto, HttpContext httpContext, HttpRequest httpRequest, HttpResponse httpResponse)
    {
        var user = await GetUserByLoginSecurely(loginDto.Login, loginDto.Password);

        await EnsureAccountNotBlocked(user.Id);

        var sourceIP = httpContext.Connection.RemoteIpAddress.ToString();
        var sourceDevice = httpRequest.Headers["User-Agent"].ToString();

        if (!_cryptoService.VerifyPassword(loginDto.Password, user.Password))
        {
            await HandleFailedLogin(user.Id, sourceIP, sourceDevice);
            throw new RequestErrorException(401, "Niepoprawny login lub hasło.");
        }
        
        if (!user.IsTFAEnabled)
        {
            if(loginDto.CheckedSave)
            {
                SaveRefreshTokenCookie(httpResponse, user.Login);
            }

            return GenerateLoginResponse(user.Login);
        }

        SaveTfaSession(httpContext, user.Id, loginDto.CheckedSave);
        return GenerateTFAResponse();
    }

    private void SaveRefreshTokenCookie(HttpResponse httpResponse, string login)
    {
        var refreshToken = _jwtService.GenerateRefreshToken(login);
        httpResponse.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(_jwtService.GetRefreshTokenLifetime())
        });
    }

    private async Task<User> GetUserByLoginSecurely(string login, string password)
    {
        var user = await _userRepository.FindByLogin(login);
        var dummyHash = "uWkxn6xDKyoHq0vH1PCZnpuhF0r5NrsBuxybA1u3J";

        if (user == null)
        {
            _cryptoService.VerifyPassword(password, dummyHash);
            throw new RequestErrorException(401, "Nieprawidłowy login lub hasło.");
        }

        return user;
    }

    private async Task EnsureAccountNotBlocked(string userId)
    {
        if (await _blockadeRepository.ExistsByUserId(userId))
        {
            throw new RequestErrorException(403, "To konto jest w tej chwili zablokowane. Spróbuj ponownie później.");
        }
    }

    private async Task HandleFailedLogin(string userId, string ip, string device)
    {
        var log = new FailedLoginLog
        {
            UserId = userId,
            SourceIP = ip,
            SourceDevice = device
        };

        await _failedLoginLogRepository.Add(log);

        var failedAttempts = await _failedLoginLogRepository.CountFailedAttempts(ip, userId, TimeSpan.FromMinutes(MaxTimeSpanMinutes));
        if (failedAttempts >= MaxFailedAttempts)
        {
            var blockade = new UserBlockade
            {
                UserId = userId
            };

            await _blockadeRepository.Add(blockade);
        }
    }

    private void SaveTfaSession(HttpContext context, string userId, bool checkedSave)
    {
        context.Session.SetString("auth_userId", userId);
        context.Session.SetString("auth_dateUntil", DateTime.UtcNow.AddMinutes(TFASessionLifetime).ToString());
        context.Session.SetInt32("auth_attempts", 0);
        context.Session.SetInt32("auth_checkedSave", checkedSave ? 1 : 0);
    }

    private LoginResponseDto GenerateLoginResponse(string login)
    {
        var token = _jwtService.GenerateAccessToken(login);
        return new LoginResponseDto
        {
            Token = token,
            ExpiresIn = _jwtService.GetAccessTokenLifetime(),
            TFARequired = false
        };
    }

    private LoginResponseDto GenerateTFAResponse()
    {
        return new LoginResponseDto
        {
            TFARequired = true
        };
    }

    public async Task Register(RegisterDto registerDto)
    {
        if(await _userRepository.ExistsByLogin(registerDto.Login))
        {
            throw new RequestErrorException(400, "Ten login jest zajęty.");
        }

        if (await _userRepository.ExistsByEmail(registerDto.Email))
        {
            throw new RequestErrorException(400, "Ten adres email jest zajęty.");
        }

        var otpKey = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));

        var user = new User
        {
            Login = registerDto.Login,
            Email = registerDto.Email,
            Password = _cryptoService.HashPassword(registerDto.Password),
            TOTPKey = _cryptoService.Encrypt(otpKey)
        };

        await _userRepository.Add(user);
    }

    public async Task<LoginResponseDto> Code(string code, HttpContext httpContext, HttpRequest httpRequest, HttpResponse httpResponse)
    {
        var userId = httpContext.Session.GetString("auth_userId");
        if (userId == null)
        {
            throw new RequestErrorException(401, "Logowanie nie zostało zainicjalizowane.");
        }

        var dateUntil = httpContext.Session.GetString("auth_dateUntil");

        if (DateTime.TryParse(dateUntil, out DateTime result))
        {
            if(result < DateTime.UtcNow)
            {
                throw new RequestErrorException(401, "Czas na podanie kodu minął. Rozpocznij proces logowania od nowa.");
            }
        }
        else
        {
            throw new RequestErrorException(401, "Rozpocznij proces logowania od nowa.");
        }

        var user = await _userRepository.FindById(userId);
        if(user == null)
        {
            throw new RequestErrorException(401, "Użytkownik nie istnieje. Rozpocznij proces logowania od nowa.");
        }

        await EnsureAccountNotBlocked(user.Id);

        var sourceIP = httpContext.Connection.RemoteIpAddress?.ToString();
        var sourceDevice = httpRequest.Headers["User-Agent"].ToString();

        var secretKey = _cryptoService.Decrypt(user.TOTPKey);

        if (!ValidateTotp(secretKey, code))
        {
            var attempts = (int)httpContext.Session.GetInt32("auth_attempts") + 1;
            if(attempts >= MaxTFACodeAttempts)
            {
                ClearTfaSession(httpContext);

                await HandleFailedLogin(user.Id, sourceIP, sourceDevice);

                throw new RequestErrorException(401, "Przekroczono liczbę prób. Rozpocznij proces logowania od nowa.");
            }

            httpContext.Session.SetInt32("auth_attempts", attempts);
            throw new RequestErrorException(401, "Niepoprawny kod.");
        }

        if(httpContext.Session.GetInt32("auth_checkedSave") == 1)
        {
            SaveRefreshTokenCookie(httpResponse, user.Login);
        }

        ClearTfaSession(httpContext);

        return GenerateLoginResponse(user.Login);
    }

    private void ClearTfaSession(HttpContext context)
    {
        context.Session.Remove("auth_userId");
        context.Session.Remove("auth_dateUntil");
        context.Session.Remove("auth_attempts");
        context.Session.Remove("auth_checkedSave");
    }

    private bool ValidateTotp(string secret, string userInput)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(userInput, out _, new VerificationWindow(previous: 1, future: 0));
    }

    public async Task InitiateResetPassword(string email)
    {
        var user = await _userRepository.FindByEmail(email);
        if(user == null)
        {
            throw new RequestErrorException(401, "Podany adres email nie jest zarejestrowany.");
        }

        await _resetPasswordTokenRepository.RemoveByUserId(user.Id);

        var token = _cryptoService.GenerateResetPasswordToken(155);
        Console.WriteLine(token);
        var resetPasswordToken = new ResetPasswordToken
        {
            UserId = user.Id,
            Token = _cryptoService.Sha256Hash(token)
        };

        await _resetPasswordTokenRepository.Add(resetPasswordToken);
    }

    public async Task ResetPassword(ResetPasswordDto resetPasswordDto)
    {
        var sha256Token = _cryptoService.Sha256Hash(resetPasswordDto.Token);
        var resetPasswordToken = await _resetPasswordTokenRepository.FindByToken(sha256Token);
        if(resetPasswordToken == null)
        {
            throw new RequestErrorException(401, "Nieprawidłowy token.");
        }

        if(resetPasswordToken.ValidUntil < DateTime.UtcNow)
        {
            await _resetPasswordTokenRepository.RemoveById(resetPasswordToken.Id);
            throw new RequestErrorException(401, "Token wygasł. Rozpocznij od nowa proces resetowania hasła.");
        }

        var userExists = await _userRepository.ExistsById(resetPasswordToken.UserId);
        if(!userExists)
        {
            await _resetPasswordTokenRepository.RemoveById(resetPasswordToken.Id);
            throw new RequestErrorException(401, "Nieprawidłowy token.");
        }

        var newPassword = _cryptoService.HashPassword(resetPasswordDto.Password);
        await _userRepository.ChangePassword(resetPasswordToken.UserId, newPassword);
        await _resetPasswordTokenRepository.RemoveById(resetPasswordToken.Id);
    }

    public async Task<LoginResponseDto> Refresh(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new RequestErrorException(401, "Brak tokenu.");
        }

        var principal = _jwtService.ValidateRefreshToken(refreshToken);
        if (principal == null)
        {
            throw new RequestErrorException(401, "Nieprawidłowy token.");
        }

        var login = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(login))
        {
            throw new RequestErrorException(401, "Nieprawidłowy token.");
        }

        var userExists = await _userRepository.ExistsByLogin(login);
        if (!userExists)
        {
            throw new RequestErrorException(401, "Użytkownik nie istnieje.");
        }

        return GenerateLoginResponse(login);
    }
}
