using dsstats.db8;
using dsstats.shared;
using dsstats.shared.Calc;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Frozen;

namespace dsstats.ratings;

public partial class RatingService
{
    public async Task ContinueComboRatings()
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var ratingSaveService = scope.ServiceProvider.GetRequiredService<IRatingsSaveService>();

        DsstatsCalcRequest dsstatsRequest = new()
        {
            FromDate = DateTime.UtcNow.AddHours(-2),
            GameModes = new List<int>() { 3, 4, 7 },
            Skip = 0,
            Take = 1000
        };

        var calcDtos = await GetContinueCalcDtos(dsstatsRequest, context);

        if (calcDtos.Count == 0)
        {
            logger.LogInformation("combo ratings continue: nothing to do.");
            return;
        }
        else
        {
            logger.LogInformation("combo ratings continue: {count}", calcDtos.Count);
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
        
        await CleanupComboPreRatings(context);

        ratingRequest.ReplayPlayerRatingAppendId = await context.ComboReplayPlayerRatings
            .OrderByDescending(o => o.ComboReplayPlayerRatingId)
            .Select(s => s.ComboReplayPlayerRatingId)
            .FirstOrDefaultAsync();
        ratingRequest.ReplayRatingAppendId = await context.ComboReplayRatings
            .OrderByDescending(o => o.ComboReplayRatingId)
            .Select(s => s.ComboReplayRatingId)
            .FirstOrDefaultAsync();

        ratingRequest.MmrIdRatings = await GetComboMmrIdRatings(calcDtos);

        
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

        await ratingSaveService.SaveComboStepResult(replayRatings,
                                                    ratingRequest.ReplayRatingAppendId,
                                                    ratingRequest.ReplayPlayerRatingAppendId);

        logger.LogInformation("combo replay ratings produced: {count}", replayRatings.Count);

        await ratingSaveService.SaveContinueComboRatings(ratingRequest.MmrIdRatings,
                                                         replayRatings);
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

        using var scope = scopeFactory.CreateAsyncScope();
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

    private async Task DebugDeleteComboRatings()
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var query = from r in context.Replays
                    join rr in context.ComboReplayRatings on r.ReplayId equals rr.ReplayId
                    orderby r.GameTime descending
                    select rr;

        var ratings = await query
           .Take(100)
           .ToListAsync();

        if (ratings.Count == 0)
        {
            return;
        }

        var replayIds = ratings.Select(s => s.ReplayId).ToList();

        var replayPlayerIds = await context.Replays
            .Where(x => replayIds.Contains(x.ReplayId))
            .SelectMany(s => s.ReplayPlayers)
            .Select(s => s.ReplayPlayerId)
            .ToListAsync();

        var replayPlayerRatings = await context.ComboReplayPlayerRatings
            .Where(x => replayPlayerIds.Contains(x.ReplayPlayerId))
            .ToListAsync();

        context.ComboReplayRatings.RemoveRange(ratings);
        context.ComboReplayPlayerRatings.RemoveRange(replayPlayerRatings);

        await context.SaveChangesAsync();
    }

    private async Task<List<CalcDto>> GetContinueCalcDtos(DsstatsCalcRequest request, ReplayContext context)
    {
        var rawDtos = await context.Replays
            .Where(x => x.Playercount == 6
             && x.Duration >= 300
             && x.WinnerTeam > 0
             && request.GameModes.Contains((int)x.GameMode)
             && x.TournamentEdition == false
             && x.GameTime >= request.FromDate
             && (x.ComboReplayRating == null || x.ComboReplayRating.IsPreRating))
            .OrderBy(o => o.GameTime)
                .ThenBy(o => o.ReplayId)
            .Select(s => new RawCalcDto()
            {
                DsstatsReplayId = s.ReplayId,
                GameTime = s.GameTime,
                Duration = s.Duration,
                Maxkillsum = s.Maxkillsum,
                GameMode = (int)s.GameMode,
                TournamentEdition = false,
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

            })
            .AsSplitQuery()
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync();

        return rawDtos.Select(s => s.GetCalcDto()).ToList();
    }
}
