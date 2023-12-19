using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace dsstats.db8services;

public class UnitmapService(ReplayContext context) : IUnitmapService
{
    public async Task<Unitmap> GetUnitMap(UnitmapRequest request)
    {
        if (!File.Exists("/data/ds/unitmap1.json"))
        {
            await ProduceUnitMap(request);
        }

        var unitmap = JsonSerializer.Deserialize<Unitmap>(File.ReadAllText("/data/ds/unitmap1.json"));

        if (unitmap is not null)
        {
            return unitmap;
        }

        return await Task.FromResult(new Unitmap());
    }

    private async Task ProduceUnitMap(UnitmapRequest request)
    {
        Dictionary<int, string> unitNames = (await context.Units
            .Select(s => new { s.UnitId, s.Name }).ToListAsync())
            .ToDictionary(k => k.UnitId, v => v.Name);

        var query1 = from r in context.Replays
                     from rp in r.ReplayPlayers
                     from sp in rp.Spawns
                     from u in sp.Units
                     where r.GameTime > new DateTime(2023, 1, 22)
                      && (rp.ComboReplayPlayerRating != null && rp.ComboReplayPlayerRating.Rating >= 1500)
                      && sp.Breakpoint == shared.Breakpoint.Min5
                      && rp.Team == 1
                      && rp.Race == Commander.Fenix
                      && rp.OppRace == Commander.Dehaka
                     select u;

        var query2 = from r in context.Replays
                     from rp in r.ReplayPlayers
                     from sp in rp.Spawns
                     from u in sp.Units
                     where r.GameTime > new DateTime(2023, 1, 22)
                      && (rp.ComboReplayPlayerRating != null && rp.ComboReplayPlayerRating.Rating >= 1500)
                      && sp.Breakpoint == shared.Breakpoint.Min5
                      && rp.Team == 2
                      && rp.Race == Commander.Fenix
                      && rp.OppRace == Commander.Dehaka
                     select u;

        var units1 = await query1.ToListAsync();
        var units2 = await query1.ToListAsync();

        RotatedArea area1 = new(Area.SpawnArea1);
        RotatedArea area2 = new(Area.SpawnArea2);

        List<PointInfo> points1 = GetPointInfos(area1, units1, unitNames);
        List<PointInfo> points2 = GetPointInfos(area2, units2, unitNames);

        Unitmap unitMap1 = new()
        {
            Infos = points1,
        };
        Unitmap unitMap2 = new()
        {
            Infos = points2,
        };

        var json1 = JsonSerializer.Serialize(unitMap1, new JsonSerializerOptions() { WriteIndented = true });
        File.WriteAllText("/data/ds/unitmap1.json", json1);
        var json2 = JsonSerializer.Serialize(unitMap2, new JsonSerializerOptions() { WriteIndented = true });
        File.WriteAllText("/data/ds/unitmap2.json", json2);
    }

    private static List<PointInfo> GetPointInfos(RotatedArea normalizedArea, List<SpawnUnit> units, Dictionary<int, string> unitNames)
    {
        Dictionary<Point, Dictionary<string, int>> infos = new();

        foreach (var unit in units)
        {
            if (string.IsNullOrEmpty(unit.Poss))
            {
                continue;
            }
            var coords = unit.Poss.Split(',', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < coords.Length; i = i + 2)
            {
                Point point = new(int.Parse(coords[i]), int.Parse(coords[i + 1]));
                if (!normalizedArea.Area.IsPointInside(point))
                {
                    continue;
                }
                if (!infos.TryGetValue(point, out var info)
                    || info is null)
                {
                    info = infos[point] = new();
                }

                var name = unitNames[unit.UnitId];

                if (!info.ContainsKey(name))
                {
                    info[name] = 1;
                }
                else
                {
                    info[name]++;
                }
            }
        }
        return infos.Select(s => new PointInfo()
        {
            Point = normalizedArea.GetNormalizedPoint(s.Key),
            UnitCounts = s.Value
        }).ToList();
    }
}


