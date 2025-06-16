using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebVOD_Backend.Model;

public class ResetPasswordToken
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [Required]
    public string Token { get; set; }

    [Required]
    public string UserId { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime ValidUntil { get; set; } = DateTime.UtcNow.AddMinutes(15);
}
