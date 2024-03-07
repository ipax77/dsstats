using dsstats.db8;
using dsstats.db8services.Import;
using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace dsstats.db8services;

public partial class TourneysService
{
    private readonly string tourneyDir = "/data/ds/Tourneys";
    public async Task SeedTourneys()
    {
        var tourneys = await GetTourneys();

        var tourneydirs = Directory.GetDirectories(tourneyDir);

        List<string> newTourneys = [];

        foreach (var tourneydir in tourneydirs)
        {
            var tourneyName = Path.GetFileName(tourneydir);
            var tourney = tourneys
                .FirstOrDefault(f => f.Name == tourneyName);

            if (tourney == null)
            {
                newTourneys.Add(tourneydir.Replace("\\", "/"));
            }
        }

        Console.WriteLine($"new tourneys: {string.Join(", ", newTourneys)}");

        foreach (var tourney in newTourneys)
        {
            var tourneyName = Path.GetFileName(tourney);

            if (tourneyName is null)
            {
                continue;
            }

            var tourneyEvent = await context.Events.FirstOrDefaultAsync(f => f.Name == tourneyName);
            if (tourneyEvent is null)
            {
                tourneyEvent = new()
                {
                    Name = tourneyName
                };
                context.Events.Add(tourneyEvent);
                await context.SaveChangesAsync();
            }

            await CreateNewTournament(tourney, tourneyEvent.EventId);
        }
    }

    private async Task CreateNewTournament(string tourneyPath, int eventId)
    {
        var jsonReplays = Directory.GetFiles(tourneyPath, "*.json", SearchOption.AllDirectories)
            .ToList();

        if (jsonReplays.Count == 0)
        {
            return;
        }

        List<ReplayDto> replays = [];

        foreach (var jsonReplay in jsonReplays)
        {
            var replay = JsonSerializer.Deserialize<ReplayDto>(File.ReadAllText(jsonReplay));
            if (replay is null)
            {
                continue;
            }
            replays.Add(replay);
        }
        await ImportReplays(replays);

        var groups = replays.GroupBy(g => Path.GetDirectoryName(g.FileName));
        foreach (var group in groups)
        {
            await CreateReplayEvent([.. group], eventId);
        }
    }

    private async Task CreateReplayEvent(List<ReplayDto> replays, int eventId)
    {
        List<string> replayHashes = replays.Select(s => s.ReplayHash).ToList();
        var dbReplays = await context.Replays
            .Include(i => i.ReplayEvent)
            .Where(x => replayHashes.Contains(x.ReplayHash))
            .ToListAsync();

        if (dbReplays.Count == 0)
        {
            return;
        }

        (var team1, var team2) = GetTeamNames(replays.First().FileName);

        ReplayEvent replayEvent = new()
        {
            WinnerTeam = team1,
            RunnerTeam = team2,
            Round = "tbd",
            EventId = eventId
        };
        context.ReplayEvents.Add(replayEvent);

        foreach (var replay in dbReplays)
        {
            replay.ReplayEvent = replayEvent;
            var replayDto = replays.FirstOrDefault(f => f.ReplayHash == replay.ReplayHash);
            replay.FileName = replayDto?.FileName ?? string.Empty;
        }
        await context.SaveChangesAsync();
    }

    private (string, string) GetTeamNames(string path)
    {
        var teamInfo = Path.GetFileName(Path.GetDirectoryName(path));
        var teams = teamInfo?.Split(" vs ", StringSplitOptions.RemoveEmptyEntries);

        if (teams != null && teams.Length == 2)
        {
            return (teams[0].Trim(), teams[1].Trim());
        }
        else
        {
            Console.Write($"Could not determine teams from {path}");
        }
        return ("unknown", "unknown");
    }

    private async Task ImportReplays(List<ReplayDto> replays)
    {
        using var scope = scopeFactory.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();

        var result = await importService.Import(replays);
        Console.WriteLine($"{result.Imported} tourney replays imported.");
    }

    public async Task CheckForNewReplays(int eventId)
    {
        var tourneyEvent = await context.Events
            .FirstOrDefaultAsync(f => f.EventId == eventId);

        if (tourneyEvent is null)
        {
            return;
        }

        var replayOsPaths = Directory.GetFiles(Path.Combine(tourneyDir, tourneyEvent.Name), "*.SC2Replay", SearchOption.AllDirectories);
        var replayPaths = replayOsPaths.Select(s => s.Replace("\\", "/")).ToHashSet();

        var replays = await context.Replays
            .Where(x => x.ReplayEvent != null
                && x.ReplayEvent.EventId == eventId)
            .Select(s => s.FileName)
            .ToListAsync();

        replayPaths.ExceptWith(replays);

        if (replayPaths.Count == 0)
        {
            return;
        }


    }
}
