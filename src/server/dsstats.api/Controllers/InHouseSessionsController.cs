using System.Security.Claims;
using dsstats.api.Authentication;
using dsstats.api.Hubs;
using dsstats.api.InHouse;
using dsstats.dbServices.InHouse;
using dsstats.shared.InHouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace dsstats.api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = InHouseBearerAuthenticationHandler.SchemeName)]
[Route("api10/inhouse/sessions")]
public sealed class InHouseSessionsController(
    IInHouseGameSessionService sessionService,
    IHubContext<InHouseHub> hubContext) : ControllerBase
{
    [HttpGet("closed")]
    public async Task<ActionResult<InHouseClosedGameSessionsPageDto>> GetClosedSessions(
        [FromQuery] InHouseClosedGameSessionsRequest request,
        CancellationToken token)
        => await ExecuteAsync(() => sessionService.GetClosedSessionsAsync(request, token));

    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<InHouseGameSessionDetailDto>> GetSession(
        Guid sessionId,
        CancellationToken token)
        => await ExecuteAsync(async () =>
        {
            var detail = await sessionService.GetSessionAsync(sessionId, GetUserId(), token);
            return detail is null
                ? throw new KeyNotFoundException("Unknown InHouse session.")
                : detail;
        });

    [HttpPost]
    public async Task<ActionResult<InHouseGameSessionDetailDto>> CreateSession(
        InHouseCreateGameSessionRequest request,
        CancellationToken token)
        => await ExecuteAsync(async () =>
        {
            var detail = await sessionService.CreateSessionAsync(GetUserId(), request, token);
            await hubContext.Clients.All.SendAsync(InHouseHub.ActiveSessionsChangedEvent, token);
            return detail;
        });

    [HttpDelete("{sessionId:guid}")]
    [Authorize(Roles = InHouseRoles.Admin)]
    public async Task<IActionResult> DeleteSession(
        Guid sessionId,
        CancellationToken token)
    {
        try
        {
            await sessionService.DeleteSessionAsync(sessionId, User, token);
            await hubContext.Clients.Group(InHouseHub.GetSessionGroupName(sessionId))
                .SendAsync(InHouseHub.SessionDeletedEvent, sessionId, token);
            await hubContext.Clients.All.SendAsync(InHouseHub.ActiveSessionsChangedEvent, token);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpPost("{sessionId:guid}/replays")]
    public async Task<ActionResult<InHouseGameSessionDetailDto>> UploadReplay(
        Guid sessionId,
        InHouseReplayUploadRequest request,
        CancellationToken token)
        => await ExecuteAsync(async () =>
        {
            var result = await sessionService.UploadReplayAsync(sessionId, GetUserId(), request, token);
            if (result.Changed)
            {
                await hubContext.Clients.Group(InHouseHub.GetSessionGroupName(sessionId))
                    .SendAsync(InHouseHub.SessionStateEvent, result.State, token);
                await hubContext.Clients.All.SendAsync(InHouseHub.ActiveSessionsChangedEvent, token);
            }
            return result.State;
        });

    private int GetUserId()
        => int.TryParse(User.FindFirstValue(InHouseClaims.UserId), out var userId)
            ? userId
            : throw new InvalidOperationException("You are not signed in.");

    private async Task<ActionResult<T>> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}
