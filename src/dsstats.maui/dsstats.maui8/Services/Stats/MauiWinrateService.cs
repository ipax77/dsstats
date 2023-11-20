
using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dsstats.maui8.Services;

public partial class MauiWinrateService(ReplayContext context, ConfigService configService) : IWinrateService
{
    public async Task<WinrateResponse> GetWinrate(StatsRequest request, CancellationToken token)
    {
        return await ProduceWinrate(request, token).ConfigureAwait(false) ?? new();
    }

    public async Task<WinrateResponse> GetWinrate(WinrateRequest request, CancellationToken token)
    {
        return await ProduceWinrate(request, token).ConfigureAwait(false) ?? new();
    }

    private async Task<WinrateResponse?> ProduceWinrate(StatsRequest request, CancellationToken token)
    {
        var data = await GetData(request, token);

        if (data is null)
        {
            return null;
        }

        if (request.RatingType == RatingType.Std || request.RatingType == RatingType.StdTE)
        {
            data = data.Where(x => x.Commander != Commander.None && (int)x.Commander <= 3).ToList();
        }
        else
        {
            data = data.Where(x => (int)x.Commander > 3).ToList();
        }

        return new()
        {
            Interest = request.Interest,
            WinrateEnts = data,
        };
    }

    private async Task<List<WinrateEnt>?> GetData(StatsRequest request, CancellationToken token)
    {
        (var fromDate, var toDate) = request.GetTimeLimits();
        var tillDate = toDate.AddDays(-2);

        var limits = request.GetFilterLimits();
        var requestNames = configService.GetRequestNames()
            .Select(s => new PlayerId(s.ToonId, s.RealmId, s.RegionId))
            .ToList();
        var toonIds = requestNames.Select(s => s.ToonId).ToList();

        var playerIds = await context.Players.Where(x => toonIds.Contains(x.ToonId))
            .Select(s => new { PlayerId = new PlayerId(s.ToonId, s.RealmId, s.RegionId), DbId = s.PlayerId })
            .ToListAsync();

        List<int> dbIds = new();
        foreach (var requestName in requestNames)
        {
            var playerId = playerIds.FirstOrDefault(f => f.PlayerId == requestName);
            if (playerId is not null)
            {
                dbIds.Add(playerId.DbId);
            }
        }

        var group = request.Interest == Commander.None ?
                    from r in context.Replays
                    from rp in r.ReplayPlayers
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where r.GameTime > fromDate
                     && (toDate > tillDate || r.GameTime <= toDate)
                     && rr.RatingType == request.RatingType
                     && (limits.FromRating <= 0 || rpr.Rating >= limits.FromRating)
                     && (limits.ToRating <= 0 || rpr.Rating <= limits.ToRating)
                     && (limits.FromExp2Win <= 0 || rr.ExpectationToWin >= limits.FromExp2Win)
                     && (limits.ToExp2Win <= 0 || rr.ExpectationToWin <= limits.ToExp2Win)
                     && (!request.WithoutLeavers || rr.LeaverType == LeaverType.None)
                     && (!request.MauiPlayers || dbIds.Contains(rp.PlayerId))
                    group new { rp, rr, rpr, r } by rp.Race into g
                    select new WinrateEnt()
                    {
                        Commander = g.Key,
                        Count = g.Count(),
                        AvgRating = Math.Round(g.Average(a => a.rpr.Rating), 2),
                        AvgGain = Math.Round(g.Average(a => a.rpr.RatingChange), 2),
                        Wins = g.Sum(s => s.rp.PlayerResult == PlayerResult.Win ? 1 : 0),
                        Replays = g.Select(s => s.r.ReplayId).Distinct().Count()
                    }
                    :
                    from r in context.Replays
                    from rp in r.ReplayPlayers
                    join rr in context.ReplayRatings on r.ReplayId equals rr.ReplayId
                    join rpr in context.RepPlayerRatings on rp.ReplayPlayerId equals rpr.ReplayPlayerId
                    where r.GameTime > fromDate
                     && (toDate > tillDate || r.GameTime <= toDate)
                     && rr.RatingType == request.RatingType
                     && (limits.FromRating <= 0 || rpr.Rating >= limits.FromRating)
                     && (limits.ToRating <= 0 || rpr.Rating <= limits.ToRating)
                     && (limits.FromExp2Win <= 0 || rr.ExpectationToWin >= limits.FromExp2Win)
                     && (limits.ToExp2Win <= 0 || rr.ExpectationToWin <= limits.ToExp2Win)
                     && rp.Race == request.Interest
                     && (!request.WithoutLeavers || rr.LeaverType == LeaverType.None)
                     && (!request.MauiPlayers || dbIds.Contains(rp.PlayerId))
                    group new { rp, rr, rpr, r } by rp.OppRace into g
                    select new WinrateEnt()
                    {
                        Commander = g.Key,
                        Count = g.Count(),
                        AvgRating = Math.Round(g.Average(a => a.rpr.Rating), 2),
                        AvgGain = Math.Round(g.Average(a => a.rpr.RatingChange), 2),
                        Wins = g.Sum(s => s.rp.PlayerResult == PlayerResult.Win ? 1 : 0),
                        Replays = g.Select(s => s.r.ReplayId).Distinct().Count()
                    };

        var list = await group.ToListAsync();

        if (request.RatingType == RatingType.Cmdr || request.RatingType == RatingType.CmdrTE)
        {
            return list.Where(x => (int)x.Commander > 3).ToList();
        }
        else
        {
            return list.Where(x => x.Commander != Commander.None && (int)x.Commander <= 3).ToList();
        }
    }
}

