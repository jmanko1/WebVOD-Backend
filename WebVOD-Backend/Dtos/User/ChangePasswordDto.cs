using System.ComponentModel.DataAnnotations;

namespace WebVOD_Backend.Dtos.User;

public class ChangePasswordDto
{
    [Required(ErrorMessage = "Podaj stare hasło.")]
    [MinLength(10, ErrorMessage = "Stare hasło musi mieć co najmniej 10 znaków.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{10,}$",
        ErrorMessage = "Stare hasło musi zawierać co najmniej jedną małą literę, jedną wielką literę, jedną cyfrę oraz jeden znak specjalny."
    )]
    public string OldPassword { get; set; }

    [Required(ErrorMessage = "Podaj nowe hasło.")]
    [MinLength(10, ErrorMessage = "Nowe hasło musi mieć co najmniej 10 znaków.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{10,}$",
        ErrorMessage = "Nowe hasło musi zawierać co najmniej jedną małą literę, jedną wielką literę, jedną cyfrę oraz jeden znak specjalny."
    )]
    public string NewPassword { get; set; }

    [Required(ErrorMessage = "Potwierdź nowe hasło.")]
    [Compare("NewPassword", ErrorMessage = "Podane hasła nie są identyczne.")]
    public string ConfirmNewPassword { get; set; }

    public string? Code { get; set; }
}
