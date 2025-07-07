using WebVOD_Backend.Model;

namespace WebVOD_Backend.Dtos.Video;

public class VideoDto
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public string Category { get; set; }
    public List<string> Tags { get; set; }
    public string VideoPath { get; set; }
    public DateTime UploadDate { get; set; }
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public int ViewsCount { get; set; }
    public AuthorDto Author { get; set; }
}

public class AuthorDto
{
    public string Id { get; set; }
    public string Login { get; set; }
    public string? ImageUrl { get; set; }
}
