using System.ComponentModel.DataAnnotations;

namespace WebVOD_Backend.Dtos.Auth;

public class ResetPasswordDto
{
    [Required]
    public string Token;

    [Required(ErrorMessage = "Podaj nowe hasło.")]
    [MinLength(10, ErrorMessage = "Hasło musi mieć co najmniej 10 znaków.")]
    [RegularExpression(
    @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{10,}$",
    ErrorMessage = "Hasło musi zawierać co najmniej jedną małą literę, jedną wielką literę, jedną cyfrę oraz jeden znak specjalny."
)]
    public string Password { get; set; }

    [Required(ErrorMessage = "Potwierdź nowe hasło.")]
    [Compare("Password", ErrorMessage = "Podane hasła nie są identyczne.")]
    public string ConfirmPassword { get; set; }

    [Required(ErrorMessage = "Podaj kod.")]
    [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "Podaj prawidłowy 6-cyfrowy kod.")]
    public string? Code { get; set; }
}
