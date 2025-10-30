namespace WebVOD_Backend.Dtos.Video;

public class UserVideoDto
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string? ThumbnailPath { get; set; }
    public DateTime UploadDate { get; set; }
    public int ViewsCount { get; set; }
    public bool TagsProposalsEnabled { get; set; }
    public int? TagsPropositionsCount { get; set; } = null;
    public int Duration { get; set; }
    public string Status { get; set; }
}
