using System.Text.Json;
using sc2arcade.crawler;

namespace sc2arcade.backfill;

public sealed class RegionProgress(Region region)
{
    public int RegionId { get; } = region.RegionId;
    public int MapId { get; } = region.MapId;
    public string? Cursor { get; set; }
    public string? Completion { get; set; }
    public int Pages { get; set; }
    public int Lobbies { get; set; }
    public int EligibleReplays { get; set; }
    public int DuplicateKeys { get; set; }
    public int UnknownWinners { get; set; }
    public DateTime? Oldest { get; set; }
    public DateTime? Newest { get; set; }
    public SortedDictionary<string, int> DailyLobbies { get; } = [];
    public SortedDictionary<string, int> Rejections { get; } = [];
    internal HashSet<string> SeenCursors { get; } = [];
    internal HashSet<ArcadeReplayId> SeenKeys { get; } = [];
}

public sealed class BackfillState
{
    public required ArchiveConfiguration Configuration { get; init; }
    public required RegionProgress[] Regions { get; init; }
    public DateTimeOffset NotBefore { get; private set; }
    public string StopReason { get; private set; } = "Ready";
    public RequestRecord? LastResponse { get; private set; }
    public RequestRecord? PreviousSuccessfulResponse { get; private set; }
    public RequestRecord? LastSuccessfulResponse { get; private set; }
    public RequestRecord? PendingRequest { get; private set; }
    public RegionProgress? Active => Regions.FirstOrDefault(r => r.Completion is null);

    public static BackfillState Read(Archive archive)
    {
        var state = new BackfillState
        {
            Configuration = archive.Configuration,
            Regions = archive.Configuration.Regions.Select(r => new RegionProgress(r)).ToArray()
        };
        foreach (var record in archive.Records.Skip(1)) state.Apply(record);
        return state;
    }

    public void Apply(RequestRecord record)
    {
        if (record.NotBefore is { } deadline)
            NotBefore = record.Kind is "Headers" or "Response" ? deadline : deadline > NotBefore ? deadline : NotBefore;
        if (record.Kind == "Stopped")
        {
            StopReason = record.Outcome ?? "Stopped";
            return;
        }
        var region = Active ?? throw new InvalidDataException("Request after completion.");
        if (record.RegionId != region.RegionId || record.MapId != region.MapId || record.Cursor != region.Cursor)
            throw new InvalidDataException("Record does not match the outstanding region/cursor.");
        if (record.Kind == "Started")
        {
            PendingRequest = record;
            StopReason = "InterruptedRequest";
            return;
        }
        if (record.Kind is not ("Headers" or "Response") || PendingRequest is null
            || record.RequestStartedAt != PendingRequest.At)
            throw new InvalidDataException("Response without matching request.");
        PreviousSuccessfulResponse = LastSuccessfulResponse;
        LastResponse = record;
        if (record.Kind == "Headers")
        {
            StopReason = "InterruptedResponse";
            return;
        }
        PendingRequest = null;
        StopReason = record.Outcome ?? "InvalidResponse";
        if (record.Outcome != "Page") return;
        var page = ParsePage(record.Body!, region.RegionId);
        if (page.Page.Next != record.Next || IsRepeatedCursor(region, page.Page.Next))
            throw new InvalidDataException("Invalid saved page cursor.");
        foreach (var lobby in page.Results)
        {
            region.Lobbies++;
            region.Oldest = region.Oldest is null || lobby.CreatedAt < region.Oldest ? lobby.CreatedAt : region.Oldest;
            region.Newest = region.Newest is null || lobby.CreatedAt > region.Newest ? lobby.CreatedAt : region.Newest;
            Increment(region.DailyLobbies, lobby.CreatedAt.ToString("yyyy-MM-dd"));
            var conversion = ArcadeReplayConverter.Convert(lobby);
            if (conversion.UnknownWinner) region.UnknownWinners++;
            if (conversion.Replay is null) Increment(region.Rejections, conversion.RejectionReason!);
            else
            {
                region.EligibleReplays++;
                if (!region.SeenKeys.Add(new(lobby.RegionId, lobby.BnetBucketId, lobby.BnetRecordId)))
                    region.DuplicateKeys++;
            }
        }
        region.Pages++;
        region.SeenCursors.Add(region.Cursor ?? "");
        region.Cursor = page.Page.Next;
        if (page.Results.Any(l => l.CreatedAt <= Configuration.Cutoff.UtcDateTime))
            region.Completion = "CutoffReached";
        else if (page.Results.Count == 0 || page.Page.Next is null)
            region.Completion = "HistoryExhaustedBeforeCutoff";
        LastSuccessfulResponse = record;
        StopReason = Active is null ? "Complete" : "Ready";
    }

    public static bool IsRepeatedCursor(RegionProgress region, string? next) => next != null
        && (next == region.Cursor || region.SeenCursors.Contains(next));

    public static LobbyHistoryResponse ParsePage(string body, int regionId)
    {
        using var document = JsonDocument.Parse(body);
        // Missing Results must not silently deserialize to an empty history page.
        if (document.RootElement.ValueKind != JsonValueKind.Object
            || !document.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array
            || !document.RootElement.TryGetProperty("page", out var page) || page.ValueKind != JsonValueKind.Object
            || !page.TryGetProperty("next", out var next) || next.ValueKind is not (JsonValueKind.Null or JsonValueKind.String))
            throw new InvalidDataException("Missing results/page in response.");
        var parsed = JsonSerializer.Deserialize<LobbyHistoryResponse>(body, Archive.Json)!;
        if (parsed.Results is null || parsed.Page is null || parsed.Page.Next == "")
            throw new InvalidDataException("Invalid page structure.");
        foreach (var lobby in parsed.Results)
        {
            if (lobby is null || lobby.RegionId != regionId || lobby.CreatedAt == default
                || lobby.CreatedAt.Kind != DateTimeKind.Utc || lobby.Slots is null
                || lobby.Slots.Any(s => s is null) || lobby.Match is { ProfileMatches: null }
                || lobby.Match?.ProfileMatches.Any(p => p is null || p.Profile is null) == true)
                throw new InvalidDataException("Invalid lobby data or non-UTC CreatedAt.");
            // Check conversion before committing progress, including duration overflow.
            _ = ArcadeReplayConverter.Convert(lobby);
        }
        return parsed;
    }

    private static void Increment(IDictionary<string, int> counts, string key) => counts[key] = counts.TryGetValue(key, out var n) ? n + 1 : 1;
}
