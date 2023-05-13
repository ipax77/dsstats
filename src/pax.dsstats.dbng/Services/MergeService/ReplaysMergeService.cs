using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;

namespace pax.dsstats.dbng.Services.MergeService;

public class ReplaysMergeService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IMapper mapper;
    private readonly ILogger<ReplaysMergeService> logger;

    public ReplaysMergeService(IServiceScopeFactory scopeFactory, IMapper mapper, ILogger<ReplaysMergeService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.mapper = mapper;
        this.logger = logger;
    }

    public async Task<MergeResultReplays> GetMergeResultReplay(PlayerId playerId, CancellationToken token)
    {
        var mergeResult = await MergeReplays(playerId, token);
        return await GetMergeResultReplays(mergeResult, token);
    }

    private async Task<MergeResult> MergeReplays(PlayerId playerId, CancellationToken token)
    {
        var arReplays = await GetArcadeReplayData(playerId, RatingType.Cmdr, token);
        var dsReplays = await GetDsstatsReplayData(playerId, RatingType.Cmdr, token);

        var mergeResult = new MergeResult()
        {
            ArCount = arReplays.Count,
            DsCount = dsReplays.Count,
        };

        foreach (var dsReplay in dsReplays)
        {
            bool foundMatch = false;

            foreach (var arReplay in arReplays)
            {
                var compareResult = Compare(dsReplay, arReplay);

                if (compareResult == ReplayCompareResult.Equal)
                {
                    mergeResult.DsAndAr.Add(new KeyValuePair<int, int>(dsReplay.ReplayId, arReplay.ReplayId));
                    foundMatch = true;
                    break;
                }
            }

            if (!foundMatch)
            {
                mergeResult.DsOnly.Add(dsReplay.ReplayId);
            }
        }

        var dsEqualIds = mergeResult.DsAndAr.Select(s => s.Key).ToArray();
        var arEqualIds = mergeResult.DsAndAr.Select(s => s.Value).ToArray();

        dsReplays.RemoveAll(a => dsEqualIds.Contains(a.ReplayId));
        arReplays.RemoveAll(a => arEqualIds.Contains(a.ReplayId));

        foreach (var arReplay in arReplays)
        {
            bool foundMatch = false;

            foreach (var dsReplay in dsReplays)
            {
                var compareResult = Compare(dsReplay, arReplay);

                if (compareResult == ReplayCompareResult.Equal)
                {
                    mergeResult.DsAndAr.Add(new KeyValuePair<int, int>(dsReplay.ReplayId, arReplay.ReplayId));
                    foundMatch = true;
                    break;
                }
            }

            if (!foundMatch)
            {
                mergeResult.ArOnly.Add(arReplay.ReplayId);
            }
        }

        logger.LogWarning($"merge result: ds: {mergeResult.DsCount} ar: {mergeResult.ArCount}");
        logger.LogWarning($"equal {mergeResult.DsAndAr.Count}");
        logger.LogWarning($"dsOnly {mergeResult.DsOnly.Count}");
        logger.LogWarning($"arOnly {mergeResult.ArOnly.Count}");

        return mergeResult;
    }

    private async Task<MergeResultReplays> GetMergeResultReplays(MergeResult mergeResult, CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var dsIds = mergeResult.DsAndAr.Select(s => s.Key).ToList();
        var arIds = mergeResult.DsAndAr.Select(s => s.Value).ToList();

        var dss = (await context.Replays
            .Where(x => dsIds.Contains(x.ReplayId))
            .ProjectTo<ReplayListDto>(mapper.ConfigurationProvider)
            .ToListAsync(token)).ToDictionary(k => k.ReplayId, v => v);

        var ars = (await context.ArcadeReplays
            .Where(x => arIds.Contains(x.ArcadeReplayId))
            .ProjectTo<ArcadeReplayListDto>(mapper.ConfigurationProvider)
            .ToListAsync(token)).ToDictionary(k => k.ArcadeReplayId, v => v);

        return new MergeResultReplays()
        {
            DsOnly = await context.Replays
                        .Where(x => mergeResult.DsOnly.Contains(x.ReplayId))
                        .ProjectTo<ReplayListDto>(mapper.ConfigurationProvider)
                        .ToListAsync(token),
            ArOnly = await context.ArcadeReplays
                        .Where(x => mergeResult.ArOnly.Contains(x.ArcadeReplayId))
                        .ProjectTo<ArcadeReplayListDto>(mapper.ConfigurationProvider)
                        .ToListAsync(token),
            DsAndAr = mergeResult.DsAndAr.Select(s => 
                    new KeyValuePair<ReplayListDto, ArcadeReplayListDto>(dss[s.Key], ars[s.Value]))
                .ToList()
        };
    }

    private static ReplayCompareResult Compare(ReplayDsRDto dsReplay, ReplayDsRDto arReplay)
    {
        int equalScore = 0;

        var timeDifference = (dsReplay.GameTime - arReplay.GameTime).Duration();

        if (timeDifference.TotalHours > 4)
        {
            return ReplayCompareResult.None;
        }
        else
        {
            equalScore++;
        }

        var dsToonIds = dsReplay.ReplayPlayers.Select(s => s.Player.ToonId).OrderBy(o => o).ToList();
        var arToonIds = arReplay.ReplayPlayers.Select(s => s.Player.ToonId).OrderBy(o => o).ToList();

        if (!dsToonIds.SequenceEqual(arToonIds))
        {
            return ReplayCompareResult.None;
        }
        else
        {
            equalScore++;
        }

        if (dsReplay.WinnerTeam == arReplay.WinnerTeam)
        {
            equalScore++;
        }
        else
        {
            equalScore--;
        }

        var durationDifference = (TimeSpan.FromSeconds(dsReplay.Duration) - TimeSpan.FromSeconds(arReplay.Duration)).Duration();
        if (durationDifference.TotalMinutes < 4)
        {
            equalScore++;
        }
        else
        {
            equalScore--;
        }

        if (equalScore > 0)
        {
            return ReplayCompareResult.Equal;
        }
        return ReplayCompareResult.None;
    }

    private async Task<List<ReplayDsRDto>> GetArcadeReplayData(PlayerId playerId,
                                                               RatingType ratingType,
                                                               CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var replays = from p in context.ArcadePlayers
                      from rp in p.ArcadeReplayPlayers
                      where p.ProfileId == playerId.ToonId
                        && p.RegionId == playerId.RegionId
                        && p.RealmId == playerId.RealmId
                        && rp.ArcadeReplay.ArcadeReplayRating.RatingType == ratingType
                        && rp.ArcadeReplay.CreatedAt > new DateTime(2021, 2, 1)
                      select rp.ArcadeReplay;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        var dsrReplays = from r in replays
                         orderby r.CreatedAt
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
            .ToListAsync(token);

        foreach (var replay in dsrReplaysList)
        {
            var rps = replay.ReplayPlayers.Select(s => s with { Duration = replay.Duration }).ToList();
            replay.ReplayPlayers.Clear();
            replay.ReplayPlayers.AddRange(rps);
        }

        return dsrReplaysList;
    }

    private async Task<List<ReplayDsRDto>> GetDsstatsReplayData(PlayerId playerId,
                                                                RatingType ratingType,
                                                                CancellationToken token)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var query = from p in context.Players
                    from rp in p.ReplayPlayers
                    where p.ToonId == playerId.ToonId
                        && p.RegionId == playerId.RegionId
                        && p.RealmId == playerId.RealmId
                        && rp.Replay.ReplayRatingInfo.RatingType == ratingType
                        && rp.Replay.GameTime > new DateTime(2021, 2, 1)
                    select rp.Replay;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        return await query
            .OrderBy(o => o.GameTime)
            .ProjectTo<ReplayDsRDto>(mapper.ConfigurationProvider)
            .ToListAsync(token);
    }
}



public enum ReplayCompareResult
{
    None = 0,
    Equal = 1,
    DsGreater = 2,
    ArGreater = 3
}