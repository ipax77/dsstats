
using dsstats.db8;
using dsstats.shared.Calc;
using dsstats.shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using dsstats.shared.Interfaces;
using System.Collections.Frozen;

namespace dsstats.ratings;

public partial class RatingService
{
    private async Task ProduceComboRatings(bool recalc)
    {
        if (!recalc)
        {
            await ContinueComboRatings();
            return;
        }

        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var ratingSaveService = scope.ServiceProvider.GetRequiredService<IRatingsSaveService>();

        await CleanupComboPreRatings(context);

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


        HashSet<int> processedReplayIds = new();
        var comboCalcDtos = await GetComboCalcDtos(dsstatsRequest, processedReplayIds, context);

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
            comboCalcDtos = await GetComboCalcDtos(dsstatsRequest, processedReplayIds, context);
        }

        await ratingSaveService.SaveComboPlayerRatings(ratingRequest.MmrIdRatings, ratingRequest.SoftBannedPlayers);
    }

    private async Task<List<CalcDto>> GetComboCalcDtos(DsstatsCalcRequest request,
                                                       HashSet<int> processedReplayIds,
                                                       ReplayContext context)
    {
        var dsstatsCalcDtos = await GetComboDsstatsCalcDtos(request, context);
        var arcadeCalcDtos = await GetComboArcadeCalcDtos(dsstatsCalcDtos, processedReplayIds, context);
        return CombineCalcDtos(dsstatsCalcDtos, arcadeCalcDtos, processedReplayIds);
    }

    private List<CalcDto> CombineCalcDtos(List<CalcDto> dsstatsCalcDtos,
                                            List<CalcDto> sc2ArcadeCalcDtos,
                                            HashSet<int> processedReplayIds)
    {
        List<CalcDto> combinedCalcDtos = new();

        var dsstatsDic = GenerateHashDic(dsstatsCalcDtos);
        var sc2arcadeDic = GenerateHashDic(sc2ArcadeCalcDtos);

        foreach (var ent in dsstatsDic)
        {
            foreach (var calcDto in ent.Value)
            {
                if (sc2arcadeDic.TryGetValue(ent.Key, out var calcDtos))
                {
                    foreach (var sc2ArcadeCalcDto in calcDtos.ToArray())
                    {
                        if (IsMatchReasonable(calcDto, sc2ArcadeCalcDto))
                        {
                            calcDtos.Remove(sc2ArcadeCalcDto);
                            break;
                        }
                    }
                }
                combinedCalcDtos.Add(calcDto);
            }
        }

        combinedCalcDtos.AddRange(sc2arcadeDic.SelectMany(s => s.Value));
        processedReplayIds.UnionWith(sc2ArcadeCalcDtos.Select(s => s.ReplayId));

        return combinedCalcDtos
            .OrderBy(o => o.GameTime)
            .ThenBy(o => o.ReplayId)
            .ToList();
    }

    private static bool IsMatchReasonable(CalcDto dsstatsCalcDto, CalcDto sc2arcadeCalcDto)
    {
        var gameTimeDiff = Math.Abs((dsstatsCalcDto.GameTime - sc2arcadeCalcDto.GameTime).TotalSeconds);
        //var durationDiff = Math.Abs(dsstatsCalcDto.Duration - sc2arcadeCalcDto.Duration);

        //var durationPerDiff = durationDiff / (double)Math.Max(dsstatsCalcDto.Duration, sc2arcadeCalcDto.Duration);

        // if (gameTimeDiff < 86400 && durationPerDiff < 0.2)
        if (gameTimeDiff < 86400)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private static Dictionary<string, List<CalcDto>> GenerateHashDic(List<CalcDto> calcDtos)
    {
        Dictionary<string, List<CalcDto>> hashDic = new();

        for (int i = 0; i < calcDtos.Count; i++)
        {
            var calcDto = calcDtos[i];

            var key = string.Join('|', calcDto.Players
                .OrderBy(o => o.Team)
                .ThenBy(o => o.PlayerId.ToonId)
                .Select(s => s.PlayerId.ToonId.ToString()));
            // .Select(s => $"{s.ProfileId},{s.RegionId},{s.RealmId}"));

            var gameMode = calcDto.GameMode switch
            {
                3 => "Cmdr",
                4 => "Cmdr",
                7 => "Std",
                _ => ""
            };

            key = gameMode + key;

            if (!hashDic.TryGetValue(key, out var dtos))
            {
                hashDic[key] = new();
            }
            hashDic[key].Add(calcDto);
        }
        return hashDic;
    }

    private static void AddHashDic(Dictionary<string, List<CalcDto>> hashDic, List<CalcDto> calcDtos)
    {
        for (int i = 0; i < calcDtos.Count; i++)
        {
            var calcDto = calcDtos[i];

            var key = string.Join('|', calcDto.Players
                .OrderBy(o => o.Team)
                .ThenBy(o => o.PlayerId.ToonId)
                .Select(s => s.PlayerId.ToonId.ToString()));
            // .Select(s => $"{s.ProfileId},{s.RegionId},{s.RealmId}"));

            var gameMode = calcDto.GameMode switch
            {
                3 => "Cmdr",
                4 => "Cmdr",
                7 => "Std",
                _ => ""
            };

            key = gameMode + key;

            if (!hashDic.TryGetValue(key, out var dtos))
            {
                hashDic[key] = new();
            }
            hashDic[key].Add(calcDto);
        }
    }

    private async Task<List<CalcDto>> GetComboArcadeCalcDtos(List<CalcDto> dsstatsCalcDtos, HashSet<int> processedReplayIds, ReplayContext context)
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
                    orderby r.MaterializedArcadeReplayId
                    where r.MaterializedArcadeReplayId >= startId
                        && r.MaterializedArcadeReplayId <= endId
                    select new
                    {
                        r.ArcadeReplayId,
                        CalcDto = new CalcDto()
                        {
                            ReplayId = r.ArcadeReplayId,
                            GameTime = r.CreatedAt,
                            Duration = r.Duration,
                            GameMode = (int)r.GameMode,
                            WinnerTeam = r.WinnerTeam,
                            TournamentEdition = false,
                            IsArcade = true,
                            Players = context.ArcadeReplayPlayers
                                .Where(x => x.ArcadeReplayId == r.ArcadeReplayId)
                                .Select(t => new PlayerCalcDto()
                                {
                                    ReplayPlayerId = t.ArcadeReplayPlayerId,
                                    GamePos = t.SlotNumber,
                                    PlayerResult = (int)t.PlayerResult,
                                    Team = t.Team,
                                    PlayerId = new(t.ArcadePlayer.ProfileId, t.ArcadePlayer.RealmId, t.ArcadePlayer.RegionId)
                                }).ToList()
                        }
                    };

        var data = await query
            .AsSplitQuery()
            .ToListAsync();

        return data.Where(x => !processedReplayIds.Contains(x.ArcadeReplayId))
            .Select(s => s.CalcDto)
            .ToList();
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
