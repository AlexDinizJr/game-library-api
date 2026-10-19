using AutoMapper;
using GameLibraryApi.DTOs;
using GameLibraryApi.Entities;

namespace GameLibraryApi.Mapping;

public sealed class GameMappingProfile : Profile
{
    public GameMappingProfile()
    {
        CreateMap<Game, GameDto>();
        CreateMap<LibraryGameMappingSource, LibraryGameDto>()
            .ConstructUsing((source, context) => new LibraryGameDto(
                context.Mapper.Map<GameDto>(source.Game),
                source.UserGame.Status,
                source.UserGame.PersonalRating,
                source.UserGame.Note,
                source.UserGame.CreatedAt,
                source.UserGame.UpdatedAt));
    }
}

public sealed record LibraryGameMappingSource(Game Game, UserGame UserGame);
