using AutoMapper;
using dsstats.mmr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using pax.dsstats.shared;
using pax.dsstats.shared.Ratings;
using System.Diagnostics;

namespace pax.dsstats.dbng.Services.Ratings;

public partial class ArcadeRatingsService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IMapper mapper;
    private readonly IOptions<DbImportOptions> dbImportOptions;
    private readonly ILogger<ArcadeRatingsService> logger;

    private SemaphoreSlim ratingSs;

    public ArcadeRatingsService(IServiceProvider serviceProvider,
                      IMapper mapper,
                      IOptions<DbImportOptions> dbImportOptions,
                      ILogger<ArcadeRatingsService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.mapper = mapper;
        this.dbImportOptions = dbImportOptions;
        this.logger = logger;
        ratingSs = new(1, 1);
    }

    public async Task ProduceRatings(bool recalc = true)
    {
        await ratingSs.WaitAsync();

        Stopwatch sw = Stopwatch.StartNew();

        try
        {
            var request = recalc == false ?
                await GetCalcRatingRequest() :
                new MmrService.CalcRatingRequest()
                {
                    CmdrMmrDic = new(),
                    MmrIdRatings = await GetMmrIdRatings(null), // todo: continue
                    MmrOptions = new(reCalc: true),
                    StartTime = new(2021, 2, 1),
                    EndTime = DateTime.Today.AddDays(2),
                };

            if (request == null)
            {
                // nothing to do
                logger.LogWarning("nothing to do2");
                return;
            }

            recalc = request.MmrOptions.ReCalc;

            if (recalc)
            {
                await GeneratePlayerRatings(request);
                await SaveRatings(request.MmrIdRatings);
            }
            else
            {
                await UpdatePlayerRatings(request.MmrIdRatings);
                await ContinueReplayRatingsFromCsv2MySql(ArcadeRatingsCsvService.csvBasePath);
                await ContinueReplayPlayerRatingsFromCsv2MySql(ArcadeRatingsCsvService.csvBasePath);
            }
        }
        finally
        {
            sw.Stop();
            logger.LogWarning($"Arcade Ratings produced ({recalc}) in {sw.ElapsedMilliseconds} ms ({sw.Elapsed.ToString(@"mm\:ss")} mins)");
            ratingSs.Release();
        }
    }

    private async Task SaveRatings(Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings)
    {
        ArcadeRatingsCsvService.CreatePlayerRatingCsv(mmrIdRatings);
        await PlayerRatingsFromCsv2MySql(ArcadeRatingsCsvService.csvBasePath);
        await ReplayRatingsFromCsv2MySql(ArcadeRatingsCsvService.csvBasePath);
        await ReplayPlayerRatingsFromCsv2MySql(ArcadeRatingsCsvService.csvBasePath);
        await SetPlayerRatingsPos();
        await SetRatingChange();
    }

    private async Task GeneratePlayerRatings(MmrService.CalcRatingRequest request)
    {
        int skip = 0;
        int take = 200000;

        request.ReplayDsRDtos = await GetReplayData(request.StartTime, skip, take);

        while (request.ReplayDsRDtos.Any())
        {
            var calcResult = MmrService.GeneratePlayerRatings(request, dry: true);

            (request.ReplayRatingAppendId, request.ReplayPlayerRatingAppendId) = ArcadeRatingsCsvService
                .CreateOrAppendReplayAndReplayPlayerRatingsCsv(calcResult.replayRatingDtos, calcResult.ReplayRatingAppendId, calcResult.ReplayPlayerRatingAppendId);

            skip += take;
            request.ReplayDsRDtos = await GetReplayData(request.StartTime, skip, take);
        }
    }

    private async Task<List<ReplayDsRDto>> GetReplayData(DateTime startTime, int skip, int take)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        List<GameMode> gameModes = new() { GameMode.Commanders, GameMode.Standard, GameMode.CommandersHeroic };

        var replays = context.ArcadeReplays
            .Include(i => i.ArcadeReplayPlayers)
                .ThenInclude(i => i.ArcadePlayer)
            .Where(r =>
                r.CreatedAt >= startTime
                && r.PlayerCount == 6
                && r.Duration >= 300
                && r.WinnerTeam > 0
                && r.ArcadeReplayRating == null // ??
                && gameModes.Contains(r.GameMode));

        var dsrReplays = from r in replays
                         orderby r.CreatedAt, r.ArcadeReplayId
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
                                     RegionId = s.ArcadePlayer.RegionId,
                                     RealmId = s.ArcadePlayer.RealmId,
                                 }
                             }).ToList()
                         };

        var dsrReplaysList = await dsrReplays
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        foreach (var replay in dsrReplaysList)
        {
            var rps = replay.ReplayPlayers.Select(s => s with { Duration = replay.Duration }).ToList();
            replay.ReplayPlayers.Clear();
            replay.ReplayPlayers.AddRange(rps);
        }

        return dsrReplaysList;
    }

    private async Task<MmrService.CalcRatingRequest?> GetCalcRatingRequest()
    {
        MmrOptions mmrOptions = new(reCalc: true);

        int replayRatingAppendId = 0;
        int replayPlayerRatingAppendId = 0;

        DateTime startTime = new DateTime(2021, 2, 1);
        DateTime endTime = DateTime.Today.AddDays(2);

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var latestRatingsProduced = await context.ArcadeReplays
            .Where(x => x.ArcadeReplayRating != null)
            .OrderByDescending(o => o.CreatedAt)
            .Select(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (latestRatingsProduced > DateTime.MinValue)
        {
            mmrOptions.ReCalc = false;
            startTime = latestRatingsProduced;

            replayPlayerRatingAppendId = await context.RepPlayerRatings
                .OrderByDescending(o => o.RepPlayerRatingId)
                .Select(s => s.RepPlayerRatingId)
                .FirstOrDefaultAsync();
            replayRatingAppendId = await context.ReplayRatings
                .OrderByDescending(o => o.ReplayRatingId)
                .Select(s => s.ReplayRatingId)
                .FirstOrDefaultAsync();
        }

        MmrService.CalcRatingRequest request = new()
        {
            CmdrMmrDic = new(),
            MmrIdRatings = await GetMmrIdRatings(mmrOptions.ReCalc ? null : startTime),
            MmrOptions = mmrOptions,
            ReplayRatingAppendId = replayRatingAppendId,
            ReplayPlayerRatingAppendId = replayPlayerRatingAppendId,
            StartTime = startTime,
            EndTime = endTime,
        };

        return request;
    }

    private async Task<Dictionary<RatingType, Dictionary<int, CalcRating>>> GetMmrIdRatings(DateTime? fromDate)
    {
        if (fromDate == null)
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
            return await GetCalcRatings(fromDate.Value);
        }
    }

    private async Task<Dictionary<RatingType, Dictionary<int, CalcRating>>> GetCalcRatings(DateTime fromDate)
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

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        var playersRatingsQuery = from r in context.ArcadeReplays
                                  from rp in r.ArcadeReplayPlayers
                                  from pr in rp.ArcadePlayer.ArcadePlayerRatings
                                  where r.CreatedAt >= fromDate
                                   && r.ArcadeReplayRating == null
                                  select pr;
        var playerRatings = await playersRatingsQuery
            .Distinct()
            .Select(s => new
            {
                s.ArcadePlayerId,
                s.Games,
                s.Wins,
                s.MmrOverTime,
                s.Deviation,
                s.RatingType,
                s.Rating
            })
            .ToListAsync();

        foreach (var pr in playerRatings)
        {
            calcRatings[pr.RatingType][pr.ArcadePlayerId] = new()
            {
                PlayerId = pr.ArcadePlayerId,
                Games = pr.Games,
                Wins = pr.Wins,
                Mmr = pr.Rating,
                MmrOverTime = RatingsService.GetTimeRatings(pr.MmrOverTime),
                Deviation = pr.Deviation
            };
        }

        return calcRatings;
    }

    private async Task SetPlayerRatingsPos()
    {
        using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "CALL SetArcadePlayerRatingPos();";
        command.CommandTimeout = 500;
        await command.ExecuteNonQueryAsync();
    }

    private async Task SetRatingChange()
    {
        try
        {
            using var connection = new MySqlConnection(dbImportOptions.Value.ImportConnectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "CALL SetArcadeRatingChange();";
            command.CommandTimeout = 500;
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            logger.LogError($"failed SetRatingChange: {ex.Message}");
        }
    }
}
