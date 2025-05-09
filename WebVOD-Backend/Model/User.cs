using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebVOD_Backend.Model;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    
    [StringLength(50)]
    public string Login { get; set; }
    
    [StringLength(80)]
    public string Email { get; set; }
    
    public string Password { get; set; }
    
    [StringLength(500)]
    public string Description { get; set; }
    
    public string ImageUrl { get; set; }
    
    public bool IsTFAEnabled { get; set; }
    
    public TFAMethod TFAMethod { get; set; }
    
    [StringLength(20)]
    public string TOTPKey { get; set; }

    public DateTime SignupDate { get; set; }
}

public enum TFAMethod
{
    ALWAYS,
    NEW_DEVICE
}