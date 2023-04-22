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
using System.Text;
using System.Text.Json;

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
        int recalcCount = 0;
        
        try
        {
            var request = recalc == false ?
                await GetCalcRatingRequest() :
                new MmrService.CalcRatingRequest()
                {
                    CmdrMmrDic = new(),
                    MmrIdRatings = GetMmrIdRatings(), // todo: continue
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
            recalcCount = request.ReplayDsRDtos.Count;

            await GeneratePlayerRatings(request);

            await SaveRatings(request.MmrIdRatings);
        }
        finally
        {
            sw.Stop();
            logger.LogWarning($"Arcade Ratings produced in {sw.ElapsedMilliseconds} ms");
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
        int take = 100000;

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
        List<ReplayDsRDto> continueReplays = new();

        if (latestRatingsProduced > DateTime.MinValue)
        {
            var importedReplaysQuery = context.ArcadeReplays
                .Where(x => x.Imported >= latestRatingsProduced
                    && x.ArcadeReplayRating == null)
                .Select(s => new { s.Imported, s.CreatedAt });

            var count = await importedReplaysQuery.CountAsync();

            if (count == 0)
            {
                return null;
            }

            var importedReplays = await importedReplaysQuery.ToListAsync();

            var oldestReplay = importedReplays.OrderBy(o => o.CreatedAt).First();
            
            if (oldestReplay.CreatedAt > latestRatingsProduced)
            {
                mmrOptions.ReCalc = false;
                startTime = latestRatingsProduced;
                continueReplays = await GetReplayData(startTime, 0, count);

                replayPlayerRatingAppendId = await context.RepPlayerRatings
                    .OrderByDescending(o => o.RepPlayerRatingId)
                    .Select(s => s.RepPlayerRatingId)
                    .FirstOrDefaultAsync();
                replayRatingAppendId = await context.ReplayRatings
                    .OrderByDescending(o => o.ReplayRatingId)
                    .Select(s => s.ReplayRatingId)
                    .FirstOrDefaultAsync();
            }
        }        

        MmrService.CalcRatingRequest request = new()
        {
            CmdrMmrDic = new(),
            MmrIdRatings = GetMmrIdRatings(), // todo: continue
            MmrOptions = mmrOptions,
            ReplayRatingAppendId = replayRatingAppendId,
            ReplayPlayerRatingAppendId = replayPlayerRatingAppendId,
            StartTime = startTime,
            EndTime = endTime,
        };

        return request;
    }

    private Dictionary<RatingType, Dictionary<int, CalcRating>> GetMmrIdRatings()
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

    public void PrintLadder(string dsrFile)
    {
        List<CalcRating> calcRatings = JsonSerializer.Deserialize<List<CalcRating>>(File.ReadAllText(dsrFile)) ?? new();

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        StringBuilder sb = new StringBuilder();
        foreach (var rating in calcRatings.Take(50))
        {
            string? name = context.ArcadePlayers
                .Where(x => x.ArcadePlayerId == rating.PlayerId)
                .Select(x => x.Name)
                .FirstOrDefault();
            sb.AppendLine($"{name} => Games {rating.Games}, WR: {Math.Round(rating.Wins * 100.0 / rating.Games, 2)} Rating: {Math.Round(rating.Mmr, 2)}");
        }
        logger.LogWarning(sb.ToString());
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
