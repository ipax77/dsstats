using dsstats.db8;
using dsstats.shared.Calc;
using dsstats.shared.Interfaces;
using dsstats.shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Frozen;
using Microsoft.EntityFrameworkCore;

namespace dsstats.ratings;

public partial class RatingService
{
    public async Task ContinueArcadeRatings()
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var ratingSaveService = scope.ServiceProvider.GetRequiredService<IRatingsSaveService>();

        // await DebugDeleteArcadeRatings();

        DsstatsCalcRequest dsstatsRequest = new()
        {
            FromDate = DateTime.UtcNow.AddDays(-6),
            GameModes = new List<int>() { 3, 4, 7 },
            Skip = 0,
            Take = 2 * 3000 * 6
        };

        await CreateMaterializedReplays();
        var calcDtos = await GetMaterializedArcadeContinueCalcDtos(dsstatsRequest, context);

        if (calcDtos.Count == 0)
        {
            logger.LogInformation("arcade ratings continue: nothing to do.");
            return;
        }
        else
        {
            logger.LogInformation("arcade ratings continue: {count}", calcDtos.Count);
        }

        var ratingRequest = new CalcRatingRequest()
        {
            RatingCalcType = RatingCalcType.Dsstats,
            MmrIdRatings = new()
                    {
                        { 1, new() },
                        { 2, new() },
                        { 3, new() },
                        { 4, new() }
                    },
            BannedPlayers = new Dictionary<PlayerId, bool>().ToFrozenDictionary()
        };


        ratingRequest.ReplayPlayerRatingAppendId = await context.ArcadeReplayDsPlayerRatings
            .OrderByDescending(o => o.ArcadeReplayDsPlayerRatingId)
            .Select(s => s.ArcadeReplayDsPlayerRatingId)
            .FirstOrDefaultAsync();
        ratingRequest.ReplayRatingAppendId = await context.ArcadeReplayRatings
            .OrderByDescending(o => o.ArcadeReplayRatingId)
            .Select(s => s.ArcadeReplayRatingId)
            .FirstOrDefaultAsync();

        ratingRequest.MmrIdRatings = await GetArcadeMmrIdRatings(calcDtos);


        List<shared.Calc.ReplayRatingDto> replayRatings = new();

        for (int i = 0; i < calcDtos.Count; i++)
        {
            var calcDto = calcDtos[i];
            var rating = ratings.lib.Ratings.ProcessReplay(calcDto, ratingRequest);
            if (rating is not null && !calcDto.IsArcade)
            {
                replayRatings.Add(rating);
            }
        }

        await ratingSaveService.SaveArcadeStepResult(replayRatings,
                                                    ratingRequest.ReplayRatingAppendId,
                                                    ratingRequest.ReplayPlayerRatingAppendId);

        logger.LogInformation("arcade replay ratings produced: {count}", replayRatings.Count);

        await ratingSaveService.SaveContinueArcadeRatings(ratingRequest.MmrIdRatings,
                                                         replayRatings);
    }

    private async Task<Dictionary<int, Dictionary<PlayerId, CalcRating>>> GetArcadeMmrIdRatings(List<CalcDto> calcDtos)
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

        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var query = from pr in context.ArcadePlayerRatings
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

    private async Task<List<CalcDto>> GetMaterializedArcadeContinueCalcDtos(DsstatsCalcRequest request, ReplayContext context)
    {
        var query = from r in context.MaterializedArcadeReplays
                    join rr in context.ArcadeReplayRatings on r.ArcadeReplayId equals rr.ArcadeReplayId into grouping
                    from g in grouping.DefaultIfEmpty()
                    where r.CreatedAt >= request.FromDate && g == null
                    orderby r.MaterializedArcadeReplayId
                    select new CalcDto()
                    {
                        ReplayId = r.ArcadeReplayId,
                        GameTime = r.CreatedAt,
                        Duration = r.Duration,
                        GameMode = (int)r.GameMode,
                        WinnerTeam = r.WinnerTeam,
                        Players = context.ArcadeReplayDsPlayers
                            .Where(x => x.ArcadeReplayId == r.ArcadeReplayId)
                            .Select(t => new PlayerCalcDto()
                            {
                                ReplayPlayerId = t.ArcadeReplayDsPlayerId,
                                GamePos = t.SlotNumber,
                                PlayerResult = (int)t.PlayerResult,
                                Team = t.Team,
                                PlayerId = new(t.Player!.ToonId, t.Player.RealmId, t.Player.RegionId)
                            }).ToList()
                    };
        return await query
            .AsSplitQuery()
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync();
    }

    private async Task DebugDeleteArcadeRatings()
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var query = from r in context.ArcadeReplays
                    join rr in context.ArcadeReplayRatings on r.ArcadeReplayId equals rr.ArcadeReplayId
                    orderby r.CreatedAt descending
                    select rr;

        var ratings = await query
           .Take(15000)
           .ToListAsync();

        if (ratings.Count == 0)
        {
            return;
        }

        var replayIds = ratings.Select(s => s.ArcadeReplayId).ToList();

        var replayPlayerIds = await context.ArcadeReplays
            .Where(x => replayIds.Contains(x.ArcadeReplayId))
            .SelectMany(s => s.ArcadeReplayDsPlayers)
            .Select(s => s.ArcadeReplayDsPlayerId)
            .ToListAsync();

        var replayPlayerRatings = await context.ArcadeReplayDsPlayerRatings
            .Where(x => replayPlayerIds.Contains(x.ArcadeReplayDsPlayerId))
            .ToListAsync();

        context.ArcadeReplayRatings.RemoveRange(ratings);
        context.ArcadeReplayDsPlayerRatings.RemoveRange(replayPlayerRatings);

        await context.SaveChangesAsync();
    }
}
