using pax.dsstats.shared.Arcade;

namespace pax.dsstats.web.Server.Services.Arcade;

public partial class CrawlerService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ILogger<CrawlerService> logger;

    public CrawlerService(IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory, ILogger<CrawlerService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.httpClientFactory = httpClientFactory;
        this.logger = logger;
    }

    public async Task GetLobbyHistory(DateTime tillTime, int fsBreak = 1000000)
    {
        var httpClient = httpClientFactory.CreateClient("sc2arcardeClient");

        int waitTime = 40*1000 / 100; // 100 request per 40 sec

        List<LobbyResult> results = new List<LobbyResult>();
        string? next = null;
        string? current = null;

        // List<CrawlInfo> crawlInfos = new() 
        // {
        //     new(regionId: 1, mapId: 208271, handle: "1-S2-1-10188255", teMap: false),
        //     new(2, 140436, "2-S2-1-226401", false),
        //     new(3, 69942, "3-S2-1-6360070", false),
        //     new(1, 327974, "1-S2-1-10188255", true),
        //     new(2, 231019, "2-S2-1-226401", true),
        // };

        List<CrawlInfo> crawlInfos = new() 
        {
            new(regionId: 1, mapId: 208271, handle: "2-S2-1-226401", teMap: false),
            new(2, 140436, "1-S2-1-10188255", false),
            new(3, 69942, "2-S2-1-226401", false),
            new(1, 327974, "2-S2-1-226401", true),
            new(2, 231019, "1-S2-1-10188255", true),
        };        

        foreach (var crawlInfo in crawlInfos)
        {
            // &includeSlotsProfile=true
            string baseRequest =
                $"lobbies/history?regionId={crawlInfo.RegionId}&mapId={crawlInfo.MapId}&profileHandle={crawlInfo.Handle}&orderDirection=desc&includeMapInfo=true&includeSlots=true&includeSlotsProfile=true&includeMatchResult=true&includeMatchPlayers=true";
            // $"lobbies/history?regionId={crawlInfo.RegionId}&mapId={crawlInfo.MapId}&profileHandle={crawlInfo.Handle}&orderDirection=desc&includeMapInfo=true&includeSlots=true&includeMatchResult=true&includeMatchPlayers=true";
            // $"lobbies/history?regionId={mapRegion.Value}&mapId={mapRegion.Key}&orderDirection=desc&includeMapInfo=true&includeSlots=true&includeMatchResult=true&includeMatchPlayers=true";
            // $"lobbies/history?regionId={mapRegion.Value}&mapId={mapRegion.Key}&profileHandle=PAX&orderDirection=desc&includeMapInfo=true&includeSlots=true&includeMatchResult=true&includeMatchPlayers=true";


            int i = 0;
            while (true)
            {
                i++;
                try
                {
                    var request = baseRequest;
                    if (!String.IsNullOrEmpty(next))
                    {
                        request += $"&after={next}";
                        if (next == current)
                        {
                            await Task.Delay(waitTime);
                        }
                        current = next;
                    }

                    var response = await httpClient.GetAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<LobbyHistoryResponse>();
                        if (result != null)
                        {
                            results.AddRange(result.Results);
                            next = result.Page.Next;
                        }
                        int responseWaitTime = GetWaitTime(response);
                        if (responseWaitTime > 0)
                        {
                            int wait = 0;
                            if (results.Any())
                            {
                                wait = await Import(results, crawlInfo, tillTime);
                                results.Clear();
                                if (wait < 0)
                                {
                                    break;
                                }
                            }
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
                        logger.LogError($"failed getting lobby result ({next}): {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"failed getting lobby result ({next}): {ex.Message}");
                    if (results.Any())
                    {
                        int wait = await Import(results, crawlInfo, tillTime);
                        results.Clear();
                        if (wait < 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        await Task.Delay(waitTime);
                    }
                }
                finally
                {
                    // await Task.Delay(waitTime);
                    logger.LogInformation($"{i}/100");
                }
                if (results.Count > 10000)
                {
                    int wait = await Import(results, crawlInfo, tillTime);
                    results.Clear();
                    if (wait < 0)
                    {
                        break;
                    }
                }
            }
            await ImportArcadeReplays(results, crawlInfo);
            results.Clear();
        }

        foreach (var crawlInfo in crawlInfos)
        {
            logger.LogWarning($"{crawlInfo}");
        }

        logger.LogWarning($"job done.");
    }

    private async Task<int> Import(List<LobbyResult> results, CrawlInfo crawlInfo, DateTime tillTime)
    {
        var start = DateTime.UtcNow;
        await ImportArcadeReplays(results, crawlInfo);
        if (results.Last().CreatedAt < tillTime)
        {
            return -1;
        }
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
    public CrawlInfo(int regionId, int mapId, string handle, bool teMap)
    {
        RegionId = regionId;
        MapId = mapId;
        Handle = handle;
        TeMap = teMap;
    }
    public int RegionId { get; init; }
    public int MapId { get; init; }
    public string Handle { get; init; }
    public bool TeMap { get; init; }
    public int Dups { get; set; }
    public int Imports { get; set; }
    public int Errors { get; set; }
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

    public PlayerId( int regionId, int realmId, int profileId)
    {
        RegionId = regionId;
        RealmId = realmId;
        ProfileId = profileId;
    }

    public int RegionId { get; set; }
    public int RealmId { get; set; }
    public int ProfileId { get; set; }
}
