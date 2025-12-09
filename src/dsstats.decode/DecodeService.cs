using dsstats.shared;
using Microsoft.Extensions.Options;
using s2protocol.NET;
using System.Text.RegularExpressions;

namespace dsstats.decode;

public partial class DecodeService(
    IReplayQueue replayQueue,
    IOptions<DecodeSettings> decodeSettings,
    ILogger<DecodeService> logger)
{
    private readonly IReplayQueue replayQueue = replayQueue;
    private readonly DecodeSettings decodeSettings = decodeSettings.Value;
    private readonly ILogger<DecodeService> logger = logger;

    public async Task<int> SaveReplays(Guid guid, List<IFormFile> files)
    {
        return await SaveAndQueueFiles(guid, files, inHouse: true);
    }

    public async Task<int> SaveReplaysRaw(Guid guid, List<IFormFile> files)
    {
        return await SaveAndQueueFiles(guid, files, inHouse: false);
    }

    private async Task<int> SaveAndQueueFiles(Guid guid, List<IFormFile> files, bool inHouse)
    {
        if (files.Count == 0)
            return 0;

        Directory.CreateDirectory(decodeSettings.ReplayFolders.Temp);

        foreach (var formFile in files)
        {
            var tempFileName = $"{guid}_{Guid.NewGuid()}.SC2Replay";
            var tempPath = Path.Combine(decodeSettings.ReplayFolders.Temp, tempFileName);

            // Save the file
            using (var stream = new FileStream(tempPath, FileMode.Create))
                await formFile.CopyToAsync(stream);

            logger.LogInformation("Saved uploaded replay to temp: {path}", tempPath);

            // Prepare job
            var job = new ReplayJob(guid, tempPath, "", inHouse);
            
            // Try enqueue
            if (!replayQueue.TryEnqueue(job))
            {
                logger.LogWarning("Replay queue full — rejecting uploaded replay: {path}", tempPath);
                return -1; // API returns 500 or 429 based on your controller
            }
        }

        return 1;
    }

    public static ReplayMetadata GetMetaData(Sc2Replay replay)
    {
        List<ReplayMetadataPlayer> players = [];

        if (replay.Initdata is null || replay.Details is null || replay.Metadata is null)
        {
            return new();
        }

        foreach (var player in replay.Initdata.LobbyState.Slots)
        {
            players.Add(new()
            {
                PlayerId = GetPlayerId(player.ToonHandle),
                Observer = player.Observe == 1,
                SlotId = player.WorkingSetSlotId
            });
        }

        int i = 0;
        foreach (var player in replay.Details.Players)
        {
            i++;
            PlayerId playerId = GetPlayerId(player.Toon);
            var metaPlayer = players.FirstOrDefault(f => f.PlayerId == playerId);
            if (metaPlayer is null)
            {
                continue;
            }
            metaPlayer.Id = i;
            metaPlayer.Name = player.Name;
            metaPlayer.AssignedRace = GetRace(player.Race);
        }

        foreach (var player in replay.Metadata.Players)
        {
            var metaPlayer = players.FirstOrDefault(f => f.Id == player.PlayerID);
            if (metaPlayer is null)
            {
                continue;
            }
            metaPlayer.SelectedRace = GetSelectedRace(player.SelectedRace);
        }

        return new()
        {
            Players = players
        };
    }

    private static Guid GetGroupIdFromFilename(string replayPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(replayPath);
        var guids = fileName.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (guids.Length > 0 && Guid.TryParse(guids[0], out var groupId)
            && groupId != Guid.Empty)
        {
            return groupId;
        }
        throw new Exception($"failed getting groupId from replayPath: {replayPath}");
    }

    private static Commander GetSelectedRace(string selectedRace)
    {
        var race = selectedRace switch
        {
            "Terr" => "Terran",
            "Prot" => "Protoss",
            "Rand" => "None",
            _ => selectedRace
        };
        return GetRace(race);
    }

    private static PlayerId GetPlayerId(s2protocol.NET.Models.Toon toon)
    {
        return new(toon.Id, toon.Realm, toon.Region);
    }

    private static PlayerId GetPlayerId(string toonHandle)
    {
        Regex rx = PlayerIdRegex();
        var match = rx.Match(toonHandle);
        if (match.Success)
        {
            int regionId = int.Parse(match.Groups[1].Value);
            int realmId = int.Parse(match.Groups[2].Value);
            int toonId = int.Parse(match.Groups[3].Value);
            return new(toonId, realmId, regionId);
        }
        return new();
    }

    private static Commander GetRace(string race)
    {
        if (Enum.TryParse(typeof(Commander), race, out var cmdrObj)
            && cmdrObj is Commander cmdr)
        {
            return cmdr;
        }
        return Commander.None;
    }

    [GeneratedRegex(@"(\d)-S2-(\d)-(\d+)")]
    private static partial Regex PlayerIdRegex();
}

public class DecodeEventArgs : EventArgs
{
    public Guid Guid { get; set; }
    public List<IhReplay> IhReplays { get; set; } = [];
    public string? Error { get; set; }
}

public class DecodeRawEventArgs : EventArgs
{
    public Guid Guid { get; set; }
    public List<ChallengeResponse> ChallengeResponses { get; set; } = [];
    public string? Error { get; set; }
}

