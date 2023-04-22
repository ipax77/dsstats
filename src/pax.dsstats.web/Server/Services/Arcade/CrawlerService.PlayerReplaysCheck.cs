using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;

namespace pax.dsstats.web.Server.Services.Arcade;

public partial class CrawlerService
{
    public async Task CheckPlayerReplays()
    {
        PlayerId playerId = new()
        {
            ProfileId = 1659576,
            RegionId = 2,
            RealmId = 1
        };

        var dsReplays = await GetDsReplays(playerId);
        var arReplays = await GetArcadeReplays(playerId);

        CompareResult result = new();

        for (int i = 0; i < dsReplays.Count; i++)
        {
            var dsIndex = result.DsMod + i;
            var arIndex = result.ArMod + i;

            if (dsIndex > dsReplays.Count || arIndex > dsReplays.Count)
            {
                break;
            }

            CompareReplays(dsReplays[dsIndex], arReplays[arIndex], playerId, result);
        }

        logger.LogWarning($"{result}");

        logger.LogWarning($"avgTimediff: {result.TimeDiffs.Average()}");
        logger.LogWarning($"avgDurdiff: {result.DurDiffs.Average()}");

    }

    private void CompareReplays(Replay dsReplay, ArcadeReplay arReplay, PlayerId playerId, CompareResult result)
    {
        var dsPlayer = dsReplay.ReplayPlayers
            .FirstOrDefault(f => f.Player.ToonId == playerId.ProfileId && f.Player.RegionId == playerId.RegionId && f.Player.RealmId == playerId.RealmId);
        var arPlayer = arReplay.ArcadeReplayPlayers
            .FirstOrDefault(f => f.ArcadePlayer.ProfileId == playerId.ProfileId && f.ArcadePlayer.RegionId == playerId.RegionId && f.ArcadePlayer.RealmId == playerId.RealmId);

        if (dsPlayer == null || arPlayer == null)
        {
            result.Bad++;
            return;
        }

        if (dsPlayer.GamePos == arPlayer.SlotNumber 
            && dsPlayer.PlayerResult == arPlayer.PlayerResult)
        {
            result.Good++;
            result.TimeDiffs.Add(Math.Abs((int)(dsReplay.GameTime - arReplay.CreatedAt).TotalSeconds));
            result.DurDiffs.Add(Math.Abs(dsReplay.Duration - arReplay.Duration));
        }
        else
        {
            result.ArMod++;
            result.DsMod = Math.Max(0, result.DsMod - 1);
        }
    }

    private async Task<List<ArcadeReplay>> GetArcadeReplays(PlayerId playerId)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();


        var replaysQuery = from r in context.ArcadeReplays
                            .Include(i => i.ArcadeReplayPlayers)
                                .ThenInclude(i => i.ArcadePlayer)
                           from rp in r.ArcadeReplayPlayers
                           where r.CreatedAt > new DateTime(2021, 2, 1)
                                && rp.ArcadePlayer.ProfileId == playerId.ProfileId
                                && rp.ArcadePlayer.RegionId == playerId.RegionId
                                && rp.ArcadePlayer.RealmId == playerId.RealmId
                                && rp.ArcadeReplay.GameMode == shared.GameMode.Commanders
                                && rp.ArcadeReplay.PlayerCount == 6
                                && rp.ArcadeReplay.WinnerTeam > 0
                           select r;

        var arReplays = await replaysQuery
            .OrderByDescending(o => o.CreatedAt)
            .Take(1000)
            .AsNoTracking()
            .ToListAsync();

        return arReplays;
    }

    private async Task<List<Replay>> GetDsReplays(PlayerId playerId)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();


        var replaysQuery = from r in context.Replays
                            .Include(i => i.ReplayPlayers)
                                .ThenInclude(i => i.Player)
                           from rp in r.ReplayPlayers
                           where r.GameTime > new DateTime(2021, 2, 1)
                                && rp.Player.ToonId == playerId.ProfileId
                                && rp.Player.RegionId == playerId.RegionId
                                && rp.Player.RealmId == playerId.RealmId
                                && rp.Replay.GameMode == shared.GameMode.Commanders
                                && rp.Replay.Playercount == 6
                                && rp.Replay.WinnerTeam > 0
                           select r;

        var dsReplays = await replaysQuery
            .OrderByDescending(o => o.GameTime)
            .Take(1000)
            .AsNoTracking()
            .ToListAsync();
        return dsReplays;
    }
}

public record CompareResult
{
    public int Good { get; set; }
    public int TeamMissmatch { get; set; }
    public int ResultMissmatch { get; set; }
    public int Bad { get; set; }
    public int ArcadeMissing { get; set; }
    public int DsstatsMissing { get; set; }
    public int ArMod { get; set; }
    public int DsMod { get; set; }
    public List<int> TimeDiffs { get; set; } = new();
    public List<int> DurDiffs { get; set; } = new();
}