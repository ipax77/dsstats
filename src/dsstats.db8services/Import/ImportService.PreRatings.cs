
using dsstats.db8;
using dsstats.ratings;
using dsstats.ratings.lib;
using dsstats.shared;
using dsstats.shared.Calc;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace dsstats.db8services.Import;

public partial class ImportService
{
    public async Task SetPreRatings()
    {
        if (IsMaui)
        {
            return;
        }

        using var scope = serviceProvider.CreateAsyncScope();
        var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();

        DsstatsCalcRequest request = new()
        {
            FromDate = DateTime.UtcNow.AddHours(-3),
            Continue = true,
            Skip = 0,
            Take = 101
        };

        var calcDtos = await ratingService.GetDsstatsCalcDtos(request);

        if (calcDtos.Count == 0)
        {
            return;
        }

        CalcRatingRequest dsRequest = new()
        {
            RatingCalcType = RatingCalcType.Dsstats,
            MmrIdRatings = await GetDsstatsMmrIdRatings(calcDtos)
        };

        CalcRatingRequest cbRequest = new()
        {
            RatingCalcType = RatingCalcType.Combo,
            MmrIdRatings = await GetComboMmrIdRatings(calcDtos)
        };

        List<shared.Calc.ReplayRatingDto> dsReplayRatings = new();
        List<shared.Calc.ReplayRatingDto> cbReplayRatings = new();

        foreach (var calcDto in calcDtos)
        {
            var dsRating = Ratings.ProcessReplay(calcDto, dsRequest);
            if (dsRating is not null)
            {
                dsReplayRatings.Add(dsRating);
            }

            var cbRating = Ratings.ProcessReplay(calcDto, cbRequest);
            if (cbRating is not null)
            {
                cbReplayRatings.Add(cbRating);
            }
        }

        await SaveDsstatsPreRatings(dsReplayRatings);
        await SaveComboPreRatings(cbReplayRatings);
    }


    private async Task SaveDsstatsPreRatings(List<shared.Calc.ReplayRatingDto> ratingDtos)
    {
        List<ReplayRating> ratings = new();
        foreach (var ratingDto in ratingDtos)
        {
            ratings.Add(new()
            {
                RatingType = (RatingType)ratingDto.RatingType,
                LeaverType = (LeaverType)ratingDto.LeaverType,
                ExpectationToWin = ratingDto.ExpectationToWin,
                ReplayId = ratingDto.ReplayId,
                IsPreRating = true,
                RepPlayerRatings = ratingDto.RepPlayerRatings
                    .Select(s => new RepPlayerRating()
                    {
                        GamePos = s.GamePos,
                        Rating = s.Rating,
                        RatingChange = s.RatingChange,
                        Games = s.Games,
                        Consistency = s.Confidence,
                        Confidence = s.Confidence,
                        ReplayPlayerId = s.ReplayPlayerId,
                    }).ToList()
            });
        }

        if (ratings.Count == 0)
        {
            return;
        }

        using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        context.ReplayRatings.AddRange(ratings);
        await context.SaveChangesAsync();
    }

    private async Task SaveComboPreRatings(List<shared.Calc.ReplayRatingDto> ratingDtos)
    {
        List<ComboReplayRating> ratings = new();
        List<ComboReplayPlayerRating> replayPlayerRatings = new();

        foreach (var ratingDto in ratingDtos)
        {
            ratings.Add(new ComboReplayRating()
            {
                RatingType = (RatingType)ratingDto.RatingType,
                LeaverType = (LeaverType)ratingDto.LeaverType,
                ExpectationToWin = ratingDto.ExpectationToWin,
                ReplayId = ratingDto.ReplayId,
                AvgRating = Convert.ToInt32(ratingDto.RepPlayerRatings.Average(a => a.Rating)),
                IsPreRating = true,
            });

            replayPlayerRatings.AddRange(ratingDto.RepPlayerRatings
                .Select(s => new ComboReplayPlayerRating()
                {
                    GamePos = s.GamePos,
                    Rating = Convert.ToInt32(s.Rating),
                    Change = s.RatingChange,
                    Games = s.Games,
                    Consistency = s.Confidence,
                    Confidence = s.Confidence,
                    ReplayPlayerId = s.ReplayPlayerId,
                }));
        }

        if (ratings.Count == 0)
        {
            return;
        }

        using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        context.ComboReplayRatings.AddRange(ratings);
        context.ComboReplayPlayerRatings.AddRange(replayPlayerRatings);
        await context.SaveChangesAsync();
    }

    private async Task<Dictionary<int, Dictionary<PlayerId, CalcRating>>> GetDsstatsMmrIdRatings(List<CalcDto> calcDtos)
    {
        var ratingTypes = calcDtos.Select(s => s.GetRatingType())
            .Distinct()
            .ToList();

        var playerIds = calcDtos.SelectMany(s => s.Players).Select(s => s.PlayerId)
            .Distinct()
            .ToList();

        var toonIds = playerIds.Select(s => s.ToonId)
            .Distinct()
            .ToList();

        Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings = new()
            {
                { 1, new() },
                { 2, new() },
                { 3, new() },
                { 4, new() }
            };

        using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var query = from pr in context.PlayerRatings
                    join p in context.Players on pr.PlayerId equals p.PlayerId
                    where ratingTypes.Contains((int)pr.RatingType)
                        && toonIds.Contains(p.ToonId)
                    select new
                    {
                        pr.RatingType,
                        PlayerId = new PlayerId(p.ToonId, p.RealmId, p.RegionId),
                        pr.Games,
                        pr.Wins,
                        pr.Mvp,
                        Mmr = pr.Rating,
                        pr.Consistency,
                        pr.Confidence,
                        pr.IsUploader,
                        pr.Main,
                        pr.MainCount
                    };

        var ratings = await query.ToListAsync();

        foreach (var playerId in playerIds)
        {
            var plRatings = ratings.Where(s => s.PlayerId == playerId).ToList();

            foreach (var plRating in plRatings)
            {
                mmrIdRatings[(int)plRating.RatingType][playerId] = new()
                {
                    PlayerId = playerId,
                    Games = plRating.Games,
                    Wins = plRating.Wins,
                    Mvps = plRating.Mvp,
                    Mmr = plRating.Mmr,
                    Consistency = plRating.Consistency,
                    Confidence = plRating.Confidence,
                    IsUploader = plRating.IsUploader,
                    CmdrCounts = RatingService.GetFakeCmdrDic(plRating.Main, plRating.MainCount, plRating.Games)
                };
            }
        }

        return mmrIdRatings;
    }

    private async Task<Dictionary<int, Dictionary<PlayerId, CalcRating>>> GetComboMmrIdRatings(List<CalcDto> calcDtos)
    {
        var ratingTypes = calcDtos.Select(s => s.GetRatingType())
            .Distinct()
            .ToList();

        var playerIds = calcDtos.SelectMany(s => s.Players).Select(s => s.PlayerId)
            .Distinct()
            .ToList();

        var toonIds = playerIds.Select(s => s.ToonId)
            .Distinct()
            .ToList();

        Dictionary<int, Dictionary<PlayerId, CalcRating>> mmrIdRatings = new()
            {
                { 1, new() },
                { 2, new() },
                { 3, new() },
                { 4, new() }
            };

        using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var query = from pr in context.ComboPlayerRatings
                    join p in context.Players on pr.PlayerId equals p.PlayerId
                    where ratingTypes.Contains((int)pr.RatingType)
                        && toonIds.Contains(p.ToonId)
                    select new
                    {
                        pr.RatingType,
                        PlayerId = new PlayerId(p.ToonId, p.RealmId, p.RegionId),
                        pr.Games,
                        pr.Wins,
                        Mmr = pr.Rating,
                        pr.Consistency,
                        pr.Confidence,
                    };

        var ratings = await query.ToListAsync();

        foreach (var playerId in playerIds)
        {
            var plRatings = ratings.Where(s => s.PlayerId == playerId).ToList();

            foreach (var plRating in plRatings)
            {
                mmrIdRatings[(int)plRating.RatingType][playerId] = new()
                {
                    PlayerId = playerId,
                    Games = plRating.Games,
                    Wins = plRating.Wins,
                    Mmr = plRating.Mmr,
                    Consistency = plRating.Consistency,
                    Confidence = plRating.Confidence,
                };
            }
        }

        return mmrIdRatings;
    }
}
