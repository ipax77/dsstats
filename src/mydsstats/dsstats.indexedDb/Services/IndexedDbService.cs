using dsstats.shared;
using dsstats.shared.Upload;
using Microsoft.JSInterop;

namespace dsstats.indexedDb.Services;

public class IndexedDbService
{
    private Task<IJSObjectReference> _moduleTask;

    public IndexedDbService(IJSRuntime js)
    {
        _moduleTask = js.InvokeAsync<IJSObjectReference>("import", "./_content/dsstats.indexedDb/js/dsstatsDb.js").AsTask();
    }

    public async ValueTask UpsertReplayAsync(string replayHash, ReplayDto replay)
    {
        replay.CompatHash = replayHash;
        var module = await _moduleTask;
        await module.InvokeVoidAsync("saveReplayFull", replayHash, replay,
            new ReplayListPwaDto(replay, replayHash),
            new ReplayMeta() { ReplayHash = replayHash, FilePath = replay.FileName, RegionId = replay.RegionId }
        );
    }

    public async Task<ReplayDto?> GetReplayByHashAsync(string replayHash)
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<ReplayDto?>($"getReplayByHash", replayHash);
    }

    public async Task<int> GetFilteredReplayListsCountAsync(ReplayFilter filter)
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<int>("getFilteredReplayListsCount", filter);
    }

    public async Task<List<ReplayListDto>> GetFilteredReplayListsAsync(ReplayFilter filter)
    {
        var module = await _moduleTask;
        var list = await module.InvokeAsync<List<ReplayListDto>>("getFilteredReplayLists", filter);
        return list;
    }

    public async Task<List<FileInfoRecord>> PickDirectoryInit(int regionId, string startName, string? dirKey, int limit = 100)
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<List<FileInfoRecord>>("pickDirectoryInit", regionId, startName, dirKey, limit);
    }

    public async Task<IJSStreamReference> GetFileContent(string path)
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<IJSStreamReference>("getFileContentStream", path);
    }

    public async Task<ExportReplays> GetExportReplays(int limit)
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<ExportReplays>("exportUnuploadedReplays", limit);
    }

    public async Task<ExportResult> GetExportReplays10(UploadRequestDto request, int limit)
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<ExportResult>("exportUnuploadedReplays10", request, limit);
    }

    public async Task MarkReplaysAsUploaded(List<string> hashes)
    {
        var module = await _moduleTask;
        await module.InvokeVoidAsync("markReplaysAsUploaded", hashes);
    }

    public async Task<HashSet<string>> GetExistingPaths()
    {
        var module = await _moduleTask;
        var metas = await module.InvokeAsync<List<ReplayMeta>>("getAllReplayMatas");
        return metas.Select(s => s.FilePath).ToHashSet();
    }

    public async Task<string> GetBase64String(string content)
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<string>("gzipString", content);
    }

    public async Task<PwaConfig> GetConfig()
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<PwaConfig>("getConfig");
    }

    public async Task SaveConfig(PwaConfig config)
    {
        var module = await _moduleTask;
        await module.InvokeVoidAsync("saveConfig", config);
    }

    public async Task TriggerBackup()
    {
        var module = await _moduleTask;
        await module.InvokeVoidAsync("downloadBackup");
    }

    public async Task RestoreBackup(bool replace = true)
    {
        var module = await _moduleTask;
        await module.InvokeVoidAsync("uploadBackup", replace);
    }

    public async Task<PlayerStatsResponse> GetPlayerStats(PlayerDto player)
    {
        var module = await _moduleTask;
        var clientStats = await module.InvokeAsync<MyPlayerStats>("getPlayerStats", player);
        return clientStats.ToPlayerStatsResponse();
    }

    public async ValueTask<List<string>> GetAllDirectoryHandles()
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<List<string>>("exportAllDirectoryHandles");
    }

    public async ValueTask<bool> DeleteDirectoryHandle(string key)
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<bool>("delDirectoryHandle", key);
    }

    public sealed class FileInfoRecord
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public long LastModified { get; set; }
    }

    private sealed class ReplayListRecord
    {
        public string ReplayHash { get; set; } = "";
        public ReplayListDto Payload { get; set; } = default!;
    }

    private sealed class ReplayListPwaDto : ReplayListDto
    {
        public ReplayListPwaDto(ReplayDto replay, string hash)
        {
            ReplayHash = hash;
            Gametime = replay.Gametime;
            Duration = replay.Duration;
            WinnerTeam = replay.WinnerTeam;
            CommandersTeam1 = replay.Players.OrderBy(o => o.GamePos).Where(x => x.GamePos <= 3).Select(s => s.Race).ToList();
            CommandersTeam2 = replay.Players.OrderBy(o => o.GamePos).Where(x => x.GamePos > 3).Select(s => s.Race).ToList();
            PlayerNames = replay.Players.OrderBy(o => o.GamePos).Select(s => s.Name).ToList();
        }
        public List<string> PlayerNames { get; set; } = [];
    }

    private sealed class ReplayMeta
    {
        public string ReplayHash { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int RegionId { get; set; }
        public int Uploaded { get; set; }
        public bool Skip { get; set; }
    }
}

public sealed class ReplayFilter
{
    public ReplayFilter(ReplaysRequest request)
    {
        Name = request.Name;
        LinkCommanders = request.LinkCommanders;
        TableOrders = request.TableOrders?.Select(s => new TableOrder()
        {
            Name = s.Column,
            Ascending = s.Ascending,
        }).ToList();
        Skip = request.Skip;
        Take = request.Take;
        if (!string.IsNullOrEmpty(request.Commander))
        {
            var commanders = request.Commander.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            Commanders = [];
            foreach (var cmdrString in commanders)
            {
                if (Enum.TryParse(typeof(Commander), cmdrString, out var cmdrObj)
                    && cmdrObj is Commander cmdr)
                {
                    Commanders.Add(cmdr);
                }
            }
        }
    }
    public string? Name { get; set; }
    public List<Commander>? Commanders { get; set; }
    public bool LinkCommanders { get; set; }
    public List<TableOrder>? TableOrders { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
}

public sealed class TableOrder
{
    public string Name { get; set; } = string.Empty;
    public bool Ascending { get; set; }
}

public sealed class ReplayRecord
{
    public string ReplayHash { get; set; } = "";
    public string CreatedUtc { get; set; } = "";
    public ReplayDto Payload { get; set; } = default!;
}

public sealed class ExportReplays
{
    public List<string> Hashes { get; set; } = [];
    public string Payload { get; set; } = string.Empty;
}

public sealed class ExportResult
{
    public List<string> Hashes { get; set; } = [];
    public byte[] Payload { get; set; } = [];
}

public sealed class MyPlayerStats
{
    public PlayerDto Player { get; set; } = new();
    public List<GameModeStats> GameModeStats { get; set; } = new();
    public List<ReplayListDto> RecentReplays { get; set; } = [];
}

public sealed class GameModeStats
{
    public int GameMode { get; set; }
    public List<CommanderStats> CommanderStats { get; set; } = new();
    public List<PlayerStats> TeammateStats { get; set; } = new();
    public List<PlayerStats> OpponentStats { get; set; } = new();
}

public sealed class CommanderStats
{
    public int Commander { get; set; }
    public int Count { get; set; }
    public int Wins { get; set; }
    public int Mvp { get; set; }
}

public sealed class PlayerStats
{
    public PlayerDto Player { get; set; } = new();
    public int Count { get; set; }
    public int Wins { get; set; }
}