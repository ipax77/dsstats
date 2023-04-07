
using pax.dsstats.dbng;

namespace pax.dsstats.web.Server.Services.Import;

public partial class ImportService
{

    private async Task<int> CreateAndMapUnits(List<Replay> replays)
    {
        int newUnits = await CreateMissingUnits(replays);
        MapUnits(replays);
        return newUnits;
    }

    private void MapUnits(List<Replay> replays)
    {
        for (int i = 0; i < replays.Count; i++)
        {
            foreach (var rp in replays[i].ReplayPlayers)
            {
                foreach (var sp in rp.Spawns)
                {
                    sp.Units = sp.Units.Select(s => new SpawnUnit()
                    {
                        Count = s.Count,
                        Poss = s.Poss,
                        SpawnId = 0,
                        UnitId = dbCache.Units[s.Unit.Name]
                    }).ToList();
                }
            }
        }
    }

    private async Task<int> CreateMissingUnits(List<Replay> replays)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        List<Unit> newUnits = new();
        for (int i = 0; i < replays.Count; i++) 
        { 
            foreach (var rp in replays[i].ReplayPlayers)
            {
                foreach (var sp in rp.Spawns)
                {
                    foreach (var spu in sp.Units)
                    {
                        if (!dbCache.Units.ContainsKey(spu.Unit.Name))
                        {
                            Unit unit = new()
                            {
                                Name = spu.Unit.Name
                            };
                            context.Units.Add(unit);
                            newUnits.Add(unit);
                            dbCache.Units[spu.Unit.Name] = 0;
                        }
                    }
                }
            }
        }
        if (newUnits.Any())
        {
            await context.SaveChangesAsync();
            newUnits.ForEach(f => dbCache.Units[f.Name] = f.UnitId);
        }
        return newUnits.Count;
    }
}