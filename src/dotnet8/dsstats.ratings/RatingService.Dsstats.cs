using dsstats.db8;
using dsstats.shared.Calc;
using dsstats.shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using dsstats.shared.Interfaces;

namespace dsstats.ratings;

public partial class RatingService
{
    private async Task ProduceDsstatsRatings(bool recalc)
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var ratingSaveService = scope.ServiceProvider.GetRequiredService<IRatingsSaveService>();

        DsstatsCalcRequest dsstatsRequest = new()
        {
            FromDate = new DateTime(2018, 1, 1),
            GameModes = new List<int>() { 3, 4, 7 },
            Skip = 0,
            Take = 50000
        };

        await CleanupPreRatings(context);
        
        var ratingRequest = recalc ? new()
        {
            RatingCalcType = RatingCalcType.Dsstats,
            StarTime = dsstatsRequest.FromDate,
            MmrIdRatings = new()
                    {
                        { 1, new() },
                        { 2, new() },
                        { 3, new() },
                    { 4, new() }
                    },
        }
           : await GetDsstatsCalcRatingRequest(context);

        if (ratingRequest == null)
        {
            logger.LogWarning("Nothing to do.");
            return;
        }

        dsstatsRequest.FromDate = ratingRequest.StarTime;
        dsstatsRequest.Continue = ratingRequest.Continue;
        var dsstatsCalcDtos = await GetDsstatsCalcDtos(dsstatsRequest);

        List<shared.Calc.ReplayRatingDto> replayRatings = new();

        while (dsstatsCalcDtos.Count > 0)
        {
            for (int i = 0; i < dsstatsCalcDtos.Count; i++)
            {
                var rating = ratings.lib.Ratings.ProcessReplay(dsstatsCalcDtos[i], ratingRequest);
                if (rating is not null)
                {
                    replayRatings.Add(rating);
                }
            }

            (ratingRequest.ReplayRatingAppendId, ratingRequest.ReplayPlayerRatingAppendId) =
                await ratingSaveService.SaveDsstatsStepResult(replayRatings,
                                                   ratingRequest.ReplayRatingAppendId,
                                                   ratingRequest.ReplayPlayerRatingAppendId);
            replayRatings = new();
            dsstatsRequest.Skip += dsstatsRequest.Take;
            dsstatsCalcDtos = await GetDsstatsCalcDtos(dsstatsRequest);
        }

        await ratingSaveService.SaveDsstatsPlayerRatings(ratingRequest.MmrIdRatings, ratingRequest.Continue);
    }

    private async Task<CalcRatingRequest?> GetDsstatsCalcRatingRequest(ReplayContext context)
    {

        CalcRatingRequest ratingRequest = new()
        {
            RatingCalcType = RatingCalcType.Dsstats,
            StarTime = new(2018, 1, 1),
            MmrIdRatings = new()
            {
                { 1, new() },
                { 2, new() },
                { 3, new() },
                { 4, new() }
            },
        };

        var latestRatingsProduced = await context.Replays
            .Where(x => x.ReplayRatingInfo != null)
            .OrderByDescending(o => o.GameTime)
            .Select(s => s.GameTime)
            .FirstOrDefaultAsync();

        // recalc
        if (latestRatingsProduced == default)
        {
            return ratingRequest;
        }

        var todoReplays = context.Replays
            .Where(x => x.Imported != null
                && x.Imported > latestRatingsProduced
                && x.GameTime >= latestRatingsProduced
                && x.ReplayRatingInfo == null)
            .Select(s => new { s.Imported, s.GameTime });

        var count = await todoReplays.CountAsync();

        // nothing to do
        if (count == 0)
        {
            return null;
        }

        // too mutch to do => recalc
        if (count > 100)
        {
            return ratingRequest;
        }

        ratingRequest.Continue = true;
        ratingRequest.StarTime = latestRatingsProduced;

        ratingRequest.ReplayPlayerRatingAppendId = await context.RepPlayerRatings
            .OrderByDescending(o => o.RepPlayerRatingId)
            .Select(s => s.RepPlayerRatingId)
            .FirstOrDefaultAsync();
        ratingRequest.ReplayRatingAppendId = await context.ReplayRatings
            .OrderByDescending(o => o.ReplayRatingId)
            .Select(s => s.ReplayRatingId)
            .FirstOrDefaultAsync();

        ratingRequest.MmrIdRatings = await GetCalcRatings(latestRatingsProduced, context);

        return ratingRequest;
    }

    public async Task<List<CalcDto>> GetDsstatsCalcDtos(DsstatsCalcRequest request)
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var query = context.Replays
            .Where(x => x.Playercount == 6
             && x.Duration >= 300
             && x.WinnerTeam > 0
             && x.GameTime >= request.FromDate
             && request.GameModes.Contains((int)x.GameMode)
             && (!request.Continue || x.ReplayRatingInfo == null))
            .OrderBy(o => o.GameTime)
                .ThenBy(o => o.ReplayId)
            .Select(s => new RawCalcDto()
            {
                DsstatsReplayId = s.ReplayId,
                GameTime = s.GameTime,
                Duration = s.Duration,
                Maxkillsum = s.Maxkillsum,
                GameMode = (int)s.GameMode,
                TournamentEdition = s.TournamentEdition,
                Players = s.ReplayPlayers.Select(t => new RawPlayerCalcDto()
                {
                    ReplayPlayerId = t.ReplayPlayerId,
                    GamePos = t.GamePos,
                    PlayerResult = (int)t.PlayerResult,
                    Race = t.Race,
                    Duration = t.Duration,
                    Kills = t.Kills,
                    Team = t.Team,
                    IsUploader = t.Player.UploaderId != null,
                    PlayerId = new(t.Player.ToonId, t.Player.RealmId, t.Player.RegionId)
                }).ToList()

            });

        var rawDtos = await query
            .AsSplitQuery()
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync();

        return rawDtos.Select(s => s.GetCalcDto()).ToList();
    }
}
