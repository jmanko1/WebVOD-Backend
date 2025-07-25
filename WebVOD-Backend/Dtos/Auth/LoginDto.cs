﻿using System.ComponentModel.DataAnnotations;

namespace WebVOD_Backend.Dtos.Auth;

public class LoginDto
{
    [Required(ErrorMessage = "Podaj login.")]
    [MinLength(4, ErrorMessage = "Login musi mieć co najmniej 4 znaki.")]
    [MaxLength(50, ErrorMessage = "Login może mieć maksymalnie 50 znaków.")]
    [RegularExpression("^[a-zA-Z0-9]*$", ErrorMessage = "Login może zawierać tylko znaki alfanumeryczne.")]
    public string Login { get; set; }

    [Required(ErrorMessage = "Podaj hasło.")]
    [MinLength(10, ErrorMessage = "Hasło musi mieć co najmniej 10 znaków.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{10,}$",
        ErrorMessage = "Hasło musi zawierać co najmniej jedną małą literę, jedną wielką literę, jedną cyfrę oraz jeden znak specjalny."
    )]
    public string Password { get; set; }

    public bool CheckedSave { get; set; }
}
