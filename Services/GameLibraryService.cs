using GameLibraryApi.DTOs;
using GameLibraryApi.Entities;
using GameLibraryApi.Enums;
using GameLibraryApi.Exceptions;
using GameLibraryApi.Interfaces;
using GameLibraryApi.Mapping;
using AutoMapper;
using MongoDB.Driver;

namespace GameLibraryApi.Services;

public sealed class GameLibraryService(
    IGameRepository gameRepository,
    IUserGameRepository userGameRepository,
    IIgdbClient igdbClient,
    IMapper mapper) : IGameLibraryService
{
    public async Task<IReadOnlyList<GameDto>> SearchGamesAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        var games = await igdbClient.SearchGamesAsync(query, limit, cancellationToken);
        return games.Select(mapper.Map<GameDto>).ToArray();
    }

    public async Task<GameDto> GetGameAsync(long externalId, CancellationToken cancellationToken)
    {
        var game = await GetOrImportGameAsync(externalId, cancellationToken);
    return mapper.Map<GameDto>(game);
    }

    public async Task<IReadOnlyList<LibraryGameDto>> GetLibraryAsync(CancellationToken cancellationToken)
    {
        var userGames = await userGameRepository.GetAllAsync(cancellationToken);
        var games = await gameRepository.GetByIdsAsync(
            userGames.Select(userGame => userGame.GameId).ToArray(),
            cancellationToken);
        var gamesById = games.ToDictionary(game => game.Id);

        if (userGames.Any(userGame => !gamesById.ContainsKey(userGame.GameId)))
        {
            throw new InvalidOperationException("The library contains a game entry without matching game metadata.");
        }

        return userGames
            .Select(userGame => MapLibraryGame(gamesById[userGame.GameId], userGame))
            .ToArray();
    }

    public async Task<LibraryGameDto> GetLibraryGameAsync(
        long externalGameId,
        CancellationToken cancellationToken)
    {
        var game = await GetOrImportGameAsync(externalGameId, cancellationToken);
        var userGame = await userGameRepository.GetByGameIdAsync(game.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Game {externalGameId} is not in the library.");
        return MapLibraryGame(game, userGame);
    }

    public async Task<LibraryGameDto> AddToLibraryAsync(
        AddGameToLibraryRequest request,
        CancellationToken cancellationToken)
    {
        var game = await GetOrImportGameAsync(request.ExternalGameId, cancellationToken);
        if (await userGameRepository.GetByGameIdAsync(game.Id, cancellationToken) is not null)
        {
            throw new LibraryEntryAlreadyExistsException();
        }

        var userGame = new UserGame
        {
            GameId = game.Id,
            Status = request.Status,
            PersonalRating = request.PersonalRating,
            Note = request.Note
        };

        try
        {
            await userGameRepository.AddAsync(userGame, cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new LibraryEntryAlreadyExistsException();
        }

        return MapLibraryGame(game, userGame);
    }

    public async Task<LibraryGameDto> UpdateLibraryGameAsync(
        long externalGameId,
        UpdateLibraryGameRequest request,
        CancellationToken cancellationToken)
    {
        var game = await gameRepository.GetByExternalIdAsync(externalGameId, cancellationToken)
            ?? throw new KeyNotFoundException($"Game {externalGameId} has not been imported yet.");
        var userGame = await userGameRepository.GetByGameIdAsync(game.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Game {externalGameId} is not in the library.");

        if (request.Status is GameStatus status)
        {
            userGame.Status = status;
        }

        if (request.PersonalRating.HasValue)
        {
            userGame.PersonalRating = request.PersonalRating;
        }

        if (request.Note is not null)
        {
            userGame.Note = request.Note;
        }

        userGame.UpdatedAt = DateTime.UtcNow;
        if (!await userGameRepository.ReplaceAsync(userGame, cancellationToken))
        {
            throw new KeyNotFoundException($"Game {externalGameId} is no longer in the library.");
        }

        return MapLibraryGame(game, userGame);
    }

    public async Task RemoveFromLibraryAsync(long externalGameId, CancellationToken cancellationToken)
    {
        var game = await gameRepository.GetByExternalIdAsync(externalGameId, cancellationToken)
            ?? throw new KeyNotFoundException($"Game {externalGameId} has not been imported yet.");
        if (!await userGameRepository.DeleteByGameIdAsync(game.Id, cancellationToken))
        {
            throw new KeyNotFoundException($"Game {externalGameId} is not in the library.");
        }
    }

    public async Task<LibraryStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken)
    {
        var userGames = await userGameRepository.GetAllAsync(cancellationToken);
        var gamesByStatus = Enum.GetValues<GameStatus>().ToDictionary(
            status => System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(status.ToString()),
            status => userGames.Count(userGame => userGame.Status == status));
        var ratings = userGames
            .Where(userGame => userGame.PersonalRating.HasValue)
            .Select(userGame => userGame.PersonalRating!.Value)
            .ToArray();

        return new LibraryStatisticsDto(
            userGames.Count,
            ratings.Length == 0 ? null : ratings.Average(),
            gamesByStatus);
    }

    private async Task<Game> GetOrImportGameAsync(long externalId, CancellationToken cancellationToken)
    {
        var existing = await gameRepository.GetByExternalIdAsync(externalId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var game = await igdbClient.GetGameAsync(externalId, cancellationToken)
            ?? throw new KeyNotFoundException($"Game {externalId} was not found in IGDB.");
        return await gameRepository.UpsertAsync(game, cancellationToken);
    }

    private LibraryGameDto MapLibraryGame(Game game, UserGame userGame) =>
        mapper.Map<LibraryGameDto>(new LibraryGameMappingSource(game, userGame));
}
