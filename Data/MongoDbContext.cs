using GameLibraryApi.Entities;
using GameLibraryApi.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace GameLibraryApi.Data;

public sealed class MongoDbContext
{
    public MongoDbContext(IMongoClient client, IOptions<MongoDbOptions> options)
    {
        var database = client.GetDatabase(options.Value.DatabaseName);
        Games = database.GetCollection<Game>("games");
        UserGames = database.GetCollection<UserGame>("userGames");
    }

    public IMongoCollection<Game> Games { get; }

    public IMongoCollection<UserGame> UserGames { get; }

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        var gameExternalIdIndex = new CreateIndexModel<Game>(
            Builders<Game>.IndexKeys.Ascending(game => game.ExternalId),
            new CreateIndexOptions { Unique = true });
        await Games.Indexes.CreateOneAsync(gameExternalIdIndex, cancellationToken: cancellationToken);

        var userGameIndex = new CreateIndexModel<UserGame>(
            Builders<UserGame>.IndexKeys.Ascending(userGame => userGame.GameId),
            new CreateIndexOptions { Unique = true });
        await UserGames.Indexes.CreateOneAsync(userGameIndex, cancellationToken: cancellationToken);
    }
}
