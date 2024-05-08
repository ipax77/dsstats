using dsstats.db8services.Import;
using dsstats.shared;
using pax.dsstats.parser;
using s2protocol.NET;
using System.Collections.Concurrent;
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
    private int queueCount = 0;
    private ConcurrentBag<string> excludeReplays = [];

    public EventHandler<DecodeEventArgs>? DecodeFinished;

    private void OnDecodeFinished(DecodeEventArgs e)
    {
        DecodeFinished?.Invoke(this, e);
    }

    public async Task<int> SaveReplays(Guid guid, List<IFormFile> files)
    {
        try
        {
            long size = files.Sum(f => f.Length);

            foreach (var formFile in files)
            {
                if (formFile.Length > 0)
                {
                    var fileGuid = Guid.NewGuid();
                    var filePath = Path.Combine(replayFolder, "todo", guid.ToString() + "_" + fileGuid.ToString() + ".SC2Replay");
                    var tmpFilePath = Path.Combine(replayFolder, "todo", guid.ToString() + "_" + fileGuid.ToString() + ".tmp");

                    {
                        using var stream = File.Create(tmpFilePath);
                        await formFile.CopyToAsync(stream);
                    }
                    File.Move(tmpFilePath, filePath);
                }
            }
            _ = Decode(guid);

            logger.LogWarning($"replays saved ({size})", size);
            return queueCount;
        }
        catch (Exception ex)
        {
            logger.LogError("failed saving replays: {error}", ex.Message);
        }
        return -1;
    }

    public async Task Decode(Guid guid)
    {
        Interlocked.Increment(ref queueCount);
        await ss.WaitAsync();
        List<IhReplay> replays = [];
        string? error = null;

        try
        {
            var replayPaths = Directory.GetFiles(Path.Combine(replayFolder, "todo"), "*SC2Replay");
            replayPaths = replayPaths.Except(excludeReplays).ToArray();

            if (replayPaths.Length == 0)
            {
                error = "No replays found.";
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
                    error = "failed decoding replays.";
                    continue;
                }

                var metaData = GetMetaData(result.Sc2Replay);

                var sc2Replay = Parse.GetDsReplay(result.Sc2Replay);

                if (sc2Replay is null)
                {
                    Error(result);
                    error = "failed decoding replays.";
                    continue;
                }

                var replayDto = Parse.GetReplayDto(sc2Replay, md5);

                if (replayDto is null)
                {
                    Error(result);
                    error = "failed decoding replays.";
                    continue;
                }

                File.Move(result.ReplayPath, Path.Combine(replayFolder, "done", Path.GetFileName(result.ReplayPath)));
                replays.Add(new IhReplay() { Replay = replayDto, Metadata = metaData });
            }

            if (replays.Count > 0)
            {
                using var scope = scopeFactory.CreateScope();
                var importService = scope.ServiceProvider.GetRequiredService<ImportService>();
                replays.ForEach(f => f.Replay.FileName = string.Empty);
                await importService.Import(replays.Select(s => s.Replay).ToList());
            }
        }
        catch (Exception ex)
        {
            logger.LogError("failed decoding replays: {error}", ex.Message);
            error = "failed decoding replays.";
        }
        finally
        {
            ss.Release();
            OnDecodeFinished(new()
            {
                Guid = guid,
                IhReplays = replays,
                Error = error,
            });
            Interlocked.Decrement(ref queueCount);
        }
    }

    private void Error(DecodeParallelResult result)
    {
        logger.LogError("failed decoding replay: {path}, {error}", result.ReplayPath, result.Exception);
        try
        {
            File.Move(result.ReplayPath, Path.Combine(replayFolder, "error", Path.GetFileName(result.ReplayPath)));
        }
        catch (Exception ex)
        {
            logger.LogWarning("failed moving error replay: {error}", ex.Message);
            excludeReplays.Add(result.ReplayPath);
        }
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

    private static Commander GetRace(string race)
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
    public string? Error { get; set; }
}

