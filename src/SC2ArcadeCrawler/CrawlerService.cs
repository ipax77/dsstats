using dsstats.shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Security.Cryptography;

namespace pax.dsstats.web.Server.Services.Arcade;

public partial class CrawlerService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IOptions<DbImportOptions> dbImportOptions;
    private readonly ILogger<CrawlerService> logger;
    private readonly MD5 md5;


    public CrawlerService(IServiceProvider serviceProvider,
                          IHttpClientFactory httpClientFactory,
                          IOptions<DbImportOptions> dbImportOptions,
                          ILogger<CrawlerService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.httpClientFactory = httpClientFactory;
        this.dbImportOptions = dbImportOptions;
        this.logger = logger;
        md5 = MD5.Create();
    }

    public async Task GetLobbyHistory(DateTime tillTime, CancellationToken token, int fsBreak = 1000)
    {
        var httpClient = httpClientFactory.CreateClient("sc2arcardeClient");

        // int waitTime = 40 * 1000 / 100; // 100 request per 40 sec

        List<CrawlInfo> crawlInfos = new()
        {
            new(regionId: 1, mapId: 208271, handle: "2-S2-1-226401", teMap: false),
            new(2, 140436, "2-S2-1-226401", false),
            // new(3, 69942, "2-S2-1-226401", false),
            // new(1, 327974, "2-S2-1-226401", true),
            // new(2, 231019, "2-S2-1-226401", true),
        };

        foreach (var crawlInfo in crawlInfos)
        {

            // &includeSlotsProfile=true
            string baseRequest =
                $"lobbies/history?regionId={crawlInfo.RegionId}&mapId={crawlInfo.MapId}&profileHandle={crawlInfo.Handle}&orderDirection=desc&includeMapInfo=false&includeSlots=true&includeSlotsProfile=true&includeMatchResult=true&includeMatchPlayers=true";

            int i = 0;
            while (!crawlInfo.Done && !token.IsCancellationRequested)
            {
                i++;
                try
                {
                    var request = baseRequest;
                    if (!String.IsNullOrEmpty(crawlInfo.Next))
                    {
                        request += $"&after={crawlInfo.Next}";
                    }

                    var response = await httpClient.GetAsync(request, token);
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<LobbyHistoryResponse>(token);
                        if (result != null)
                        {
                            crawlInfo.Results.AddRange(result.Results);
                            crawlInfo.Next = result.Page.Next;
                        }
                        int responseWaitTime = GetWaitTime(response);
                        if (responseWaitTime > 0)
                        {
                            int wait = await Import(crawlInfo, tillTime, token);
                            responseWaitTime -= wait;
                            if (responseWaitTime > 0)
                            {
                                await Task.Delay(responseWaitTime);
                            }
                        }
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        int responseWaitTime = GetWaitTime(response);
                        if (responseWaitTime > 0)
                        {
                            await Task.Delay(responseWaitTime);
                        }
                    }
                    else
                    {
                        logger.LogError("failed getting lobby result ({next}): {statusCode}", crawlInfo.Next, response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError("failed getting lobby result ({next}): {error}", crawlInfo.Next, ex.Message);
                    await Import(crawlInfo, tillTime, token);
                }

                if (crawlInfo.Results.Count > 10000)
                {
                    await Import(crawlInfo, tillTime, token);
                }

                if (crawlInfo.Next == null)
                {
                    await Import(crawlInfo, tillTime, token);
                    break;
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

    private static int GetWaitTime_(HttpResponseMessage response)
    {
        int rateLimitRemaining = 0;
        int rateLimitReset = 0;
        if (response.Headers.TryGetValues("x-ratelimit-remaining", out var remainValues)
            && int.TryParse(remainValues.FirstOrDefault(), out int _rateLimitRemaining))
        {
            rateLimitRemaining = _rateLimitRemaining;
        }

        if (response.Headers.TryGetValues("x-ratelimit-reset", out var resetValues)
            && int.TryParse(resetValues.FirstOrDefault(), out int _rateLimitReset))
        {
            rateLimitReset = _rateLimitReset;
        }

        if (rateLimitRemaining > 0)
        {
            return 0;
        }
        else
        {
            return Math.Max(rateLimitReset * 1000, 1000);
        }
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
                return rateLimitRemaining * 1000;
            }
        }
        else
        {
            return 2000;
        }
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

public record PlayerId
{
    public PlayerId()
    {

    }

    public PlayerId(int regionId, int realmId, int profileId)
    {
        RegionId = regionId;
        RealmId = realmId;
        ProfileId = profileId;
    }

    public int RegionId { get; set; }
    public int RealmId { get; set; }
    public int ProfileId { get; set; }
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