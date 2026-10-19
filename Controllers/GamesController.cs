using GameLibraryApi.DTOs;
using GameLibraryApi.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GameLibraryApi.Controllers;

[ApiController]
[Route("api/games")]
public sealed class GamesController(IGameLibraryService gameLibraryService) : ControllerBase
{
    [HttpGet("search")]
    [ProducesResponseType<IReadOnlyList<GameDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IReadOnlyList<GameDto>>> Search(
        [FromQuery] string query,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "A search query is required."
            });
        }

        if (limit is < 1 or > 50)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Limit must be between 1 and 50."
            });
        }

        return Ok(await gameLibraryService.SearchGamesAsync(query, limit, cancellationToken));
    }

    [HttpGet("{externalId:long}")]
    [ProducesResponseType<GameDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<GameDto>> GetById(
        long externalId,
        CancellationToken cancellationToken)
    {
        return Ok(await gameLibraryService.GetGameAsync(externalId, cancellationToken));
    }
}
