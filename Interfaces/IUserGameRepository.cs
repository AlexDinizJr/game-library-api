using GameLibraryApi.Entities;

namespace GameLibraryApi.Interfaces;

public interface IUserGameRepository
{
    Task<IReadOnlyList<UserGame>> GetAllAsync(CancellationToken cancellationToken);

    Task<UserGame?> GetByGameIdAsync(string gameId, CancellationToken cancellationToken);

    Task AddAsync(UserGame userGame, CancellationToken cancellationToken);

    Task<bool> ReplaceAsync(UserGame userGame, CancellationToken cancellationToken);

    Task<bool> DeleteByGameIdAsync(string gameId, CancellationToken cancellationToken);
}
