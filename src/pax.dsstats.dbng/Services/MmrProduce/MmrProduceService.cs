﻿using AutoMapper;
using AutoMapper.QueryableExtensions;
using dsstats.mmr;
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
    private readonly IMapper mapper;
    private readonly ILogger<MmrProduceService> logger;
    private static Dictionary<RatingType, DateTime> latestReplay = new();

    public MmrProduceService(IServiceProvider serviceProvider, IMapper mapper, ILogger<MmrProduceService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.mapper = mapper;
        this.logger = logger;
    }

    public async Task ProduceRatings(MmrOptions mmrOptions,
                                        DateTime startTime = default,
                                        DateTime endTime = default,
                                        bool reCalc = true)
    {
        Stopwatch sw = Stopwatch.StartNew();
        var cmdrMmrDic = await GetCommanderMmrsDic(true);

        if ((latestReplay.Count == 0) || reCalc) {
            latestReplay[RatingType.Cmdr] = startTime;
            latestReplay[RatingType.Std] = startTime;
        }

        latestReplay[RatingType.Cmdr] = await ProduceCmdrRatings(mmrOptions, cmdrMmrDic, latestReplay[RatingType.Cmdr], endTime);
        latestReplay[RatingType.Std] = await ProduceStdRatings(mmrOptions, cmdrMmrDic, latestReplay[RatingType.Std], endTime);

        await SaveCommanderMmrsDic(cmdrMmrDic);
        sw.Stop();
        logger.LogWarning($"ratings produced in {sw.ElapsedMilliseconds} ms");
    }

    public async Task<DateTime> ProduceCmdrRatings(MmrOptions mmrOptions,
                                         Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic,
                                         DateTime startTime = default,
                                         DateTime endTime = default)
    {
        DateTime _startTime = startTime == DateTime.MinValue ? new DateTime(2018, 1, 1) : startTime;
        DateTime _endTime = endTime == DateTime.MinValue ? DateTime.Today.AddDays(2) : endTime;

        Dictionary<int, CalcRating> mmrIdRatings = new();
        HashSet<PlayerDsRDto> players = new();

        using var scope = serviceProvider.CreateScope();
        var ratingRepository = scope.ServiceProvider.GetRequiredService<IRatingRepository>();

        DateTime latestReplay = DateTime.MinValue;

        while (_startTime < _endTime)
        {
            var chunkEndTime = _startTime.AddYears(1);

            if (chunkEndTime > _endTime)
            {
                chunkEndTime = _endTime;
            }

            var replays = await GetCmdrReplayDsRDtos(_startTime, chunkEndTime);

            _startTime = _startTime.AddYears(1);

            if (!replays.Any())
            {
                continue;
            }

            if (mmrOptions.Continue)
            {
                var calcRatings = await ratingRepository.GetCalcRatings(RatingType.Cmdr, replays, mmrIdRatings.Keys.ToList());
                foreach (var calcRating in calcRatings)
                {
                    mmrIdRatings[calcRating.Key] = calcRating.Value;
                }
            }

            latestReplay = replays.Last().GameTime;

            players.UnionWith(replays.SelectMany(s => s.ReplayPlayers).Select(s => s.Player).Distinct());

            mmrIdRatings = await MmrService.GeneratePlayerRatings(replays,
                                                                            cmdrMmrDic,
                                                                            mmrIdRatings,
                                                                            ratingRepository,
                                                                            mmrOptions);

            var result = await ratingRepository.UpdateRavenPlayers(MmrService.GetRavenPlayers(players.ToList(), mmrIdRatings), RatingType.Cmdr);
        }
        return latestReplay;
    }

    public async Task<DateTime> ProduceStdRatings(MmrOptions mmrOptions,
                                        Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic,
                                        DateTime startTime = default,
                                        DateTime endTime = default)
    {
        DateTime _startTime = startTime == DateTime.MinValue ? new DateTime(2018, 1, 1) : startTime;
        DateTime _endTime = endTime == DateTime.MinValue ? DateTime.Today.AddDays(2) : endTime;

        Dictionary<int, CalcRating> mmrIdRatings = new();
        HashSet<PlayerDsRDto> players = new();

        using var scope = serviceProvider.CreateScope();
        var ratingRepository = scope.ServiceProvider.GetRequiredService<IRatingRepository>();

        DateTime latestReplay = DateTime.MinValue;

        while (_startTime < _endTime)
        {
            var chunkEndTime = _startTime.AddYears(1);

            if (chunkEndTime > _endTime)
            {
                chunkEndTime = _endTime;
            }

            var replays = await GetStdReplayDsRDtos(_startTime, chunkEndTime);

            _startTime = _startTime.AddYears(1);

            if (!replays.Any())
            {
                continue;
            }

            if (mmrOptions.Continue)
            {
                var calcRatings = await ratingRepository.GetCalcRatings(RatingType.Std, replays, mmrIdRatings.Keys.ToList());
                foreach (var calcRating in calcRatings)
                {
                    mmrIdRatings[calcRating.Key] = calcRating.Value;
                }
            }

            latestReplay = replays.Last().GameTime;

            players.UnionWith(replays.SelectMany(s => s.ReplayPlayers).Select(s => s.Player).Distinct());

            mmrIdRatings = await MmrService.GeneratePlayerRatings(replays,
                                                                            cmdrMmrDic,
                                                                            mmrIdRatings,
                                                                            ratingRepository,
                                                                            mmrOptions);

            var result = await ratingRepository.UpdateRavenPlayers(MmrService.GetRavenPlayers(players.ToList(), mmrIdRatings), RatingType.Std);
        }
        return latestReplay;
    }

    private async Task<List<ReplayDsRDto>> GetStdReplayDsRDtos(DateTime startTime, DateTime endTime)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var replays = context.Replays
            .Include(r => r.ReplayPlayers)
                .ThenInclude(rp => rp.Player)
            .Where(r => r.Playercount == 6
                && r.Duration >= 300
                && r.WinnerTeam > 0
                && r.GameMode == GameMode.Standard)
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

    private async Task<List<ReplayDsRDto>> GetCmdrReplayDsRDtos(DateTime startTime, DateTime endTime)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var replays = context.Replays
            .Include(r => r.ReplayPlayers)
                .ThenInclude(rp => rp.Player)
            .Where(r => r.Playercount == 6
                && r.Duration >= 300
                && r.WinnerTeam > 0
                && (r.GameMode == GameMode.Commanders || r.GameMode == GameMode.CommandersHeroic))
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

    private async Task<Dictionary<CmdrMmmrKey, CmdrMmmrValue>> GetCommanderMmrsDic(bool clean)
    {
        if (clean)
        {
            return GetCleanCommnaderMmrsDic();
        }

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var cmdrMmrs = await context.CommanderMmrs
            .AsNoTracking()
            .ToListAsync()
        ;

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

                        SynergyMmr = MmrService.startMmr,
                        AntiSynergyMmr = MmrService.startMmr
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

    private Dictionary<CmdrMmmrKey, CmdrMmmrValue> GetCleanCommnaderMmrsDic()
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

                    SynergyMmr = MmrService.startMmr,
                    AntiSynergyMmr = MmrService.startMmr
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
