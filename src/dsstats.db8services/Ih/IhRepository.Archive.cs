using dsstats.db8;
using dsstats.db8services.Import;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace dsstats.db8services;

public partial class IhRepository
{
    public async Task ArchiveSession(Guid groupId)
    {
        var session = await context.IhSessions
            .Include(i => i.IhSessionPlayers)
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

        IProperty property = context.Entry(session).Property(nameof(IhSession.GroupStateV2)).Metadata;
        context.Entry(session).Property(property).IsModified = true;
        
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

    public async Task ArchiveV1()
    {
        var v1Sessions = await context.IhSessions
            .Include(i => i.IhSessionPlayers)
            .ToListAsync();

        using var scope = scopeFactory.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        foreach (var session in v1Sessions)
        {
            if (session.GroupState is null)
            {
                continue;
            }
            var newGroupState = GetGroupState(session.GroupState);

            session.GroupStateV2 = newGroupState;
            session.GroupState = null;
            session.IhSessionPlayers = await GetSessionPlayers(newGroupState, importService);

            await context.SaveChangesAsync();
        }
    }

    private async Task<List<IhSessionPlayer>> GetSessionPlayers(GroupStateV2 groupState, ImportService importService)
    {
        List<IhSessionPlayer> players = [];

        var replays = await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(t => t.Player)
            .Include(i => i.ReplayPlayers)
                .ThenInclude(t => t.ReplayPlayerRatingInfo)
            .Where(x => groupState.ReplayHashes.Contains(x.ReplayHash))
            .ToListAsync();

        Dictionary<PlayerId, int> playerIds = [];

        foreach (var replay in replays)
        {
            foreach (var replayPlayer in replay.ReplayPlayers)
            {
                PlayerId playerId = new(replayPlayer.Player.ToonId, replayPlayer.Player.RealmId, replayPlayer.Player.RegionId);

                if (!playerIds.TryGetValue(playerId, out int id)
                    || id == 0)
                {
                    id = playerIds[playerId] = await importService.GetPlayerIdAsync(playerId, replayPlayer.Player.Name);
                }

                var player = players.FirstOrDefault(f => f.PlayerId == id);
                if (player is null)
                {
                    player = new()
                    {
                        Name = replayPlayer.Player.Name,
                        RatingStart = Convert.ToInt32(replayPlayer.ReplayPlayerRatingInfo?.Rating ?? 0),
                        PlayerId = id
                    };
                    players.Add(player);
                }

                if (player.RatingStart == 0)
                {
                    player.RatingStart = Convert.ToInt32(replayPlayer.ReplayPlayerRatingInfo?.Rating ?? 0);
                }
                player.Games++;
                player.RatingEnd += Convert.ToInt32(replayPlayer.ReplayPlayerRatingInfo?.RatingChange ?? 0);
                if (replayPlayer.PlayerResult == PlayerResult.Win)
                {
                    player.Wins++;
                }
            }
        }
        players.ForEach(f => f.RatingEnd += f.RatingStart);

        return players;
    }

    private static GroupStateV2 GetGroupState(GroupState groupState)
    {
        GroupStateV2 newGroupState = new()
        {
            RatingType = groupState.RatingType,
            RatingCalcType = groupState.RatingCalcType,
            GroupId = groupState.GroupId,
            ReplayHashes = groupState.ReplayHashes,
            Created = groupState.Created
        };
        return newGroupState;
    }

    //public async Task ArchiveOldSessions()
    //{

    //}
}
