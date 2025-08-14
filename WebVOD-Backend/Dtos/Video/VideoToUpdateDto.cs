namespace WebVOD_Backend.Dtos.Video;

public class VideoToUpdateDto
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public string Category { get; set; }
    public List<string> Tags { get; set; }
    public string? ThumbnailPath { get; set; }
}
