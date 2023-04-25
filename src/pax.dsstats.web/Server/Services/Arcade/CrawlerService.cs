using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using pax.dsstats.shared;
using pax.dsstats.shared.Arcade;

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

    public async Task GetLobbyHistory(DateTime tillTime, int fsBreak = 1000)
    {
        var httpClient = httpClientFactory.CreateClient("sc2arcardeClient");

        // int waitTime = 40 * 1000 / 100; // 100 request per 40 sec

        List<CrawlInfo> crawlInfos = new()
        {
            new(regionId: 1, mapId: 208271, handle: "2-S2-1-226401", teMap: false),
            new(2, 140436, "2-S2-1-226401", false),
            new(3, 69942, "2-S2-1-226401", false),
            new(1, 327974, "2-S2-1-226401", true),
            new(2, 231019, "2-S2-1-226401", true),
        };

        foreach (var crawlInfo in crawlInfos)
        {

            // &includeSlotsProfile=true
            string baseRequest =
                $"lobbies/history?regionId={crawlInfo.RegionId}&mapId={crawlInfo.MapId}&profileHandle={crawlInfo.Handle}&orderDirection=desc&includeMapInfo=false&includeSlots=true&includeSlotsProfile=true&includeMatchResult=true&includeMatchPlayers=true";

            int i = 0;
            while (!crawlInfo.Done)
            {
                i++;
                try
                {
                    var request = baseRequest;
                    if (!String.IsNullOrEmpty(crawlInfo.Next))
                    {
                        request += $"&after={crawlInfo.Next}";
                    }

                    var response = await httpClient.GetAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<LobbyHistoryResponse>();
                        if (result != null)
                        {
                            crawlInfo.Results.AddRange(result.Results);
                            crawlInfo.Next = result.Page.Next;
                        }
                        int responseWaitTime = GetWaitTime(response);
                        if (responseWaitTime > 0)
                        {
                            int wait = await Import(crawlInfo, tillTime);
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
                        logger.LogError($"failed getting lobby result ({crawlInfo.Next}): {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"failed getting lobby result ({crawlInfo.Next}): {ex.Message}");
                    await Import(crawlInfo, tillTime);
                }

                logger.LogInformation($"{i} => {crawlInfo.Results.Count}, {crawlInfo.Next}");

                if (crawlInfo.Results.Count > 10000)
                {
                    await Import(crawlInfo, tillTime);
                }

                if (crawlInfo.Next == null)
                {
                    await Import(crawlInfo, tillTime);
                    logger.LogInformation($"breaking {crawlInfo}");
                    break;
                }
            }
            await Import(crawlInfo, tillTime);
            await Task.Delay(3000);
        }

        foreach (var crawlInfo in crawlInfos)
        {
            logger.LogWarning($"{crawlInfo}");
        }

        logger.LogWarning($"job done.");
    }

    private async Task<int> Import(CrawlInfo crawlInfo, DateTime tillTime)
    {
        if (!crawlInfo.Results.Any())
        {
            return 0;
        }

        var start = DateTime.UtcNow;
        if (crawlInfo.Results.Last().CreatedAt < tillTime)
        {
            crawlInfo.Done = true;
        }
        await ImportArcadeReplays(crawlInfo);
        crawlInfo.Results.Clear();
        return (int)(DateTime.UtcNow - start).TotalMilliseconds;
    }

    private static int GetWaitTime(HttpResponseMessage response)
    {
        // Get the rate limit headers
        // int rateLimit = int.Parse(response.Headers.GetValues("x-ratelimit-limit").FirstOrDefault() ?? "0");
        int rateLimitRemaining = int.Parse(response.Headers.GetValues("x-ratelimit-remaining").FirstOrDefault() ?? "0");
        int rateLimitReset = int.Parse(response.Headers.GetValues("x-ratelimit-reset").FirstOrDefault() ?? "0");

        if (rateLimitRemaining > 0)
        {
            return 0;
        }
        else
        {
            return Math.Max(rateLimitReset * 1000, 1000);
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
