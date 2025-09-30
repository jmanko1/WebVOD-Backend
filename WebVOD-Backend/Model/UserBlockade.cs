using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebVOD_Backend.Model;

public class UserBlockade
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [Required]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; }

    [Required]
    public string SourceIP { get; set; }

    [Required]
    public DateTime Since { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime Until { get; set; } = DateTime.UtcNow.AddMinutes(10);
}
