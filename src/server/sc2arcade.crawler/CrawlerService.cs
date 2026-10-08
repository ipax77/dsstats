using dsstats.dbServices;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace sc2arcade.crawler;

public partial class CrawlerService : ICrawlerService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<CrawlerService> logger;
    private readonly Sc2ArcadeRequestGate gate;

    public CrawlerService(IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory,
        ILogger<CrawlerService> logger, Sc2ArcadeRequestGate? requestGate = null)
    {
        this.serviceProvider = serviceProvider;
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
        gate = requestGate ?? new();
    }

    /// <summary>Crawl the UTC five-day window, stopping all regions on the first failure.</summary>
    public async Task GetLobbyHistory(DateTime tillTime, CancellationToken token)
    {
        await gate.RunLock.WaitAsync(token);
        var startTime = DateTime.UtcNow.AddSeconds(-1);
        List<CrawlInfo> regions = [new(1, 208271, "2-S2-1-226401", false), new(2, 140436, "2-S2-1-226401", false)];
        bool failed = false;
        try
        {
            using var client = httpClientFactory.CreateClient("sc2arcardeClient");
            foreach (var region in regions)
            {
                if (failed) { region.StopReason = "SkippedAfterFailure"; continue; }
                var seen = new HashSet<string>();
                while (!region.Done)
                {
                    token.ThrowIfCancellationRequested();
                    await gate.WaitAsync(token);
                    try
                    {
                        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                        timeout.CancelAfter(TimeSpan.FromSeconds(60));
                        using var response = await client.GetAsync(BuildRequestUri(region), HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                        gate.Observe(response);
                        if (!response.IsSuccessStatusCode)
                        {
                            region.StopReason = "HttpError";
                            logger.LogWarning("SC2Arcade HTTP {Status}: region={Region}, map={Map}, next={Next}; stopping ALL regions without retry; notBefore={NotBefore}, headers={Headers}",
                                (int)response.StatusCode, region.RegionId, region.MapId, region.Next, gate.NotBefore,
                                JsonSerializer.Serialize(Sc2ArcadeRequestPolicy.SelectHeaders(response)));
                            failed = true;
                        }
                        else
                        {
                            string body = await response.Content.ReadAsStringAsync(timeout.Token);
                            if (body.TrimStart().StartsWith('<')) throw new InvalidDataException("HTML challenge response.");
                            using var doc = JsonDocument.Parse(body);
                            if (doc.RootElement.ValueKind != JsonValueKind.Object
                                || !doc.RootElement.TryGetProperty("results", out var rows) || rows.ValueKind != JsonValueKind.Array
                                || !doc.RootElement.TryGetProperty("page", out var page) || page.ValueKind != JsonValueKind.Object
                                || !page.TryGetProperty("next", out var next) || next.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                                throw new InvalidDataException("Missing results or pagination.");
                            var data = JsonSerializer.Deserialize<LobbyHistoryResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
                            if (data.Page.Next != null && (data.Page.Next.Length == 0 || data.Page.Next == region.Next || seen.Contains(data.Page.Next)))
                                throw new InvalidDataException("Repeated pagination cursor.");
                            // Import before advancing the cursor, so a failed write never skips a page.
                            region.Results = data.Results.ToList();
                            await ImportArcadeReplays(region, token);
                            region.Results.Clear();
                            seen.Add(region.Next ?? "");
                            region.Next = data.Page.Next;
                            region.Pages++;
                            region.Lobbies += data.Results.Count;
                            if (data.Results.Any(r => r.CreatedAt <= tillTime)) region.StopReason = "DateCutoffReached";
                            else if (data.Results.Count == 0 || region.Next == null) region.StopReason = "EndOfHistory";
                            region.Done = region.StopReason != null;
                        }
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
                    catch (Exception ex)
                    {
                        region.StopReason = ex is JsonException or InvalidDataException ? "InvalidResponse" : "RequestOrImportError";
                        logger.LogError(ex, "SC2Arcade stopped ALL regions: region={Region}, map={Map}, next={Next}", region.RegionId, region.MapId, region.Next);
                        failed = true;
                    }
                    if (failed) { region.RequestFailures++; region.Done = true; }
                }
            }
            using var scope = serviceProvider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IRatingService>().MatchWithNewArcadeReplays(startTime);
        }
        finally
        {
            foreach (var region in regions)
                logger.LogWarning("SC2Arcade crawl finished: region={Region}, map={Map}, stopReason={Reason}, pages={Pages}, lobbies={Lobbies}, submittedForImport={Imports}, winnerTeamErrors={Errors}, requestFailures={Failures}, next={Next}",
                    region.RegionId, region.MapId, region.StopReason ?? "Cancelled", region.Pages, region.Lobbies, region.Imports, region.Errors, region.RequestFailures, region.Next);
            try
            {
                using var scope = serviceProvider.CreateScope();
                scope.ServiceProvider.GetRequiredService<IImportService>().ClearExistingArcadeReplayKeys();
            }
            finally { gate.RunLock.Release(); }
        }
    }

    private static string BuildRequestUri(CrawlInfo region)
    {
        string uri = $"lobbies/history?regionId={region.RegionId}&mapId={region.MapId}&profileHandle={Uri.EscapeDataString(region.Handle)}&orderDirection=desc&includeMapInfo=false&includeSlots=true&includeSlotsProfile=true&includeMatchResult=true&includeMatchPlayers=true&limit=200";
        return region.Next is null ? uri : uri + "&after=" + Uri.EscapeDataString(region.Next);
    }
}
public record CrawlInfo
{
    public CrawlInfo() { }

    public CrawlInfo(int regionId, int mapId, string handle, bool teMap)
    {
        RegionId = regionId;
        MapId = mapId;
        Handle = handle;
        TeMap = teMap;
    }
    public int RegionId { get; init; }
    public int MapId { get; init; }
    public string Handle { get; init; } = string.Empty;
    public bool TeMap { get; init; }
    public int Dups { get; set; }
    public int Imports { get; set; }
    public int Errors { get; set; }
    public string? Next { get; set; }
    public List<LobbyResult> Results { get; set; } = new();
    public bool Done { get; set; }
    public string? StopReason { get; set; }
    public int Pages { get; set; }
    public int Lobbies { get; set; }
    public int RequestFailures { get; set; }
}

public record PlayerSuccess
{
    public string Name { get; set; } = string.Empty;
    public int Games { get; set; }
    public int Wins { get; set; }
    public double Winrate => Games == 0 ? 0 : Math.Round(Wins * 100.0 / (double)Games, 2);
}

public record LobbyHistoryResponse
{
    public ResponsePage Page { get; set; } = new();
    public List<LobbyResult> Results { get; set; } = new();
}

public record ResponsePage
{
    public string? Prev { get; set; }
    public string? Next { get; set; }
}

public record LobbyResult
{
    public int Id { get; set; }

    public int RegionId { get; set; }

    public long BnetBucketId { get; set; }

    public long BnetRecordId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public string Status { get; set; } = string.Empty;

    public int MapBnetId { get; set; }

    public int? ExtModBnetId { get; set; }

    public int? MultiModBnetId { get; set; }

    public int? MapVariantIndex { get; set; }

    public string MapVariantMode { get; set; } = string.Empty;

    public string LobbyTitle { get; set; } = string.Empty;

    public string HostName { get; set; } = string.Empty;

    public int? SlotsHumansTotal { get; set; }

    public int? SlotsHumansTaken { get; set; }

    public Match? Match { get; set; }

    // public Map? Map { get; set; } = new();

    public object? ExtMod { get; set; }

    public object? MultiMod { get; set; }

    public List<Slot> Slots { get; set; } = new();
}

public record Slot
{
    public int? SlotNumber { get; set; }

    public int? Team { get; set; }

    public string Kind { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public PlayerProfile? Profile { get; set; }
}

public record Match
{
    public int Result { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<ArcadePlayerResult> ProfileMatches { get; set; } = new();
}

public record ArcadePlayerResult
{
    public string Decision { get; set; } = string.Empty;
    public PlayerProfile Profile { get; set; } = new();
}

public record PlayerProfile
{
    public int RegionId { get; set; }
    public int RealmId { get; set; }
    public int ProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Discriminator { get; set; }
    public string? Avatar { get; set; }
}
