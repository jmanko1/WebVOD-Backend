using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace WebVOD_Backend.Model;

public class FailedLoginLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [Required]
    public string UserId { get; set; }

    [Required]
    public string SourceIP { get; set; }

    [Required]
    public string SourceDevice { get; set; }

    [Required]
    public DateTime LogAt { get; set; } = DateTime.Now;
}
