using GameLibraryApi.Entities;

namespace GameLibraryApi.Interfaces;

public interface IIgdbClient
{
    Task<IReadOnlyList<Game>> SearchGamesAsync(string query, int limit, CancellationToken cancellationToken);

    Task<Game?> GetGameAsync(long externalId, CancellationToken cancellationToken);
}
