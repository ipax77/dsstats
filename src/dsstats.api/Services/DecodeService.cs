using dsstats.db8services.Import;
using dsstats.shared;
using pax.dsstats.parser;
using s2protocol.NET;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace dsstats.api.Services;

public class DecodeService(ILogger<DecodeService> logger, IServiceScopeFactory scopeFactory)
{
    private readonly string replayFolder = "/data/ds/decode";

    private readonly SemaphoreSlim ss = new(1, 1);
    private ReplayDecoder? replayDecoder;
    public static readonly string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

    public EventHandler<DecodeEventArgs>? DecodeFinished;

    private void OnDecodeFinished(DecodeEventArgs e)
    {
        DecodeFinished?.Invoke(this, e);
    }

    public async Task<bool> SaveReplays(Guid guid, List<IFormFile> files)
    {
        long size = files.Sum(f => f.Length);

        foreach (var formFile in files)
        {
            if (formFile.Length > 0)
            {
                var filePath = Path.Combine(replayFolder, "todo", guid.ToString() + "_" + Guid.NewGuid().ToString() + ".SC2Replay");

                using (var stream = File.Create(filePath))
                {
                    await formFile.CopyToAsync(stream);
                }
            }
        }
        _ = Decode(guid);
        return true;
    }

    public async Task Decode(Guid guid)
    {
        await ss.WaitAsync();
        List<IhReplay> replays = [];

        try
        {
            var replayPaths = Directory.GetFiles(Path.Combine(replayFolder, "todo"), "*SC2Replay");

            if (replayPaths.Length == 0)
            {
                return;
            }

            if (replayDecoder is null)
            {
                replayDecoder = new(assemblyPath);
            }

            var options = new ReplayDecoderOptions()
            {
                Initdata = true,
                Details = true,
                Metadata = true,
                TrackerEvents = true,
            };

            using var md5 = MD5.Create();

            await foreach (var result in replayDecoder.DecodeParallelWithErrorReport(replayPaths, 2, options))
            {
                if (result.Sc2Replay is null)
                {
                    Error(result);
                    continue;
                }

                var metaData = GetMetaData(result.Sc2Replay);

                var sc2Replay = Parse.GetDsReplay(result.Sc2Replay);

                if (sc2Replay is null)
                {
                    Error(result);
                    continue;
                }

                var replayDto = Parse.GetReplayDto(sc2Replay, md5);

                if (replayDto is null)
                {
                    Error(result);
                    continue;
                }

                File.Move(result.ReplayPath, Path.Combine(replayFolder, "done", Path.GetFileName(result.ReplayPath)));
                replays.Add(new IhReplay() {  Replay = replayDto, Metadata = metaData });
            }

            if (replays.Count > 0)
            {
                using var scope = scopeFactory.CreateScope();
                var importService = scope.ServiceProvider.GetRequiredService<ImportService>();
                await importService.Import(replays.Select(s => s.Replay).ToList());
            }
        }
        catch (Exception ex)
        {
            logger.LogError("failed decoding replays: {error}", ex.Message);
        }
        finally
        {
            ss.Release();
            OnDecodeFinished(new()
            {
                Guid = guid,
                IhReplays = replays
            });
        }
    }

    private void Error(DecodeParallelResult result)
    {
        logger.LogError("failed decoding replay: {path}", result.ReplayPath);
        File.Move(result.ReplayPath, Path.Combine(replayFolder, "error", Path.GetFileName(result.ReplayPath)));
    }

    private ReplayMetadata GetMetaData(Sc2Replay replay)
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

    private Commander GetSelectedRace(string selectedRace)
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

    private PlayerId GetPlayerId(s2protocol.NET.Models.Toon toon)
    {
        return new(toon.Id, toon.Realm, toon.Region);
    }

    private PlayerId GetPlayerId(string toonHandle)
    {
        Regex rx = new(@"(\d)-S2-(\d)-(\d+)");
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

    private Commander GetRace(string race)
    {
        if (Enum.TryParse(typeof(Commander), race, out var cmdrObj)
            && cmdrObj is Commander cmdr)
        {
            return cmdr;
        }
        return Commander.None;
    }
}

public class DecodeEventArgs : EventArgs
{
    public Guid Guid { get; set; }
    public List<IhReplay> IhReplays { get; set; } = [];
}

public record IhReplay
{
    public ReplayDto Replay { get; set; } = null!;
    public ReplayMetadata Metadata { get; set; } = null!;
}

public record ReplayMetadata
{
    public List<ReplayMetadataPlayer> Players { get; set; } = [];
}

public record ReplayMetadataPlayer
{
    public PlayerId PlayerId { get; set; } = new();
    public string Name { get; set; } = string.Empty;
    public bool Observer { get; set; }
    public int Id { get; set; }
    public int SlotId { get; set; }
    public Commander SelectedRace { get; set; }
    public Commander AssignedRace { get; set; }

}