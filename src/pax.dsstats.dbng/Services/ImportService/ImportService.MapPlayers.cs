
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class ImportService
{
    private async Task<int> CreateAndMapPlayers(ICollection<Replay> replays)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var playersDic = await GetPlayerDic(context);
        var newPlayers = await CreateMissingPlayers(context, playersDic, replays);
        newPlayers.ForEach(f => playersDic[f.ToonId] = f.PlayerId);
        MapPlayers(playersDic, replays);
        return newPlayers.Count;
    }

    private async Task<Dictionary<int, int>> GetPlayerDic(ReplayContext context)
    {
        var players = await context.Players
            .AsNoTracking()
            .Select(s => new { s.PlayerId, s.ToonId })
            .ToListAsync();

        return players.ToDictionary(k => k.ToonId, v => v.PlayerId);
    }

    private static void MapPlayers(Dictionary<int, int> playersDic, ICollection<Replay> replays)
    {
        foreach (var replayPlayer in replays.SelectMany(s => s.ReplayPlayers))
        {
#pragma warning disable CS8602 // set playerId without loading the entity
#pragma warning disable CS8625 // set playerId without loading the entity
            replayPlayer.PlayerId = playersDic[replayPlayer.Player.ToonId];
            replayPlayer.Player = null;
#pragma warning restore CS8602
#pragma warning restore CS8625
        }
    }

    private async Task<List<Player>> CreateMissingPlayers(ReplayContext context, Dictionary<int, int> playersDic, ICollection<Replay> replays)
    {
        List<Player> newPlayers = new();
        foreach (var player in replays.SelectMany(s => s.ReplayPlayers).Select(s => mapper.Map<PlayerDto>(s.Player)).Distinct())
        {
            if (!playersDic.ContainsKey(player.ToonId))
            {
                var newPlayer = mapper.Map<Player>(player);
                newPlayers.Add(newPlayer);
                context.Players.Add(newPlayer);

                if (newPlayers.Count % 1000 == 0)
                {
                    await context.SaveChangesAsync();
                }
                playersDic[player.ToonId] = newPlayer.PlayerId;
            }
        }
        if (newPlayers.Count > 0)
        {
            await context.SaveChangesAsync();
        }
        return newPlayers;
    }
}