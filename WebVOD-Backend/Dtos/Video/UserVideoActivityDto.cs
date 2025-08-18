namespace WebVOD_Backend.Dtos.Video;

public class UserVideoActivityDto
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string? ThumbnailPath { get; set; }
    public DateTime UploadDate { get; set; }
    public int ViewsCount { get; set; }
    public int Duration { get; set; }
    public string AuthorLogin { get; set; }
}
