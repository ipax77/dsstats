
using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class StatsService
{
    public async Task GetUpgradeStats(BuildRequest buildRequest)
    {
        List<int> toonIds = buildRequest.PlayerNames.Select(s => s.ToonId).ToList();

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
                           select new
                           {
                               Breakpoint = g.Key,
                               Count = g.Count(),
                               Upgrades = g.Sum(s => s.UpgradeSpent),
                               Army = g.Sum(s => s.ArmyValue)
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
                            select new
                            {
                                Breakpoint = g.Key,
                                Count = g.Count(),
                                Upgrades = g.Sum(s => s.UpgradeSpent),
                                Army = g.Sum(s => s.ArmyValue)
                            };

            var lupgrades = await upgrades.ToListAsync();

            foreach (var upgrade in lupgrades)
            {
                Console.WriteLine($"{buildRequest.Interest}, {upgrade.Breakpoint}, {upgrade.Count}, {upgrade.Upgrades / upgrade.Count}, {upgrade.Army / upgrade.Count}");
            }
        }
    }
}


