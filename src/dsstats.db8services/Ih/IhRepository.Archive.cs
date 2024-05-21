using dsstats.db8;
using dsstats.db8services.Import;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace dsstats.db8services;

public partial class IhRepository
{
    public async Task ArchiveSession(Guid groupId)
    {
        var session = await context.IhSessions
            .Include(i => i.IhSessionPlayers)
            .Include(i => i.GroupStateV2)
            .FirstOrDefaultAsync(f => f.GroupId == groupId);

        if (session is null)
        {
            return;
        }

        if (session.GroupStateV2 is null)
        {
            return;
        }

        session.IhSessionPlayers.Clear();

        using var scope = scopeFactory.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();
        
        foreach (var playerState in session.GroupStateV2.PlayerStates)
        {
            var playerId = await importService.GetPlayerIdAsync(playerState.PlayerId, playerState.Name);
            var ihSessionPlayer = GetIhSessionPlayer(playerState, playerId);
            session.IhSessionPlayers.Add(ihSessionPlayer);
        }

        session.GroupStateV2.PlayerStates.Clear();
        session.Players = session.IhSessionPlayers.Count;
        session.Games = session.GroupStateV2.ReplayHashes.Count;
        session.Closed = true;
        await context.SaveChangesAsync();
    }

    private IhSessionPlayer GetIhSessionPlayer(PlayerStateV2 playerState, int playerId)
    {
        return new()
        {
            Name = playerState.Name,
            Games = playerState.Games,
            Wins = playerState.Wins,
            Obs = playerState.Observer,
            RatingStart = playerState.RatingStart,
            RatingEnd = playerState.RatingStart + playerState.RatingChange,
            Performance = playerState.Performance,
            PlayerId = playerId
        };
    }

    //public async Task ArchiveOldSessions()
    //{

    //}
}
