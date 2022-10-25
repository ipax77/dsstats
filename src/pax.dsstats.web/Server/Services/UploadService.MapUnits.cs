using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng;
using pax.dsstats.shared;

namespace pax.dsstats.web.Server.Services;

public partial class UploadService
{
    private SemaphoreSlim ssMapUnits = new(1, 1);
    private Dictionary<UnitDicKey, int> unitsDic = new();

    public async Task MapUnits(ICollection<Replay> replays)
    {
        await ssMapUnits.WaitAsync();
        try
        {
            using var scope = serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            if (!unitsDic.Any())
            {
                await InitUnitsDic(context);
            }

            await CreateMissingUnits(context, unitsDic, replays);

            MapSpawnUnits(unitsDic, replays);
        }
        catch (Exception ex)
        {
            logger.LogError($"failed mapping units: {ex.Message}");
        }
        finally
        {
            ssMapUnits.Release();
        }
    }

    private async Task InitUnitsDic(ReplayContext context)
    {
        var untrackedDbUnits = await context.Units
            .AsNoTracking()
            .ToListAsync();

        unitsDic = untrackedDbUnits.ToDictionary(key => new UnitDicKey(key.Name, key.Commander), value => value.UnitId);
    }

    private static void MapSpawnUnits(Dictionary<UnitDicKey, int> untrackedDbUnits, ICollection<Replay> replays)
    {
        foreach (var spawn in replays.SelectMany(s => s.Players).SelectMany(s => s.Spawns))
        {
            spawn.Units = spawn.Units.Select(s => new SpawnUnit()
            {
                Count = s.Count,
                Poss = s.Poss,
                SpawnId = 0,
                UnitId = untrackedDbUnits[new(s.Unit.Name, s.Unit.Commander)]
            }).ToList();
        }
    }

    private async Task CreateMissingUnits(ReplayContext context, Dictionary<UnitDicKey, int> untrackedDbUnits, ICollection<Replay> replays)
    {
        foreach (var unit in replays.SelectMany(s => s.Players).SelectMany(s => s.Spawns).SelectMany(s => s.Units).Select(s => mapper.Map<UnitDto>(s.Unit)).Distinct())
        {
            var dicKey = new UnitDicKey(unit.Name, unit.Commander);
            if (!untrackedDbUnits.ContainsKey(dicKey))
            {
                var dbUnit = await CreateUnit(context, unit);
                untrackedDbUnits[dicKey] = dbUnit.UnitId;
            }
        }
    }

    private async Task<Unit> CreateUnit(ReplayContext context, UnitDto unitDto)
    {
        var unit = mapper.Map<Unit>(unitDto);
        context.Units.Add(unit);
        await context.SaveChangesAsync();
        return unit;
    }
}

internal record UnitDicKey
{
    public UnitDicKey(string name, Commander commander)
    {
        Name = name;
        Commander = commander;
    }
    public string Name { get; init; } = null!;
    public Commander Commander { get; init; }
}
