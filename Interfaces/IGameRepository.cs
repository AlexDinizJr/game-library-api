using GameLibraryApi.Entities;

namespace GameLibraryApi.Interfaces;

public interface IGameRepository
{
    Task<Game?> GetByExternalIdAsync(long externalId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Game>> GetByIdsAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken);

    Task<Game> UpsertAsync(Game game, CancellationToken cancellationToken);
}
