using GameLibraryApi.DTOs;
using GameLibraryApi.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GameLibraryApi.Controllers;

[ApiController]
[Route("api/library")]
public sealed class LibraryController(IGameLibraryService gameLibraryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<LibraryGameDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LibraryGameDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await gameLibraryService.GetLibraryAsync(cancellationToken));
    }

    [HttpGet("statistics")]
    [ProducesResponseType<LibraryStatisticsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LibraryStatisticsDto>> GetStatistics(CancellationToken cancellationToken)
    {
        return Ok(await gameLibraryService.GetStatisticsAsync(cancellationToken));
    }

    [HttpGet("{externalGameId:long}")]
    [ProducesResponseType<LibraryGameDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LibraryGameDto>> GetLibraryGame(
        long externalGameId,
        CancellationToken cancellationToken)
    {
        return Ok(await gameLibraryService.GetLibraryGameAsync(externalGameId, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<LibraryGameDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LibraryGameDto>> Add(
        AddGameToLibraryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await gameLibraryService.AddToLibraryAsync(request, cancellationToken);
        return CreatedAtAction(
            nameof(GetLibraryGame),
            new { externalGameId = result.Game.ExternalId },
            result);
    }

    [HttpPatch("{externalGameId:long}")]
    [ProducesResponseType<LibraryGameDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LibraryGameDto>> Update(
        long externalGameId,
        UpdateLibraryGameRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await gameLibraryService.UpdateLibraryGameAsync(
            externalGameId,
            request,
            cancellationToken));
    }

    [HttpDelete("{externalGameId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(long externalGameId, CancellationToken cancellationToken)
    {
        await gameLibraryService.RemoveFromLibraryAsync(externalGameId, cancellationToken);
        return NoContent();
    }
}
