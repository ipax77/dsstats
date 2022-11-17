
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;
using System.Text;

namespace pax.dsstats.dbng.Services;

public partial class ImportService
{
    private async Task<int> CreateAndMapPlayers(ICollection<Replay> replays)
    {
        Dictionary<int, KeyValuePair<int, int>> playersDic;
        List<Player> newPlayers;
        using var scope = serviceProvider.CreateScope();
        using (var context = scope.ServiceProvider.GetRequiredService<ReplayContext>())
        {
            playersDic = await GetPlayerDic(context);
            newPlayers = await CreateMissingPlayers(context, playersDic, replays);
        }

        newPlayers.ForEach(f => playersDic[f.ToonId] = new KeyValuePair<int, int>(f.PlayerId, f.RegionId));
        var playerIdRegionIdFix = MapPlayers(playersDic, replays);

        await FixPlayersRegionId(playerIdRegionIdFix);
        return newPlayers.Count;
    }

    private static async Task<Dictionary<int, KeyValuePair<int, int>>> GetPlayerDic(ReplayContext context)
    {
        var players = await context.Players
            .AsNoTracking()
            .Select(s => new { s.PlayerId, s.ToonId, s.RegionId })
            .ToListAsync();

        return players.ToDictionary(k => k.ToonId, v => new KeyValuePair<int, int>(v.PlayerId, v.RegionId));
    }

    private Dictionary<int, int> MapPlayers(Dictionary<int, KeyValuePair<int, int>> playersDic, ICollection<Replay> replays)
    {
        Dictionary<int, int> playerIdRegionIdFix = new();
        foreach (var replayPlayer in replays.SelectMany(s => s.ReplayPlayers))
        {
            if (replayPlayer.Player == null)
            {
                continue;
            }

            var playerDic = playersDic[replayPlayer.Player.ToonId];
            if (replayPlayer.Player.RegionId > 0)
            {
                if (playerDic.Value == 0)
                {
                    playerIdRegionIdFix[playerDic.Key] = replayPlayer.Player.RegionId;
                }
                else if (playerDic.Value != replayPlayer.Player.RegionId)
                {
                    logger.LogWarning($"switching region for {replayPlayer.Player.ToonId} from {playerDic.Value} to {replayPlayer.Player.RegionId}");
                    playerIdRegionIdFix[playerDic.Key] = replayPlayer.Player.RegionId;
                }
            }

#pragma warning disable CS8625 // set playerId without loading the entity
            replayPlayer.PlayerId = playersDic[replayPlayer.Player.ToonId].Key;
            replayPlayer.Player = null;
#pragma warning restore CS8625
        }
        return playerIdRegionIdFix;
    }

    private async Task<List<Player>> CreateMissingPlayers(ReplayContext context, Dictionary<int, KeyValuePair<int, int>> playersDic, ICollection<Replay> replays)
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
                playersDic[player.ToonId] = new KeyValuePair<int, int>(newPlayer.PlayerId, newPlayer.RegionId);
            }
        }
        if (newPlayers.Count > 0)
        {
            await context.SaveChangesAsync();
        }
        return newPlayers;
    }

    private async Task FixPlayersRegionId(Dictionary<int, int> playerIdRegionIdFix)
    {
        if (!playerIdRegionIdFix.Any())
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        StringBuilder sb = new();
        int i = 0;

        foreach (var ent in playerIdRegionIdFix)
        {
            sb.Append($"UPDATE {nameof(ReplayContext.Players)}" +
                $" SET {nameof(Player.RegionId)} = {ent.Value}" +
                $" WHERE {nameof(Player.PlayerId)} = {ent.Key}; ");

            i++;
            if (i % 500 == 0)
            {
                await context.Database.ExecuteSqlRawAsync(sb.ToString());
                sb.Clear();
            }
        }

        if (sb.Length > 0)
        {
            await context.Database.ExecuteSqlRawAsync(sb.ToString());
        }
    }
}