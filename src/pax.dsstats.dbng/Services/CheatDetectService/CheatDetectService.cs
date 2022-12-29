
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pax.dsstats.shared;

namespace pax.dsstats.dbng.Services;

public partial class CheatDetectService
{
    private readonly ReplayContext context;
    private readonly IMapper mapper;
    private readonly ILogger<CheatDetectService> logger;

    public CheatDetectService(ReplayContext context,
                              IMapper mapper,
                              ILogger<CheatDetectService> logger)
    {
        this.context = context;
        this.mapper = mapper;
        this.logger = logger;
    }

    public async Task<CheatResult> Detect(bool dry = false)
    {
        await DetectNoUpload(dry);

        var replays = await context.Replays
            .Include(i => i.ReplayPlayers)
            .Where(x => x.WinnerTeam == 0
              && x.Playercount == 6
              && x.Duration >= 60)
            .AsNoTracking()
            .OrderByDescending(o => o.GameTime)
                .ThenBy(o => o.ReplayId)
            .ToListAsync();

        CheatResult cheatResult = new()
        {
            NoResultGames = replays.Count
        };

        Dictionary<int, DetectInfo> playerIdInfos = new();
        Dictionary<int, int> replayIdNewResult = new();

        foreach (var replay in replays)
        {
            var uploaders = replay.ReplayPlayers.Where(x => x.IsUploader).ToList();

            var uploaderTeams = uploaders.Select(s => s.Team).ToHashSet();

            if (uploaderTeams.Count != 1)
            {
                cheatResult.UnknownGames++;
                continue;
            }

            var uploaderTeam = uploaderTeams.First();

            var incomeDiff = replay.ReplayPlayers.Where(x => x.Team == 1).Sum(s => s.Income)
                            - replay.ReplayPlayers.Where(x => x.Team == 2).Sum(s => s.Income);

            var armyDiff = replay.ReplayPlayers.Where(x => x.Team == 1).Sum(s => s.Army)
                            - replay.ReplayPlayers.Where(x => x.Team == 2).Sum(s => s.Army);

            var killsDiff = replay.ReplayPlayers.Where(x => x.Team == 1).Sum(s => s.Kills)
                            - replay.ReplayPlayers.Where(x => x.Team == 2).Sum(s => s.Kills);

            var uploaderLastMiddleHoldDuration = GetUploaderLastMiddleHold(replay.Middle, replay.Duration, uploaderTeam);

            if (uploaderTeam != 1)
            {
                incomeDiff *= -1;
                armyDiff *= -1;
                killsDiff *= -1;
            }            

            var rqScore = GetRqScore(incomeDiff, armyDiff, killsDiff, uploaderLastMiddleHoldDuration);

            if (rqScore > 0)
            {
                cheatResult.RqGames++;
                foreach (var uploader in uploaders)
                {
                    if (!playerIdInfos.TryGetValue(uploader.PlayerId, out DetectInfo? info))
                    {
                        info = playerIdInfos[uploader.PlayerId] = new();
                    }
                    info.RqGames++;
                }
                replayIdNewResult[replay.ReplayId] = uploaderTeam == 1 ? 2 : 1;
            }
            else
            {
                foreach (var uploader in uploaders)
                {
                    if (!playerIdInfos.TryGetValue(uploader.PlayerId, out DetectInfo? info))
                    {
                        info = playerIdInfos[uploader.PlayerId] = new();
                    }
                    info.DcGames++;
                }
                cheatResult.DcGames++;
            }
        }

        if (!dry)
        {
            await SetInfo(playerIdInfos);
            await CorrectReplayResults(replayIdNewResult);
        }

        logger.LogWarning(cheatResult.ToString());
        return cheatResult;
    }

    private async Task SetInfo(Dictionary<int, DetectInfo> playerIdInfos)
    {
        int i = 0;
        foreach (var info in playerIdInfos)
        {
            var player = await context.Players.FirstOrDefaultAsync(f => f.PlayerId == info.Key);
            if (player == null)
            {
                continue;
            }
            player.RageQuitCount += info.Value.RqGames;
            player.DisconnectCount = info.Value.DcGames;
            i++;
            if (i % 1000 == 0)
            {
                await context.SaveChangesAsync();
            }
        }
        await context.SaveChangesAsync();
    }

    private async Task CorrectReplayResults(Dictionary<int, int> replayIdNewResults)
    {
        int i = 0;
        foreach (var result in replayIdNewResults)
        {
            var replay = await context.Replays
                .Include(i => i.ReplayPlayers)
                .FirstOrDefaultAsync(f => f.ReplayId == result.Key);

            if (replay == null)
            {
                continue;
            }

            foreach (var replayPlayer in replay.ReplayPlayers)
            {
                if (replayPlayer.Team == result.Value)
                {
                    replayPlayer.PlayerResult = PlayerResult.Win;
                }
                else
                {
                    replayPlayer.PlayerResult = PlayerResult.Los;
                }
            }

            replay.WinnerTeam = result.Value;
            replay.ResultCorrected = true;

            i++;
            if (i % 1000 == 0)
            {
                await context.SaveChangesAsync();
            }
        }
        await context.SaveChangesAsync();
    }

    private static int GetRqScore(int incomeDiff, int armyDiff, int killsDiff, int uploaderLastMiddleHoldDuration)
    {
        int rqScore = 0;

        rqScore += GetNumberScore(incomeDiff);
        rqScore += GetNumberScore(armyDiff);
        rqScore += GetNumberScore(killsDiff);

        rqScore += GetMiddleScore(uploaderLastMiddleHoldDuration);

        return rqScore;
    }

    private static int GetMiddleScore(int middle)
    {
        if (middle > 0)
        {
            return -2;
        }
        if (middle < -800)
        {
            return 3;
        }
        if (middle < -400)
        {
            return 2;
        }
        if (middle < 0)
        {
            return 1;
        }
        return 0;
    }

    private static int GetNumberScore(int number)
    {
        if (number < 0)
        {
            return 1;
        }
        if (number > 0)
        {
            return -1;
        }
        return 0;
    }

    private static int GetUploaderLastMiddleHold(string middle, int duration, int uploaderTeam)
    {
        if (String.IsNullOrEmpty(middle) || duration == 0)
        {
            return 0;
        }

        int totalDuration = (int)(duration * 22.4);
        var ents = middle.Split('|', StringSplitOptions.RemoveEmptyEntries);

        if (ents.Length < 2)
        {
            return 0;
        }

        int currentTeam = int.Parse(ents[0]);
        int lastLoop = int.Parse(ents[1]);

        int sumTeam1 = 0;
        int sumTeam2 = 0;

        if (ents.Length > 2)
        {
            for (int i = 2; i < ents.Length; i++)
            {
                int currentLoop = int.Parse(ents[i]);
                if (currentTeam == 1)
                {
                    sumTeam1 += currentLoop - lastLoop;
                }
                else
                {
                    sumTeam2 += currentLoop - lastLoop;
                }
                currentTeam = currentTeam == 1 ? 2 : 1;
                lastLoop = currentLoop;
            }
        }

        int lastHoldDuration = totalDuration - sumTeam1 - sumTeam2;

        // sumTeam1 = currentTeam == 1 ? sumTeam1 + lastHoldDuration : sumTeam1;
        // sumTeam2 = currentTeam == 2 ? sumTeam2 + lastHoldDuration : sumTeam2;

        // var mid1 = sumTeam1 * 100.0 / totalDuration;
        // var mid2 = sumTeam2 * 100.0 / totalDuration;

        return uploaderTeam == currentTeam ? lastHoldDuration : lastHoldDuration * -1;
    }
}

public record CheatResult
{
    public int NoResultGames { get; init; }
    public int RqGames { get; set; }
    public int DcGames { get; set; }
    public int UnknownGames { get; set; }
}

internal record DetectInfo
{
    public int RqGames { get; set; }
    public int DcGames { get; set; }
}