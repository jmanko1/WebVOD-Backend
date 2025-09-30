using System.Text.Json;
using System.Text.Json.Serialization;
using WebVOD_Backend.Services.Interfaces;

namespace WebVOD_Backend.Services.Implementations;

public class CaptchaService : ICaptchaService
{
    private readonly IConfiguration _configuration;

    public CaptchaService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<bool> VerifyCaptchaToken(string captchaToken)
    {
        if (string.IsNullOrEmpty(captchaToken))
            return false;

        var secretKey = _configuration.GetSection("Captcha").GetValue<string>("SecretKey");

        using var client = new HttpClient();

        var parameters = new Dictionary<string, string>
        {
            { "secret", secretKey },
            { "response", captchaToken }
        };

        var content = new FormUrlEncodedContent(parameters);
        var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);

        if (!response.IsSuccessStatusCode)
            return false;

        var json = await response.Content.ReadAsStringAsync();

        var captchaResult = JsonSerializer.Deserialize<CaptchaVerifyResponse>(json);

        return captchaResult?.Success ?? false;
    }
}


public class CaptchaVerifyResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error-codes")]
    public string[]? ErrorCodes { get; set; }
}
