using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services.Ratings;

public partial class RatingsMergeService
{
    private readonly ReplayContext context;
    private readonly IMapper mapper;
    private readonly ILogger<RatingsMergeService> logger;

    public RatingsMergeService(ReplayContext context, IMapper mapper, ILogger<RatingsMergeService> logger)
    {
        this.context = context;
        this.mapper = mapper;
        this.logger = logger;
    }

    public async Task Merge()
    {
        int skip = 0;
        int take = 1000;

        var dsstatsReplays = await GetDsstatsReplayData(DateTime.Today.AddDays(-20),  skip, take);
        var arcadeReplays = await GetArcadeReplayData(DateTime.Today.AddDays(-20), skip, take);

        var mergedReplays = GetMergedReplayData(dsstatsReplays, arcadeReplays);
    }

    private List<ReplayDsRDto> GetMergedReplayData(List<ReplayDsRDto> dsstatsReplays, List<ReplayDsRDto> arcadeReplays)
    {
        List<ReplayDsRDto> mergedReplays = new();

        Dictionary<string, ReplayDsRDto> dsstatsDic = GetReplayDsRDtosHashDic(dsstatsReplays);

        logger.LogWarning($"comparing arcde {arcadeReplays.Count} and dsstats {dsstatsReplays.Count}");

        int dsstatsDups = 0;
        int deepDups = 0;
        foreach (var arcadeReplay in arcadeReplays)
        {
            if (dsstatsDic.TryGetValue(arcadeReplay.GetHash(), out var dsstatsReplay))
            {
                dsstatsReplays.Remove(dsstatsReplay);
                dsstatsDups++;
            }
            mergedReplays.Add(arcadeReplay);
        }

        foreach (var dsstatsReplay in dsstatsReplays)
        {
            if (DeepFindArcadeReplay(dsstatsReplay, arcadeReplays))
            {
                deepDups++;
            }
            mergedReplays.Add(dsstatsReplay);
        }
        // mergedReplays.AddRange(dsstatsReplays);

        logger.LogWarning($"dsstats dups found: {dsstatsDups}/{dsstatsDic.Count} - deepDups: {deepDups}");

        return mergedReplays;
    }

    private bool DeepFindArcadeReplay(ReplayDsRDto dsstatsReplay, List<ReplayDsRDto> arcadeReplays)
    {
        List<int> playerToonIds = dsstatsReplay
            .ReplayPlayers
            .Select(s => s.Player.ToonId).Distinct().ToList();

        var dupReplays = arcadeReplays.Where(x => x.ReplayPlayers.All(a => playerToonIds.Contains(a.Player.ToonId)));

        if (dupReplays.Any())
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private Dictionary<string, ReplayDsRDto> GetReplayDsRDtosHashDic(List<ReplayDsRDto> replays)
    {
        Dictionary<string, ReplayDsRDto> replaysDic = new();
        foreach (var replay in replays)
        {
            var hash = replay.GetHash();
            if (!replaysDic.ContainsKey(hash))
            {
                replaysDic.Add(hash, replay);
            }
            else
            {
                logger.LogWarning($"duplicate replays hash: {replay.ReplayHash}");
            }
        }
        return replaysDic;
    }

    private async Task<List<ReplayDsRDto>> GetArcadeReplayData(DateTime startTime, int skip, int take)
    {
        List<GameMode> gameModes = new() { GameMode.Commanders, GameMode.Standard, GameMode.CommandersHeroic };

        var replays = context.ArcadeReplays
            .Include(i => i.ArcadeReplayPlayers)
                .ThenInclude(i => i.ArcadePlayer)
            .Where(r => r.CreatedAt >= startTime
                && r.PlayerCount == 6
                && r.Duration >= 300
                && r.WinnerTeam > 0
                && r.TournamentEdition == false
                && gameModes.Contains(r.GameMode));

        var dsrReplays = from r in replays
                         where r.Duration >= 300
                            && r.TournamentEdition == false
                         orderby r.CreatedAt, r.ArcadeReplayId
                         select new ReplayDsRDto()
                         {
                             ReplayId = r.ArcadeReplayId,
                             GameTime = r.CreatedAt,
                             WinnerTeam = r.WinnerTeam,
                             Duration = r.Duration,
                             GameMode = r.GameMode,
                             Playercount = (byte)r.PlayerCount,
                             TournamentEdition = r.TournamentEdition,
                             ReplayPlayers = r.ArcadeReplayPlayers.Select(s => new ReplayPlayerDsRDto()
                             {
                                 ReplayPlayerId = s.ArcadeReplayPlayerId,
                                 GamePos = s.SlotNumber,
                                 Team = s.Team,
                                 PlayerResult = s.PlayerResult,
                                 Player = new PlayerDsRDto()
                                 {
                                     PlayerId = s.ArcadePlayer.ArcadePlayerId,
                                     Name = s.ArcadePlayer.Name,
                                     ToonId = s.ArcadePlayer.ProfileId,
                                     RegionId = s.ArcadePlayer.RegionId
                                 }
                             }).ToList()
                         };

        var dsrReplaysList = await dsrReplays
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        foreach (var replay in dsrReplaysList)
        {
            var rps = replay.ReplayPlayers.Select(s => s with { Duration = replay.Duration }).ToList();
            replay.ReplayPlayers.Clear();
            replay.ReplayPlayers.AddRange(rps);
        }

        return dsrReplaysList;
    }

    private async Task<List<ReplayDsRDto>> GetDsstatsReplayData(DateTime startTime, int skip, int take)
    {
        List<GameMode> gameModes = new() { GameMode.Commanders, GameMode.Standard, GameMode.CommandersHeroic };

        return await context.Replays
            .Where(r => r.GameTime >= startTime
                && r.Playercount == 6
                && r.Duration >= 300
                && r.WinnerTeam > 0
                && gameModes.Contains(r.GameMode))
            .OrderBy(o => o.GameTime)
                .ThenBy(o => o.ReplayId)
            .ProjectTo<ReplayDsRDto>(mapper.ConfigurationProvider)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
}
