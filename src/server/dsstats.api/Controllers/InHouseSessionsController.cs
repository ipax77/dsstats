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
    [HttpGet]
    public async Task<ActionResult<List<InHouseGameSessionListDto>>> GetActiveSessions(CancellationToken token)
        => await sessionService.GetActiveSessionsAsync(token);

    [HttpPost]
    public async Task<ActionResult<InHouseGameSessionDetailDto>> CreateSession(
        InHouseCreateGameSessionRequest request,
        CancellationToken token)
        => await ExecuteAsync(async () =>
        {
            var detail = await sessionService.CreateSessionAsync(GetUserId(), request, token);
            await BroadcastSessionChangedAsync(detail.SessionId);
            return detail;
        });

    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<InHouseGameSessionDetailDto>> GetSession(Guid sessionId, CancellationToken token)
    {
        var detail = await sessionService.GetSessionAsync(sessionId, GetUserId(), token);
        return detail is null ? NotFound() : detail;
    }

    [HttpPost("{sessionId:guid}/replays")]
    public async Task<ActionResult<InHouseGameSessionDetailDto>> UploadReplay(
        Guid sessionId,
        InHouseReplayUploadRequest request,
        CancellationToken token)
        => await ExecuteAsync(async () =>
        {
            var detail = await sessionService.UploadReplayAsync(sessionId, GetUserId(), request, token);
            await hubContext.Clients.Group(InHouseHub.GetSessionGroupName(sessionId))
                .SendAsync(InHouseHub.ReplayAddedEvent, detail, token);
            return detail;
        });

    [HttpPost("{sessionId:guid}/close")]
    public async Task<ActionResult<InHouseGameSessionDetailDto>> CloseSession(Guid sessionId, CancellationToken token)
        => await ExecuteAsync(async () =>
        {
            var detail = await sessionService.CloseSessionAsync(sessionId, GetUserId(), token);
            await BroadcastSessionChangedAsync(sessionId);
            return detail;
        });

    private async Task BroadcastSessionChangedAsync(Guid sessionId)
    {
        await hubContext.Clients.Group(InHouseHub.GetSessionGroupName(sessionId))
            .SendAsync(InHouseHub.SessionChangedEvent, sessionId);
    }

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
    }
}
