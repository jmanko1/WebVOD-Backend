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

    public AuthService(IUserRepository userRepository, IFailedLoginLogRepository failedLoginLogRepository, IUserBlockadeRepository blockadeRepository, ICryptoService cryptoService, IJwtService jwtService)
    {
        _userRepository = userRepository;
        _failedLoginLogRepository = failedLoginLogRepository;
        _blockadeRepository = blockadeRepository;
        _cryptoService = cryptoService;
        _jwtService = jwtService;
    }

    public async Task<LoginResponseDto> Authenticate(LoginDto loginDto, HttpContext httpContext, HttpRequest httpRequest)
    {
        var user = await _userRepository.FindByLogin(loginDto.Login);
        if(user == null)
        {
            throw new RequestErrorException(401, "Nieprawidłowy login lub hasło.");
        }

        var isBlockade = await _blockadeRepository.ExistsByUserId(user.Id);
        if(isBlockade)
        {
            throw new RequestErrorException(403, "To konto jest w tej chwili zablokowane. Spróbuj ponownie później.");
        }

        var sourceIP = httpContext.Connection.RemoteIpAddress?.ToString();
        var sourceDevice = httpRequest.Headers["User-Agent"].ToString();

        if (!_cryptoService.VerifyPassword(loginDto.Password, user.Password))
        {
            var log = new FailedLoginLog
            {
                UserId = user.Id,
                SourceIP = sourceIP,
                SourceDevice = sourceDevice
            };

            await _failedLoginLogRepository.Add(log);

            var failedAttempts = await _failedLoginLogRepository.CountFailedAttempts(sourceIP, user.Id, TimeSpan.FromMinutes(1));
            if (failedAttempts >= 5)
            {
                var blockade = new UserBlockade
                {
                    UserId = user.Id
                };

                await _blockadeRepository.Add(blockade);
            }

            throw new RequestErrorException(401, "Niepoprawny login lub hasło.");
        }

        if(!user.IsTFAEnabled)
        {
            var token = _jwtService.GenerateJwtToken(user.Id);
            var loginResponse = new LoginResponseDto
            {
                StatusCode = 200,
                Token = token,
                ExpiresIn = _jwtService.GetExpiresIn()
            };

            return loginResponse;
        }

        var redirectResponse = new LoginResponseDto
        {
            StatusCode = 301,
            RedirectUrl = "http://localhost:3000/login/code"
        };

        return redirectResponse;
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

        var otpKey = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(32));

        var user = new User
        {
            Login = registerDto.Login,
            Email = registerDto.Email,
            Password = _cryptoService.HashPassword(registerDto.Password),
            TOTPKey = _cryptoService.Encrypt(otpKey)
        };

        await _userRepository.Add(user);
    }
}
