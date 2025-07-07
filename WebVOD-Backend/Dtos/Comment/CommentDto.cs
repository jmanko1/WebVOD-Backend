using WebVOD_Backend.Dtos.Video;

namespace WebVOD_Backend.Dtos.Comment;

public class CommentDto
{
    public string Id { get; set; }
    public string Content { get; set; }
    public DateTime UploadDate { get; set; }
    public AuthorDto Author { get; set; }
}
