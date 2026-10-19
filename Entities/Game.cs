using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GameLibraryApi.Entities;

public sealed class Game
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("externalId")]
    public long ExternalId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public List<string> Genres { get; set; } = [];

    public List<string> Platforms { get; set; } = [];

    public List<string> Developers { get; set; } = [];

    public List<string> Publishers { get; set; } = [];

    public string? CoverImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
