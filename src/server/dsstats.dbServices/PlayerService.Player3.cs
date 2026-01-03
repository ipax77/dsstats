using dsstats.shared;

namespace dsstats.dbServices;

public partial class PlayerService
{
    private static readonly TimePeriod _defaultGainTimePeriod = TimePeriod.Last90Days;

    private static RatingDetails GenerateStats3(
        List<ReplayPlayerStatsData> replays,
        Dictionary<int, ReplayRatingPlayerStatsData> ratings,
        int playerId)
    {
        var state = InitializeState();
        state.Past90Days = DateTime.Today.AddDays(-90);

        int total = replays.Count;
        for (int i = 0; i < total; i++)
        {
            var replay = replays[i];
            ratings.TryGetValue(replay.ReplayId, out var rating);

            var ctx = BuildReplayContext(replay, rating, playerId, i, total);

            ProcessGameMode(state, ctx);
            ProcessLastReplays(state, ctx);
            ProcessSelfPlayer(state, ctx);
            ProcessOtherPlayers(state, ctx);
            ProcessStreaks(state, ctx);
            ProcessTopRating(state, ctx);
        }

        return BuildFinalDetails(state);
    }

    private static StatsState InitializeState() => new();

    private static ReplayContext BuildReplayContext(
        ReplayPlayerStatsData replay,
        ReplayRatingPlayerStatsData? rating,
        int playerId,
        int index,
        int totalCount)
    {
        var selfPlayer = replay.Players.FirstOrDefault(p => p.PlayerId == playerId);

        var dict = rating?.PlayerRatings?
            .GroupBy(pr => pr.PlayerId)
            .ToDictionary(g => g.Key, g => g.First())
            ?? new Dictionary<int, ReplayPlayerRatingPlayerStatsData>();

        dict.TryGetValue(playerId, out var selfRating);

        return new ReplayContext(replay, rating, selfPlayer, selfRating, dict, index, totalCount);
    }

    // ---------------------- Processing Helpers ----------------------
    private static void ProcessGameMode(StatsState s, ReplayContext ctx)
    {
        var gm = ctx.Replay.GameMode;
        s.GameModeCounts[gm] = s.GameModeCounts.GetValueOrDefault(gm) + 1;
    }

    private static void ProcessLastReplays(StatsState s, ReplayContext ctx)
    {
        // Keep the last 12 replays (same condition as original: i > replays.Count - 12)
        if (ctx.Index <= ctx.TotalCount - 12)
            return;

        var replay = ctx.Replay;
        var rating = ctx.Rating;

        var dto = new ReplayListDto
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
            PlayerPos = ctx.SelfPlayer?.GamePos ?? 0
        };

        s.LastReplays.Add(dto);
    }

    private static void ProcessSelfPlayer(StatsState s, ReplayContext ctx)
    {
        var self = ctx.SelfPlayer;
        if (self is null)
            return;

        // Commander counts
        s.CommanderCounts[self.Race] = s.CommanderCounts.GetValueOrDefault(self.Race) + 1;

        // Position stats
        if (self.GamePos > 0)
        {
            if (!s.PosStats.TryGetValue(self.GamePos, out var posStat) || posStat is null)
            {
                posStat = new PosPlayerStats
                {
                    GamePos = self.GamePos,
                    Count = 1,
                    Wins = self.TeamId == ctx.Replay.WinnerTeam ? 1 : 0
                };
                s.PosStats[self.GamePos] = posStat;
            }
            else
            {
                posStat.Count++;
                if (self.TeamId == ctx.Replay.WinnerTeam)
                    posStat.Wins++;
            }
        }

        // If rated (self rating exists), update rating-related stats
        if (ctx.SelfRating is not null)
        {
            s.RatedGames++;

            // Avg gains in last 90 days by commander
            if (ctx.Replay.Gametime >= s.Past90Days)
            {
                if (!s.CommanderAvgGains.TryGetValue(self.Race, out var avgGain) || avgGain is null)
                {
                    avgGain = new PlayerCmdrAvgGain
                    {
                        Commander = self.Race,
                        Count = 1,
                        AvgGain = ctx.SelfRating.RatingDelta,
                        Wins = self.TeamId == ctx.Replay.WinnerTeam ? 1 : 0
                    };
                    s.CommanderAvgGains[self.Race] = avgGain;
                }
                else
                {
                    avgGain.Count++;
                    avgGain.AvgGain += ctx.SelfRating.RatingDelta;
                    if (self.TeamId == ctx.Replay.WinnerTeam)
                        avgGain.Wins++;
                }
            }

            // rating history entry
            s.RatingHistory.Add((ctx.Replay.Gametime, (float)(ctx.SelfRating.RatingBefore + ctx.SelfRating.RatingDelta), ctx.SelfRating.Games));
        }
    }

    private static void ProcessOtherPlayers(StatsState s, ReplayContext ctx)
    {
        // We only process teammates/opponents if there's an interest player and we have rating data for other players
        var interest = ctx.SelfPlayer;
        var interestRating = ctx.SelfRating;
        if (interest is null || ctx.Rating is null)
            return;

        // Build fast lookup for player ratings (already provided in ctx)
        var ratingsById = ctx.RatingsByPlayerId;

        foreach (var player in ctx.Replay.Players)
        {
            if (player.PlayerId == interest.PlayerId)
                continue;

            if (!ratingsById.TryGetValue(player.PlayerId, out var playerRating) || playerRating is null)
                continue;

            // Determine which dictionary (teammates or opponents)
            var toonId = new ToonIdRec(player.ToonId.Region, player.ToonId.Realm, player.ToonId.Id);
            var targetDict = player.TeamId == interest.TeamId ? s.Teammates : s.Opponents;

            if (!targetDict.TryGetValue(toonId, out var otherStats) || otherStats is null)
            {
                otherStats = new OtherPlayerStats
                {
                    Player = new PlayerDto
                    {
                        ToonId = new ToonIdDto
                        {
                            Region = player.ToonId.Region,
                            Realm = player.ToonId.Realm,
                            Id = player.ToonId.Id
                        }
                    },
                    Count = 1,
                    AvgGain = (float?)interestRating?.RatingDelta ?? 0f,
                    Wins = player.TeamId == ctx.Replay.WinnerTeam ? 1 : 0
                };
                targetDict[toonId] = otherStats;
            }
            else
            {
                otherStats.Count++;
                otherStats.AvgGain += (float?)interestRating?.RatingDelta ?? 0f;
                if (player.TeamId == ctx.Replay.WinnerTeam)
                    otherStats.Wins++;
            }

            // Track teammate/opponent average rating
            if (player.TeamId == interest.TeamId)
            {
                s.TeammateRatings += Convert.ToInt32(playerRating.RatingBefore);
                s.TeammateRatingsCount++;
            }
            else
            {
                s.OpponentRatings += Convert.ToInt32(playerRating.RatingBefore);
                s.OpponentRatingsCount++;
            }
        }
    }

    private static void ProcessStreaks(StatsState s, ReplayContext ctx)
    {
        var self = ctx.SelfPlayer;
        var selfRating = ctx.SelfRating;
        if (self is null || selfRating is null)
            return;

        bool isWin = self.TeamId == ctx.Replay.WinnerTeam;

        if (ctx.Index == 0)
        {
            // initialize
            if (isWin)
            {
                s.OnWinStreak = true;
                s.CurrentWin.Count = 1;
                s.CurrentWin.StartDate = ctx.Replay.Gametime;
                s.CurrentWin.EndDate = ctx.Replay.Gametime;
            }
            else
            {
                s.OnWinStreak = false;
                s.CurrentLose.Count = 1;
                s.CurrentLose.StartDate = ctx.Replay.Gametime;
                s.CurrentLose.EndDate = ctx.Replay.Gametime;
            }
            return;
        }

        if (isWin)
        {
            if (s.OnWinStreak)
            {
                // Continue win streak
                s.CurrentWin.Count++;
                s.CurrentWin.EndDate = ctx.Replay.Gametime;
            }
            else
            {
                // Switching from lose to win streak
                // Only update BestLose if current lose streak was longer
                if (s.CurrentLose.Count > s.BestLose.Count)
                {
                    s.BestLose = s.CurrentLose with { EndDate = ctx.Replay.Gametime };
                }
                s.CurrentWin = new StreakPlayerStats
                {
                    Count = 1,
                    StartDate = ctx.Replay.Gametime,
                    EndDate = ctx.Replay.Gametime
                };
                s.OnWinStreak = true;
            }
        }
        else
        {
            if (!s.OnWinStreak)
            {
                // Continue lose streak
                s.CurrentLose.Count++;
                s.CurrentLose.EndDate = ctx.Replay.Gametime;
            }
            else
            {
                // Switching from win to lose streak
                // Only update BestWin if current win streak was longer
                if (s.CurrentWin.Count > s.BestWin.Count)
                {
                    s.BestWin = s.CurrentWin with { EndDate = ctx.Replay.Gametime };
                }
                s.CurrentLose = new StreakPlayerStats
                {
                    Count = 1,
                    StartDate = ctx.Replay.Gametime,
                    EndDate = ctx.Replay.Gametime
                };
                s.OnWinStreak = false;
            }
        }
    }

    private static void ProcessTopRating(StatsState s, ReplayContext ctx)
    {
        if (ctx.SelfRating is null)
            return;

        double newRating = ctx.SelfRating.RatingBefore + ctx.SelfRating.RatingDelta;
        if (newRating > s.TopRating.Rating)
        {
            s.TopRating = new TopRating
            {
                Rating = newRating,
                DateAchieved = ctx.Replay.Gametime
            };
        }
    }

    // ---------------------- Final assembly ----------------------
    private static RatingDetails BuildFinalDetails(StatsState s)
    {
        var details = new RatingDetails
        {
            GameModes = s.GameModeCounts
                .Select(kvp => new GameModeCount { GameMode = kvp.Key, Count = kvp.Value })
                .ToList(),

            Commanders = s.CommanderCounts
                .Select(kvp => new CommanderCount { Commander = kvp.Key, Count = kvp.Value })
                .ToList(),

            Ratings = s.RatingHistory
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

            Replays = s.LastReplays.OrderByDescending(r => r.Gametime).ToList(),

            AvgGainResponses = new List<CmdrAvgGainResponse>
                {
                    new CmdrAvgGainResponse
                    {
                        TimePeriod = _defaultGainTimePeriod,
                        AvgGains = s.CommanderAvgGains.Values.ToList()
                    }
                },

            TeammateStats = s.Teammates.Values.Where(x => x.Count > 10).ToList(),
            OpponentStats = s.Opponents.Values.Where(x => x.Count > 10).ToList(),
            PosStats = s.PosStats.Values.ToList(),
            AvgTeammateRating = s.TeammateRatingsCount == 0 ? 0 : (int)(s.TeammateRatings / s.TeammateRatingsCount),
            AvgOpponentRating = s.OpponentRatingsCount == 0 ? 0 : (int)(s.OpponentRatings / s.OpponentRatingsCount),
            LongestWinStreak = s.BestWin.Count >= s.CurrentWin.Count ? s.BestWin : s.CurrentWin,
            LongestLoseStreak = s.BestLose.Count >= s.CurrentLose.Count ? s.BestLose : s.CurrentLose,
            CurrentStreak = s.OnWinStreak ? s.CurrentWin : s.CurrentLose with { Count = s.CurrentLose.Count * -1 },
            TopRating = s.TopRating
        };

        return details;
    }
}


// ---------------------- State & Context ----------------------
internal class StatsState
{
    public Dictionary<GameMode, int> GameModeCounts { get; } = new();
    public Dictionary<Commander, int> CommanderCounts { get; } = new();
    public Dictionary<Commander, PlayerCmdrAvgGain> CommanderAvgGains { get; } = new();
    public List<ReplayListDto> LastReplays { get; } = new();
    public Dictionary<ToonIdRec, OtherPlayerStats> Teammates { get; } = new();
    public Dictionary<ToonIdRec, OtherPlayerStats> Opponents { get; } = new();
    public Dictionary<int, PosPlayerStats> PosStats { get; } = new();
    public List<(DateTime Date, float Rating, int Games)> RatingHistory { get; } = new();
    public int RatedGames { get; set; } = 0;
    public long TeammateRatings { get; set; } = 0;
    public long OpponentRatings { get; set; } = 0;
    public int TeammateRatingsCount { get; set; } = 0;
    public int OpponentRatingsCount { get; set; } = 0;

    public bool OnWinStreak { get; set; }
    public StreakPlayerStats CurrentWin { get; set; } = new();
    public StreakPlayerStats CurrentLose { get; set; } = new();
    public StreakPlayerStats BestWin { get; set; } = new();
    public StreakPlayerStats BestLose { get; set; } = new();

    public TopRating TopRating { get; set; } = new() { Rating = 0, DateAchieved = DateTime.MinValue };

    public DateTime Past90Days { get; set; }
}

internal record ReplayContext(
    ReplayPlayerStatsData Replay,
    ReplayRatingPlayerStatsData? Rating,
    ReplayPlayerPlayerStatsData? SelfPlayer,
    ReplayPlayerRatingPlayerStatsData? SelfRating,
    Dictionary<int, ReplayPlayerRatingPlayerStatsData> RatingsByPlayerId,
    int Index,
    int TotalCount
);