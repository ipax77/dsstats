using dsstats.db8;
using dsstats.ratings.lib;
using dsstats.shared;
using dsstats.shared.Calc;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dsstats.ratings;

public partial class RatingService
{

    public async Task CombineTest()
    {
        DsstatsCalcRequest request = new()
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
        };

        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();
        var ratingSaveService = scope.ServiceProvider.GetRequiredService<IRatingsSaveService>();

        var dsReps = await GetCombineDsstatsCalcDtos(request, context);
        var acReps = await GetCombineArcadeCalcDtos(dsReps, context);

        var dsDic = new Dictionary<string, List<CalcDto>>();

        // ensure non overlapping acrade replays
        Dictionary<int, bool> processedArcadeIds = new(6_000_000);

        // ensure non duplicate dsstats overlapping arcade replays
        Dictionary<string, List<shared.Calc.ReplayRatingDto>> ratings = new();

        while (dsReps.Count > 0)
        {
            Dictionary<string, List<shared.Calc.ReplayRatingDto>> stepRatings = new();
            List<shared.Calc.ReplayRatingDto> impRatings = new();
            var cbReps = CombineLists(dsReps, acReps, processedArcadeIds);

            foreach (var ent in cbReps)
            {
                if (!stepRatings.TryGetValue(ent.Key, out var keyRatings))
                {
                    keyRatings = stepRatings[ent.Key] = new();
                }

                foreach (var rep in ent.Value)
                {
                    var rating = Ratings.ProcessReplay(rep, ratingRequest);
                    if (rating is not null && !ratings.ContainsKey(ent.Key))
                    {
                        keyRatings.Add(rating);
                        if (!rep.IsArcade) // dsstats replay infos, only
                        {
                            impRatings.Add(rating);
                        }
                    }
                }
            }

            // int count = stepRatings.SelectMany(s => s.Value).Count();
            // logger.LogWarning("step count: {count}", count);


            (ratingRequest.ReplayRatingAppendId, ratingRequest.ReplayPlayerRatingAppendId) =
                await ratingSaveService.SaveComboStepResult(impRatings,
                                                   ratingRequest.ReplayRatingAppendId,
                                                   ratingRequest.ReplayPlayerRatingAppendId);
            ratings = stepRatings;
            request.Skip += request.Take;
            dsReps = await GetCombineDsstatsCalcDtos(request, context);
            acReps = await GetCombineArcadeCalcDtos(dsReps, context);
        }

        await ratingSaveService.SaveComboPlayerRatings(ratingRequest.MmrIdRatings, ratingRequest.SoftBannedPlayers);
    }

    private Dictionary<string, List<CalcDto>> CombineLists(List<CalcDto> dsstatsCalcDtos,
                              List<CalcDto> sc2ArcadeCalcDtos,
                              Dictionary<int, bool> processedArcadeIds)
    {
        var dsstatsDic = GenerateHashDic(dsstatsCalcDtos);
        var sc2arcadeDic = GenerateHashDic(sc2ArcadeCalcDtos);

        var dsMultiHashes = dsstatsDic.Values.Where(x => x.Count > 1).Count();
        var acMutliHashes = sc2arcadeDic.Values.Where(x => x.Count > 1).Count();

        int hits = 0;
        int reasonableHits = 0;

        foreach (var ent in dsstatsDic)
        {
            foreach (var calcDto in ent.Value.ToArray())
            {
                if (sc2arcadeDic.TryGetValue(ent.Key, out var calcDtos))
                {
                    foreach (var sc2ArcadeCalcDto in calcDtos.ToArray())
                    {
                        if (processedArcadeIds.ContainsKey(sc2ArcadeCalcDto.ReplayId))
                        {
                            continue;
                        }
                        hits++;
                        if (IsMatchReasonable(calcDto, sc2ArcadeCalcDto))
                        {
                            reasonableHits++;
                        }
                        else
                        {
                            processedArcadeIds.Add(sc2ArcadeCalcDto.ReplayId, true);
                            ent.Value.Add(sc2ArcadeCalcDto);
                        }
                    }
                }
            }
        }

        foreach (var ent in sc2arcadeDic)
        {
            if (!dsstatsDic.ContainsKey(ent.Key))
            {
                dsstatsDic[ent.Key] = ent.Value;
            }
        }

        logger.LogWarning("MultiHashes: {dsMultiHashes}|{acMutliHashes}, hits: {hits}|{reasonableHits}",
            dsMultiHashes, acMutliHashes, hits, reasonableHits);
        return dsstatsDic;
    }

    private async Task<List<CalcDto>> GetCombineArcadeCalcDtos(List<CalcDto> dsstatsCalcDtos, ReplayContext context)
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
                    select new CalcDto()
                    {
                        ReplayId = r.ArcadeReplayId,
                        GameTime = r.CreatedAt,
                        Duration = r.Duration,
                        GameMode = (int)r.GameMode,
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
                    };

        return await query
            .AsSplitQuery()
            .ToListAsync();
    }

    private async Task<List<CalcDto>> GetCombineDsstatsCalcDtos(DsstatsCalcRequest request, ReplayContext context)
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
}
