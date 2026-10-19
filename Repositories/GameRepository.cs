using GameLibraryApi.Data;
using GameLibraryApi.Entities;
using GameLibraryApi.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GameLibraryApi.Repositories;

public sealed class GameRepository(MongoDbContext context) : IGameRepository
{
    public async Task<Game?> GetByExternalIdAsync(long externalId, CancellationToken cancellationToken) =>
        await context.Games.Find(game => game.ExternalId == externalId).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Game>> GetByIdsAsync(
        IReadOnlyCollection<string> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await context.Games
            .Find(game => ids.Contains(game.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<Game> UpsertAsync(Game game, CancellationToken cancellationToken)
    {
        var existingGame = await GetByExternalIdAsync(game.ExternalId, cancellationToken);
        game.Id = existingGame?.Id ?? ObjectId.GenerateNewId().ToString();
        game.UpdatedAt = DateTime.UtcNow;
        await context.Games.ReplaceOneAsync(
            existing => existing.ExternalId == game.ExternalId,
            game,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);

        return await context.Games
            .Find(existing => existing.ExternalId == game.ExternalId)
            .FirstAsync(cancellationToken);
    }
}
