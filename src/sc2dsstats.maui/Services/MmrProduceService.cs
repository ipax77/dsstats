using AutoMapper;
using AutoMapper.QueryableExtensions;
using dsstats.mmr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using pax.dsstats.shared.Raven;
using System.Diagnostics;

namespace sc2dsstats.maui.Services;

public partial class MmrProduceService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMapper mapper;
    private readonly ILogger<MmrProduceService> logger;

    public MmrProduceService(IServiceProvider serviceProvider, IMapper mapper, ILogger<MmrProduceService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.mapper = mapper;
        this.logger = logger;
    }

    public async Task ProduceRatings(DateTime startTime = default, DateTime endTime = default)
    {
        Stopwatch sw = Stopwatch.StartNew();
        var cmdrMmrDic = await GetCommanderMmrsDic();
        double maxMmr = MmrService.startMmr;

        await ProduceStdRatings(cmdrMmrDic, maxMmr, startTime, endTime);
        await ProduceCmdrRatings(cmdrMmrDic, maxMmr, startTime, endTime);

        // todo: save cmdrMmrDic

        sw.Stop();
        logger.LogWarning($"ratings produced in {sw.ElapsedMilliseconds} ms");
    }

    public async Task ProduceCmdrRatings(Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic,
                                         double maxMmr,
                                         DateTime startTime = default,
                                         DateTime endTime = default)
    {
        DateTime _startTime = (startTime == DateTime.MinValue) ? new DateTime(2018, 1, 1) : startTime;
        DateTime _endTime = (endTime == DateTime.MinValue) ? DateTime.Today.AddDays(2) : endTime;

        Dictionary<int, CalcRating> mmrIdRatings = new();
        HashSet<PlayerDsRDto> players = new();

        using var scope = serviceProvider.CreateScope();
        var ratingRepository = scope.ServiceProvider.GetRequiredService<IRatingRepository>();

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

            players.UnionWith(replays.SelectMany(s => s.ReplayPlayers).Select(s => s.Player).Distinct());

            (mmrIdRatings, maxMmr) = await MmrService.GeneratePlayerRatings(replays,
                                                                            cmdrMmrDic,
                                                                            mmrIdRatings,
                                                                            MmrService.startMmr,
                                                                            ratingRepository,
                                                                            new());

            var result = await ratingRepository.UpdateRavenPlayers(MmrService.GetRavenPlayers(players.ToList(), mmrIdRatings), RatingType.Cmdr);
        }
    }

    public async Task ProduceStdRatings(Dictionary<CmdrMmmrKey, CmdrMmmrValue> cmdrMmrDic, double maxMmr, DateTime startTime = default, DateTime endTime = default)
    {
        DateTime _startTime = (startTime == DateTime.MinValue) ? new DateTime(2018, 1, 1) : startTime;
        DateTime _endTime = (endTime == DateTime.MinValue) ? DateTime.Today.AddDays(2) : endTime;

        Dictionary<int, CalcRating> mmrIdRatings = new();
        HashSet<PlayerDsRDto> players = new();

        using var scope = serviceProvider.CreateScope();
        var ratingRepository = scope.ServiceProvider.GetRequiredService<IRatingRepository>();

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

            players.UnionWith(replays.SelectMany(s => s.ReplayPlayers).Select(s => s.Player).Distinct());

            (mmrIdRatings, maxMmr) = await MmrService.GeneratePlayerRatings(replays,
                                                                            cmdrMmrDic,
                                                                            mmrIdRatings,
                                                                            MmrService.startMmr,
                                                                            ratingRepository,
                                                                            new());

            var result = await ratingRepository.UpdateRavenPlayers(MmrService.GetRavenPlayers(players.ToList(), mmrIdRatings), RatingType.Std);
        }
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
            replays = replays.Where(x => x.GameTime >= startTime);
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
            replays = replays.Where(x => x.GameTime >= startTime);
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

    private async Task<Dictionary<CmdrMmmrKey, CmdrMmmrValue>> GetCommanderMmrsDic()
    {
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
}
