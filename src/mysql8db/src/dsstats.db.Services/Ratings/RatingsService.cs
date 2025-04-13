
using System.Diagnostics;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using dsstats.shared;
using dsstats.shared8;

namespace dsstats.db.Services.Ratings;

public sealed partial class RatingsService(DsstatsContext context, IOptions<DbImportOptions8> importOptions, ILogger<RatingsService> logger)
{
    private readonly bool DEBUG = false;
    private readonly SemaphoreSlim ratingsSS = new(1, 1);

    public async Task ContinueCalculateRatings()
    {
        await ratingsSS.WaitAsync();
        try
        {
            logger.LogInformation("Start continue calculating.");
            Stopwatch sw = Stopwatch.StartNew();

            DateTime fromDate = DateTime.UtcNow.AddHours(-2);
            int skip = 0;
            int take = 5000;
            int count = 0;

            int replayPlayerRatingsId = await context.ReplayPlayerRatings
                .OrderByDescending(o => o.ReplayPlayerRatingId)
                .Select(s => s.ReplayPlayerRatingId)
                .FirstOrDefaultAsync();
            int replayRatingsId = await context.ReplayRatings
                .OrderByDescending(o => o.ReplayRatingId)
                .Select(s => s.ReplayRatingId)
                .FirstOrDefaultAsync();

            var ratingStore = new PlayerRatingStore([]);

            while (true)
            {
                var replays = await context.Replays
                    .Include(i => i.ReplayRatings)
                    .Where(x => x.PlayerCount == 6 && x.Duration >= 300 && x.WinnerTeam > 0)
                    .Where(x => x.ReplayRatings.Count == 0)
                    .Where(x => x.GameTime >= fromDate)
                    .OrderBy(r => r.GameTime)
                        .ThenBy(t => t.ReplayId)
                    .Select(s => new ReplayCalcDto()
                    {
                        ReplayId = s.ReplayId,
                        GameTime = s.GameTime,
                        GameMode = s.GameMode,
                        WinnerTeam = s.WinnerTeam,
                        Duration = s.Duration,
                        IsTE = s.IsTE,
                        ReplayPlayers = s.ReplayPlayers.Select(t => new ReplayPlayerCalcDto()
                        {
                            ReplayPlayerId = t.ReplayPlayerId,
                            GamePos = t.GamePos,
                            PlayerResult = t.PlayerResult,
                            IsLeaver = t.Duration < s.Duration - 90,
                            IsMvp = t.Kills == s.Maxkillsum,
                            Team = t.Team,
                            Race = t.Race,
                            PlayerId = t.PlayerId,
                            IsUploader = t.IsUploader,
                        }).ToList()
                    })
                    .Skip(skip)
                    .Take(take)
                    .AsSplitQuery()
                    .ToListAsync();

                if (replays.Count == 0) break;

                await ratingStore.LoadRatings(replays, context);

                count += replays.Count;

                List<ReplayPlayerRating> newReplayPlayerRatings = [];
                List<ReplayRating> newReplayRatings = [];

                Dictionary<RatingKey, CalcRating> ratings = [];

                foreach (var replay in replays)
                {
                    if (replay.IsArcade)
                    {
                        CorrectPlayerResults(replay);
                    }

                    var result = Ratings.ProcessReplay(replay, ratingStore);

                    if (!replay.IsArcade && result is not null)
                    {
                        newReplayPlayerRatings.AddRange(result.ReplayPlayerRatings);
                        newReplayRatings.AddRange(result.ReplayRatings);
                    }
                }

                await SaveStepResult(newReplayPlayerRatings, replayPlayerRatingsId);
                replayPlayerRatingsId += newReplayPlayerRatings.Count;
                newReplayPlayerRatings.Clear();

                await SaveStepResult(newReplayRatings, replayRatingsId);
                replayRatingsId += newReplayRatings.Count;
                newReplayRatings.Clear();

                skip += take;
            }

            await Csv2Mysql(GetFileName(RatingCalcType.Combo, nameof(DsstatsContext.ReplayRatings)),
             nameof(DsstatsContext.ReplayRatings));
            await Csv2Mysql(GetFileName(RatingCalcType.Combo, nameof(DsstatsContext.ReplayPlayerRatings)),
             nameof(DsstatsContext.ReplayPlayerRatings));

            await ContinuePlayerRatings(ratingStore.GetPlayerRatingsDict());

            sw.Stop();
            logger.LogWarning("Ratings calculated in {time}min", Math.Round(sw.Elapsed.TotalMinutes, 2));
        }
        catch (Exception ex)
        {
            logger.LogError("Failed continue calculating: {error}", ex.Message);
        }
        finally
        {
            ratingsSS.Release();
        }
    }

    public async Task CalculateRatings()
    {
        await ratingsSS.WaitAsync();
        try
        {
            logger.LogInformation("Start calculating.");
            Stopwatch sw = Stopwatch.StartNew();

            await DeletePreRatings();

            int skip = 0;
            int take = 5000;
            int count = 0;
            var fromTime = new DateTime(2021, 2, 1);

            int replayPlayerRatingsId = 0;
            int replayRatingsId = 0;

            var ratingStore = new PlayerRatingStore([]);

            while (true)
            {
                long beforeMemory = DEBUG ? GC.GetTotalMemory(false) : 0;

                var replays = await GetReplays(skip, take, fromTime);

                if (replays.Count == 0) break;

                count += replays.Count;

                List<ReplayPlayerRating> newReplayPlayerRatings = [];
                List<ReplayRating> newReplayRatings = [];

                Dictionary<RatingKey, CalcRating> ratings = [];

                foreach (var replay in replays)
                {
                    if (replay.IsArcade)
                    {
                        CorrectPlayerResults(replay);
                    }

                    var result = Ratings.ProcessReplay(replay, ratingStore);

                    if (!replay.IsArcade && result is not null)
                    {
                        newReplayPlayerRatings.AddRange(result.ReplayPlayerRatings);
                        newReplayRatings.AddRange(result.ReplayRatings);
                    }
                }
                long afterMemory = DEBUG ? GC.GetTotalMemory(false) : 0;
                long memoryUsed = afterMemory - beforeMemory;
                logger.LogInformation("Processed {count} replays. Memory used: {memoryUsed} MB",
                     count, memoryUsed / 1024 / 1024);

                await SaveStepResult(newReplayPlayerRatings, replayPlayerRatingsId);
                replayPlayerRatingsId += newReplayPlayerRatings.Count;
                newReplayPlayerRatings.Clear();

                await SaveStepResult(newReplayRatings, replayRatingsId);
                replayRatingsId += newReplayRatings.Count;
                newReplayRatings.Clear();

                skip += take;
            }

            await Csv2Mysql(GetFileName(RatingCalcType.Combo, nameof(DsstatsContext.ReplayRatings)),
             nameof(DsstatsContext.ReplayRatings));
            await Csv2Mysql(GetFileName(RatingCalcType.Combo, nameof(DsstatsContext.ReplayPlayerRatings)),
             nameof(DsstatsContext.ReplayPlayerRatings));

            await SavePlayerRatings(ratingStore);
            await Csv2Mysql(GetFileName(RatingCalcType.Combo, nameof(DsstatsContext.PlayerRatings)),
             nameof(DsstatsContext.PlayerRatings));

            sw.Stop();
            logger.LogWarning("Ratings calculated in {time}min", Math.Round(sw.Elapsed.TotalMinutes, 2));
        }
        catch (Exception ex)
        {
            logger.LogError("Failed calculting: {error}", ex.Message);
        }
        finally
        {
            ratingsSS.Release();
        }
    }

    private async Task<List<ReplayCalcDto>> GetReplays(int skip, int take, DateTime fromTime)
    {
        var dsstatsReplays = await GetDsstatsReplayCalcDtos(skip, take, fromTime);
        var arcadeReplays = await GetArcadeReplayCalcDtos(dsstatsReplays);
        return CombineReplayCalcDtos(dsstatsReplays, arcadeReplays);
    }

    private List<ReplayCalcDto> CombineReplayCalcDtos(List<ReplayCalcDto> dsstatsCalcDtos,
                                    List<ReplayCalcDto> sc2ArcadeCalcDtos)
    {
        return dsstatsCalcDtos
            .Concat(sc2ArcadeCalcDtos)
            .OrderBy(o => o.GameTime)
                .ThenBy(o => o.ReplayId)
            .ToList();
    }

    private async Task<List<ReplayCalcDto>> GetDsstatsReplayCalcDtos(int skip, int take, DateTime fromTime)
    {
        return await context.Replays
                .Where(x => x.PlayerCount == 6 && x.Duration >= 300 && x.WinnerTeam > 0 && x.GameTime >= fromTime)
                .OrderBy(r => r.GameTime)
                    .ThenBy(t => t.ReplayId)
                .Select(s => new ReplayCalcDto()
                {
                    ReplayId = s.ReplayId,
                    GameTime = s.GameTime,
                    GameMode = s.GameMode,
                    WinnerTeam = s.WinnerTeam,
                    Duration = s.Duration,
                    IsTE = s.IsTE,
                    ReplayPlayers = s.ReplayPlayers.Select(t => new ReplayPlayerCalcDto()
                    {
                        ReplayPlayerId = t.ReplayPlayerId,
                        GamePos = t.GamePos,
                        PlayerResult = t.PlayerResult,
                        IsLeaver = t.Duration < s.Duration - 90,
                        IsMvp = t.Kills == s.Maxkillsum,
                        Team = t.Team,
                        Race = t.Race,
                        PlayerId = t.PlayerId,
                        IsUploader = t.IsUploader,
                    }).ToList()
                })
                .Skip(skip)
                .Take(take)
                .AsSplitQuery()
                .ToListAsync();
    }

    private async Task<List<ReplayCalcDto>> GetArcadeReplayCalcDtos(List<ReplayCalcDto> dsstatsCalcDtos)
    {
        if (dsstatsCalcDtos.Count == 0)
        {
            return [];
        }

        var oldestReplayDate = dsstatsCalcDtos.First().GameTime.AddDays(-1);
        var latestReplayDate = dsstatsCalcDtos.Last().GameTime.AddDays(1);

        var query = from r in context.ArcadeReplays
                    join m in context.ReplayArcadeMatches on r.ArcadeReplayId equals m.ArcadeReplayId into grouping
                    from m in grouping.DefaultIfEmpty()
                    orderby r.ArcadeReplayId
                    where r.CreatedAt >= oldestReplayDate
                        && r.CreatedAt <= latestReplayDate
                        && m == null
                    select new ReplayCalcDto()
                    {
                        ReplayId = r.ArcadeReplayId,
                        GameTime = r.CreatedAt,
                        Duration = r.Duration,
                        GameMode = r.GameMode,
                        WinnerTeam = r.WinnerTeam,
                        IsTE = false,
                        IsArcade = true,
                        ReplayPlayers = context.ArcadeReplayPlayers
                                .Where(x => x.ArcadeReplayId == r.ArcadeReplayId)
                                .Select(t => new ReplayPlayerCalcDto()
                                {
                                    ReplayPlayerId = t.PlayerId,
                                    GamePos = t.SlotNumber,
                                    PlayerResult = t.PlayerResult,
                                    Team = t.Team,
                                    PlayerId = t.PlayerId
                                }).ToList()
                    };

        return await query
            .AsSplitQuery()
            .ToListAsync();
    }

    private async Task DeletePreRatings()
    {
        var query = from r in context.Replays
                    from rr in r.ReplayRatings
                    from rp in r.ReplayPlayers
                    from rpr in rp.ReplayPlayerRatings
                    where rr.IsPreRating
                        && rpr.RatingType == rr.RatingType
                    select rpr.ReplayPlayerRatingId;

        var replayPlayerRatingIds = await query.ToListAsync();

        await context.ReplayPlayerRatings
            .Where(x => replayPlayerRatingIds.Contains(x.ReplayPlayerRatingId))
            .ExecuteDeleteAsync();

        await context.ReplayRatings
            .Where(x => x.IsPreRating)
            .ExecuteDeleteAsync();
    }

    public async Task SavePlayerRatings(PlayerRatingStore ratingStore)
    {
        var fileName = GetFileName(RatingCalcType.Combo, nameof(DsstatsContext.PlayerRatings));
        FileMode fileMode = FileMode.Create;

        using var stream = File.Open(fileName, fileMode);
        using var writer = new StreamWriter(stream);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false
        });
        await csv.WriteRecordsAsync(ratingStore.GetPlayerRatings()
            .Select((s, index) => new PlayerRatingCsv(index + 1, s)));
    }

    private async Task SaveStepResult(List<ReplayPlayerRating> replayPlayerRatings, int startId = 0)
    {
        bool append = startId > 0;

        var fileName = GetFileName(RatingCalcType.Combo, nameof(DsstatsContext.ReplayPlayerRatings));
        FileMode fileMode = append ? FileMode.Append : FileMode.Create;

        using var stream = File.Open(fileName, fileMode);
        using var writer = new StreamWriter(stream);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false
        });
        await csv.WriteRecordsAsync(replayPlayerRatings
            .Select((s, index) => new ReplayPlayerRatingCsv(index + startId + 1, s)));
    }

    private async Task SaveStepResult(List<ReplayRating> replayRatings, int startId = 0)
    {
        bool append = startId > 0;

        var fileName = GetFileName(RatingCalcType.Combo, nameof(DsstatsContext.ReplayRatings));
        FileMode fileMode = append ? FileMode.Append : FileMode.Create;

        using var stream = File.Open(fileName, fileMode);
        using var writer = new StreamWriter(stream);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false
        });
        await csv.WriteRecordsAsync(replayRatings
            .Select((s, index) => new ReplayRatingCsv(index + startId + 1, s)));
    }

    private string GetFileName(RatingCalcType calcType, string job)
    {
        var path = Path.Combine(importOptions.Value.MySqlImportDir, $"{calcType}_{job}.csv");
        return path.Replace("\\", "/");
    }

    private async Task ContinuePlayerRatings(Dictionary<RatingNgType, Dictionary<int, PlayerRating>> ratings)
    {
        var playerIds = ratings.Values.SelectMany(s => s.Keys).ToList();

        var playerRatings = await context.PlayerRatings
            .Where(x => playerIds.Contains(x.PlayerId))
            .ToDictionaryAsync(k => new PlayerRatingKey(k.RatingType, k.PlayerId));

        foreach (var ratingType in ratings.Keys)
        {
            foreach (var playerId in ratings[ratingType].Keys)
            {
                var newRating = ratings[ratingType][playerId];
                if (playerRatings.TryGetValue(new(ratingType, playerId), out var plRating))
                {
                    plRating.Games = newRating.Games;
                    plRating.Wins = newRating.Wins;
                    plRating.Mvp = newRating.Mvp;
                    plRating.Rating = newRating.Rating;
                    plRating.Confidence = newRating.Confidence;
                    plRating.Consistency = newRating.Consistency;
                }
                else
                {
                    context.PlayerRatings.Add(newRating);
                }
            }
        }

        await context.SaveChangesAsync();
    }

    private static void CorrectPlayerResults(ReplayCalcDto calcDto)
    {
        foreach (var pl in calcDto.ReplayPlayers)
        {
            pl.PlayerResult = calcDto.WinnerTeam == 0 ? PlayerResult.None :
             pl.Team == calcDto.WinnerTeam ? PlayerResult.Win : PlayerResult.Los;
        }
    }
}

public readonly record struct RatingKey(int Id, RatingNgType RatingType);


public sealed record ReplayCalcDto
{
    public int ReplayId { get; set; }
    public DateTime GameTime { get; init; }
    public GameMode GameMode { get; set; }
    public int WinnerTeam { get; init; }
    public int Duration { get; init; }
    public bool IsTE { get; init; }
    public bool IsArcade { get; init; }
    public List<ReplayPlayerCalcDto> ReplayPlayers { get; init; } = [];

    public List<RatingNgType> GetRatingTypes()
    {
        if (GameMode == GameMode.Tutorial || ReplayPlayers.Count != 6)
        {
            return [];
        }

        List<RatingNgType> ratingNgTypes = [RatingNgType.Global];

        if (IsTE && ReplayPlayers.Count == 6)
        {
            if (GameMode == GameMode.Commanders)
            {
                ratingNgTypes.Add(RatingNgType.CommandersTE);
            }
            else if (GameMode == GameMode.Standard)
            {
                ratingNgTypes.Add(RatingNgType.StandardTE);
            }
        }
        else if (ReplayPlayers.Count == 6)
        {
            if (GameMode == GameMode.Commanders || GameMode == GameMode.CommandersHeroic)
            {
                ratingNgTypes.Add(RatingNgType.Commanders_3v3);
            }
            else if (GameMode == GameMode.Standard)
            {
                ratingNgTypes.Add(RatingNgType.Standard_3v3);
            }
        }
        return ratingNgTypes;
    }

    public LeaverType GetLeaverType()
    {
        int leavers = ReplayPlayers.Count(c => c.IsLeaver);

        if (leavers == 0)
        {
            return LeaverType.None;
        }

        if (leavers == 1)
        {
            return LeaverType.OneLeaver;
        }

        if (leavers > 2)
        {
            return LeaverType.MoreThanTwo;
        }

        var leaverPlayers = ReplayPlayers.Where(x => x.IsLeaver);
        var teamsCount = leaverPlayers.Select(s => s.Team).Distinct().Count();

        if (teamsCount == 1)
        {
            return LeaverType.TwoSameTeam;
        }
        else
        {
            return LeaverType.OneEachTeam;
        }
    }
}

public sealed record ReplayPlayerCalcDto
{
    public int ReplayPlayerId { get; init; }
    public int GamePos { get; init; }
    public PlayerResult PlayerResult { get; set; }
    public bool IsLeaver { get; init; }
    public bool IsMvp { get; init; }
    public int Team { get; init; }
    public Commander Race { get; init; }
    public int PlayerId { get; init; }
    public bool IsUploader { get; set; }
    public PlayerRating PlayerRating { get; set; } = new();
}

public record CalcRating
{
    public int PlayerId { get; set; } = new();
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Mvps { get; set; }
    public double Rating { get; set; }
    public double Consistency { get; set; }
    public double Confidence { get; set; }
    public bool IsUploader { get; set; }
    public Dictionary<Commander, int> CmdrCounts { get; set; } = [];
}

public sealed record ReplayPlayerRatingCsv
{
    public ReplayPlayerRatingCsv(int id, ReplayPlayerRating rating)
    {
        ReplayPlayerRatingId = id;
        RatingType = (int)rating.RatingType;
        GamePos = 0;
        Rating = rating.Rating;
        Change = rating.Change;
        Games = rating.Games;
        Consistency = rating.Consistency;
        Confidence = rating.Confidence;
        ReplayPlayerId = rating.ReplayPlayerId;
    }
    public int ReplayPlayerRatingId { get; init; }
    public int RatingType { get; init; }
    public int GamePos { get; init; }
    public int Rating { get; init; }
    public decimal Change { get; init; }
    public int Games { get; init; }
    public decimal Consistency { get; init; }
    public decimal Confidence { get; init; }
    public int ReplayPlayerId { get; init; }
}

public sealed record ReplayRatingCsv
{
    public ReplayRatingCsv(int id, ReplayRating rating)
    {
        ReplayRatingId = id;
        RatingType = (int)rating.RatingType;
        LeaverType = (int)rating.LeaverType;
        ExpectationToWin = rating.ExpectationToWin;
        AvgRating = rating.AvgRating;
        ReplayId = rating.ReplayId;
        IsPreRating = rating.IsPreRating ? 1 : 0;
    }
    public int ReplayRatingId { get; set; }
    public int RatingType { get; set; }
    public int LeaverType { get; set; }
    public decimal ExpectationToWin { get; set; }
    public int IsPreRating { get; set; }
    public int AvgRating { get; set; }
    public int ReplayId { get; set; }
}

public sealed record PlayerRatingCsv
{
    public PlayerRatingCsv(int id, PlayerRating rating)
    {
        PlayerRatingId = id;
        RatingType = (int)rating.RatingType;
        Games = rating.Games;
        DsstatsGames = rating.DsstatsGames;
        ArcadeGames = rating.ArcadeGames;
        Wins = rating.Wins;
        Mvp = rating.Mvp;
        Rating = rating.Rating;
        Consistency = rating.Consistency;
        Confidence = rating.Confidence;
        Main = (int)rating.Main;
        MainCount = rating.MainCount;
        Pos = rating.Pos;
        PlayerId = rating.PlayerId;
    }
    public int PlayerRatingId { get; set; }
    public int RatingType { get; set; }
    public int Games { get; set; }
    public int DsstatsGames { get; set; }
    public int ArcadeGames { get; set; }
    public int Wins { get; set; }
    public int Mvp { get; set; }
    public double Rating { get; set; } = 1000;
    public double Consistency { get; set; }
    public double Confidence { get; set; }
    public int Main { get; set; }
    public int MainCount { get; set; }
    public int Pos { get; set; }
    public int PlayerId { get; set; }
}

public sealed class PlayerRatingStore(Dictionary<RatingNgType, Dictionary<int, PlayerRating>> ratings)
{
    private readonly Dictionary<RatingNgType, Dictionary<int, PlayerRating>> ratings = ratings;
    private readonly Dictionary<int, Dictionary<Commander, int>> cmdrCounts = [];
    public PlayerRating GetPlayerRating(RatingNgType ratingType, int playerId)
    {
        if (!ratings.TryGetValue(ratingType, out var typeRatings))
        {
            typeRatings = ratings[ratingType] = [];
        }
        if (!typeRatings.TryGetValue(playerId, out var plRating))
        {
            plRating = typeRatings[playerId] = new() { PlayerId = playerId, RatingType = ratingType };
        }
        return plRating;
    }
    public void SetCmdr(int playerId, Commander commander)
    {
        if (!cmdrCounts.TryGetValue(playerId, out var playerCmdrs))
        {
            playerCmdrs = cmdrCounts[playerId] = [];
        }
        if (!playerCmdrs.ContainsKey(commander))
        {
            playerCmdrs[commander] = 1;
        }
        else
        {
            playerCmdrs[commander]++;
        }
    }
    public List<PlayerRating> GetPlayerRatings()
    {
        List<PlayerRating> playerRatings = [];
        foreach (var ratingType in ratings.Keys)
        {
            int pos = 1;
            foreach (var rating in ratings[ratingType].Values
                .OrderByDescending(o => o.Rating))
            {
                rating.Pos = pos++;
                playerRatings.Add(rating);
            }
        }
        return playerRatings;
    }
    public async Task LoadRatings(List<ReplayCalcDto> replays, DsstatsContext context)
    {
        var playerIds = replays.SelectMany(m => m.ReplayPlayers).Select(s => s.PlayerId).ToList();
        var playerRatings = await context.PlayerRatings
            .Where(x => playerIds.Contains(x.PlayerId))
            .AsNoTracking()
            .ToListAsync();

        foreach (var rating in playerRatings)
        {
            if (!ratings.TryGetValue(rating.RatingType, out var typeRatings))
            {
                typeRatings = ratings[rating.RatingType] = [];
            }
            typeRatings[rating.PlayerId] = rating;
        }
    }
    public Dictionary<RatingNgType, Dictionary<int, PlayerRating>> GetPlayerRatingsDict()
    {
        return ratings;
    }
}

internal record struct PlayerRatingKey(RatingNgType RatingType, int PlayerId);