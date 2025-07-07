using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebVOD_Backend.Model;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Login { get; set; }
    
    [StringLength(80)]
    [Required]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    [Required]
    public bool IsTFAEnabled { get; set; } = false;

    [Required]
    public string TOTPKey { get; set; }

    [Required]
    public DateTime SignupDate { get; set; } = DateTime.UtcNow;

    [Required]
    public int VideosCount { get; set; } = 0;
}