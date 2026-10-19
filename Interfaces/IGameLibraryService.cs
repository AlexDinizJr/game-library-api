using GameLibraryApi.DTOs;
using GameLibraryApi.Enums;

namespace GameLibraryApi.Interfaces;

public interface IGameLibraryService
{
    Task<IReadOnlyList<GameDto>> SearchGamesAsync(string query, int limit, CancellationToken cancellationToken);

    Task<GameDto> GetGameAsync(long externalId, CancellationToken cancellationToken);

    Task<IReadOnlyList<LibraryGameDto>> GetLibraryAsync(CancellationToken cancellationToken);

    Task<LibraryGameDto> GetLibraryGameAsync(long externalGameId, CancellationToken cancellationToken);

    Task<LibraryGameDto> AddToLibraryAsync(AddGameToLibraryRequest request, CancellationToken cancellationToken);

    Task<LibraryGameDto> UpdateLibraryGameAsync(
        long externalGameId,
        UpdateLibraryGameRequest request,
        CancellationToken cancellationToken);

    Task RemoveFromLibraryAsync(long externalGameId, CancellationToken cancellationToken);

    Task<LibraryStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken);
}
