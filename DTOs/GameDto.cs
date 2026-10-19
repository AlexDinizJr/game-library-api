namespace GameLibraryApi.DTOs;

public sealed record GameDto(
    long ExternalId,
    string Title,
    string? Description,
    DateTime? ReleaseDate,
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> Platforms,
    IReadOnlyList<string> Developers,
    IReadOnlyList<string> Publishers,
    string? CoverImageUrl);
