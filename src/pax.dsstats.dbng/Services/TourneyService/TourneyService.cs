using AutoMapper;
using Microsoft.EntityFrameworkCore;
using pax.dsstats.dbng.Repositories;
using pax.dsstats.shared;
using System.Text.Json;

namespace pax.dsstats.dbng.Services;

public partial class TourneyService
{
    private const string tourneyFolder = "/data/ds/Tourneys";

    private readonly ReplayContext context;
    private readonly IReplayRepository replayRepository;

    public TourneyService(ReplayContext context, IReplayRepository replayRepository)
    {
        this.context = context;
        this.replayRepository = replayRepository;
    }

    public async Task CollectTourneyReplays()
    {
        var newReplayInfos = await ScanForNewReplays();

        if (!newReplayInfos.Any())
        {
            return;
        }

        var units = (await context.Units.AsNoTracking().ToListAsync()).ToHashSet();
        var upgrades = (await context.Upgrades.AsNoTracking().ToListAsync()).ToHashSet();

        foreach (var replayInfo in newReplayInfos)
        {
            foreach (var eventReplay in replayInfo.Replays)
            {
                var replayJson = eventReplay[..^9] + "json";

                if (!File.Exists(replayJson))
                {
                    continue;
                }

                var replayDto = JsonSerializer.Deserialize<ReplayDto>(File.ReadAllText(replayJson));

                if (replayDto == null)
                {
                    continue;
                }

                var replay = await context.Replays
                    .Include(i => i.ReplayEvent)
                    .ThenInclude(j => j.Event)
                    .FirstOrDefaultAsync(f => f.FileName == eventReplay || f.ReplayHash == f.ReplayHash);

                if (replay == null)
                {
                    (units, upgrades, replay) = await replayRepository.SaveReplay(replayDto, units, upgrades, new ReplayEventDto()
                    {
                        Round = replayInfo.Round,
                        WinnerTeam = replayInfo.WinnerTeam,
                        RunnerTeam = replayInfo.RunnerTeam,
                        Ban1 = replayInfo.Ban1,
                        Ban2 = replayInfo.Ban2,
                        Ban3 = replayInfo.Ban3,
                        Ban4 = replayInfo.Ban4,
                        Ban5 = replayInfo.Ban5,
                        Event = new()
                        {
                            Name = replayInfo.Event
                        }
                    });
                }
                else
                {
                    var dbEvent = await context.Events.FirstOrDefaultAsync(f => f.Name == replayInfo.Event);

                    if (dbEvent == null)
                    {
                        dbEvent = new Event()
                        {
                            Name = replayInfo.Event
                        };
                        context.Events.Add(dbEvent);
                    }

                    replay.FileName = eventReplay;
                    replay.ReplayEvent = new ReplayEvent()
                    {
                        Round = replayInfo.Round,
                        WinnerTeam = replayInfo.WinnerTeam,
                        RunnerTeam = replayInfo.RunnerTeam,
                        Ban1 = replayInfo.Ban1,
                        Ban2 = replayInfo.Ban2,
                        Ban3 = replayInfo.Ban3,
                        Ban4 = replayInfo.Ban4,
                        Ban5 = replayInfo.Ban5,
                        Event = dbEvent
                    };
                    await context.SaveChangesAsync();
                }
            }
        }
    }

    private async Task<ICollection<ReplayInfo>> ScanForNewReplays()
    {
        var dbReplayPaths = await context.Replays
            .Where(x => !String.IsNullOrEmpty(x.FileName))
            .Select(s => s.FileName)
            .ToListAsync();
        var replayInfos = GetHdReplayPaths();

        var hdReplayPaths = replayInfos.SelectMany(s => s.Replays);
        var newReplays = hdReplayPaths.Except(dbReplayPaths).ToList();

        HashSet<ReplayInfo> newInfos = new();
        foreach (var newReplay in newReplays)
        {
            foreach (var replayInfo in replayInfos)
            {
                if (replayInfo.Replays.Contains(newReplay))
                {
                    newInfos.Add(replayInfo);
                }
            }
        }
        return newInfos;
    }

    private ICollection<ReplayInfo> GetHdReplayPaths()
    {
        List<ReplayInfo> replayInfos = new();
        foreach (var tourneyDir in Directory.GetDirectories(tourneyFolder))
        {
            string eventName = Path.GetFileName(tourneyDir);

            foreach (var roundDir in Directory.GetDirectories(tourneyDir))
            {
                var replayInfo = GetReplayInfo(eventName, roundDir);
                replayInfo.Replays.AddRange(Directory.GetFiles(roundDir, "*.SC2Replay", SearchOption.AllDirectories).Select(s => s.Replace('\\', '/')));
                replayInfos.Add(replayInfo);
            }

        }
        return replayInfos;
    }

    private ReplayInfo GetReplayInfo(string eventName, string roundDir)
    {
        var dirName = new DirectoryInfo(roundDir).Name;

        var roundSplit = dirName.Split('-');

        string team1 = "";
        string team2 = "";
        var teamSplit = roundSplit[1].Split(" vs ");

        team1 = teamSplit[0].Trim();
        team2 = teamSplit[1].Trim();

        var bans = new List<Commander>()
        {
            Commander.None,
            Commander.None,
            Commander.None,
            Commander.None,
            Commander.None
        };

        var banSplit = roundSplit.Length == 3 ? roundSplit[2].Split('_') : null;
        if (banSplit != null)
        {
            for (int i = 0; i < Math.Min(banSplit.Length, bans.Count); i++)
            {
                bans[i] = Data.GetCommander(banSplit[i].Trim());
            }
        }

        return new()
        {
            Event = eventName,
            WinnerTeam = team1,
            RunnerTeam = team2,
            Round = roundSplit[0].Trim(),
            Ban1 = bans[0],
            Ban2 = bans[1],
            Ban3 = bans[2],
            Ban4 = bans[3],
            Ban5 = bans[4],
        };
    }
}

internal record ReplayInfo()
{
    public string Event { get; init; } = null!;
    public string Round { get; init; } = null!;
    public string WinnerTeam { get; init; } = null!;
    public string RunnerTeam { get; init; } = null!;
    public Commander Ban1 { get; init; }
    public Commander Ban2 { get; init; }
    public Commander Ban3 { get; init; }
    public Commander Ban4 { get; init; }
    public Commander Ban5 { get; init; }
    public List<string> Replays { get; } = new();
}