using dsstats.dbServices;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;

namespace sc2arcade.crawler;

public partial class CrawlerService : ICrawlerService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<CrawlerService> logger;


    public CrawlerService(IServiceProvider serviceProvider,
                          IHttpClientFactory httpClientFactory,
                          ILogger<CrawlerService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    /// <summary>
    /// Crawl SC2Arcade lobby information from latest to tillTime
    /// </summary>
    /// <param name="tillTime"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task GetLobbyHistory(DateTime tillTime, CancellationToken token)
    {
        var httpClient = httpClientFactory.CreateClient("sc2arcardeClient");

        var startTime = DateTime.UtcNow;

        List<CrawlInfo> crawlInfos =
        [
            new(regionId: 1, mapId: 208271, handle: "2-S2-1-226401", teMap: false),
            new(2, 140436, "2-S2-1-226401", false),
            // new(3, 69942, "2-S2-1-226401", false),
            // new(1, 327974, "2-S2-1-226401", true),
            // new(2, 231019, "2-S2-1-226401", true),
        ];

        foreach (var crawlInfo in crawlInfos)
        {

            // &includeSlotsProfile=true
            string baseRequest =
                $"lobbies/history?regionId={crawlInfo.RegionId}&mapId={crawlInfo.MapId}&profileHandle={crawlInfo.Handle}&orderDirection=desc&includeMapInfo=false&includeSlots=true&includeSlotsProfile=true&includeMatchResult=true&includeMatchPlayers=true";

            while (!crawlInfo.Done && !token.IsCancellationRequested)
            {
                try
                {
                    var requestUri = BuildRequestUri(crawlInfo, baseRequest);
                    var response = await httpClient.GetAsync(requestUri, token);
                    response.EnsureSuccessStatusCode();

                    int waitTime = await HandleResponse(response, crawlInfo, tillTime, token);

                    if (crawlInfo.Results.Count > 10000)
                    {
                        await Import(crawlInfo, tillTime, token);
                    }

                    if (crawlInfo.Next == null)
                    {
                        await Import(crawlInfo, tillTime, token);
                        break;
                    }

                    if (waitTime > 0)
                    {
                        await Task.Delay(waitTime, token);
                    }
                }
                catch (Exception ex)
                {
                    await HandleError(ex, crawlInfo, tillTime, token);
                }
            }

            await Import(crawlInfo, tillTime, token);
            await Task.Delay(3000, token);
        }

        foreach (var crawlInfo in crawlInfos)
        {
            logger.LogWarning("{crawlInfo}", crawlInfo);
        }
        logger.LogWarning("job done.");

        using var scope = serviceProvider.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<IImportService>();
        importService.ClearExistingArcadeReplayKeys();

        var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();
        await ratingService.MatchWithNewArcadeReplays(startTime);
    }

    private static string BuildRequestUri(CrawlInfo crawlInfo, string baseRequest)
    {
        if (!string.IsNullOrEmpty(crawlInfo.Next))
        {
            return baseRequest + $"&after={crawlInfo.Next}";
        }
        return baseRequest;
    }

    private async Task<int> HandleResponse(HttpResponseMessage response, CrawlInfo crawlInfo, DateTime tillTime, CancellationToken token)
    {
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<LobbyHistoryResponse>(cancellationToken: token);
            if (result != null)
            {
                crawlInfo.Results.AddRange(result.Results);
                crawlInfo.Next = result.Page.Next;
            }

            int waitTime = GetWaitTime(response);
            int importDuration = await Import(crawlInfo, tillTime, token);
            return Math.Max(0, waitTime - importDuration);
        }
        else if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return GetWaitTime(response);
        }
        else
        {
            logger.LogError("Failed request ({next}): {statusCode}", crawlInfo.Next, response.StatusCode);
            return 2000; // fallback
        }
    }

    private async Task HandleError(Exception ex, CrawlInfo crawlInfo, DateTime tillTime, CancellationToken token)
    {
        logger.LogError("Failed request ({next}): {error}", crawlInfo.Next, ex.Message);
        await Import(crawlInfo, tillTime, token);
    }

    private async Task<int> Import(CrawlInfo crawlInfo, DateTime tillTime, CancellationToken token)
    {
        if (crawlInfo.Results.Count == 0)
        {
            return 0;
        }

        var start = DateTime.UtcNow;
        if (crawlInfo.Results.Last().CreatedAt < tillTime)
        {
            crawlInfo.Done = true;
        }
        await ImportArcadeReplays(crawlInfo, token);
        crawlInfo.Results.Clear();
        return (int)(DateTime.UtcNow - start).TotalMilliseconds;
    }

    private static int GetWaitTime(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("x-ratelimit-remaining", out var remainValues)
            && response.Headers.TryGetValues("x-ratelimit-reset", out var resetValues)
            && int.TryParse(remainValues.FirstOrDefault(), out int rateLimitRemaining)
            && int.TryParse(resetValues.FirstOrDefault(), out int rateLimitReset))
        {
            if (rateLimitRemaining > 0)
            {
                return 0;
            }
            else
            {
                return rateLimitReset * 1000;
            }
        }

        // no headers → fall back to small wait
        return 2000;
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