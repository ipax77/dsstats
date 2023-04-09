using AutoMapper;
using dsstats.mmr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MySqlConnector;
using pax.dsstats.dbng;
using pax.dsstats.shared;
using pax.dsstats.shared.Ratings;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace dsstats.ratings.api.Services;

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

    public async Task ProduceRatings()
    {
        await ratingSs.WaitAsync();

        Stopwatch sw = Stopwatch.StartNew();
        int recalcCount = 0;
        bool recalc = false;
        try
        {
            var request = GetCalcRatingRequest();

            recalc = request.MmrOptions.ReCalc;
            recalcCount = request.ReplayDsRDtos.Count;

            await GeneratePlayerRatings(request);

            await SaveRatings(request.MmrIdRatings);
        }
        finally
        {
            sw.Stop();
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

    private void SaveRatings2Json(Dictionary<RatingType, Dictionary<int, CalcRating>> mmrIdRatings)
    {
        foreach (RatingType ratingType in Enum.GetValues(typeof(RatingType)))
        {
            if (ratingType == RatingType.None || !mmrIdRatings[ratingType].Any())
            {
                continue;
            }

            var values = mmrIdRatings[ratingType].Values.Where(x => x.Games >= 10).ToList();
            values.ForEach(x => x.MmrOverTime.Clear());

            var json = JsonSerializer.Serialize(values.OrderByDescending(o => o.Mmr), new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText($"/data/ds/arcaderating{ratingType}.json", json);
        }
    }

    private async Task GeneratePlayerRatings(MmrService.CalcRatingRequest request)
    {
        var _startTime = request.StartTime;
        var _endTime = request.EndTime;

        while (_startTime < _endTime)
        {
            var chunkEndTime = _startTime.AddMonths(3);

            if (chunkEndTime > _endTime)
            {
                chunkEndTime = _endTime;
            }

            request.ReplayDsRDtos = await GetReplayData(_startTime, chunkEndTime);

            _startTime = _startTime.AddMonths(3);

            if (!request.ReplayDsRDtos.Any())
            {
                continue;
            }

            var calcResult = MmrService.GeneratePlayerRatings(request, dry: true);

            (request.ReplayRatingAppendId, request.ReplayPlayerRatingAppendId) = ArcadeRatingsCsvService
                .CreateOrAppendReplayAndReplayPlayerRatingsCsv(calcResult.replayRatingDtos, calcResult.ReplayRatingAppendId, calcResult.ReplayPlayerRatingAppendId);
        }
    }

    private async Task<List<ReplayDsRDto>> GetReplayData(DateTime startTime, DateTime endTime)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ReplayContext>();

        List<GameMode> gameModes = new() { GameMode.Commanders, GameMode.Standard, GameMode.CommandersHeroic };

        var replays = context.ArcadeReplays
            .Include(i => i.ArcadeReplayPlayers)
                .ThenInclude(i => i.ArcadePlayer)
            .Where(r => r.PlayerCount == 6
                && r.Duration >= 300
                && r.WinnerTeam > 0
                && gameModes.Contains(r.GameMode)
                && r.ArcadeReplayPlayers.All(a => a.ArcadePlayer.ProfileId > 0));

        if (startTime != DateTime.MinValue)
        {
            replays = replays.Where(x => x.CreatedAt > startTime);
        }

        if (endTime != DateTime.MinValue && endTime < DateTime.Today)
        {
            replays = replays.Where(x => x.CreatedAt < endTime);
        }

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

        var dsrReplaysList = await dsrReplays.ToListAsync();
        
        foreach (var replay in dsrReplaysList)
        {
            var rps = replay.ReplayPlayers.Select(s => s with { Duration = replay.Duration }).ToList();
            replay.ReplayPlayers.Clear();
            replay.ReplayPlayers.AddRange(rps);
        }

        return dsrReplaysList;
    }

    private MmrService.CalcRatingRequest GetCalcRatingRequest()
    {
        MmrOptions mmrOptions = new(reCalc: true);

        int replayRatingAppendId = 0;
        int replayPlayerRatingAppendId = 0;

        DateTime startTime = new DateTime(2021, 2, 1);
        DateTime endTime = DateTime.Today.AddDays(2);


        MmrService.CalcRatingRequest request = new()
        {
            CmdrMmrDic = new(),
            MmrIdRatings = GetMmrIdRatings(),
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
        foreach (var  rating in calcRatings.Take(50))
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
        } catch (Exception ex)
        {
            logger.LogError($"failed SetRatingChange: {ex.Message}");
        }
    }
}
