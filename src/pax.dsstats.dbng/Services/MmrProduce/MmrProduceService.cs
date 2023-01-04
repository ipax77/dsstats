using AutoMapper;
using AutoMapper.QueryableExtensions;
using dsstats.mmr;
using dsstats.mmr.ProcessData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;
using pax.dsstats.shared.Raven;
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

    public async Task<List<ReplayData>> ProduceRatings(MmrOptions mmrOptions,
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
        int mmrChangesAppendId = await GetMmrChangesAppendId(mmrOptions);

        if (mmrOptions.ReCalc)
        {
            latestReplay = startTime;
        }

        (latestReplay, List<ReplayData> replayDatas) = await ProduceRatings(mmrOptions, cmdrMmrDic, mmrIdRatings, ratingRepository, mmrChangesAppendId, latestReplay, endTime);

        await SaveCommanderMmrsDic(cmdrMmrDic);
        sw.Stop();
        logger.LogWarning($"ratings produced in {sw.ElapsedMilliseconds} ms");

        return replayDatas;
    }



    private async Task<int> GetMmrChangesAppendId(MmrOptions mmrOptions)
    {
        if (mmrOptions.ReCalc)
        {
            return 0;
        }
        else
        {
            using var scope = serviceProvider.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            return await context.ReplayPlayerRatings
                .OrderByDescending(o => o.ReplayPlayerRatingId)
                .Select(s => s.ReplayPlayerRatingId)
                .FirstOrDefaultAsync();
        }
    }

    public async Task<(DateTime, List<ReplayData>)> ProduceRatings(MmrOptions mmrOptions,
                                         Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic,
                                         Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings,
                                         IRatingRepository ratingRepository,
                                         int mmrChangesAppendId,
                                         DateTime startTime = default,
                                         DateTime endTime = default)
    {
        var allReplayDatas = new List<ReplayData>();

        DateTime _startTime = startTime == DateTime.MinValue ? new DateTime(2018, 1, 1) : startTime;
        DateTime _endTime = endTime == DateTime.MinValue ? DateTime.Today.AddDays(2) : endTime;

        DateTime latestReplay = DateTime.MinValue;

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

            (mmrIdRatings, mmrChangesAppendId, var replayDatas) = await MmrService.GeneratePlayerRatings(replays, cmdrMmrDic, mmrIdRatings, mmrOptions, mmrChangesAppendId, ratingRepository);
            allReplayDatas.AddRange(replayDatas);
        }
        var result = await ratingRepository.UpdateRavenPlayers(mmrIdRatings, !mmrOptions.ReCalc);

        return (latestReplay, allReplayDatas);
    }

    private async Task<Dictionary<RatingType, Dictionary<int, CalcRating>>> GetMmrIdRatings(MmrOptions mmrOptions, IRatingRepository ratingRepository, List<ReplayDsRDto>? dependentReplays)
    {
        if (mmrOptions.ReCalc || dependentReplays == null)
        {
            return new Dictionary<RatingType, Dictionary<int, CalcRating>>()
            {
                { RatingType.Cmdr, new() },
                { RatingType.Std, new() },
            };
        }
        else
        {
            // return await ratingRepository.GetCalcRatings(dependentReplays!);
            return await GetCalcRatings(dependentReplays);
        }
    }

    private async Task<List<ReplayDsRDto>> GetReplayDsRDtos(DateTime startTime, DateTime endTime)
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

    private async Task SaveCommanderMmrsDic(Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic)
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

    private async Task<Dictionary<CmdrMmmrKey, CmdrMmmrValue>> GetCommanderMmrsDic(MmrOptions mmrOptions, bool clean)
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
        return cmdrMmrs.ToDictionary(k => new CmdrMmmrKey(k.Race, k.OppRace), v => new CmdrMmmrValue()
        {
            SynergyMmr = v.SynergyMmr,
            AntiSynergyMmr = v.AntiSynergyMmr
        });
    }

    private Dictionary<CmdrMmmrKey, CmdrMmmrValue> GetCleanCommnaderMmrsDic(MmrOptions mmrOptions)
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

        return cmdrMmrs.ToDictionary(k => new CmdrMmmrKey(k.Race, k.OppRace), v => new CmdrMmmrValue()
        {
            SynergyMmr = v.SynergyMmr,
            AntiSynergyMmr = v.AntiSynergyMmr
        });
    }
}