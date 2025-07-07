using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebVOD_Backend.Model;

public class BlacklistedToken
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Jti { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }

    [Required]
    public DateTime BlacklistedAt { get; set; } = DateTime.UtcNow;
}
