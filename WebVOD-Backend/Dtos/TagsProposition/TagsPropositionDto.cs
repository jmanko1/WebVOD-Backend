using WebVOD_Backend.Dtos.Video;

namespace WebVOD_Backend.Dtos.TagsProposition;

public class TagsPropositionDto
{
    public string Id { get; set; }
    public string VideoId { get; set; }
    public AuthorDto Author { get; set; }
    public List<string> Tags { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ValidUntil { get; set; }
}
