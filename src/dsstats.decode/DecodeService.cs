using dsstats.shared;
using Microsoft.Extensions.Options;
using pax.dsstats.parser;
using s2protocol.NET;
using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace dsstats.decode;

public partial class DecodeService(IOptions<DecodeSettings> decodeSettings,
                                   IHttpClientFactory httpClientFactory,
                                   ILogger<DecodeService> logger)
{

    private readonly SemaphoreSlim ss = new(1, 1);
    private readonly SemaphoreSlim fileSemaphore = new SemaphoreSlim(1, 1);
    private ReplayDecoder? replayDecoder;
    public static readonly string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
    private int queueCount = 0;
    private ConcurrentBag<string> excludeReplays = [];

    public EventHandler<DecodeEventArgs>? DecodeFinished;

    private async void OnDecodeFinished(DecodeEventArgs e)
    {
        var httpClient = httpClientFactory.CreateClient("callback");
        try
        {
            var result = await httpClient.PostAsJsonAsync($"/api8/v1/upload/decoderesult/{e.Guid}", e.IhReplays);
            result.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            logger.LogError("failed reporting decoderesult: {error}", ex.Message);
        }
        DecodeFinished?.Invoke(this, e);
    }

    public async Task<int> SaveReplays(Guid guid, List<IFormFile> files)
    {
        int filesSaved = 0;

        try
        {
            foreach (var formFile in files)
            {
                if (formFile.Length > 0)
                {
                    string fileHash;
                    using (var md5 = MD5.Create())
                    {
                        using var stream = formFile.OpenReadStream();
                        fileHash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                    }

                    var destinationFile = Path.Combine(decodeSettings.Value.ReplayFolders.Done, $"{fileHash}.SC2Replay");
                    var todoFile = Path.Combine(decodeSettings.Value.ReplayFolders.ToDo, $"{guid}_{fileHash}.SC2Replay");

                    if (File.Exists(destinationFile))
                    {
                        logger.LogInformation("File {FileName} already exists. Skipping upload.", formFile.FileName);
                        continue;
                    }

                    try
                    {
                        var tmpFile = todoFile + ".tmp";
                        using var fileStream = File.Create(tmpFile);
                        await formFile.CopyToAsync(fileStream);
                        File.Move(tmpFile, todoFile);
                        filesSaved++;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error saving file {FileName}.", formFile.FileName);
                    }
                }
                else
                {
                    logger.LogWarning("File {FileName} is empty and will be skipped.", formFile.FileName);
                }
            }

            _ = Decode();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error in SaveReplays.");
            return -1;
        }

        logger.LogInformation("{FilesSaved} files saved for GUID {Guid}.", filesSaved, guid);
        return filesSaved;
    }


    public async Task Decode()
    {
        Interlocked.Increment(ref queueCount);
        await ss.WaitAsync();
        ConcurrentDictionary<Guid, ConcurrentBag<IhReplay>> replays = [];
        string? error = null;

        try
        {
            var replayPaths = Directory.GetFiles(Path.Combine(decodeSettings.Value.ReplayFolders.ToDo), "*SC2Replay");
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

            await foreach (var result in
                replayDecoder.DecodeParallelWithErrorReport(replayPaths, decodeSettings.Value.Threads, options))
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
                var destination = Path.Combine(decodeSettings.Value.ReplayFolders.Done,
                    Path.GetFileNameWithoutExtension(result.ReplayPath)[..36] +
                    "_" +
                    replayDto.ReplayHash +
                    Path.GetExtension(result.ReplayPath));
                await fileSemaphore.WaitAsync();
                try
                {
                    if (!File.Exists(destination))
                    {
                        File.Move(result.ReplayPath, destination);
                        var groupId = GetGroupIdFromFilename(result.ReplayPath);
                        var ihReplay = new IhReplay() { Replay = replayDto, Metadata = metaData };
                        replays.AddOrUpdate(groupId, [ihReplay], (k, v) => { v.Add(ihReplay); return v; });
                    }
                }
                finally
                {
                    fileSemaphore.Release();
                }
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
            foreach (var ent in replays)
            {
                OnDecodeFinished(new()
                {
                    Guid = ent.Key,
                    IhReplays = [.. ent.Value],
                    Error = error,
                });
            }
            Interlocked.Decrement(ref queueCount);
        }
    }

    private void Error(DecodeParallelResult result)
    {
        logger.LogError("failed decoding replay: {path}, {error}", result.ReplayPath, result.Exception);
        try
        {
            File.Move(result.ReplayPath, Path.Combine(decodeSettings.Value.ReplayFolders.Error, Path.GetFileName(result.ReplayPath)));
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

