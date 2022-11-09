
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class ImportService
{

    public async Task<int> CreateAndMapUnits(ICollection<Replay> replays)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var unitsDic = await GetUnitsDic(context);
        var newUnits = await CreateMissingUnits(context, unitsDic, replays);
        newUnits.ForEach(f => unitsDic[f.Name] = f.UnitId);
        MapSpawnUnits(unitsDic, replays);
        return newUnits.Count;
    }

    private static async Task<Dictionary<string, int>> GetUnitsDic(ReplayContext context)
    {
        var untrackedDbUnits = await context.Units
            .AsNoTracking()
            .ToListAsync();

        return untrackedDbUnits.ToDictionary(key => key.Name, value => value.UnitId);
    }

    private static void MapSpawnUnits(Dictionary<string, int> untrackedDbUnits, ICollection<Replay> replays)
    {
        foreach (var spawn in replays.SelectMany(s => s.ReplayPlayers).SelectMany(s => s.Spawns))
        {
            spawn.Units = spawn.Units.Select(s => new SpawnUnit()
            {
                Count = s.Count,
                Poss = s.Poss,
                SpawnId = 0,
                UnitId = untrackedDbUnits[s.Unit.Name]
            }).ToList();
        }
    }

    private async Task<List<Unit>> CreateMissingUnits(ReplayContext context, Dictionary<string, int> untrackedDbUnits, ICollection<Replay> replays)
    {
        List<Unit> newUnits = new();
        foreach (var unitName in replays.SelectMany(s => s.ReplayPlayers).SelectMany(s => s.Spawns).SelectMany(s => s.Units).Select(s => s.Unit.Name).Distinct())
        {
            if (!untrackedDbUnits.ContainsKey(unitName))
            {
                var dbUnit = new Unit()
                {
                    Name = unitName
                };
                context.Units.Add(dbUnit);
                newUnits.Add(dbUnit);

                if (newUnits.Count % 1000 == 0)
                {
                    await context.SaveChangesAsync();
                }
            }
        }
        if (newUnits.Count > 0)
        {
            await context.SaveChangesAsync();
        }
        return newUnits;
    }
}