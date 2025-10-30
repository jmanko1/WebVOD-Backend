using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebVOD_Backend.Model;

public class TagsProposition
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [Required]
    [BsonRepresentation(BsonType.ObjectId)]
    public string VideoId { get; set; }

    [Required]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; }

    public List<string> Tags { get; set; }

    [StringLength(100)]
    public string? Comment { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime ValidUntil { get; set; } = DateTime.UtcNow.AddDays(14);
}
