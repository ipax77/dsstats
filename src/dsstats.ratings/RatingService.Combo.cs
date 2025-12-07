
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
    private async Task ProduceComboRatings(bool recalc)
    {
        
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var ratingSaveService = scope.ServiceProvider.GetRequiredService<IRatingsSaveService>();
        var comboRatings = scope.ServiceProvider.GetRequiredService<ComboRatings>();

        context.Database.SetCommandTimeout(840);
        
        await CleanupComboPreRatings(context);

        if (!recalc)
        {
            await ContinueComboRatings();
            return;
        }
        
        await comboRatings.CombineDsstatsSc2ArcadeReplays();

        DsstatsCalcRequest dsstatsRequest = new()
        {
            FromDate = new DateTime(2021, 2, 1),
            GameModes = new List<int>() { 3, 4, 7 },
            Skip = 0,
            Take = 5000
        };

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


        var comboCalcDtos = await GetComboCalcDtos(dsstatsRequest, context);

        List<shared.Calc.ReplayRatingDto> replayRatings = new();

        while (comboCalcDtos.Count > 0)
        {
            for (int i = 0; i < comboCalcDtos.Count; i++)
            {
                var calcDto = comboCalcDtos[i];
                if (calcDto.IsArcade)
                {
                    CorrectPlayerResults(calcDto);
                }
                var rating = ratings.lib.Ratings.ProcessReplay(calcDto, ratingRequest);
                if (rating is not null && !calcDto.IsArcade)
                {
                    replayRatings.Add(rating);
                }
            }

            (ratingRequest.ReplayRatingAppendId, ratingRequest.ReplayPlayerRatingAppendId) =
                await ratingSaveService.SaveComboStepResult(replayRatings,
                                                   ratingRequest.ReplayRatingAppendId,
                                                   ratingRequest.ReplayPlayerRatingAppendId);
            replayRatings = new();
            dsstatsRequest.Skip += dsstatsRequest.Take;
            comboCalcDtos = await GetComboCalcDtos(dsstatsRequest, context);
        }

        await ratingSaveService.SaveComboPlayerRatings(ratingRequest.MmrIdRatings, ratingRequest.SoftBannedPlayers);
    }

    private async Task<List<CalcDto>> GetComboCalcDtos(DsstatsCalcRequest request,
                                                       ReplayContext context)
    {
        var dsstatsCalcDtos = await GetComboDsstatsCalcDtos(request, context);
        var arcadeCalcDtos = await GetComboArcadeCalcDtos(dsstatsCalcDtos, context);
        return CombineCalcDtos(dsstatsCalcDtos, arcadeCalcDtos);
    }

    private List<CalcDto> CombineCalcDtos(List<CalcDto> dsstatsCalcDtos,
                                            List<CalcDto> sc2ArcadeCalcDtos)
    {
        return dsstatsCalcDtos
            .Concat(sc2ArcadeCalcDtos)
            .OrderBy(o => o.GameTime)
                .ThenBy(o => o.ReplayId)
            .ToList();
    }

    private async Task<List<CalcDto>> GetComboArcadeCalcDtos(List<CalcDto> dsstatsCalcDtos, ReplayContext context)
    {
        if (dsstatsCalcDtos.Count == 0)
        {
            return new();
        }

        var oldestReplayDate = dsstatsCalcDtos.First().GameTime.AddDays(-1);
        var latestReplayDate = dsstatsCalcDtos.Last().GameTime.AddDays(1);

        var startId = await context.MaterializedArcadeReplays
            .Where(x => x.CreatedAt > oldestReplayDate)
            .OrderBy(o => o.MaterializedArcadeReplayId)
            .Select(s => s.MaterializedArcadeReplayId)
            .FirstOrDefaultAsync();

        var endId = await context.MaterializedArcadeReplays
            .Where(x => x.CreatedAt < latestReplayDate)
            .OrderBy(o => o.MaterializedArcadeReplayId)
            .Select(s => s.MaterializedArcadeReplayId)
            .LastOrDefaultAsync();

        if (startId == endId || startId > endId)
        {
            logger.LogWarning("arcade ids missmatch {id1}, {id2}", startId, endId);
            return new();
        }

        var query = from r in context.MaterializedArcadeReplays
                    join m in context.ReplayArcadeMatches on r.ArcadeReplayId equals m.ArcadeReplayId into grouping
                    from m in grouping.DefaultIfEmpty()
                    orderby r.MaterializedArcadeReplayId
                    where r.MaterializedArcadeReplayId >= startId
                        && r.MaterializedArcadeReplayId <= endId
                        && m == null
                    select new CalcDto()
                    {
                        ReplayId = r.ArcadeReplayId,
                        GameTime = r.CreatedAt,
                        Duration = r.Duration,
                        GameMode = (int)r.GameMode,
                        WinnerTeam = r.WinnerTeam,
                        TournamentEdition = false,
                        IsArcade = true,
                        Players = context.ArcadeReplayDsPlayers
                                .Where(x => x.ArcadeReplayId == r.ArcadeReplayId)
                                .Select(t => new PlayerCalcDto()
                                {
                                    ReplayPlayerId = t.PlayerId,
                                    GamePos = t.SlotNumber,
                                    PlayerResult = (int)t.PlayerResult,
                                    Team = t.Team,
                                    PlayerId = new(t.Player!.ToonId, t.Player.RealmId, t.Player.RegionId)
                                }).ToList()
                    };

        return await query
            .AsSplitQuery()
            .ToListAsync();
    }

    private async Task<List<CalcDto>> GetComboDsstatsCalcDtos(DsstatsCalcRequest request, ReplayContext context)
    {
        var rawDtos = await context.Replays
            .Where(x => x.Playercount == 6
             && x.Duration >= 300
             && x.WinnerTeam > 0
             && request.GameModes.Contains((int)x.GameMode)
             && x.TournamentEdition == false
             && x.GameTime >= request.FromDate
             && (request.Continue ? x.ReplayRatingInfo == null : true))
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

    private async Task CleanupComboPreRatings(ReplayContext context)
    {
        var preRatings = await context.ComboReplayRatings
            .Where(x => x.IsPreRating)
            .ToListAsync();

        if (preRatings.Count == 0)
        {
            return;
        }

        var replayIds = preRatings.Select(s => s.ReplayId).ToList();

        var replayPlayerIds = await context.Replays
            .Where(x => replayIds.Contains(x.ReplayId))
            .SelectMany(s => s.ReplayPlayers)
            .Select(s => s.ReplayPlayerId)
            .ToListAsync();

        var replayPlayerRatings = await context.ComboReplayPlayerRatings
            .Where(x => replayPlayerIds.Contains(x.ReplayPlayerId))
            .ToListAsync();

        context.ComboReplayRatings.RemoveRange(preRatings);
        context.ComboReplayPlayerRatings.RemoveRange(replayPlayerRatings);

        await context.SaveChangesAsync();
    }
}
