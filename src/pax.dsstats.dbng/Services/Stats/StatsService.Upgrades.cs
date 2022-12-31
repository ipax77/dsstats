
using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    public async Task<StatsUpgradesResponse> GetUpgradeStats(BuildRequest buildRequest, CancellationToken token)
    {
        List<int> toonIds = buildRequest.PlayerNames.Select(s => s.ToonId).ToList();

        StatsUpgradesResponse statsUpgradesResponse = new();

        foreach (var cmdr in Data.GetCommanders(Data.CmdrGet.NoNone))
        {
            buildRequest.Interest = cmdr;

            var upgrades = buildRequest.Versus == Commander.None ?
                           from r in context.Replays
                           from rp in r.ReplayPlayers
                           from s in rp.Spawns
                           where r.GameTime > buildRequest.StartTime
                            && toonIds.Contains(rp.Player.ToonId)
                            && rp.Race == buildRequest.Interest
                            && (int)s.Breakpoint < 4
                           group s by s.Breakpoint into g
                           select new StatsUpgradesBpInfo()
                           {
                               Breakpoint = g.Key,
                               Count = g.Count(),
                               UpgradeSpent = g.Sum(s => s.UpgradeSpent),
                               ArmyValue = g.Sum(s => s.ArmyValue),
                               Kills = g.Sum(s => s.KilledValue)
                           }
                          : from r in context.Replays
                            from rp in r.ReplayPlayers
                            from s in rp.Spawns
                            where r.GameTime > buildRequest.StartTime
                         && toonIds.Contains(rp.Player.ToonId)
                         && rp.Race == buildRequest.Interest
                         && rp.OppRace == buildRequest.Versus
                         && (int)s.Breakpoint < 4
                            group s by s.Breakpoint into g
                            select new StatsUpgradesBpInfo()
                            {
                                Breakpoint = g.Key,
                                Count = g.Count(),
                                UpgradeSpent = g.Sum(s => s.UpgradeSpent),
                                ArmyValue = g.Sum(s => s.ArmyValue),
                                Kills = g.Sum(s => s.KilledValue)
                            };

            var lupgrades = await upgrades.ToListAsync(token);
            statsUpgradesResponse.BpInfos[cmdr] = lupgrades;
        }
        return statsUpgradesResponse;
    }
}


