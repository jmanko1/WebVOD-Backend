using System.ComponentModel.DataAnnotations;

namespace WebVOD_Backend.Dtos.Comment;

public class NewCommentDto
{
    [Required]
    public string VideoId { get; set; }

    [Required(ErrorMessage = "Podaj treść komentarza.")]
    [MinLength(1, ErrorMessage = "Podaj treść komentarza.")]
    [MaxLength(500, ErrorMessage = "Komentarz może mieć maksymalnie 500 znaków.")]
    public string Content { get; set; }
}
