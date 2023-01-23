using AutoMapper;
using AutoMapper.QueryableExtensions;
using dsstats.mmr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;
using System.Diagnostics;

namespace pax.dsstats.dbng.Services;

public partial class MmrProduceService
{
    private readonly IServiceProvider serviceProvider;
    //private readonly IMapper mapper;
    private readonly ILogger<MmrProduceService> logger;

    public MmrProduceService(IServiceProvider serviceProvider, IMapper mapper, ILogger<MmrProduceService> logger)
    {
        this.serviceProvider = serviceProvider;
        //this.mapper = mapper;
        this.logger = logger;
    }

    public async Task ProduceRatings(MmrOptions mmrOptions,
                                        DateTime latestReplay = default,
                                        List<ReplayDsRDto>? dependentReplays = null,
                                        DateTime startTime = default,
                                        DateTime endTime = default)
    {
        Stopwatch sw = Stopwatch.StartNew();

        using var scope = serviceProvider.CreateScope();
        var ratingRepository = scope.ServiceProvider.GetRequiredService<IRatingRepository>();

        var cmdrMmrDic = await GetCommanderMmrsDic(mmrOptions, true);

        if (!mmrOptions.ReCalc
            && dependentReplays != null
            && dependentReplays.Any()
            && dependentReplays.Any(a => a.GameTime < latestReplay))
        {
            mmrOptions.ReCalc = true;
            dependentReplays = null;
        }

        var mmrIdRatings = await GetMmrIdRatings(mmrOptions, ratingRepository, dependentReplays);
        (int replayRatingAppendId, int replayPlayerAppendId) = await GetMmrChangesAppendId(mmrOptions);

        if (mmrOptions.ReCalc)
        {
            latestReplay = startTime;
        }

        latestReplay = await ProduceRatings(mmrOptions,
                                            cmdrMmrDic,
                                            mmrIdRatings,
                                            ratingRepository,
                                            replayRatingAppendId,
                                            replayPlayerAppendId,
                                            latestReplay,
                                            endTime);

        await SaveCommanderMmrsDic(cmdrMmrDic);
        sw.Stop();
        logger.LogWarning($"ratings produced in {sw.ElapsedMilliseconds} ms");
    }



    public async Task<(int, int)> GetMmrChangesAppendId(MmrOptions mmrOptions)
    {
        if (mmrOptions.ReCalc)
        {
            return (0, 0);
        }
        else
        {
            using var scope = serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            var replayPlayerAppendId = await context.RepPlayerRatings
                .OrderByDescending(o => o.RepPlayerRatingId)
                .Select(s => s.RepPlayerRatingId)
                .FirstOrDefaultAsync();

            var replayRatingAppendId = await context.ReplayRatings
                .OrderByDescending(o => o.ReplayRatingId)
                .Select(s => s.ReplayRatingId)
                .FirstOrDefaultAsync();

            return (replayRatingAppendId, replayPlayerAppendId);
        }
    }

    public async Task<DateTime> ProduceRatings(MmrOptions mmrOptions,
                                         Dictionary<CmdrMmrKey, CmdrMmrValue> cmdrMmrDic,
                                         Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings,
                                         IRatingRepository ratingRepository,
                                         int replayRatingAppendId,
                                         int replayPlayerRatingAppendId,
                                         DateTime startTime = default,
                                         DateTime endTime = default)
    {
        DateTime _startTime = startTime == DateTime.MinValue ? new DateTime(2018, 1, 1) : startTime;
        DateTime _endTime = endTime == DateTime.MinValue ? DateTime.Today.AddDays(2) : endTime;

        DateTime latestReplay = DateTime.MinValue;

        MmrService.CalcRatingRequest request = new()
        {
            CmdrMmrDic = cmdrMmrDic,
            MmrIdRatings = mmrIdRatings,
            MmrOptions = mmrOptions,
            ReplayRatingAppendId = replayRatingAppendId,
            ReplayPlayerRatingAppendId = replayPlayerRatingAppendId,
        };

        while (_startTime < _endTime)
        {
            var chunkEndTime = _startTime.AddYears(1);

            if (chunkEndTime > _endTime)
            {
                chunkEndTime = _endTime;
            }

            var replays = await GetReplayDsRDtos(_startTime, chunkEndTime);

            _startTime = _startTime.AddYears(1);

            if (!replays.Any())
            {
                continue;
            }

            latestReplay = replays.Last().GameTime;

            request.ReplayDsRDtos = replays;


            var calcResult = await MmrService.GeneratePlayerRatings(request, ratingRepository);

            request.ReplayRatingAppendId = calcResult.ReplayRatingAppendId;
            request.ReplayPlayerRatingAppendId = calcResult.ReplayPlayerRatingAppendId;
        }

        var result = await ratingRepository.UpdateRavenPlayers(mmrIdRatings, !mmrOptions.ReCalc);

        return latestReplay;
    }

    public async Task<Dictionary<RatingType, Dictionary<int, CalcRating>>> GetMmrIdRatings(MmrOptions mmrOptions, IRatingRepository ratingRepository, List<ReplayDsRDto>? dependentReplays)
    {
        if (mmrOptions.ReCalc || dependentReplays == null)
        {
            Dictionary<RatingType, Dictionary<int, CalcRating>> calcRatings = new();

            foreach (RatingType ratingType in Enum.GetValues(typeof(RatingType)))
            {
                if (ratingType == RatingType.None)
                {
                    continue;
                }
                calcRatings[ratingType] = new();
            }
            return calcRatings;
        }
        else
        {
            // return await ratingRepository.GetCalcRatings(dependentReplays!);
            return await GetCalcRatings(dependentReplays);
        }
    }

    public async Task<List<ReplayDsRDto>> GetReplayDsRDtos(DateTime startTime, DateTime endTime)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        List<GameMode> gameModes = new() { GameMode.Commanders, GameMode.Standard, GameMode.CommandersHeroic };

        var replays = context.Replays
            .Where(r => r.Playercount == 6
                && r.Duration >= 300
                && r.WinnerTeam > 0
                && gameModes.Contains(r.GameMode))
            .AsNoTracking();

        if (startTime != DateTime.MinValue)
        {
            replays = replays.Where(x => x.GameTime > startTime);
        }

        if (endTime != DateTime.MinValue && endTime < DateTime.Today)
        {
            replays = replays.Where(x => x.GameTime < endTime);
        }

        return await replays
            .OrderBy(o => o.GameTime)
                .ThenBy(o => o.ReplayId)
            .ProjectTo<ReplayDsRDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task SaveCommanderMmrsDic(Dictionary<CmdrMmrKey, CmdrMmrValue> cmdrMmrDic)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var dbMmrs = await context.CommanderMmrs
            .ToListAsync();

        foreach (var ent in cmdrMmrDic)
        {
            var dbMmr = dbMmrs.FirstOrDefault(f => f.Race == ent.Key.Race && f.OppRace == ent.Key.OppRace);
            if (dbMmr != null)
            {
                dbMmr.SynergyMmr = ent.Value.SynergyMmr;
                dbMmr.AntiSynergyMmr = ent.Value.AntiSynergyMmr;
            }
            else
            {
                context.CommanderMmrs.Add(new CommanderMmr()
                {
                    Race = ent.Key.Race,
                    OppRace = ent.Key.OppRace,
                    SynergyMmr = ent.Value.SynergyMmr,
                    AntiSynergyMmr = ent.Value.AntiSynergyMmr
                });
            }
        }
        await context.SaveChangesAsync();
    }

    public async Task<Dictionary<CmdrMmrKey, CmdrMmrValue>> GetCommanderMmrsDic(MmrOptions mmrOptions, bool clean)
    {
        if (clean)
        {
            return GetCleanCommnaderMmrsDic(mmrOptions);
        }

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var cmdrMmrs = await context.CommanderMmrs
            .AsNoTracking()
            .ToListAsync();

        if (!cmdrMmrs.Any())
        {
            var commanderMmrs = await context.CommanderMmrs.ToListAsync();
            var allCommanders = Data.GetCommanders(Data.CmdrGet.NoStd);

            foreach (var race in allCommanders)
            {
                foreach (var oppRace in allCommanders)
                {
                    CommanderMmr cmdrMmr = new()
                    {
                        Race = race,
                        OppRace = oppRace,

                        SynergyMmr = mmrOptions.StartMmr,
                        AntiSynergyMmr = mmrOptions.StartMmr
                    };
                    cmdrMmrs.Add(cmdrMmr);
                }
            }
            context.CommanderMmrs.AddRange(cmdrMmrs);
            await context.SaveChangesAsync();
        }
        return cmdrMmrs.ToDictionary(k => new CmdrMmrKey(k.Race, k.OppRace), v => new CmdrMmrValue()
        {
            SynergyMmr = v.SynergyMmr,
            AntiSynergyMmr = v.AntiSynergyMmr
        });
    }

    public Dictionary<CmdrMmrKey, CmdrMmrValue> GetCleanCommnaderMmrsDic(MmrOptions mmrOptions)
    {
        List<CommanderMmr> cmdrMmrs = new();

        var allCommanders = Data.GetCommanders(Data.CmdrGet.NoStd);

        foreach (var race in allCommanders)
        {
            foreach (var oppRace in allCommanders)
            {
                CommanderMmr cmdrMmr = new()
                {
                    Race = race,
                    OppRace = oppRace,

                    SynergyMmr = mmrOptions.StartMmr,
                    AntiSynergyMmr = mmrOptions.StartMmr
                };
                cmdrMmrs.Add(cmdrMmr);
            }
        }

        return cmdrMmrs.ToDictionary(k => new CmdrMmrKey(k.Race, k.OppRace), v => new CmdrMmrValue()
        {
            SynergyMmr = v.SynergyMmr,
            AntiSynergyMmr = v.AntiSynergyMmr
        });
    }
}