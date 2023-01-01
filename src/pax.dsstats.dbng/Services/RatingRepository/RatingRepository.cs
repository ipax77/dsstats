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

    public async Task<Dictionary<RatingType, Dictionary<int, CalcRating>>> GetCalcRatings(List<ReplayDsRDto> replayDsRDtos, MmrOptions mmrOptions)
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

                    calcRatings[ratingType].Add(ratingMemory.RavenPlayer.ToonId, GetCalcRating(ratingMemory.RavenPlayer, ratingMemory.CmdrRavenRating, mmrOptions));
                }
                else if (ratingType == RatingType.Std)
                {
                    if (ratingMemory.StdRavenRating == null)
                    {
                        //ToDo
                        ratingMemory.StdRavenRating = new RavenRating();
                    }

                    calcRatings[ratingType].Add(ratingMemory.RavenPlayer.ToonId, GetCalcRating(ratingMemory.RavenPlayer, ratingMemory.StdRavenRating, mmrOptions));
                }
            }
        }

        return await Task.FromResult(calcRatings);
    }

    private static CalcRating GetCalcRating(RavenPlayer ravenPlayer, RavenRating ravenRating, MmrOptions mmrOptions)
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

            Mmr = ravenRating?.Mmr ?? mmrOptions.StartMmr,
            MmrOverTime = GetTimeRatings(ravenRating?.MmrOverTime),

            CmdrCounts = new(), // ToDo ???
        };
    }

    public List<int> GetNameToonIds(string name)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return context.Players
            .Where(x => x.Name == name)
            .Select(s => s.ToonId)
            .ToList();

        //return RatingMemory.Values
        //    .Where(x => x.RavenPlayer.Name == name)
        //    .Select(s => s.RavenPlayer.ToonId)
        //    .ToList();
    }

    public async Task<List<RequestNames>> GetRequestNames(string name)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return await context.Players
            .Where(x => x.Name == name)
            .Select(s => new RequestNames()
            {
                Name = s.Name,
                ToonId = s.ToonId,
                RegionId = s.RegionId
            })
            .ToListAsync();

        //return RatingMemory.Values
        //    .Where(x => x.RavenPlayer.Name == name)
        //    .Select(s => s.RavenPlayer.ToonId)
        //    .ToList();
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
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return await context.Players
            .Where(x => x.ToonId == toonId)
            .Select(s => s.Name)
            .FirstOrDefaultAsync();
    }

    public async Task<RequestNames?> GetRequestNames(int toonId)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return await context.Players
            .Where(x => x.ToonId == toonId)
            .Select(s => new RequestNames()
            {
                Name = s.Name,
                ToonId = toonId,
                RegionId = s.RegionId,
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<RequestNames>> GetTopPlayers(RatingType ratingType, int minGames)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        return await context.PlayerRatings
            .Where(x => x.RatingType == ratingType
                && x.Games >= minGames)
            .OrderByDescending(o => o.Rating)
            .Take(5)
            .Select(s => new RequestNames()
            {
                Name = s.Player.Name,
                ToonId = s.Player.ToonId,
                RegionId = s.Player.RegionId
            })
            .ToListAsync();
    }

    public async Task SetReplayListMmrChanges(List<ReplayListDto> replays, int toonId, CancellationToken token = default)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replayIds = replays.Select(s => s.ReplayId).Distinct().ToList();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var replayPlayerRatings = await context.ReplayPlayerRatings
            .Where(x => replayIds.Contains(x.ReplayId))
            .Select(s => new MmrChangesList()
            {
                ReplayId = s.ReplayId,
                ReplayPlayerId = s.ReplayPlayerId,
                Commander = s.ReplayPlayer.Race,
                Pos = s.Pos,
                MmrChange = Math.Round(s.MmrChange, 1)
            })
            .ToListAsync(token);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        var replayPlayerIds = await context.ReplayPlayers
            .Where(x => x.Player.ToonId == toonId
                && replayIds.Contains(x.ReplayId))
            .Select(s => s.ReplayPlayerId)
            .ToListAsync();

        for (int i = 0; i < replays.Count; i++)
        {
            var replay = replays[i];

            var mmrChange = replayPlayerRatings
                .FirstOrDefault(f => f.ReplayId == replay.ReplayId
                    && replayPlayerIds.Contains(f.ReplayPlayerId));

            if (mmrChange != null)
            {
                replay.MmrChange = mmrChange.MmrChange;
                replay.Commander = mmrChange.Commander;
            }
        }
    }

    public async Task SetReplayListMmrChanges(List<ReplayListDto> replays, string? searchPlayer = null, CancellationToken token = default)
    {
        if (String.IsNullOrEmpty(searchPlayer) && !replays.Any(a => a.PlayerPos > 0))
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var replayIds = replays.Select(s => s.ReplayId).Distinct().ToList();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var replayPlayerRatings = await context.ReplayPlayerRatings
            .Where(x => replayIds.Contains(x.ReplayId))
            .Select(s => new MmrChangesList()
            {
                ReplayId = s.ReplayId,
                Name = s.ReplayPlayer.Name,
                Commander = s.ReplayPlayer.Race,
                Pos = s.Pos,
                MmrChange = Math.Round(s.MmrChange, 1)
            })
            .ToListAsync(token);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        string? interest = null;
        if (!String.IsNullOrEmpty(searchPlayer))
        {
            interest = replayPlayerRatings
                .Select(s => s.Name)
                .FirstOrDefault(f => f.ToUpper().Contains(searchPlayer.ToUpper()));
        }

        for (int i = 0; i < replays.Count; i++)
        {
            var replay = replays[i];

            MmrChangesList? mmrChange;
            if (interest == null)
            {
                if (replay.PlayerPos == 0)
                {
                    continue;
                }
                mmrChange = replayPlayerRatings
                    .FirstOrDefault(f => f.ReplayId == replay.ReplayId && f.Pos == replay.PlayerPos);
            }
            else
            {
                mmrChange = replayPlayerRatings
                    .FirstOrDefault(f => f.ReplayId == replay.ReplayId && f.Name == interest);
            }

            if (mmrChange != null)
            {
                replay.MmrChange = mmrChange.MmrChange;
                replay.Commander = mmrChange.Commander;
            }
        }
    }

    public async Task<int> UpdateMmrChanges(List<MmrChange> replayPlayerMmrChanges, int appendId, string csvBasePath)
    {
        if (Data.IsMaui)
        {
            return await MauiUpdateMmrChanges(replayPlayerMmrChanges, appendId);
        }
        else
        {
            return WriteMmrChangeCsv(replayPlayerMmrChanges, appendId, csvBasePath);
            // return await MysqlUpdateMmrChanges(replayPlayerMmrChanges, appendId);
        }
    }

    public async Task<UpdateResult> UpdateRavenPlayers(Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings,
                                                       bool continueCalc,
                                                       string csvBasePath)
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
            if (!continueCalc)
            {
                // ReCalc
                CreatePlayerRatingCsv(mmrIdRatings, csvBasePath);
            }
            else
            {
                // Continue
                await MysqlUpdateRavenPlayers(mmrIdRatings);
            }
            await Csv2MySql(continueCalc, csvBasePath);
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

internal record MmrChangesList
{
    public int ReplayId { get; init; }
    public int ReplayPlayerId { get; init; }
    public string Name { get; init; } = "Anonymous";
    public Commander Commander { get; init; }
    public int Pos { get; init; }
    public double MmrChange { get; init; }
}