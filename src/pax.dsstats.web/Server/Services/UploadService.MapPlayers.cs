using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.shared;

namespace pax.dsstats.web.Server.Services;

public partial class UploadService
{
    private SemaphoreSlim ssMapPlayers = new(1, 1);
    private Dictionary<int, int> playersDic = new();

    public async Task MapPlayers(ICollection<Replay> replays)
    {
        await ssMapPlayers.WaitAsync();
        try
        {
            using var scope = serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            if (!playersDic.Any())
            {
                await InitPlayerDic(context);
            }

            await CreateMissingPlayers(context, playersDic, replays);

            MapPlayers(playersDic, replays);
        }
        catch (Exception ex)
        {
            logger.LogError($"failed mapping players: {ex.Message}");
        }
        finally
        {
            ssMapPlayers.Release();
        }
    }

    private async Task InitPlayerDic(ReplayContext context)
    {
        var players = await context.Players
            .AsNoTracking()
            .Select(s => new { s.PlayerId, s.ToonId })
            .ToListAsync();

        playersDic = players.ToDictionary(k => k.ToonId, v => v.PlayerId);
    }

    private static void MapPlayers(Dictionary<int, int> playersDic, ICollection<Replay> replays)
    {
        foreach (var replayPlayer in replays.SelectMany(s => s.ReplayPlayers))
        {
#pragma warning disable CS8602
#pragma warning disable CS8625
            replayPlayer.PlayerId = playersDic[replayPlayer.Player.ToonId];
            replayPlayer.Player = null;
#pragma warning restore CS8602
#pragma warning restore CS8625

        }
    }

    private async Task CreateMissingPlayers(ReplayContext context, Dictionary<int, int> playersDic, ICollection<Replay> replays)
    {
        int i = 0;
        foreach (var player in replays.SelectMany(s => s.ReplayPlayers).Select(s => mapper.Map<PlayerDto>(s.Player)).Distinct())
        {
            if (!playersDic.ContainsKey(player.ToonId))
            {
                context.Players.Add(mapper.Map<Player>(player));
                i++;
                if (i % 1000 == 0)
                {
                    await context.SaveChangesAsync();
                }
            }
        }
        if (i > 0)
        {
            await context.SaveChangesAsync();
            await InitPlayerDic(context);
        }
    }
}
