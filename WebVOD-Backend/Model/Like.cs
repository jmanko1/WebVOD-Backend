using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebVOD_Backend.Model;

public class Like
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [Required]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; }

    [Required]
    [BsonRepresentation(BsonType.ObjectId)]
    public string VideoId { get; set; }
}
