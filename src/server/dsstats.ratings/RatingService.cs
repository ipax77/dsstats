
using dsstats.db;
using dsstats.db.Extensions;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace dsstats.ratings;

public partial class RatingService(IServiceScopeFactory scopeFactory, IOptions<ImportOptions> importOptions, ILogger<RatingService> logger) : IRatingService
{
    private readonly SemaphoreSlim ratingLock = new(1, 1);
    public async Task CreateRatings()
    {
        await ratingLock.WaitAsync();
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            await ClearRatings();
            await BatchImportCombinedReplays();

            var arcadeMatches = await GetArcadeMatches();
            int processed = 0;

            // DateTime fromTime = DateTime.MinValue;
            DateTime fromTime = new DateTime(2021, 2, 1);
            // DateTime fromTime = new DateTime(2025, 10, 1);
            int take = 50_000;

            var playerRatingsStore = new PlayerRatingsStore();
            List<ReplayRating> replayRatingsToInsert = [];
            List<ReplayRating> arcadeReplayRatingsToInsert = [];
            HashSet<int> skipDsstatsReplayIds = [];
            HashSet<int> skipArcadeReplayIds = [];
            TimeSpan elapsed = TimeSpan.Zero;
            int replayRatingId = 0;
            int replayPlayerRatingId = 0;
            int arcadeReplayRatingId = 0;

            while (true)
            {
                var replays = await GetCombinedReplayCalcDtos(fromTime, skipDsstatsReplayIds, skipArcadeReplayIds, take);
                if (replays.Count == 0)
                {
                    break;
                }
                processed += replays.Count;
                foreach (var replay in replays)
                {
                    bool applyChanges = !(replay.IsArcade && arcadeMatches.Contains(replay.ReplayId));

                    var ratingTypes = GetRatingTypes(replay);
                    foreach (var ratingType in ratingTypes)
                    {
                        if (!applyChanges && ratingType != RatingType.All)
                        {
                            continue;
                        }
                        ReplayRating? replayRating = ProcessReplay(replay, ratingType, playerRatingsStore, applyChanges);
                        if (replayRating != null)
                        {
                            if (replay.IsArcade && ratingType == RatingType.All)
                            {
                                arcadeReplayRatingsToInsert.Add(replayRating);
                            }
                            else if (!replay.IsArcade)
                            {
                                replayRatingsToInsert.Add(replayRating);
                            }
                        }
                    }


                    fromTime = replay.Gametime;
                }
                (replayRatingId, replayPlayerRatingId) =
                    await SaveCsvStepResult(replayRatingsToInsert, replayRatingId, replayPlayerRatingId);
                arcadeReplayRatingId = await SaveCsvArcadeReplayRatings(arcadeReplayRatingsToInsert, arcadeReplayRatingId);

                var skipReplays = replays
                    .Where(x => x.Gametime == fromTime)
                    .Select(s => new { s.ReplayId, s.IsArcade })
                    .ToList();
                skipArcadeReplayIds = skipReplays.Where(x => x.IsArcade).Select(s => s.ReplayId).ToHashSet();
                skipDsstatsReplayIds = skipReplays.Where(x => !x.IsArcade).Select(s => s.ReplayId).ToHashSet();

                replayRatingsToInsert.Clear();
                arcadeReplayRatingsToInsert.Clear();
                logger.LogInformation("{date} - {count}, {time}sec", fromTime.ToShortDateString(),
                    processed, elapsed == TimeSpan.Zero ? Math.Round(sw.Elapsed.TotalSeconds, 2)
                    : Math.Round(sw.Elapsed.TotalSeconds - elapsed.TotalSeconds, 2));
                elapsed = sw.Elapsed;
            }

            var connectionString = importOptions.Value.ConnectionString;
            await SaveCsvPlayerRatings(playerRatingsStore.GetAll(), connectionString);
            await Csv2MySQL(Path.Combine(csvDir, "replayRatings.csv"), "ReplayRatings_tmp", connectionString);
            await Csv2MySQL(Path.Combine(csvDir, "replayPlayerRatings.csv"), "ReplayPlayerRatings_tmp", connectionString);
            await Csv2MySQL(Path.Combine(csvDir, "arcadeReplayRatings.csv"), "ArcadeReplayRatings_tmp", connectionString);

            await SwapStagingTables();
        }
        catch (Exception ex)
        {
            logger.LogError("failed creating rating: {error}", ex.Message);
        }
        finally
        {
            ratingLock.Release();
        }
        sw.Stop();
        logger.LogWarning("Ratings produced in {time} min", Math.Round(sw.Elapsed.TotalMinutes, 2));
    }

    private async Task ClearRatings()
    {
        using var scope = scopeFactory.CreateAsyncScope();
        using var context = scope.ServiceProvider.GetRequiredService<StagingDsstatsContext>();


        await context.Database.ExecuteSqlRawAsync(@"
            DROP TABLE IF EXISTS PlayerRatings_tmp;
            CREATE TABLE PlayerRatings_tmp LIKE PlayerRatings;

            DROP TABLE IF EXISTS ReplayRatings_tmp;
            CREATE TABLE ReplayRatings_tmp LIKE ReplayRatings;

            DROP TABLE IF EXISTS ReplayPlayerRatings_tmp;
            CREATE TABLE ReplayPlayerRatings_tmp LIKE ReplayPlayerRatings;

            DROP TABLE IF EXISTS ArcadeReplayRatings_tmp;
            CREATE TABLE ArcadeReplayRatings_tmp LIKE ArcadeReplayRatings;
        ");
    }

    private async Task SwapStagingTables()
    {
        using var scope = scopeFactory.CreateAsyncScope();
        using var context = scope.ServiceProvider.GetRequiredService<StagingDsstatsContext>();
        var sql = @"
            RENAME TABLE 
                ReplayPlayerRatings TO ReplayPlayerRatings_old,
                ReplayRatings TO ReplayRatings_old,
                PlayerRatings TO PlayerRatings_old,
                ArcadeReplayRatings TO ArcadeReplayRatings_old,
                ReplayPlayerRatings_tmp TO ReplayPlayerRatings,
                ReplayRatings_tmp TO ReplayRatings,
                PlayerRatings_tmp TO PlayerRatings,
                ArcadeReplayRatings_tmp TO ArcadeReplayRatings;

            DROP TABLE ReplayPlayerRatings_old;
            DROP TABLE ReplayRatings_old;
            DROP TABLE PlayerRatings_old;
            DROP TABLE ArcadeReplayRatings_old;
        ";
        await context.Database.ExecuteSqlRawAsync(sql);
    }

    private async Task SavePlayerRatings(Dictionary<int, Dictionary<RatingType, PlayerRatingCalcDto>> playerRatingsDict)
    {
        using var scope = scopeFactory.CreateAsyncScope();
        using var context = scope.ServiceProvider.GetRequiredService<StagingDsstatsContext>();
        List<PlayerRating> playerRatings = [];
        foreach (var kvp in playerRatingsDict)
        {
            int playerId = kvp.Key;
            foreach (var ent in kvp.Value)
            {
                var ratingType = ent.Key;
                playerRatings.Add(new()
                {
                    PlayerId = playerId,
                    RatingType = ratingType,
                    Games = ent.Value.Games,
                    Wins = ent.Value.Wins,
                    Mvps = ent.Value.Mvps,
                    Rating = ent.Value.Rating,
                    Confidence = ent.Value.Confidence,
                    Consistency = ent.Value.Consistency,
                });
            }
        }
        var groups = playerRatings.GroupBy(g => g.RatingType);
        foreach (var group in groups)
        {
            int pos = 1;
            foreach (var rating in group.OrderByDescending(o => o.Rating))
            {
                rating.Position = pos++;
            }
        }

        await context.AddRangeAsync(playerRatings);
        await context.SaveChangesAsync();
    }

    private async Task SaveStepResult(List<ReplayRating> replayRatings)
    {
        if (replayRatings.Count == 0)
        {
            return;
        }
        using var scope = scopeFactory.CreateAsyncScope();
        using var context = scope.ServiceProvider.GetRequiredService<StagingDsstatsContext>();

        await context.Database.OpenConnectionAsync();
        await context.Database.ExecuteSqlRawAsync("SET unique_checks=0;");

        await context.AddRangeAsync(replayRatings);
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlRawAsync("SET unique_checks=1;");
        await context.Database.CloseConnectionAsync();
    }

    public static HashSet<RatingType> GetRatingTypes(ReplayCalcDto replay)
    {
        List<RatingType> ratingTypes = [RatingType.All];
        if (replay.PlayerCount == 6)
        {
            if (replay.GameMode == GameMode.Standard)
            {
                if (replay.TE)
                    ratingTypes.Add(RatingType.StandardTE);
                else
                    ratingTypes.Add(RatingType.Standard);
            }
            else if (replay.GameMode == GameMode.Commanders)
            {
                if (replay.TE)
                    ratingTypes.Add(RatingType.CommandersTE);
                else
                    ratingTypes.Add(RatingType.Commanders);
            }
        }
        return ratingTypes.ToHashSet();
    }

    private async Task<List<ReplayCalcDto>> GetContinueReplayCalcDtos(DateTime fromTime, int take)
    {
        using var scope = scopeFactory.CreateAsyncScope();
        using var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        bool noFromTime = fromTime == DateTime.MinValue;

        return await context.Replays
            .Include(i => i.Players)
            .Include(i => i.Ratings)
            .Where(x => noFromTime || (x.Gametime >= fromTime))
            .Where(x => x.Ratings.Count == 0)
            .ToReplayCalcDtos()
            .Take(take)
            .ToListAsync();
    }

    private async Task<List<ReplayCalcDto>> GetCombinedReplayCalcDtos(DateTime fromTime, HashSet<int> skipDsstatsReplayIds, HashSet<int> skipArcadeReplayIds, int take)
    {
        using var scope = scopeFactory.CreateAsyncScope();
        using var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();

        var query = from r in context.CombinedReplays
                    where r.Gametime >= fromTime
                    orderby r.Gametime
                    select new
                    {
                        Replay = r,
                        ReplayPlayers = context.ReplayPlayers
                            .Where(p => p.ReplayId == r.ReplayId)
                            .Select(p => new PlayerCalcDto
                            {
                                ReplayPlayerId = p.ReplayPlayerId,
                                IsLeaver = p.Duration < r.Duration - 90,
                                IsMvp = p.IsMvp,
                                Team = p.TeamId,
                                Race = p.Race,
                                PlayerId = p.PlayerId
                            }).ToList(),
                        ArcadePlayers = context.ArcadeReplayPlayers
                            .Where(p => p.ArcadeReplayId == r.ArcadeReplayId)
                            .OrderBy(p => p.SlotNumber)
                            .Select(p => new PlayerCalcDto
                            {
                                ReplayPlayerId = p.ArcadeReplayPlayerId,
                                IsLeaver = false,
                                IsMvp = false,
                                Team = p.Team,
                                Race = Commander.None,
                                PlayerId = p.PlayerId
                            }).ToList()
                    };

        var raw = await query.Take(take).ToListAsync();

        raw = raw.Where(x => (x.Replay.ReplayId != null && !skipDsstatsReplayIds.Contains(x.Replay.ReplayId.Value))
            || (x.Replay.ArcadeReplayId != null && !skipArcadeReplayIds.Contains(x.Replay.ArcadeReplayId.Value)))
            .ToList();

        // merge players into ReplayCalcDto
        var result = raw.Select(x => new ReplayCalcDto
        {
            ReplayId = x.Replay.ReplayId ?? x.Replay.ArcadeReplayId!.Value,
            Gametime = x.Replay.Gametime,
            GameMode = x.Replay.GameMode,
            PlayerCount = x.Replay.PlayerCount,
            WinnerTeam = x.Replay.WinnerTeam,
            TE = x.Replay.TE,
            IsArcade = x.Replay.ReplayId == null,
            Players = (x.Replay.ReplayId != null ? x.ReplayPlayers : x.ArcadePlayers).ToList()
        }).ToList();

        return result;
    }

    private static async Task<List<ReplayCalcDto>> GetReplayCalcDtos(List<int> replayIds, DsstatsContext context)
    {
        return await context.Replays
            .Include(i => i.Players)
            .Where(x => replayIds.Contains(x.ReplayId))
            .ToReplayCalcDtos()
            .ToListAsync();
    }

    private async Task<HashSet<int>> GetArcadeMatches()
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        await MatchNewDsstatsReplays();
        var data = await context.ReplayArcadeMatches
            .Select(s => s.ArcadeReplayId)
            .ToListAsync();
        return data.ToHashSet();
    }

    private async Task BatchImportCombinedReplays()
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        context.Database.SetCommandTimeout(TimeSpan.FromMinutes(20));
        await context.Database.ExecuteSqlRawAsync("CALL BatchImportCombinedReplays();");
    }
}



