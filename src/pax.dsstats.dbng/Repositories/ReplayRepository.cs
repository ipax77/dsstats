using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pax.dsstats.dbng.Extensions;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Repositories;

public partial class ReplayRepository : IReplayRepository
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

    public async Task<ReplayDetailsDto?> GetDetailReplay(string replayHash, bool dry = false, CancellationToken token = default)
    {
        var replay = await context.Replays
            .AsNoTracking()
            .AsSplitQuery()
            .Where(x => x.ReplayHash == replayHash)
            .ProjectTo<ReplayDetailsDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(token);

        if (replay == null)
        {
            return null;
        }

        if (!dry)
        {
            context.ReplayViewCounts.Add(new ReplayViewCount()
            {
                ReplayHash = replay.ReplayHash
            });
            await context.SaveChangesAsync();
        }
        return replay with { Views = replay.Views + 1 };
    }

    public async Task<ReplayDto?> GetReplay(string replayHash, bool dry = false, CancellationToken token = default)
    {
        var replay = await context.Replays
            .AsNoTracking()
            .AsSplitQuery()
            .ProjectTo<ReplayDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(f => f.ReplayHash == replayHash, token);

        if (replay == null)
        {
            return null;
        }

        if (!dry)
        {
            context.ReplayViewCounts.Add(new ReplayViewCount()
            {
                ReplayHash = replay.ReplayHash
            });
            await context.SaveChangesAsync();
        }
        return replay with { Views = replay.Views + 1 };
    }

    public async Task<ReplayDetailsDto?> GetLatestReplay(CancellationToken token = default)
    {
        var hash = await context.Replays
            .OrderByDescending(o => o.GameTime)
            .Select(s => s.ReplayHash)
            .FirstOrDefaultAsync(token);

        if (String.IsNullOrEmpty(hash))
        {
            return null;
        }

        return await GetDetailReplay(hash, true, token);
    }

    public async Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default)
    {
        var replays = GetRequestReplays(request);
        return await replays.CountAsync(token);
    }

    public async Task<ICollection<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token = default)
    {
        var replays = GetRequestReplays(request);

        replays = SortReplays(request, replays);

        if (token.IsCancellationRequested)
        {
            return new List<ReplayListDto>();
        }

        if (request.WithMmrChange && (!String.IsNullOrEmpty(request.SearchPlayers) || Data.IsMaui))
        {
            var mmrlist = await replays
                .Skip(request.Skip)
                .Take(request.Take)
                .AsNoTracking()
                .ProjectTo<ReplayListRatingDto>(mapper.ConfigurationProvider)
                .ToListAsync(token);

            if (request.ToonId > 0)
            {
                for (int i = 0; i < mmrlist.Count; i++)
                {
                    var rep = mmrlist[i];

                    if (rep.ReplayRatingInfo == null)
                    {
                        continue;
                    }

                    var pl = rep.ReplayPlayers.FirstOrDefault(f => f.Player.ToonId == request.ToonId);
                    if (pl != null)
                    {
                        var rat = rep.ReplayRatingInfo.RepPlayerRatings.FirstOrDefault(f => f.GamePos == pl.GamePos);
                        rep.MmrChange = rat?.RatingChange ?? 0;
                        rep.Commander = pl.Race;
                    }
                }
            }
            else if (Data.IsMaui && String.IsNullOrEmpty(request.SearchPlayers))
            {
                for (int i = 0; i < mmrlist.Count; i++)
                {
                    var rep = mmrlist[i];

                    if (rep.ReplayRatingInfo == null)
                    {
                        continue;
                    }

                    var pl = rep.ReplayPlayers.FirstOrDefault(f => f.GamePos == rep.PlayerPos);
                    if (pl != null)
                    {
                        var rat = rep.ReplayRatingInfo.RepPlayerRatings.FirstOrDefault(f => f.GamePos == pl.GamePos);
                        rep.MmrChange = rat?.RatingChange ?? 0;
                        rep.Commander = pl.Race;
                    }
                }
            }
            else
            {
                string? interest = request.SearchPlayers?
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                for (int i = 0; i < mmrlist.Count; i++)
                {
                    var rep = mmrlist[i];

                    if (rep.ReplayRatingInfo == null)
                    {
                        continue;
                    }

                    var pl = rep.ReplayPlayers.FirstOrDefault(f => f.Name == interest);
                    if (pl != null)
                    {
                        var rat = rep.ReplayRatingInfo.RepPlayerRatings.FirstOrDefault(f => f.GamePos == pl.GamePos);
                        rep.MmrChange = rat?.RatingChange ?? 0;
                        rep.Commander = pl.Race;
                    }
                }

            }
            mmrlist.ForEach(f =>
            {
                f.ReplayPlayers.Clear();
                f.ReplayRatingInfo = null;
            });
            return mmrlist.Cast<ReplayListDto>().ToList();
        }

        var list = await replays
            .Skip(request.Skip)
            .Take(request.Take)
            .AsNoTracking()
            .ProjectTo<ReplayListDto>(mapper.ConfigurationProvider)
            .ToListAsync(token);

        return list;
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
        var replays = context.Replays
            .Where(x => x.GameTime > new DateTime(2018, 1, 1))
            .AsNoTracking();

        if (request.DefaultFilter)
        {
            replays = replays.Where(x => x.DefaultFilter);
        }

        if (request.PlayerCount != 0)
        {
            replays = replays.Where(x => x.Playercount == request.PlayerCount);
        }

        if (request.GameModes.Any())
        {
            replays = replays.Where(x => request.GameModes.Contains(x.GameMode));
        }

        if (request.ResultAdjusted)
        {
            replays = replays.Where(x => x.ResultCorrected);
        }

        if (request.ToonId == 0)
        {
            replays = SearchReplays(replays, request);
        }
        else
        {
            replays = SearchToonIds(replays, request);
        }

        return replays;
    }

    private IQueryable<Replay> SearchToonIds(IQueryable<Replay> replays, ReplaysRequest request)
    {
        if (request.ToonId == 0)
        {
            return replays;
        }

        if (request.ToonIdWith > 0)
        {
            replays = from r in replays
                      from rp in r.ReplayPlayers
                      from w in r.ReplayPlayers
                      where rp.Player.ToonId == request.ToonId
                      where w.Team == rp.Team && w.Player.ToonId == request.ToonIdWith
                      select r;
        }
        else if (request.ToonIdVs > 0)
        {
            replays = from r in replays
                      from rp in r.ReplayPlayers
                      from w in r.ReplayPlayers
                      where rp.Player.ToonId == request.ToonId
                      where w.Team != rp.Team && w.Player.ToonId == request.ToonIdVs
                      select r;
        }
        else
        {
            replays = replays.Where(x => x.ReplayPlayers.Any(a => a.Player.ToonId == request.ToonId));
        }
        return replays;
    }

    private IQueryable<Replay> SearchReplays(IQueryable<Replay> replays, ReplaysRequest request, bool withEvent = false)
    {
        if (String.IsNullOrEmpty(request.SearchPlayers) && String.IsNullOrEmpty(request.SearchString))
        {
            return replays;
        }

        var searchStrings = request.SearchString?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().ToList() ?? new List<string>();
        var searchPlayers = request.SearchPlayers?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().ToList() ?? new List<string>();
        var searchCmdrs = searchStrings.SelectMany(s => GetSearchCommanders(s)).Distinct().ToList();


        if (request.LinkSearch)
        {
            return LinkReplays(replays, searchCmdrs, searchPlayers);
        }

        replays = FilterCommanders(replays, searchCmdrs);
        replays = FilterNames(replays, searchPlayers, withEvent);

        return replays;
    }

    private IQueryable<Replay> FilterCommanders(IQueryable<Replay> replays, List<Commander> searchCmdrs)
    {
        foreach (var cmdr in searchCmdrs)
        {
            replays = replays
                .Where(x => x.CommandersTeam1.Contains($"|{(int)cmdr}|")
                    || x.CommandersTeam2.Contains($"|{(int)cmdr}|")
                );
        }
        return replays;
    }

    private IQueryable<Replay> FilterNames(IQueryable<Replay> replays, List<string> searchPlayers, bool withEvent = false)
    {
        foreach (var player in searchPlayers)
        {
            if (withEvent)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                replays = replays
                    .Where(x => x.ReplayEvent.WinnerTeam.ToUpper().Contains(player.ToUpper())
                    || x.ReplayEvent.RunnerTeam.ToUpper().Contains(player.ToUpper())
                    || x.ReplayPlayers.Any(a => a.Name.ToUpper().Contains(player.ToUpper())));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
            else
            {
                replays = replays.Where(x => x.ReplayPlayers.Any(a => a.Name.ToUpper().Contains(player.ToUpper())));
            }
        }
        return replays;
    }

    private IQueryable<Replay> LinkReplays(IQueryable<Replay> replays, List<Commander> searchCmdrs, List<string> searchPlayers)
    {
        int links = Math.Min(searchCmdrs.Count, searchPlayers.Count);
        if (links > 0)
        {
            for (int i = 0; i < links; i++)
            {
                var cmdr = searchCmdrs[i];
                var name = searchPlayers[i].ToUpper();
                replays = replays.Where(x => x.ReplayPlayers.Any(a => a.Race == cmdr && a.Name.ToUpper().Contains(name)));
            }
        }

        if (searchCmdrs.Count > links)
        {
            replays = FilterCommanders(replays, searchCmdrs.Skip(links).ToList());
        }

        if (searchPlayers.Count > links)
        {
            replays = FilterNames(replays, searchPlayers.Skip(links).ToList());
        }

        return replays;
    }

    private List<Commander> GetSearchCommanders(string searchString)
    {
        List<Commander> cmdrs = new();
        foreach (var cmdr in Enum.GetValues(typeof(Commander)).Cast<Commander>())
        {
            if (cmdr.ToString().ToUpper().Contains(searchString.ToUpper()))
            {
                //commanders.Add($"|{(int)cmdr}|");
                cmdrs.Add(cmdr);
            }
        }
        return cmdrs;
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

    public async Task<(HashSet<Unit>, HashSet<Upgrade>, Replay)> SaveReplay(ReplayDto replayDto, HashSet<Unit> units, HashSet<Upgrade> upgrades, ReplayEventDto? replayEventDto)
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
                    EventStart = DateTime.UtcNow.Date
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

        bool isComputer = false;

        foreach (var replayPlayer in dbReplay.ReplayPlayers)
        {
            if (replayPlayer.Player.ToonId == 0)
            {
                isComputer = true;
            }

            var dbPlayer = await context.Players.FirstOrDefaultAsync(f => f.ToonId == replayPlayer.Player.ToonId);
            if (dbPlayer == null)
            {
                dbPlayer = new()
                {
                    Name = replayPlayer.Player.Name,
                    ToonId = replayPlayer.Player.ToonId,
                    RegionId = replayPlayer.Player.RegionId,
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
            else
            {
                dbPlayer.RegionId = replayPlayer.Player.RegionId;
                dbPlayer.Name = replayPlayer.Player.Name;
            }

            replayPlayer.Player = dbPlayer;
            replayPlayer.Name = dbPlayer.Name;

            foreach (var spawn in replayPlayer.Spawns)
            {
                (spawn.Units, units) = await GetMapedSpawnUnits(spawn, replayPlayer.Race, units);
            }

            (replayPlayer.Upgrades, upgrades) = await GetMapedPlayerUpgrades(replayPlayer, upgrades);

        }

        if (isComputer)
        {
            dbReplay.GameMode = GameMode.Tutorial;
        }

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

        return (units, upgrades, dbReplay);
    }

    private async Task<(ICollection<SpawnUnit>, HashSet<Unit>)> GetMapedSpawnUnits(Spawn spawn, Commander commander, HashSet<Unit> units)
    {
        List<SpawnUnit> spawnUnits = new();
        foreach (var spawnUnit in spawn.Units)
        {
            var listUnit = units.FirstOrDefault(f => f.Name.Equals(spawnUnit.Unit.Name));
            if (listUnit == null)
            {
                listUnit = new()
                {
                    Name = spawnUnit.Unit.Name
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
# pragma warning disable CS8602
        var replay = await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Spawns)
                    .ThenInclude(i => i.Units)
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Upgrades)
            .Include(i => i.ReplayRatingInfo)
                .ThenInclude(i => i.RepPlayerRatings)
            .FirstOrDefaultAsync(f => f.FileName == fileName);

        if (replay != null)
        {
            context.Replays.Remove(replay);
            await context.SaveChangesAsync();
        }
# pragma warning restore CS8602
    }

    public async Task DeleteReplayAfterDate(DateTime startTime)
    {
# pragma warning disable CS8602
        var replays = await context.Replays
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Spawns)
                    .ThenInclude(i => i.Units)
            .Include(i => i.ReplayPlayers)
                .ThenInclude(i => i.Upgrades)
            .Include(i => i.ReplayRatingInfo)
                .ThenInclude(i => i.RepPlayerRatings)
            .Where(x => x.GameTime > startTime)
            .AsSplitQuery()
            .ToListAsync();

        if (replays.Any())
        {
            foreach (var replay in replays)
            {
                context.Replays.Remove(replay);
                await context.SaveChangesAsync();
            }
        }
# pragma warning restore CS8602
    }

    public async Task<List<EventListDto>> GetTournaments()
    {
        return await context.Events
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ProjectTo<EventListDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task SetReplayViews()
    {
        var viewedHashes = await context.ReplayViewCounts
            .ToListAsync();

        var replayHashViews = viewedHashes.GroupBy(g => g.ReplayHash)
            .Select(s => new { Hash = s.Key, Count = s.Count() })
            .ToDictionary(k => k.Hash, v => v.Count);

        int i = 0;
        foreach (var ent in replayHashViews)
        {
            var replay = await context.Replays
                .FirstOrDefaultAsync(f => f.ReplayHash == ent.Key);
            if (replay != null)
            {
                replay.Views += ent.Value;
            }
            if (i % 1000 == 0)
            {
                await context.SaveChangesAsync();
            }
        }
        await context.SaveChangesAsync();

        context.ReplayViewCounts.RemoveRange(viewedHashes);

        await context.SaveChangesAsync();
    }

    public async Task SetReplayDownloads()
    {
        var downloadedHashes = await context.ReplayDownloadCounts
            .ToListAsync();

        var replayHashDownloads = downloadedHashes.GroupBy(g => g.ReplayHash)
            .Select(s => new { Hash = s.Key, Count = s.Count() })
            .ToDictionary(k => k.Hash, v => v.Count);

        int i = 0;
        foreach (var ent in replayHashDownloads)
        {
            var replay = await context.Replays
                .FirstOrDefaultAsync(f => f.ReplayHash == ent.Key);
            if (replay != null)
            {
                replay.Downloads += ent.Value;
            }
            if (i % 1000 == 0)
            {
                await context.SaveChangesAsync();
            }
        }
        await context.SaveChangesAsync();

        context.ReplayDownloadCounts.RemoveRange(downloadedHashes);

        await context.SaveChangesAsync();
    }
}
