using System.ComponentModel.DataAnnotations;

namespace WebVOD_Backend.Dtos.Auth;

public class InitiateResetPasswordDto
{
    [Required(ErrorMessage = "Podaj adres email.")]
    [EmailAddress(ErrorMessage = "Podaj prawidłowy adres email.")]
    [MaxLength(80, ErrorMessage = "Adres email może mieć maksymalnie 80 znaków.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Potwierdź, że nie jesteś robotem.")]
    public string CaptchaToken { get; set; }
}
