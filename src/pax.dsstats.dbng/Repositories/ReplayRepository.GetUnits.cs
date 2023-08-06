using Microsoft.EntityFrameworkCore;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Repositories;

public partial class ReplayRepository
{
    private async Task<IQueryable<Replay>> GetUnitReplays(IQueryable<Replay> replays, ReplaysRequest request)
    {
        if (request.UnitRequest == null)
        {
            return replays;
        }

        replays = await GetUnitRequestReplays(replays, request.UnitRequest);
        replays = await GetUpgradeRequestReplays(replays, request.UnitRequest);
        return replays;
    }

    private async Task<IQueryable<Replay>> GetUnitRequestReplays(IQueryable<Replay> replays, ReplaysUnitsRequest request)
    {
        foreach (var unitRequest in request.UnitRequests)
        {
            if (string.IsNullOrEmpty(unitRequest.UnitName))
            {
                continue;
            }

            var unitId = await context.Units
                .Where(x => x.Name == unitRequest.UnitName)
                .Select(s => s.UnitId)
                .FirstOrDefaultAsync();

            if (unitId == 0)
            {
                continue;
            }

            replays = from r in replays
                        from rp in r.ReplayPlayers
                        from s in rp.Spawns
                        from u in s.Units
                        where s.Breakpoint == unitRequest.Breakpoint
                        && u.UnitId == unitId
                        && (unitRequest.Count == 0 ? true :
                            unitRequest.Less ? u.Count < unitRequest.Count : u.Count >= unitRequest.Count)
                        select r;
        }
        return replays;
    }

    private async Task<IQueryable<Replay>> GetUpgradeRequestReplays(IQueryable<Replay> replays, ReplaysUnitsRequest request)
    {
        foreach (var upgradeRequest in request.UpgradeRequests) 
        { 
            if (string.IsNullOrEmpty(upgradeRequest.UpgradeName))
            {
                continue;
            }

            var upgradeId = await context.Upgrades
                .Where(x => x.Name == upgradeRequest.UpgradeName)
                .Select(s => s.UpgradeId)
                .FirstOrDefaultAsync();

            if (upgradeId == 0)
            {
                continue;
            }

            int gameloop = (int)(upgradeRequest.Minutes * 60 * 22.4);
            replays = from r in replays
                      from rp in r.ReplayPlayers
                      from u in rp.Upgrades
                      where u.UpgradeId == upgradeId
                        && (upgradeRequest.Minutes == 0 ? true :
                           upgradeRequest.Less ? u.Gameloop < gameloop : u.Gameloop >= gameloop)
                      select r;
        }
        return replays;
    }

    public async Task<List<string>> GetUnitNames()
    {
        var names = await context.Units
            .Select(s => s.Name)
            .ToListAsync();

        HashSet<string> cleanNames = new();
        List<string> cmdrNames = Data.GetCommanders(Data.CmdrGet.NoNone).Select(s => s.ToString()).ToList();

        foreach (var name in names)
        {
            string cleanName = name;
            foreach (var cmdr in cmdrNames)
            {
                cleanName = cleanName.Replace(cmdr, string.Empty);
            }
            cleanNames.Add(cleanName);
        }
        return cleanNames.OrderBy(o => o).ToList();
    }

    public async Task<List<string>> GetUpgradeNames()
    {
        return await context.Upgrades
            .OrderBy(o => o.Name)
            .Select(s => s.Name)
            .ToListAsync();
    }
}
