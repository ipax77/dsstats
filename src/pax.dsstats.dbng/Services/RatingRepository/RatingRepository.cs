using AutoMapper;
using dsstats.mmr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;
using pax.dsstats.shared.Raven;

namespace pax.dsstats.dbng.Services;

public partial class RatingRepository : IRatingRepository
{
    private static Dictionary<int, RatingMemory> RatingMemory = new();
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IMapper mapper;
    private readonly ILogger<RatingRepository> logger;

    public RatingRepository(IServiceScopeFactory scopeFactory, IMapper mapper, ILogger<RatingRepository> logger)
    {
        this.scopeFactory = scopeFactory;
        this.mapper = mapper;
        this.logger = logger;
    }

    public async Task<Dictionary<RatingType, Dictionary<int, CalcRating>>> GetCalcRatings(List<ReplayDsRDto> replayDsRDtos)
    {
        Dictionary<RatingType, Dictionary<int, CalcRating>> calcRatings = new()
        {
            { RatingType.Cmdr, new() },
            { RatingType.Std, new() },
        };

        foreach (var replayDsrDto in replayDsRDtos)
        {
            foreach (var replayPlayerDsRDto in replayDsrDto.ReplayPlayers)
            {
                if (!RatingMemory.TryGetValue(replayPlayerDsRDto.Player.ToonId, out var ratingMemory))
                {
                    ratingMemory = RatingMemory[replayPlayerDsRDto.Player.ToonId] = new RatingMemory()
                    {
                        RavenPlayer = new RavenPlayer()
                        {
                            RegionId = replayPlayerDsRDto.Player.RegionId,
                            PlayerId = replayPlayerDsRDto.Player.PlayerId,
                            IsUploader = replayPlayerDsRDto.IsUploader,
                            Name = replayPlayerDsRDto.Player.Name,
                            ToonId = replayPlayerDsRDto.Player.ToonId
                        }
                    };
                }

                RatingType ratingType = MmrService.GetRatingType(replayDsrDto);

                if (ratingType == RatingType.Cmdr)
                {
                    if (ratingMemory.CmdrRavenRating == null)
                    {
                        //ToDo
                        ratingMemory.CmdrRavenRating = new RavenRating();
                    }

                    calcRatings[ratingType].Add(ratingMemory.RavenPlayer.ToonId, GetCalcRating(ratingMemory.RavenPlayer, ratingMemory.CmdrRavenRating));
                }
                else if (ratingType == RatingType.Std)
                {
                    if (ratingMemory.StdRavenRating == null)
                    {
                        //ToDo
                        ratingMemory.StdRavenRating = new RavenRating();
                    }

                    calcRatings[ratingType].Add(ratingMemory.RavenPlayer.ToonId, GetCalcRating(ratingMemory.RavenPlayer, ratingMemory.StdRavenRating));
                }
            }
        }

        return await Task.FromResult(calcRatings);
    }

    private static CalcRating GetCalcRating(RavenPlayer ravenPlayer, RavenRating ravenRating)
    {
        return new CalcRating()
        {
            IsUploader = ravenPlayer.IsUploader,
            Confidence = ravenRating?.Confidence ?? 0,
            Consistency = ravenRating?.Consistency ?? 0,
            Games = ravenRating?.Games ?? 0,
            TeamGames = ravenRating?.TeamGames ?? 0,
            Wins = ravenRating?.Wins ?? 0,
            Mvp = ravenRating?.Mvp ?? 0,

            Mmr = ravenRating?.Mmr ?? MmrService.startMmr,
            MmrOverTime = GetTimeRatings(ravenRating?.MmrOverTime),

            CmdrCounts = new(), // ToDo ???
        };
    }

    public List<int> GetNameToonIds(string name)
    {
        return RatingMemory.Values
            .Where(x => x.RavenPlayer.Name == name)
            .Select(s => s.RavenPlayer.ToonId)
            .ToList();
    }

    public async Task<RavenPlayerDetailsDto> GetPlayerDetails(int toonId, CancellationToken token = default)
    {
        if (RatingMemory.ContainsKey(toonId))
        {
            var ratingMemory = RatingMemory[toonId];
            RavenPlayerDetailsDto dto = new()
            {
                Name = ratingMemory.RavenPlayer.Name,
                ToonId = ratingMemory.RavenPlayer.ToonId,
                RegionId = ratingMemory.RavenPlayer.RegionId,
                IsUploader = ratingMemory.RavenPlayer.IsUploader,
            };

            if (ratingMemory.CmdrRavenRating != null)
            {
                dto.Ratings.Add(new()
                {
                    Type = ratingMemory.CmdrRavenRating.Type,
                    Games = ratingMemory.CmdrRavenRating.Games,
                    Wins = ratingMemory.CmdrRavenRating.Wins,
                    Mvp = ratingMemory.CmdrRavenRating.Mvp,
                    TeamGames = ratingMemory.CmdrRavenRating.TeamGames,
                    Main = ratingMemory.CmdrRavenRating.Main,
                    MainPercentage = ratingMemory.CmdrRavenRating.MainPercentage,
                    Mmr = ratingMemory.CmdrRavenRating.Mmr,
                    MmrOverTime = ratingMemory.CmdrRavenRating.MmrOverTime,
                });
            }

            if (ratingMemory.StdRavenRating != null)
            {
                dto.Ratings.Add(new()
                {
                    Type = ratingMemory.StdRavenRating.Type,
                    Games = ratingMemory.StdRavenRating.Games,
                    Wins = ratingMemory.StdRavenRating.Wins,
                    Mvp = ratingMemory.StdRavenRating.Mvp,
                    TeamGames = ratingMemory.StdRavenRating.TeamGames,
                    Main = ratingMemory.StdRavenRating.Main,
                    MainPercentage = ratingMemory.StdRavenRating.MainPercentage,
                    Mmr = ratingMemory.StdRavenRating.Mmr,
                    MmrOverTime = ratingMemory.StdRavenRating.MmrOverTime,
                });
            }
            return dto;
        }
        else
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

            var ratings = await context.PlayerRatings
                .Include(i => i.Player)
                .Where(x => x.Player != null && x.Player.ToonId == toonId)
                .ToListAsync(token);

            if (!ratings.Any())
            {
                return new RavenPlayerDetailsDto();
            }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            RavenPlayerDetailsDto dto = new()
            {
                Name = ratings.First().Player.Name,
                ToonId = ratings.First().Player.ToonId,
                RegionId = ratings.First().Player.RegionId,
                IsUploader = ratings.First().IsUploader,
            };
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            foreach (var rating in ratings)
            {
                dto.Ratings.Add(new()
                {
                    Type = rating.RatingType,
                    Games = rating.Games,
                    Wins = rating.Wins,
                    Mvp = rating.Mvp,
                    TeamGames = rating.TeamGames,
                    Main = rating.Main,
                    MainPercentage = rating.Games == 0 ? 0 : Math.Round(rating.MainCount * 100.0 / rating.Games, 2),
                    Mmr = rating.Rating,
                    MmrOverTime = rating.MmrOverTime,
                });
            }
            return dto;

        }
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviation()
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return await context.PlayerRatings
            .Where(x => x.RatingType == RatingType.Cmdr)
            .GroupBy(g => Math.Round(g.Rating, 0))
            .Select(s => new MmrDevDto
            {
                Count = s.Count(),
                Mmr = s.Average(a => Math.Round(a.Rating, 0))
            })
            .OrderBy(o => o.Mmr)
            .ToListAsync();
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviationStd()
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return await context.PlayerRatings
            .Where(x => x.RatingType == RatingType.Std)
            .GroupBy(g => Math.Round(g.Rating, 0))
            .Select(s => new MmrDevDto
            {
                Count = s.Count(),
                Mmr = s.Average(a => Math.Round(a.Rating, 0))
            })
            .OrderBy(o => o.Mmr)
            .ToListAsync();
    }

    public async Task<List<PlChange>> GetReplayPlayerMmrChanges(string replayHash, CancellationToken token = default)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replayId = await context.Replays
            .Where(x => x.ReplayHash == replayHash)
            .Select(s => s.ReplayId)
            .FirstOrDefaultAsync();

        return await context.ReplayPlayerRatings
            .Where(x => x.ReplayId == replayId)
            .Select(s => new PlChange()
            {
                Pos = s.Pos,
                ReplayPlayerId = s.ReplayPlayerId,
                Change = Math.Round(s.MmrChange, 1)
            })
            .ToListAsync();
    }

    public async Task<string?> GetToonIdName(int toonId)
    {
        if (RatingMemory.ContainsKey(toonId))
        {
            return RatingMemory[toonId].RavenPlayer.Name;
        }
        return await Task.FromResult("Anonymous");
    }

    public List<RequestNames> GetTopPlayers(RatingType ratingType, int minGames)
    {
        if (ratingType == RatingType.Cmdr)
        {
            return RatingMemory.Values
                .Where(x => x.CmdrRavenRating != null && x.CmdrRavenRating.Games >= minGames)
                .OrderByDescending(o => o.CmdrRavenRating?.Wins * 100.0 / o.CmdrRavenRating?.Games)
                .Take(5)
                .Select(s => new RequestNames() { Name = s.RavenPlayer.Name, ToonId = s.RavenPlayer.ToonId })
                .ToList();
        }
        else if (ratingType == RatingType.Std)
        {
            return RatingMemory.Values
                .Where(x => x.StdRavenRating != null && x.StdRavenRating.Games >= minGames)
                .OrderByDescending(o => o.StdRavenRating?.Wins * 100.0 / o.StdRavenRating?.Games)
                .Take(5)
                .Select(s => new RequestNames() { Name = s.RavenPlayer.Name, ToonId = s.RavenPlayer.ToonId })
                .ToList();
        }
        return new();
    }

    public async Task SetReplayListMmrChanges(List<ReplayListDto> replays, CancellationToken token = default)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        for (int i = 0; i < replays.Count; i++)
        {
            if (replays[i].PlayerPos == 0)
            {
                continue;
            }

            if (token.IsCancellationRequested)
            {
                return;
            }
            replays[i].MmrChange = await context.ReplayPlayerRatings
                .Where(f => f.ReplayId == replays[i].ReplayId
                    && f.Pos == replays[i].PlayerPos)
                .Select(s => Math.Round(s.MmrChange, 1))
                .FirstOrDefaultAsync(token);
        }
    }

    public async Task<int> UpdateMmrChanges(List<MmrChange> replayPlayerMmrChanges, int appendId)
    {
        if (Data.IsMaui)
        {
            return await MauiUpdateMmrChanges(replayPlayerMmrChanges, appendId);
        }
        else
        {
            return WriteMmrChangeCsv(replayPlayerMmrChanges, appendId);
            // return await MysqlUpdateMmrChanges(replayPlayerMmrChanges, appendId);
        }
    }

    public async Task<UpdateResult> UpdateRavenPlayers(Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings)
    {
        if (!mmrIdRatings.Any())
        {
            return new();
        }

        if (Data.IsMaui)
        {
            return await MauiUpdateRavenPlayers(mmrIdRatings);
        }
        else
        {
            // ReCalc
            CreatePlayerRatingCsv(mmrIdRatings);
            await Csv2MySql();

            // Continue
            //return await MysqlUpdateRavenPlayers(mmrIdRatings);
        }
        return new();
    }


    private static List<TimeRating> GetTimeRatings(string? mmrOverTime)
    {
        if (string.IsNullOrEmpty(mmrOverTime))
        {
            return new();
        }

        List<TimeRating> timeRatings = new();

        foreach (var ent in mmrOverTime.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var timeMmr = ent.Split(',');
            if (timeMmr.Length == 2)
            {
                if (double.TryParse(timeMmr[0], out double mmr))
                {
                    timeRatings.Add(new TimeRating()
                    {
                        Mmr = mmr,
                        Date = timeMmr[1]
                    });
                }
            }
        }
        return timeRatings;
    }
}

internal record RatingMemory
{
    public RavenPlayer RavenPlayer { get; set; } = null!;
    public RavenRating? CmdrRavenRating { get; set; }
    public RavenRating? StdRavenRating { get; set; }
}