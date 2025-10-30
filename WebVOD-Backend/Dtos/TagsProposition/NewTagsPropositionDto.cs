using System.ComponentModel.DataAnnotations;

namespace WebVOD_Backend.Dtos.TagsProposition;

public class NewTagsPropositionDto
{
    [Required(ErrorMessage = "Podaj listę tagów.")]
    [MinLength(1, ErrorMessage = "Lista nie może być pusta.")]
    [MaxLength(5, ErrorMessage = "Lista może składać się maksymalnie z 5 tagów.")]
    public List<string> Tags { get; set; }

    [MaxLength(100, ErrorMessage = "Komentarz może mieć maksymalnie 100 znaków.")]
    public string? Comment { get; set; }
}
