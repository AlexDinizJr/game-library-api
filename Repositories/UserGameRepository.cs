using GameLibraryApi.Data;
using GameLibraryApi.Entities;
using GameLibraryApi.Interfaces;
using MongoDB.Driver;

namespace GameLibraryApi.Repositories;

public sealed class UserGameRepository(MongoDbContext context) : IUserGameRepository
{
    public async Task<IReadOnlyList<UserGame>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.UserGames
            .Find(FilterDefinition<UserGame>.Empty)
            .SortByDescending(userGame => userGame.UpdatedAt)
            .ToListAsync(cancellationToken);

    public async Task<UserGame?> GetByGameIdAsync(string gameId, CancellationToken cancellationToken) =>
        await context.UserGames.Find(userGame => userGame.GameId == gameId).FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(UserGame userGame, CancellationToken cancellationToken) =>
        await context.UserGames.InsertOneAsync(userGame, cancellationToken: cancellationToken);

    public async Task<bool> ReplaceAsync(UserGame userGame, CancellationToken cancellationToken)
    {
        var result = await context.UserGames.ReplaceOneAsync(
            existing => existing.Id == userGame.Id,
            userGame,
            cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
    }

    public async Task<bool> DeleteByGameIdAsync(string gameId, CancellationToken cancellationToken)
    {
        var result = await context.UserGames.DeleteOneAsync(
            userGame => userGame.GameId == gameId,
            cancellationToken);
        return result.DeletedCount == 1;
    }
}
