using dsstats.shared;
using dsstats.shared.InHouse;
using dsstats.shared.Upload;
using Microsoft.JSInterop;
using System.Reflection;

namespace dsstats.indexedDb.Services;

public class IndexedDbService
{
    private static readonly string ModuleVersion =
        typeof(IndexedDbService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0]
        ?? typeof(IndexedDbService).Assembly.GetName().Version?.ToString()
        ?? "1.0.0";

    private Task<IJSObjectReference> _moduleTask;

    public IndexedDbService(IJSRuntime js)
    {
        _moduleTask = js.InvokeAsync<IJSObjectReference>(
            "import",
            $"./_content/dsstats.indexedDb/js/dsstatsDb.js?v={Uri.EscapeDataString(ModuleVersion)}").AsTask();
    }

    public async ValueTask UpsertReplayAsync(string replayHash, ReplayDto replay, long size = 0, long lastModified = 0)
    {
        replay.CompatHash = replayHash;
        var module = await _moduleTask;
        await module.InvokeVoidAsync("saveReplayFull", replayHash, replay,
            new ReplayListPwaDto(replay, replayHash),
            new ReplayMeta()
            {
                ReplayHash = replayHash,
                FilePath = replay.FileName,
                RegionId = replay.RegionId,
                Size = size,
                LastModified = lastModified
            }
        );
    }

    public async Task<ReplayDto?> GetReplayByHashAsync(string replayHash)
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<ReplayDto?>($"getReplayByHash", replayHash);
    }

    public async Task SaveReplayRatingAsync(string replayHash, ReplayRatingDto rating)
    {
        var module = await _moduleTask;
        await module.InvokeVoidAsync("saveReplayRating", replayHash, rating);
    }

    public async Task<ReplayRatingDto?> GetReplayRatingAsync(string replayHash)
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<ReplayRatingDto?>("getReplayRating", replayHash);
    }

    public async Task<int> GetFilteredReplayListsCountAsync(ReplayFilter filter)
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<int>("getFilteredReplayListsCount", filter);
    }

    public async Task<int> GetTotalReplayCountAsync()
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<int>("getFilteredReplayListsCount", new { });
    }

    public async Task<List<ReplayListDto>> GetFilteredReplayListsAsync(ReplayFilter filter)
    {
        var module = await _moduleTask;
        var list = await module.InvokeAsync<List<ReplayListDto>>("getFilteredReplayLists", filter);
        return list;
    }

    public async Task<List<FileInfoRecord>> PickDirectoryInit(string startName, string? dirKey, int limit = 100)
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<List<FileInfoRecord>>("pickDirectoryInit", startName, dirKey, limit);
    }

    public async Task<string?> PickDirectoryHandle(string startName)
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<string?>("pickDirectoryHandle", startName);
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

    public async Task<List<TrackedProfileDto>> GetTrackedProfiles()
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<List<TrackedProfileDto>>("getTrackedProfiles");
    }

    public async Task SaveTrackedProfiles(List<TrackedProfileDto> profiles)
    {
        var module = await _moduleTask;
        await module.InvokeVoidAsync("saveTrackedProfiles", profiles);
    }

    public async Task<SessionWindowSettingsDto?> GetSessionWindowSettings()
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<SessionWindowSettingsDto?>("getSessionWindowSettings");
    }

    public async Task SaveSessionWindowSettings(SessionWindowSettingsDto settings)
    {
        var module = await _moduleTask;
        await module.InvokeVoidAsync("saveSessionWindowSettings", settings);
    }

    public async Task<InHouseSessionDto?> GetInHouseSession()
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<InHouseSessionDto?>("getInHouseSession");
    }

    public async Task SaveInHouseSession(InHouseSessionDto session)
    {
        var module = await _moduleTask;
        await module.InvokeVoidAsync("saveInHouseSession", session);
    }

    public async Task ClearInHouseSession()
    {
        var module = await _moduleTask;
        await module.InvokeVoidAsync("clearInHouseSession");
    }

    public async Task<List<ProfileCandidateDto>> DetectTrackedProfileCandidates(int replayLimit = 10)
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<List<ProfileCandidateDto>>("detectTrackedProfileCandidates", replayLimit);
    }

    public async Task<List<string>> GetRecentReplayHashes(int limit)
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<List<string>>("getRecentReplayHashes", limit);
    }

    public async Task<List<string>> GetReplayHashesSince(DateTime fromUtc)
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<List<string>>("getReplayHashesSince", fromUtc.ToUniversalTime().ToString("O"));
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

    public async ValueTask<List<string>> VerifyAllDirectoryPermissions(List<string> keys)
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<List<string>>("verifyAllDirectoryPermissions", keys);
    }

    public async ValueTask<List<DirHandleEntry>> GetAllDirectoryHandleEntries()
    {
        var module = await _moduleTask;
        try
        {
            return await module.InvokeAsync<List<DirHandleEntry>>("exportAllDirectoryHandleEntries");
        }
        catch (JSException ex) when (IsMissingJsFunction(ex, "exportAllDirectoryHandleEntries"))
        {
            var keys = await module.InvokeAsync<List<string>>("exportAllDirectoryHandles");
            return keys.Select(key => new DirHandleEntry
            {
                Key = key,
                DisplayName = key
            }).ToList();
        }
    }

    public async ValueTask RenameDirectoryHandle(string key, string newDisplayName)
    {
        var module = await _moduleTask;
        await module.InvokeVoidAsync("renameDirectoryHandle", key, newDisplayName);
    }

    public async ValueTask<bool> DeleteDirectoryHandle(string key)
    {
        var module = await _moduleTask;
        return await module.InvokeAsync<bool>("delDirectoryHandle", key);
    }

    public sealed class DirHandleEntry
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    public sealed class FileInfoRecord
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public long LastModified { get; set; }
    }

    private static bool IsMissingJsFunction(JSException ex, string functionName)
    {
        return ex.Message.Contains($"'{functionName}' is not a function", StringComparison.Ordinal)
            || ex.Message.Contains($"{functionName} is not a function", StringComparison.Ordinal);
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
            GameMode = replay.GameMode;
            CommandersTeam1 = replay.Players.OrderBy(o => o.GamePos).Where(x => x.GamePos <= 3).Select(s => s.Race).ToList();
            CommandersTeam2 = replay.Players.OrderBy(o => o.GamePos).Where(x => x.GamePos > 3).Select(s => s.Race).ToList();
            PlayerNames = replay.Players.OrderBy(o => o.GamePos).Select(s => s.Name).ToList();
            PlayerCount = replay.Players.Count;
            TournamentEdition = replay.Title.EndsWith("TE", StringComparison.Ordinal);
        }
        public List<string> PlayerNames { get; set; } = [];
        public int PlayerCount { get; set; }
        public bool TournamentEdition { get; set; }
    }

    private sealed class ReplayMeta
    {
        public string ReplayHash { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int RegionId { get; set; }
        public int Uploaded { get; set; }
        public bool Skip { get; set; }
        public long Size { get; set; }
        public long LastModified { get; set; }
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
            Name = ToClientOrderName(s.Column),
            Ascending = s.Ascending,
        }).ToList();
        Skip = ((request.Page - 1) * request.PageSize) + request.Skip;
        Take = request.Take;
        DetailFilter = request.Filter is null ? null : new ReplayDetailFilter(request.Filter);
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
    public ReplayDetailFilter? DetailFilter { get; set; }

    private static string ToClientOrderName(string column) => column switch
    {
        "GameTime" => "gametime",
        "GameMode" => "gameMode",
        "Duration" => "duration",
        "WinnerTeam" => "winnerTeam",
        "Exp2Win" => "exp2Win",
        "AvgRating" => "avgRating",
        "LeaverType" => "leaverType",
        "PlayerPos" => "playerPos",
        _ => column,
    };
}

public sealed class ReplayDetailFilter
{
    public ReplayDetailFilter(ReplaysFilter filter)
    {
        Playercount = filter.Playercount;
        TournamentEdition = filter.TournamentEdition;
        GameModes = filter.GameModes;
        PosFilters = filter.PosFilters.Select(posFilter => new ReplayPosDetailFilter(posFilter)).ToList();
    }

    public int Playercount { get; set; }
    public bool TournamentEdition { get; set; }
    public List<GameMode> GameModes { get; set; } = [];
    public List<ReplayPosDetailFilter> PosFilters { get; set; } = [];
}

public sealed class ReplayPosDetailFilter
{
    public ReplayPosDetailFilter(ReplaysPosFilter filter)
    {
        GamePos = filter.GamePos;
        Commander = filter.Commander;
        OppCommander = filter.OppCommander;
        PlayerNameOrId = filter.PlayerNameOrId;
        UnitFilters = filter.UnitFilters.Select(unitFilter => new ReplayPosUnitDetailFilter(unitFilter)).ToList();
    }

    public int GamePos { set; get; }
    public Commander Commander { get; set; }
    public Commander OppCommander { get; set; }
    public string PlayerNameOrId { get; set; } = string.Empty;
    public List<ReplayPosUnitDetailFilter> UnitFilters { get; set; } = [];
}

public sealed class ReplayPosUnitDetailFilter
{
    public ReplayPosUnitDetailFilter(ReplaysPosUnitFilter filter)
    {
        Breakpoint = filter.Breakpoint;
        Name = filter.Name;
        Count = filter.Count;
        Min = filter.Min;
    }

    public Breakpoint Breakpoint { set; get; } = Breakpoint.All;
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool Min { get; set; } = true;
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
