using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.dbng.Services;
using pax.dsstats.shared;
using System.Numerics;

namespace pax.dsstats.dbng.Repositories;

public class ReplayRepository : IReplayRepository
{
    private readonly ILogger<ReplayRepository> logger;
    private readonly ReplayContext context;
    private readonly IMapper mapper;

    public ReplayRepository(ILogger<ReplayRepository> logger, ReplayContext context, IMapper mapper)
    {
        this.logger = logger;
        this.context = context;
        this.mapper = mapper;
    }

    public async Task<ReplayDto?> GetReplay(string replayHash, CancellationToken token = default)
    {
        var replay = await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(t => t.Spawns)
                    .ThenInclude(t => t.Units)
                        .ThenInclude(t => t.Unit)
            .Include(i => i.ReplayPlayers)
                .ThenInclude(t => t.Player)
            .AsNoTracking()
            .AsSplitQuery()
            .ProjectTo<ReplayDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(f => f.ReplayHash == replayHash, token);

        if (replay == null)
        {
            return null;
        }

        context.ReplayViewCounts.Add(new ReplayViewCount()
        {
            ReplayHash = replay.ReplayHash
        });
        await context.SaveChangesAsync();

        return replay with { Views = replay.Views + 1 };
    }

    public async Task<ReplayDto?> GetLatestReplay(CancellationToken token = default)
    {
        return await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(t => t.Spawns)
                    .ThenInclude(t => t.Units)
                        .ThenInclude(t => t.Unit)
            .Include(i => i.ReplayPlayers)
                .ThenInclude(t => t.Player)
            .AsNoTracking()
            .AsSplitQuery()
            .OrderByDescending(o => o.GameTime)
            .ProjectTo<ReplayDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(token);
    }

    public async Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default)
    {
        var replays = GetRequestReplays(request);

        if (token.IsCancellationRequested)
        {
            return 0;
        }
        else
        {
            return await replays.CountAsync(token);
        }
    }

    public async Task<ICollection<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token = default)
    {
        var replays = GetRequestReplays(request);

        replays = SortReplays(request, replays);

        if (token.IsCancellationRequested)
        {
            return new List<ReplayListDto>();
        }
        else
        {
            return await replays
                .Skip(request.Skip)
                .Take(request.Take)
                .AsNoTracking()
                .ProjectTo<ReplayListDto>(mapper.ConfigurationProvider)
                .ToListAsync(token);
        }
    }

    private IQueryable<Replay> SortReplays(ReplaysRequest request, IQueryable<Replay> replays)
    {

        foreach (var order in request.Orders)
        {
            if (order.Property == "Group/Round")
            {
                if (order.Ascending)
                {
                    replays = replays.AppendOrderBy("ReplayEvent.Round");
                }
                else
                {
                    replays = replays.AppendOrderByDescending("ReplayEvent.Round");
                }
            }
            else if (order.Property == "Teams")
            {
                if (order.Ascending)
                {
                    replays = replays.AppendOrderBy("ReplayEvent.WinnerTeam").AppendOrderBy("ReplayEvent.RunnerTeam");
                }
                else
                {
                    replays = replays.AppendOrderByDescending("ReplayEvent.WinnerTeam").AppendOrderByDescending("ReplayEvent.RunnerTeam");
                }
            }
            else if (order.Property == "Event")
            {
                if (order.Ascending)
                {
                    replays = replays.AppendOrderBy("ReplayEvent.Event.Name");
                }
                else
                {
                    replays = replays.AppendOrderByDescending("ReplayEvent.Event.Name");
                }
            }
            else
            {
                if (order.Ascending)
                {
                    replays = replays.AppendOrderBy(order.Property);
                }
                else
                {
                    replays = replays.AppendOrderByDescending(order.Property);
                }
            }
        }
        return replays;
    }

    private IQueryable<Replay> GetRequestReplays(ReplaysRequest request)
    {

#pragma warning disable CS8602
        var replays = (String.IsNullOrEmpty(request.Tournament), String.IsNullOrEmpty(request.SearchPlayers)) switch
        {
            (true, true) => context.Replays
                                .Include(i => i.ReplayEvent)
                                    .ThenInclude(i => i.Event)
                                .Include(i => i.ReplayPlayers)
                                .AsNoTracking(),
            (true, false) => context.Replays
                                .Include(i => i.ReplayEvent)
                                    .ThenInclude(i => i.Event)
                                .AsNoTracking(),
            (false, true) => context.Replays
                                .Include(i => i.ReplayPlayers)
                                .AsNoTracking(),
            _ => context.Replays
                .AsNoTracking()
        };
#pragma warning restore CS8602

        replays = replays.Where(x => x.GameTime >= request.StartTime);

        if (request.EndTime != null)
        {
            replays = replays.Where(x => x.GameTime < request.EndTime);
        }

        if (!String.IsNullOrEmpty(request.Tournament))
        {
            replays = replays.Where(x => x.ReplayEvent != null
                && x.ReplayEvent.Event.Name.Equals(request.Tournament));
        }

        if (request.GameModes.Any())
        {
            replays = replays.Where(x => request.GameModes.Contains(x.GameMode));
        }

        if (!String.IsNullOrEmpty(request.SearchPlayers))
        {
            foreach (string player in request.SearchPlayers.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                replays = replays.Where(x => x.ReplayPlayers.Any(a => a.Name.ToUpper().Contains(player.ToUpper())));
            }
        }

        replays = SearchReplays(replays, request);

        return replays;
    }

    private IQueryable<Replay> SearchReplays(IQueryable<Replay> replays, ReplaysRequest request)
    {
        if (String.IsNullOrEmpty(request.SearchString))
        {
            return replays;
        }

        var searchstrings = request.SearchString.Split(' ').ToHashSet();
        foreach (var search in searchstrings.Where(x => !String.IsNullOrEmpty(x)))
        {
            var cmdrs = GetSearchCommanders(search);
            if (cmdrs.Any())
            {
                //replays = replays
                //    .Where(x => (x.ReplayEvent != null && x.ReplayEvent.RunnerTeam.ToUpper().Contains(search.ToUpper()))
                //        || (x.ReplayEvent != null && x.ReplayEvent.WinnerTeam.ToUpper().Contains(search.ToUpper()))
                //        || (cmdrs.Any(a => x.CommandersTeam1.Contains(a)) || cmdrs.Any(a => x.CommandersTeam2.Contains(a)))
                //    );

                replays = replays
                    .Where(x => (x.ReplayEvent != null && x.ReplayEvent.RunnerTeam.ToUpper().Contains(search.ToUpper()))
                        || (x.ReplayEvent != null && x.ReplayEvent.WinnerTeam.ToUpper().Contains(search.ToUpper()))
                        || x.CommandersTeam1.Contains(cmdrs.First())
                        || x.CommandersTeam2.Contains(cmdrs.First())
                    );
            }
            else
            {
                replays = replays
                    .Where(x => (x.ReplayEvent != null && x.ReplayEvent.RunnerTeam.ToUpper().Contains(search.ToUpper()))
                        || (x.ReplayEvent != null && x.ReplayEvent.WinnerTeam.ToUpper().Contains(search.ToUpper()))
                    );
            }
        }
        return replays;
    }

    private List<string> GetSearchCommanders(string searchString)
    {
        var commanders = new List<string>();
        foreach (var cmdr in Enum.GetValues(typeof(Commander)).Cast<Commander>())
        {
            if (cmdr.ToString().ToUpper().Contains(searchString.ToUpper()))
            {
                commanders.Add($"|{(int)cmdr}|");
            }
        }
        return commanders;
    }

    public async Task<ICollection<string>> GetReplayPaths()
    {
        var skipReplayPaths = await context.SkipReplays
            .AsNoTracking()
            .Select(s => s.Path)
            .ToListAsync();

        if (skipReplayPaths.Any())
        {
            return (await context.Replays
                .AsNoTracking()
                .OrderByDescending(o => o.GameTime)
                .Select(s => s.FileName)
                .ToListAsync())
                .Union(skipReplayPaths)
                .ToList();
        }
        else
        {
            return await context.Replays
                .AsNoTracking()
                .OrderByDescending(o => o.GameTime)
                .Select(s => s.FileName)
                .ToListAsync();
        }
    }

    public async Task<(HashSet<Unit>, HashSet<Upgrade>)> SaveReplay(ReplayDto replayDto, HashSet<Unit> units, HashSet<Upgrade> upgrades, ReplayEventDto? replayEventDto)
    {
        var dbReplay = mapper.Map<Replay>(replayDto);
        dbReplay.SetDefaultFilter();

        if (replayDto.ReplayEvent != null)
        {
            replayEventDto = replayDto.ReplayEvent;
        }

        if (replayEventDto != null)
        {
            var dbEvent = await context.Events.FirstOrDefaultAsync(f => f.Name == replayEventDto.Event.Name);

            if (dbEvent == null)
            {
                dbEvent = new()
                {
                    Name = replayEventDto.Event.Name,
                    EventStart = DateTime.Today
                };
                context.Events.Add(dbEvent);
                await context.SaveChangesAsync();
            }

            var replayEvent = await context.ReplayEvents.FirstOrDefaultAsync(f => f.Event == dbEvent && f.Round == replayEventDto.Round && f.WinnerTeam == replayEventDto.WinnerTeam && f.RunnerTeam == replayEventDto.RunnerTeam);

            if (replayEvent == null)
            {
                replayEvent = new()
                {
                    Round = replayEventDto.Round,
                    WinnerTeam = replayEventDto.WinnerTeam,
                    RunnerTeam = replayEventDto.RunnerTeam,
                    Ban1 = replayEventDto.Ban1,
                    Ban2 = replayEventDto.Ban2,
                    Ban3 = replayEventDto.Ban3,
                    Ban4 = replayEventDto.Ban4,
                    Ban5 = replayEventDto.Ban5,
                    Event = dbEvent
                };
                context.ReplayEvents.Add(replayEvent);
                await context.SaveChangesAsync();
            }
            dbReplay.ReplayEvent = replayEvent;
        }

        foreach (var player in dbReplay.ReplayPlayers)
        {
            var dbPlayer = await context.Players.FirstOrDefaultAsync(f => f.ToonId == player.Player.ToonId);
            if (dbPlayer == null)
            {
                dbPlayer = new()
                {
                    Name = player.Player.Name,
                    ToonId = player.Player.ToonId
                };
                context.Players.Add(dbPlayer);
                try
                {
                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError($"failed saving replay: {ex.Message}");
                    throw;
                }
            }
            player.Player = dbPlayer;
            player.Name = dbPlayer.Name;

            foreach (var spawn in player.Spawns)
            {
                (spawn.Units, units) = await GetMapedSpawnUnits(spawn, player.Race, units);
            }

            (player.Upgrades, upgrades) = await GetMapedPlayerUpgrades(player, upgrades);

        }

        //await AddDbCommanderMmr((dbReplay.CommandersTeam1.TrimEnd('|') + dbReplay.CommandersTeam2).Trim('|'));
        await SeedCommanderMmrs();

        context.Replays.Add(dbReplay);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError($"failed saving replay: {ex.Message}");
            throw;
        }

        return (units, upgrades);
    }

    //private async Task AddDbCommanderMmr(string commanders)
    //{
    //    var commandersEnums = commanders.Split('|').Select(c => Enum.Parse<Commander>(c));

    //    foreach (var commander in commandersEnums) {
    //        foreach (var synCommander in commandersEnums) {
    //            var dbCommanderMmr = await context.CommanderMmrs.FirstOrDefaultAsync(f => (f.Commander == commander) && (f.SynCommander == synCommander));
    //            if (dbCommanderMmr == null) {
    //                dbCommanderMmr = new() {
    //                    Commander = commander,
    //                    SynCommander = synCommander
    //                };
    //                context.CommanderMmrs.Add(dbCommanderMmr);

    //                try {
    //                    await context.SaveChangesAsync();
    //                } catch (Exception ex) {
    //                    logger.LogError($"failed saving replay: {ex.Message}");
    //                    throw;
    //                }
    //            }
    //        }
    //    }
    //}

    private async Task SeedCommanderMmrs()
    {
        //using var scope = serviceProvider.CreateScope();
        //using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        if (!context.CommanderMmrs.Any()) {
            var allCommanders = Data.GetCommanders(Data.CmdrGet.NoStd);

            for (int i = 0; i < allCommanders.Count; i++) {
                for (int k = i; k < allCommanders.Count; k++) {
                    context.CommanderMmrs.Add(new() {
                        SynergyMmr = FireMmrService.startMmr,

                        Commander_1 = allCommanders[i],
                        Commander_2 = allCommanders[k],

                        AntiSynergyMmr_1 = FireMmrService.startMmr,
                        AntiSynergyMmr_2 = FireMmrService.startMmr,

                        AntiSynergyElo_1 = 0.5,
                        AntiSynergyElo_2 = 0.5
                    });
                }
            }


            //foreach (Commander cmdr in Data.GetCommanders(Data.CmdrGet.NoStd)) {
            //    foreach (Commander synCmdr in Data.GetCommanders(Data.CmdrGet.NoStd)) {
            //        context.CommanderMmrs.Add(new() {
            //            Commander = cmdr,
            //            SynCommander = synCmdr,
            //            Synergy = FireMmrService.startMmr,
            //            AntiSynergy = FireMmrService.startMmr
            //        });
            //    }
            //}
        }
        await context.SaveChangesAsync();
    }

    private async Task<(ICollection<SpawnUnit>, HashSet<Unit>)> GetMapedSpawnUnits(Spawn spawn, Commander commander, HashSet<Unit> units)
    {
        List<SpawnUnit> spawnUnits = new();
        foreach (var spawnUnit in spawn.Units)
        {
            var listUnit = units.FirstOrDefault(f => f.Name.Equals(spawnUnit.Unit.Name) && f.Commander.Equals(commander));
            if (listUnit == null)
            {
                listUnit = new()
                {
                    Name = spawnUnit.Unit.Name,
                    Commander = commander,
                };
                context.Units.Add(listUnit);
                await context.SaveChangesAsync();
                units.Add(listUnit);
            }

            spawnUnits.Add(new()
            {
                Count = spawnUnit.Count,
                Poss = spawnUnit.Poss,
                UnitId = listUnit.UnitId,
                SpawnId = spawn.SpawnId
            });
        }
        return (spawnUnits, units);
    }

    private async Task<(ICollection<PlayerUpgrade>, HashSet<Upgrade>)> GetMapedPlayerUpgrades(ReplayPlayer player, HashSet<Upgrade> upgrades)
    {
        List<PlayerUpgrade> playerUpgrades = new();
        foreach (var playerUpgrade in player.Upgrades)
        {
            var listUpgrade = upgrades.FirstOrDefault(f => f.Name.Equals(playerUpgrade.Upgrade.Name));
            if (listUpgrade == null)
            {
                listUpgrade = new()
                {
                    Name = playerUpgrade.Upgrade.Name
                };
                context.Upgrades.Add(listUpgrade);
                await context.SaveChangesAsync();
                upgrades.Add(listUpgrade);
            }

            playerUpgrades.Add(new()
            {
                Gameloop = playerUpgrade.Gameloop,
                UpgradeId = listUpgrade.UpgradeId,
                ReplayPlayerId = player.ReplayPlayerId
            });
        }
        return (playerUpgrades, upgrades);
    }

    public async Task<List<string>> GetSkipReplays()
    {
        return await context.SkipReplays
            .AsNoTracking()
            .OrderBy(o => o.Path)
            .Select(s => s.Path)
            .ToListAsync();
    }

    public async Task AddSkipReplay(string replayPath)
    {
        var skipReplay = await context.SkipReplays.FirstOrDefaultAsync(f => f.Path == replayPath);
        if (skipReplay == null)
        {
            context.SkipReplays.Add(new()
            {
                Path = replayPath
            });
            await context.SaveChangesAsync();
        }
    }

    public async Task RemoveSkipReplay(string replayPath)
    {
        var skipReplay = await context.SkipReplays.FirstOrDefaultAsync(f => f.Path == replayPath);
        if (skipReplay != null)
        {
            context.SkipReplays.Remove(skipReplay);
            await context.SaveChangesAsync();
        }
    }

    public async Task DeleteReplayByFileName(string fileName)
    {
        var replay = await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Spawns)
                    .ThenInclude(i => i.Units)
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Upgrades)

            .FirstOrDefaultAsync(f => f.FileName == fileName);

        if (replay != null)
        {
            context.Replays.Remove(replay);
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<string>> GetTournaments()
    {
        return await context.Events
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .Select(s => s.Name)
            .ToListAsync();
    }
}
