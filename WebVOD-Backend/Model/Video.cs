using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebVOD_Backend.Model;

public class Video
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [BsonRepresentation(BsonType.String)]
    public VideoCategory Category { get; set; }

    public List<string> Tags { get; set; } = new();

    [Required]
    public string VideoPath { get; set; }

    [Required]
    public string ThumbnailPath { get; set; }

    [Required]
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;

    [Required]
    public int LikesCount { get; set; } = 0;

    [Required]
    public int CommentsCount { get; set; } = 0;

    [Required]
    public int ViewsCount { get; set; } = 0;

    [Required]
    public int Duration { get; set; }

    [Required]
    [BsonRepresentation(BsonType.String)]
    public VideoStatus Status { get; set; } = VideoStatus.UPLOADING;

    [Required]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AuthorId { get; set; }
}

public enum VideoCategory
{
    EDUKACJA,
    FILM,
    GRY,
    BLOG,
    MUZYKA,
    NAUKA,
    ROZRYWKA,
    SPORT,
    PORADNIK,
    PODRÓŻE
}

public enum VideoStatus
{
    UPLOADING,
    PUBLISHED
}
