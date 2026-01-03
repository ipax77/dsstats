using dsstats.db;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace dsstats.dbServices;

public partial class PlayerService
{
    public async Task<RatingDetails> GetRatingDetails(
        PlayerStatsRequest request,
        CancellationToken token = default)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var importService = scope.ServiceProvider.GetRequiredService<IImportService>();

        Stopwatch sw = Stopwatch.StartNew();
        int playerId = importService.GetPlayerId(request.ToonId);
        RatingType ratingType = request.RatingType;

        var replayIds = (await context.ReplayPlayers
            .Where(p => p.PlayerId == playerId)
            .Select(p => p.ReplayId)
            .ToListAsync(token)).ToHashSet();

        var replays = await GetDsstatsReplays(context, playerId, replayIds, token);

        var ratings = await GetDsstatsRatings(context, ratingType, playerId, replayIds, token);

        var ratingDetails = GenerateStats3(replays, ratings, playerId);

        foreach (var stat in ratingDetails.TeammateStats)
        {
            var name = importService.GetPlayerName(stat.Player.ToonId);
            stat.Player.Name = name;
            stat.AvgGain = MathF.Round(stat.AvgGain / (float)stat.Count, 2);
        }

        foreach (var stat in ratingDetails.OpponentStats)
        {
            var name = importService.GetPlayerName(stat.Player.ToonId);
            stat.Player.Name = name;
            stat.AvgGain = stat.Count == 0 ? 0 : MathF.Round(stat.AvgGain / (float)stat.Count, 2);
        }

        foreach (var ent in ratingDetails.AvgGainResponses)
        {
            foreach (var stat in ent.AvgGains)
            {
                stat.AvgGain = stat.Count == 0 ? 0 : Math.Round(stat.AvgGain / stat.Count, 2);
            }
        }

        ratingDetails.PercentileMaxRank = await GetPercentileMaxRank(ratingType);

        sw.Stop();
        logger.LogWarning("GetPlayerDetailsNg {PlayerId} {RatingType} {Count} replays in {ElapsedMilliseconds} ms",
            playerId, ratingType, replays.Count, sw.ElapsedMilliseconds);

        return ratingDetails;
    }

    private static async Task<List<ReplayPlayerStatsData>> GetDsstatsReplays(DsstatsContext context, int playerId, HashSet<int> replayIds, CancellationToken token)
    {
        return await context.Replays
            .AsNoTracking()
            .Where(r => replayIds.Contains(r.ReplayId))
            .Select(s => new ReplayPlayerStatsData
            {
                ReplayId = s.ReplayId,
                ReplayHash = s.ReplayHash,
                Gametime = s.Gametime,
                GameMode = s.GameMode,
                Duration = s.Duration,
                WinnerTeam = s.WinnerTeam,
                TE = s.TE,
                IsArcade = false,
                Players = s.Players
                    .Select(p => new ReplayPlayerPlayerStatsData
                    {
                        GamePos = p.GamePos,
                        Race = p.Race,
                        PlayerId = p.PlayerId,
                        ToonId = p.Player!.ToonId,
                        TeamId = p.TeamId,
                    }).ToList(),
            })
            .OrderBy(o => o.Gametime)
            .ToListAsync(token);
    }

    private static async Task<Dictionary<int, ReplayRatingPlayerStatsData>> GetDsstatsRatings(DsstatsContext context, RatingType ratingType, int playerId, HashSet<int> replayIds, CancellationToken token)
    {
        return await context.ReplayRatings
            .AsNoTracking()
            .Where(r => replayIds.Contains(r.ReplayId) &&
                        r.RatingType == ratingType)
            .Select(rr => new ReplayRatingPlayerStatsData
            {
                ReplayId = rr.ReplayId,
                LeaverType = rr.LeaverType,
                ExpectedWinProbability = rr.ExpectedWinProbability,
                AvgRating = rr.AvgRating,
                PlayerRatings = rr.ReplayPlayerRatings
                    .Select(pr => new ReplayPlayerRatingPlayerStatsData
                    {
                        PlayerId = pr.PlayerId,
                        RatingBefore = pr.RatingBefore,
                        RatingDelta = pr.RatingDelta,
                        Games = pr.Games
                    })
                    .ToList()
            })
            .ToDictionaryAsync(k => k.ReplayId, v => v, token);
    }

    private static async Task<List<ReplayPlayerStatsData>> GetArcadeReplays(DsstatsContext context, int playerId, HashSet<int> replayIds, CancellationToken token)
    {
        return await context.ArcadeReplays
            .AsNoTracking()
            .Where(r => replayIds.Contains(r.ArcadeReplayId))
            .Select(s => new ReplayPlayerStatsData
            {
                ReplayId = s.ArcadeReplayId,
                ReplayHash = string.Empty,
                Gametime = s.CreatedAt,
                GameMode = s.GameMode,
                Duration = s.Duration,
                WinnerTeam = s.WinnerTeam,
                TE = false,
                IsArcade = true,
                Players = s.Players
                    .Select(p => new ReplayPlayerPlayerStatsData
                    {
                        GamePos = p.SlotNumber,
                        Race = Commander.None,
                        PlayerId = p.PlayerId,
                        ToonId = p.Player!.ToonId,
                        TeamId = p.Team,
                    }).ToList(),
            })
            .OrderBy(o => o.Gametime)
            .ToListAsync(token);
    }

    private static async Task<Dictionary<int, ReplayRatingPlayerStatsData>> GetArcadeRatings(DsstatsContext context,
                                                                                             int playerId,
                                                                                             HashSet<int> replayIds,
                                                                                             List<ReplayPlayerStatsData> replays,
                                                                                             CancellationToken token)
    {
        var replayRatings = await context.ArcadeReplayRatings
            .AsNoTracking()
            .Where(r => replayIds.Contains(r.ArcadeReplayId))
            .ToListAsync(token);

        var result = new Dictionary<int, ReplayRatingPlayerStatsData>();
        foreach (var r in replayRatings)
        {
            var replay = replays.FirstOrDefault(rp => rp.ReplayId == r.ArcadeReplayId);
            if (replay is null)
            {
                continue;
            }
            var ratingData = new ReplayRatingPlayerStatsData
            {
                ReplayId = r.ArcadeReplayId,
                LeaverType = LeaverType.None,
                ExpectedWinProbability = r.ExpectedWinProbability,
                AvgRating = r.AvgRating,
            };
            foreach (var (p, index) in replay.Players.OrderBy(o => o.GamePos).Select((p, index) => (p, index)))
            {
                var playerRating = new ReplayPlayerRatingPlayerStatsData
                {
                    PlayerId = p.PlayerId,
                    RatingBefore = r.PlayerRatings[index],
                    RatingDelta = r.PlayerRatingDeltas[index],
                    Games = 0
                };
                ratingData.PlayerRatings.Add(playerRating);
            }
            result[r.ArcadeReplayId] = ratingData;
        }
        return result;
    }

    private static RatingDetails GenerateStats(List<ReplayPlayerStatsData> replays, Dictionary<int, ReplayRatingPlayerStatsData> ratings, int playerId)
    {
        Dictionary<GameMode, int> gameModeCounts = [];
        Dictionary<Commander, int> commanderCounts = [];
        Dictionary<Commander, PlayerCmdrAvgGain> commanderAvgGains = [];
        List<ReplayListDto> lastReplays = [];
        DateTime past90Days = DateTime.Today.AddDays(-90);
        Dictionary<ToonIdRec, OtherPlayerStats> teammates = [];
        Dictionary<ToonIdRec, OtherPlayerStats> opponents = [];
        Dictionary<int, PosPlayerStats> posStats = [];
        List<(DateTime Date, float Rating, int Games)> ratingHistory = [];
        int ratedGames = 0;
        long teammateRatings = 0;
        long opponentsRatings = 0;
        int teammateRatingsCount = 0;
        int opponentsRatingsCount = 0;
        bool onWinstreak = false;
        StreakPlayerStats currentWinStreak = new();
        StreakPlayerStats currentLoseStreak = new();
        StreakPlayerStats bestWinStreak = new();
        StreakPlayerStats bestLoseStreak = new();
        TopRating topRating = new() { Rating = 0, DateAchieved = DateTime.MinValue };

        for (int i = 0; i < replays.Count; i++)
        {
            var replay = replays[i];
            ratings.TryGetValue(replay.ReplayId, out var rating);

            if (gameModeCounts.ContainsKey(replay.GameMode))
            {
                gameModeCounts[replay.GameMode]++;
            }
            else
            {
                gameModeCounts[replay.GameMode] = 1;
            }
            ReplayListDto? replayDto = null;
            if (i > replays.Count - 12)
            {
                replayDto = new()
                {
                    ReplayHash = replay.ReplayHash,
                    Gametime = replay.Gametime,
                    GameMode = replay.GameMode,
                    Duration = replay.Duration,
                    WinnerTeam = replay.WinnerTeam,
                    CommandersTeam1 = replay.Players
                        .Where(p => p.TeamId == 1)
                        .OrderBy(p => p.GamePos)
                        .Select(s => s.Race).ToList(),
                    CommandersTeam2 = replay.Players
                        .Where(p => p.TeamId == 2)
                        .OrderBy(p => p.GamePos)
                        .Select(s => s.Race).ToList(),
                    Exp2Win = rating?.ExpectedWinProbability,
                    AvgRating = rating?.AvgRating,
                    LeaverType = rating?.LeaverType ?? LeaverType.None,
                };
                lastReplays.Add(replayDto);
            }
            var interestPlayer = replay.Players.FirstOrDefault(p => p.PlayerId == playerId);
            var interestPlayerRating = rating?.PlayerRatings.FirstOrDefault(pr => pr.PlayerId == playerId);

            foreach (var player in replay.Players)
            {
                var playerRating = rating?.PlayerRatings.FirstOrDefault(pr => pr.PlayerId == player.PlayerId);

                if (player.PlayerId == playerId)
                {
                    if (replayDto is not null)
                    {
                        replayDto.PlayerPos = player.GamePos;
                    }

                    if (commanderCounts.ContainsKey(player.Race))
                    {
                        commanderCounts[player.Race]++;
                    }
                    else
                    {
                        commanderCounts[player.Race] = 1;
                    }

                    if (player.GamePos > 0)
                    {
                        if (posStats.TryGetValue(player.GamePos, out var posStat) && posStat is not null)
                        {
                            posStat.Count++;
                            if (player.TeamId == replay.WinnerTeam)
                            {
                                posStat.Wins++;
                            }
                        }
                        else
                        {
                            posStat = new PosPlayerStats
                            {
                                GamePos = player.GamePos,
                                Count = 1,
                                Wins = player.TeamId == replay.WinnerTeam ? 1 : 0,
                            };
                            posStats[player.GamePos] = posStat;
                        }
                    }

                    if (playerRating is not null)
                    {
                        ratedGames++;
                        if (replay.Gametime >= past90Days)
                        {
                            if (commanderAvgGains.TryGetValue(player.Race, out var avgGain) && avgGain is not null)
                            {
                                avgGain.Count++;
                                avgGain.AvgGain += playerRating.RatingDelta;
                                if (player.TeamId == replay.WinnerTeam)
                                {
                                    avgGain.Wins++;
                                }
                            }
                            else
                            {
                                avgGain = new PlayerCmdrAvgGain
                                {
                                    Commander = player.Race,
                                    Count = 1,
                                    AvgGain = playerRating.RatingDelta,
                                    Wins = player.TeamId == replay.WinnerTeam ? 1 : 0,
                                };
                                commanderAvgGains[player.Race] = avgGain;
                            }
                        }
                        ratingHistory.Add((replay.Gametime, (float)(playerRating.RatingBefore + playerRating.RatingDelta), playerRating.Games));

                        if (i == 0)
                        {
                            // Initialize streaks
                            if (player.TeamId == replay.WinnerTeam)
                            {
                                onWinstreak = true;
                                currentWinStreak.Count = 1;
                                currentWinStreak.StartDate = replay.Gametime;
                            }
                            else
                            {
                                onWinstreak = false;
                                currentLoseStreak.Count = 1;
                                currentLoseStreak.StartDate = replay.Gametime;
                            }
                        }
                        else
                        {
                            // Update streaks
                            if (player.TeamId == replay.WinnerTeam)
                            {
                                if (onWinstreak)
                                {
                                    currentWinStreak.Count++;
                                    currentWinStreak.EndDate = replay.Gametime;
                                }
                                else
                                {
                                    // Switch to win streak
                                    if (currentLoseStreak.Count > bestLoseStreak.Count)
                                    {
                                        bestLoseStreak = currentLoseStreak with { EndDate = replay.Gametime };
                                    }
                                    currentWinStreak.Count = 1;
                                    currentWinStreak.StartDate = replay.Gametime;
                                    onWinstreak = true;
                                }
                            }
                            else
                            {
                                if (!onWinstreak)
                                {
                                    currentLoseStreak.Count++;
                                    currentLoseStreak.EndDate = replay.Gametime;
                                }
                                else
                                {
                                    // Switch to lose streak
                                    if (currentWinStreak.Count > bestWinStreak.Count)
                                    {
                                        bestWinStreak = currentWinStreak with { EndDate = replay.Gametime };
                                    }
                                    currentLoseStreak.Count = 1;
                                    currentLoseStreak.StartDate = replay.Gametime;
                                    onWinstreak = false;
                                }
                            }
                        }

                        if (playerRating.RatingBefore + playerRating.RatingDelta > topRating.Rating)
                        {
                            topRating = new TopRating
                            {
                                Rating = (float)(playerRating.RatingBefore + playerRating.RatingDelta),
                                DateAchieved = replay.Gametime
                            };
                        }
                    }
                    else if (interestPlayer is not null && playerRating is not null)
                    {
                        ToonIdRec toonId = new(player.ToonId.Region, player.ToonId.Realm, player.ToonId.Id);
                        var playerStatsDict = player.TeamId == interestPlayer.TeamId ? teammates : opponents;
                        if (playerStatsDict.TryGetValue(toonId, out var playerStats) && playerStats is not null)
                        {
                            playerStats.Count++;
                            playerStats.AvgGain += (float?)interestPlayerRating?.RatingDelta ?? 0;
                            if (player.TeamId == replay.WinnerTeam)
                            {
                                playerStats.Wins++;
                            }
                        }
                        else
                        {
                            playerStats = new OtherPlayerStats
                            {
                                Player = new PlayerDto
                                {
                                    ToonId = new()
                                    {
                                        Region = player.ToonId.Region,
                                        Realm = player.ToonId.Realm,
                                        Id = player.ToonId.Id
                                    },
                                },
                                Count = 1,
                                AvgGain = (float?)interestPlayerRating?.RatingDelta ?? 0,
                                Wins = player.TeamId == replay.WinnerTeam ? 1 : 0,
                            };
                            playerStatsDict[toonId] = playerStats;
                        }
                        if (player.TeamId == interestPlayer.TeamId)
                        {
                            teammateRatings += Convert.ToInt32(playerRating.RatingBefore);
                            teammateRatingsCount++;
                        }
                        else
                        {
                            opponentsRatings += Convert.ToInt32(playerRating.RatingBefore);
                            opponentsRatingsCount++;
                        }
                    }
                }
            }
        }
        RatingDetails details = new()
        {
            GameModes = gameModeCounts
                .Select(kvp => new GameModeCount
                {
                    GameMode = kvp.Key,
                    Count = kvp.Value
                })
                .ToList(),
            Commanders = commanderCounts
                .Select(kvp => new CommanderCount
                {
                    Commander = kvp.Key,
                    Count = kvp.Value
                })
                .ToList(),
            Ratings = ratingHistory
                .OrderBy(x => x.Date)
                .GroupBy(x => GetIso8601Week(x.Date))
                .Select(g => new RatingAtDateTime
                {
                    Year = g.Key.Year,
                    Week = g.Key.Week,
                    Games = g.Max(x => x.Games),
                    Rating = g.Last().Rating
                })
                .ToList(),
            Replays = lastReplays.OrderByDescending(o => o.Gametime).ToList(),
            AvgGainResponses = [new CmdrAvgGainResponse()
            {
                TimePeriod = TimePeriod.Last90Days,
                AvgGains = commanderAvgGains.Values.ToList(),
            }],
            TeammateStats = teammates.Values.Where(x => x.Count > 10).ToList(),
            OpponentStats = opponents.Values.Where(x => x.Count > 10).ToList(),
            PosStats = posStats.Values.ToList(),
            AvgTeammateRating = teammateRatingsCount == 0 ? 0 : (int)(teammateRatings / teammateRatingsCount),
            AvgOpponentRating = opponentsRatingsCount == 0 ? 0 : (int)(opponentsRatings / opponentsRatingsCount),
            LongestWinStreak = bestWinStreak.Count >= currentWinStreak.Count ? bestWinStreak : currentWinStreak,
            LongestLoseStreak = bestLoseStreak.Count >= currentLoseStreak.Count ? bestLoseStreak : currentLoseStreak,
            TopRating = topRating,
        };

        return details;
    }

    private static (int Year, int Week) GetIso8601Week(DateTime date)
    {
        var calendar = System.Globalization.CultureInfo.InvariantCulture.Calendar;
        var week = calendar.GetWeekOfYear(
            date,
            System.Globalization.CalendarWeekRule.FirstFourDayWeek,
            DayOfWeek.Monday);

        int year = date.Year;

        // Handle week 53 spilling into next year
        if (week == 53 && date.Month == 1)
            year--;

        // Handle week 1 spilling into previous year
        if (week == 1 && date.Month == 12)
            year++;

        return (year, week);
    }

    private async Task<int> GetPercentileMaxRank(RatingType ratingType)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DsstatsContext>();
        var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
        string cacheKey = $"PercentileRank_{ratingType}";
        if (!memoryCache.TryGetValue(cacheKey, out int maxRank))
        {
            maxRank = await context.PlayerRatings
                .Where(pr => pr.RatingType == ratingType)
                .OrderByDescending(pr => pr.Position)
                .Select(pr => pr.Position)
                .FirstOrDefaultAsync();
            memoryCache.Set(cacheKey, maxRank, TimeSpan.FromHours(20));
        }
        return maxRank;
    }
}

internal sealed class ReplayPlayerStatsData
{
    public string ReplayHash { get; init; } = string.Empty;
    public int ReplayId { get; init; }
    public DateTime Gametime { get; init; }
    public GameMode GameMode { get; init; }
    public int Duration { get; init; }
    public int WinnerTeam { get; init; }
    public bool TE { get; init; }
    public bool IsArcade { get; init; }
    public List<ReplayPlayerPlayerStatsData> Players { get; init; } = [];

}

internal sealed class ReplayRatingPlayerStatsData
{
    public int ReplayId { get; init; }
    public LeaverType LeaverType { get; init; }
    public double ExpectedWinProbability { get; init; }
    public int AvgRating { get; init; }
    public List<ReplayPlayerRatingPlayerStatsData> PlayerRatings { get; init; } = [];
}

internal sealed class ReplayPlayerPlayerStatsData
{
    public int GamePos { get; init; }
    public Commander Race { get; init; }
    public int PlayerId { get; init; }
    public ToonId ToonId { get; init; } = new();
    public int TeamId { get; init; }
}

internal sealed class ReplayPlayerRatingPlayerStatsData
{
    public int PlayerId { get; init; }
    public double RatingBefore { get; init; }
    public double RatingDelta { get; init; }
    public int Games { get; init; }
}