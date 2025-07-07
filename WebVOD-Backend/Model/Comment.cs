using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebVOD_Backend.Model;

public class Comment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [Required]
    [StringLength(500)]
    public string Content { get; set; }

    [Required]
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;

    [Required]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AuthorId { get; set; }

    [Required]
    [BsonRepresentation(BsonType.ObjectId)]
    public string VideoId { get; set; }
}
