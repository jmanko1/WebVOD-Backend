﻿using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebVOD_Backend.Model;

public class UserDevice
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    public string UserId { get; set; }

    [Required]
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime ValidUntil { get; set; } = DateTime.UtcNow.AddDays(90);
}
