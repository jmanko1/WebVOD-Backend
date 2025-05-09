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

    [StringLength(500)]
    public string Description { get; set; } = null!;

    public string ImageUrl { get; set; } = null!;

    [Required]
    public bool IsTFAEnabled { get; set; } = false;

    [Required]
    [BsonRepresentation(BsonType.String)]
    public TFAMethod TFAMethod { get; set; } = TFAMethod.ALWAYS;

    [Required]
    public string TOTPKey { get; set; }

    [Required]
    public DateTime SignupDate { get; set; } = DateTime.Now;
}

public enum TFAMethod
{
    ALWAYS,
    NEW_DEVICE
}