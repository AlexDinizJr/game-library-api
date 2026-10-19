using System.ComponentModel.DataAnnotations;
using GameLibraryApi.Enums;

namespace GameLibraryApi.DTOs;

public sealed record AddGameToLibraryRequest(
    [Range(1, long.MaxValue)] long ExternalGameId,
    GameStatus Status = GameStatus.WantToPlay,
    [Range(0, 10)] double? PersonalRating = null,
    [StringLength(2000)] string? Note = null);

public sealed record UpdateLibraryGameRequest(
    GameStatus? Status = null,
    [Range(0, 10)] double? PersonalRating = null,
    [StringLength(2000)] string? Note = null);

public sealed record LibraryGameDto(
    GameDto Game,
    GameStatus Status,
    double? PersonalRating,
    string? Note,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record LibraryStatisticsDto(
    int TotalGames,
    double? AverageRating,
    IReadOnlyDictionary<string, int> GamesByStatus);
