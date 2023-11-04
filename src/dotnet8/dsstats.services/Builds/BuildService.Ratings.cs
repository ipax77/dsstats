
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace dsstats.services;

public partial class BuildService
{


    private async Task<BuildResponse> ProduceBuild(BuildRequest request, CancellationToken token)
    {
        (var start, var end) = Data.TimeperiodSelected(request.TimePeriod);
        bool noEnd = end >= DateTime.Today.AddDays(-2);
        var ratingTypes = GetRatingTypes(request);

        var unitsquery = from rp in context.ReplayPlayers
                         from sp in rp.Spawns
                         from su in sp.SpawnUnits
                         where rp.Race == request.Interest
                          && rp.Replay.GameTime >= start
                          && (noEnd ? true : rp.Replay.GameTime < end)
                          && rp.Replay.ReplayRating!.LeaverType == LeaverType.None
                          && ratingTypes.Contains(rp.Replay.ReplayRating!.RatingType)
                          && sp.Breakpoint == request.Breakpoint
                          && (request.Versus == Commander.None ? true : rp.OppRace == request.Versus)
                          && (request.FromRating <= Data.MinBuildRating ? true : rp.ComboReplayPlayerRating!.Rating > request.FromRating)
                          && (request.ToRating >= Data.MaxBuildRating ? true : rp.ComboReplayPlayerRating!.Rating < request.ToRating)
                         group su by new { su.UnitId, su.Unit.Name } into g
                         select new
                         {
                             g.Key.UnitId,
                             g.Key.Name,
                             UnitCount = g.Sum(s => s.Count),
                         };

        var result = await unitsquery
            .OrderByDescending(o => o.UnitCount)
            .ToListAsync(token);

        var buildCounts = await GetCountResult(request, token);

        return new()
        {
            BuildCounts = buildCounts,
            Units = result.Select(s => new BuildResponseBreakpointUnit()
            {
                Name = s.Name,
                Count = buildCounts.Count == 0 ? s.UnitCount : Math.Round(s.UnitCount / (double)buildCounts.Count, 2)
            }).ToList()
        };
    }

    private List<RatingType> GetRatingTypes(BuildRequest request)
    {
        return request.RatingType switch
        {
            RatingType.Cmdr => new() { RatingType.Cmdr, RatingType.CmdrTE },
            RatingType.Std => new() { RatingType.Std, RatingType.StdTE },
            _ => new() { request.RatingType }
        };
    }

    private async Task<BuildCounts> GetCountResult(BuildRequest request, CancellationToken token)
    {
        (var start, var end) = Data.TimeperiodSelected(request.TimePeriod);
        bool noEnd = end >= DateTime.Today.AddDays(-2);


        var sql =
        $@"select count(distinct r.ReplayId) as count,
 round(sum(case when rp.PlayerResult = 1 THEN 1 ELSE 0 END) * 100 / count(rp.ReplayPlayerId), 2) as winrate,
 round(avg(rpr.Change), 2) as avggain,
 round(avg(r.Duration), 2) as duration,
 round(avg(sp.GasCount), 2) as gas,
 round(avg(sp.UpgradeSpent), 2) as upgrades
from ReplayPlayers as rp
inner join Spawns as sp on rp.ReplayPlayerId = sp.ReplayPlayerId
inner join Replays as r on rp.ReplayId = r.ReplayId
inner join ReplayRatings as rr on r.ReplayId = rr.ReplayId
inner join ComboReplayPlayerRatings as rpr on rp.ReplayPlayerId = rpr.ReplayPlayerId
where rr.LeaverType = 0
    and rr.RatingType in ({string.Join(',', GetRatingTypes(request).Select(s => (int)s))})
    and r.GameTime >= '{start.ToString("yyyy-MM-dd")}'
    and {(noEnd ? "true" : $"r.GameTime < '{end.ToString("yyyy-MM-dd")}'")}
    and rp.Race = {(int)request.Interest}
    and {(request.Versus == Commander.None ? "true" : $"rp.OppRace = {(int)request.Versus}")}
    and {(request.FromRating > Data.MinBuildRating ? $"rpr.Rating >= {request.FromRating}" : "true")}
    and {(request.ToRating < Data.MaxBuildRating ? $"rpr.Rating <= {request.ToRating}" : "true")}
    and sp.Breakpoint = {(int)request.Breakpoint};";

        Console.WriteLine(sql);

        try
        {
            var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(token);

            var command = new MySqlCommand(sql, connection);

            var reader = await command.ExecuteReaderAsync(token);


            while (await reader.ReadAsync(token))
            {
                int count = reader.GetInt32(0);
                double winrate = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                double avggain = reader.IsDBNull(2) ? 0 : reader.GetDouble(2);
                double duration = reader.IsDBNull(3) ? 0 : reader.GetDouble(3);
                double gas = reader.IsDBNull(4) ? 0 : reader.GetDouble(4);
                double upgrades = reader.IsDBNull(5) ? 0 : reader.GetDouble(5);

                BuildCounts buildCounts = new()
                {
                    Count = count,
                    Winrate = winrate,
                    AvgGain = avggain,
                    Duration = duration,
                    Gas = gas,
                    Upgrades = upgrades
                };
                return buildCounts;
            }
        }
        catch (Exception ex)
        {
            logger.LogError("failed getting build count: {error}", ex.Message);
        }
        return new();
    }
}