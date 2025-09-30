using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
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
    private readonly IBlacklistedTokenRepository _blacklistedTokenRepository;
    private readonly IEmailService _emailService;
    private readonly ICaptchaService _captchaService;

    private const int MaxTimeSpanMinutes = 1;
    private const int MaxFailedAttempts = 5;
    private const int TFASessionLifetime = 5;
    private const int MaxTFACodeAttempts = 2;
    private const string recommendationsAPI = "http://localhost:5000";


    public AuthService(IUserRepository userRepository, IFailedLoginLogRepository failedLoginLogRepository, IUserBlockadeRepository blockadeRepository, ICryptoService cryptoService, IJwtService jwtService, IResetPasswordTokenRepository resetPasswordTokenRepository, IBlacklistedTokenRepository blacklistedTokenRepository, IEmailService emailService, ICaptchaService captchaService)
    {
        _userRepository = userRepository;
        _failedLoginLogRepository = failedLoginLogRepository;
        _blockadeRepository = blockadeRepository;
        _cryptoService = cryptoService;
        _jwtService = jwtService;
        _resetPasswordTokenRepository = resetPasswordTokenRepository;
        _blacklistedTokenRepository = blacklistedTokenRepository;
        _emailService = emailService;
        _captchaService = captchaService;
    }

    public async Task<LoginResponseDto> Authenticate(LoginDto loginDto, HttpContext httpContext, HttpRequest httpRequest, HttpResponse httpResponse)
    {
        ClearTfaSession(httpContext);

        var user = await GetUserByLoginSecurely(loginDto.Login, loginDto.Password);
        var sourceIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Nieznany";
        var sourceDevice = httpRequest.Headers["User-Agent"].ToString();

        await EnsureAccountNotBlocked(user.Id, sourceIP);

        if (!_cryptoService.VerifyPassword(loginDto.Password, user.Password))
        {
            await HandleFailedLogin(user.Id, sourceIP, sourceDevice);
            throw new RequestErrorException(401, "Niepoprawny login lub hasło.");
        }

        if (!await _captchaService.VerifyCaptchaToken(loginDto.CaptchaToken))
        {
            throw new RequestErrorException(400, "Potwierdź, że nie jesteś robotem.");
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

    private async Task EnsureAccountNotBlocked(string userId, string sourceIP)
    {
        if (await _blockadeRepository.ExistsByUserIdAndSourceIP(userId, sourceIP))
        {
            var dummyHash = "uWkxn6xDKyoHq0vH1PCZnpuhF0r5NrsBuxybA1u3J";
            var password = "abc123";
            _cryptoService.VerifyPassword(password, dummyHash);
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
                UserId = userId,
                SourceIP = ip
            };

            await _blockadeRepository.Add(blockade);
        }
    }

    private void SaveTfaSession(HttpContext context, string userId, bool checkedSave)
    {
        context.Session.SetString("auth_userId", userId);
        context.Session.SetString("auth_dateUntil", DateTime.UtcNow.AddMinutes(TFASessionLifetime).ToString("o", CultureInfo.InvariantCulture));
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

        if (await _userRepository.ExistsByLogin(registerDto.Login))
        {
            throw new RequestErrorException(400, "Ten login jest zajęty.");
        }

        if (await _userRepository.ExistsByEmail(registerDto.Email))
        {
            throw new RequestErrorException(400, "Ten adres email jest zajęty.");
        }

        if (!await _captchaService.VerifyCaptchaToken(registerDto.CaptchaToken))
        {
            throw new RequestErrorException(400, "Potwierdź, że nie jesteś robotem.");
        }

        //var otpKey = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));

        //var user = new User
        //{
        //    Login = registerDto.Login,
        //    Email = registerDto.Email,
        //    Password = _cryptoService.HashPassword(registerDto.Password),
        //    TOTPKey = _cryptoService.Encrypt(otpKey)
        //};

        //await _userRepository.Add(user);

        //var templatePath = Path.Combine("Templates", "WelcomeEmail.html");
        //var htmlBody = File.ReadAllText(templatePath);
        //htmlBody = htmlBody.Replace("[USER]", user.Login)
        //                    .Replace("[LOGIN_URL]", "http://localhost:3000/login");
        //var subject = "Potwierdzenie rejestracji w WebVOD";

        //await _emailService.SendEmail(user.Email, subject, htmlBody);

        //_ = Task.Run(async () =>
        //{
        //    var userData = new
        //    {
        //        Id = user.Id,
        //        Login = user.Login
        //    };

        //    var json = JsonSerializer.Serialize(userData);
        //    var content = new StringContent(json, Encoding.UTF8, "application/json");

        //    using var client = new HttpClient();

        //    await client.PostAsync($"{recommendationsAPI}/add-channel", content);
        //});
    }

    public async Task<LoginResponseDto> Code(string code, HttpContext httpContext, HttpRequest httpRequest, HttpResponse httpResponse)
    {
        var userId = httpContext.Session.GetString("auth_userId");
        if (userId == null)
        {
            throw new RequestErrorException(401, "Logowanie nie zostało zainicjalizowane.");
        }

        var dateUntil = httpContext.Session.GetString("auth_dateUntil");

        if (DateTime.TryParseExact(dateUntil, "o", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime result))
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
        if (user == null)
        {
            throw new RequestErrorException(401, "Rozpocznij proces logowania od nowa.");
        }

        var sourceIP = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Nieznany";
        var sourceDevice = httpRequest.Headers["User-Agent"].ToString();

        await EnsureAccountNotBlocked(user.Id, sourceIP);

        var secretKey = _cryptoService.Decrypt(user.TOTPKey);

        if (!ValidateTotp(secretKey, code))
        {
            var attempts = (httpContext.Session.GetInt32("auth_attempts") ?? 0) + 1;
            if (attempts >= MaxTFACodeAttempts)
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

    public async Task InitiateResetPassword(InitiateResetPasswordDto initiateResetPasswordDto)
    {
        if (!await _captchaService.VerifyCaptchaToken(initiateResetPasswordDto.CaptchaToken))
        {
            throw new RequestErrorException(400, "Potwierdź, że nie jesteś robotem.");
        }

        var user = await _userRepository.FindByEmail(initiateResetPasswordDto.Email);
        if(user == null)
        {
            return;
        }

        await _resetPasswordTokenRepository.RemoveByUserId(user.Id);

        var token = _cryptoService.GenerateResetPasswordToken(155);
        var resetPasswordToken = new ResetPasswordToken
        {
            UserId = user.Id,
            Token = _cryptoService.Sha256Hash(token)
        };

        await _resetPasswordTokenRepository.Add(resetPasswordToken);
        Console.WriteLine(token);

        //var templatePath = Path.Combine("Templates", "ResetPasswordEmail.html");
        //var htmlBody = File.ReadAllText(templatePath);
        //htmlBody = htmlBody.Replace("[USER]", user.Login)
        //                   .Replace("[RESET_URL]", $"http://localhost:3000/reset-password/{token}")
        //                   .Replace("[CZAS]", "20");
        //var subject = "Resetowanie hasła w WebVOD";

        //await _emailService.SendEmail(user.Email, subject, htmlBody);
    }

    public async Task ResetPassword(ResetPasswordDto resetPasswordDto)
    {
        var sha256Token = _cryptoService.Sha256Hash(resetPasswordDto.Token);
        var resetPasswordToken = await _resetPasswordTokenRepository.FindByToken(sha256Token);
        if(resetPasswordToken == null)
        {
            throw new RequestErrorException(401, "Nieprawidłowy link resetujący hasło.");
        }

        if(resetPasswordToken.ValidUntil < DateTime.UtcNow)
        {
            throw new RequestErrorException(401, "Link wygasł. Rozpocznij od nowa proces resetowania hasła.");
        }

        var userExists = await _userRepository.ExistsById(resetPasswordToken.UserId);
        if(!userExists)
        {
            await _resetPasswordTokenRepository.RemoveById(resetPasswordToken.Id);
            throw new RequestErrorException(401, "Nieprawidłowy link resetujący hasło.");
        }

        var newPassword = _cryptoService.HashPassword(resetPasswordDto.Password);
        await _userRepository.ChangePassword(resetPasswordToken.UserId, newPassword);
        await _resetPasswordTokenRepository.RemoveById(resetPasswordToken.Id);
    }

    public async Task<LoginResponseDto> Refresh(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new RequestErrorException(401);
        }

        var principal = await _jwtService.ValidateRefreshToken(refreshToken);
        if (principal == null)
        {
            throw new RequestErrorException(401);
        }

        var login = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(login))
        {
            throw new RequestErrorException(401);
        }

        var userExists = await _userRepository.ExistsByLogin(login);
        if (!userExists)
        {
            throw new RequestErrorException(401);
        }

        return GenerateLoginResponse(login);
    }

    public async Task Logout(string accessToken, string refreshToken)
    {
        var accessJti = _jwtService.GetJti(accessToken);
        var accessTokenExpiresAt = _jwtService.GetExpiresAt(accessToken);
        var revokedAccessToken = new BlacklistedToken
        {
            Jti = accessJti,
            ExpiresAt = accessTokenExpiresAt
        };

        await _blacklistedTokenRepository.Add(revokedAccessToken);

        if (!string.IsNullOrEmpty(refreshToken))
        {
            var principal = await _jwtService.ValidateRefreshToken(refreshToken);
            if (principal == null)
            {
                throw new RequestErrorException(401);
            }

            var refreshJti = _jwtService.GetJti(refreshToken);
            var refreshTokenExpiresAt = _jwtService.GetExpiresAt(refreshToken);
            var revokedRefreshToken = new BlacklistedToken
            {
                Jti = refreshJti,
                ExpiresAt = refreshTokenExpiresAt
            };

            await _blacklistedTokenRepository.Add(revokedRefreshToken);
        }
    }
}
