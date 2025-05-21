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
    private readonly IUserDeviceRepository _userDeviceRepository;

    public AuthService(IUserRepository userRepository, IFailedLoginLogRepository failedLoginLogRepository, IUserBlockadeRepository blockadeRepository, ICryptoService cryptoService, IJwtService jwtService, IUserDeviceRepository userDeviceRepository)
    {
        _userRepository = userRepository;
        _failedLoginLogRepository = failedLoginLogRepository;
        _blockadeRepository = blockadeRepository;
        _cryptoService = cryptoService;
        _jwtService = jwtService;
        _userDeviceRepository = userDeviceRepository;
    }

    public async Task<LoginResponseDto> Authenticate(LoginDto loginDto, HttpContext httpContext, HttpRequest httpRequest)
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

        var savedDevice = await _userDeviceRepository.FindByNameAndUserId(sourceDevice, user.Id);

        if (!user.IsTFAEnabled)
        {
            await HandleTrustedDevice(savedDevice, sourceDevice, user.Id);
            return GenerateLoginResponse(user.Id);
        }

        if (IsTfaRequired(user, savedDevice))
        {
            SaveTfaSession(httpContext, user.Id);
            return GenerateTFAResponse();
        }

        await _userDeviceRepository.UpdateLastLoginAt(savedDevice.Id, DateTime.Now);

        return GenerateLoginResponse(user.Id);
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

        var failedAttempts = await _failedLoginLogRepository.CountFailedAttempts(ip, userId, TimeSpan.FromMinutes(1));
        if (failedAttempts >= 5)
        {
            var blockade = new UserBlockade
            {
                UserId = userId
            };

            await _blockadeRepository.Add(blockade);
        }
    }

    private async Task HandleTrustedDevice(UserDevice? savedDevice, string deviceName, string userId, bool updateValidationDates = false)
    {
        if (savedDevice == null)
        {
            var newDevice = new UserDevice
            {
                Name = deviceName,
                UserId = userId
            };

            await _userDeviceRepository.Add(newDevice);
        }
        else
        {
            if(updateValidationDates && savedDevice.ValidUntil < DateTime.Now)
                await _userDeviceRepository.UpdateDates(savedDevice.Id, DateTime.Now, DateTime.Now.AddDays(90));

            await _userDeviceRepository.UpdateLastLoginAt(savedDevice.Id, DateTime.Now);
        }
    }

    private bool IsTfaRequired(User user, UserDevice? savedDevice)
    {
        if (user.TFAMethod == TFAMethod.ALWAYS)
            return true;

        return savedDevice == null || savedDevice.ValidUntil < DateTime.Now;
    }

    private void SaveTfaSession(HttpContext context, string userId)
    {
        context.Session.SetString("auth_userId", userId);
        context.Session.SetString("auth_dateUntil", DateTime.Now.AddMinutes(5).ToString());
        context.Session.SetInt32("auth_attempts", 0);
    }

    private LoginResponseDto GenerateLoginResponse(string userId)
    {
        var token = _jwtService.GenerateJwtToken(userId);
        return new LoginResponseDto
        {
            Token = token,
            ExpiresIn = _jwtService.GetExpiresIn(),
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

    public async Task<LoginResponseDto> Code(string code, HttpContext httpContext, HttpRequest httpRequest)
    {
        var userId = httpContext.Session.GetString("auth_userId");
        if (userId == null)
        {
            throw new RequestErrorException(401, "Logowanie nie zostało zainicjalizowane.");
        }

        var dateUntil = httpContext.Session.GetString("auth_dateUntil");

        if (DateTime.TryParse(dateUntil, out DateTime result))
        {
            if(result < DateTime.Now)
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
            if(attempts >= 2)
            {
                ClearTfaSession(httpContext);

                await HandleFailedLogin(user.Id, sourceIP, sourceDevice);

                throw new RequestErrorException(401, "Przekroczono liczbę prób. Rozpocznij proces logowania od nowa.");
            }

            httpContext.Session.SetInt32("auth_attempts", attempts);
            throw new RequestErrorException(401, "Niepoprawny kod.");
        }

        ClearTfaSession(httpContext);

        var savedDevice = await _userDeviceRepository.FindByNameAndUserId(sourceDevice, user.Id);
        await HandleTrustedDevice(savedDevice, sourceDevice, user.Id, updateValidationDates: true);

        return GenerateLoginResponse(user.Id);
    }

    private void ClearTfaSession(HttpContext context)
    {
        context.Session.Remove("auth_userId");
        context.Session.Remove("auth_dateUntil");
        context.Session.Remove("auth_attempts");
    }

    private bool ValidateTotp(string secret, string userInput)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(userInput, out _, new VerificationWindow(previous: 1, future: 1));
    }
}
