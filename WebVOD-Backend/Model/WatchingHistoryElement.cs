using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebVOD_Backend.Model;

public class WatchingHistoryElement
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [Required]
    [BsonRepresentation(BsonType.ObjectId)]
    public string VideoId { get; set; }

    [Required]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ViewerId { get; set; }

    [Required]
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
}
