using System.ComponentModel.DataAnnotations;
using WebVOD_Backend.Model;

namespace WebVOD_Backend.Dtos.Video;

public class CreateVideoDto
{
    [Required(ErrorMessage = "Podaj tytuł.")]
    [MinLength(5, ErrorMessage = "Tytuł musi mieć przynajmniej 5 znaków.")]
    [MaxLength(100, ErrorMessage = "Tytuł może mieć maksymalnie 100 znaków.")]
    public string Title { get; set; }

    [MaxLength(500, ErrorMessage = "Opis może mieć maksymalnie 500 znaków.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Podaj kategorię.")]
    public VideoCategory Category { get; set; }

    public List<string> Tags { get; set; }
    public int Duration { get; set; }
}
