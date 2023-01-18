using AutoMapper;
using dsstats.mmr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class RatingRepository : IRatingRepository
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IMapper mapper;
    private readonly ILogger<RatingRepository> logger;

    public RatingRepository(IServiceScopeFactory scopeFactory, IMapper mapper, ILogger<RatingRepository> logger)
    {
        this.scopeFactory = scopeFactory;
        this.mapper = mapper;
        this.logger = logger;
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
                Pos = rating.Pos,
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

    public async Task<(int, int)> UpdateMmrChanges(List<ReplayRatingDto> replayRatingDtos, int replayAppendId, int playerAppendId, string csvBasePath)
    {
        if (Data.IsMaui)
        {
            (replayAppendId, playerAppendId) = await MauiUpdateMmrChanges(replayRatingDtos, replayAppendId, playerAppendId);
            return (replayAppendId, playerAppendId);
        }
        else
        {
            return WriteMmrChangeCsv(replayRatingDtos, replayAppendId, playerAppendId, csvBasePath);
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

