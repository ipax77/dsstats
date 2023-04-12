using pax.dsstats.shared.Arcade;
using System.Text;
using System.Text.Json;

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

    public async Task GetLobbyHistory(DateTime tillTime, int fsBreak = 10000)
    {
        var httpClient = httpClientFactory.CreateClient("sc2arcardeClient");

        int waitTime = 40*1000 / 100; // 100 request per 40 sec

        List<LobbyResult> results = new List<LobbyResult>();
        string? next = null;
        string? current = null;

        Dictionary<int, int> mapRegions = new Dictionary<int, int>()
        {
             { 208271, 1 }, // NA
             { 140436, 2 }, // EU
             { 69942, 3 },  // As
            // { 231019, 2 }, // TE EU
            // { 327974, 1 }, // TE NA
        };

        foreach (var mapRegion in mapRegions)
        {
            string baseRequest =
                $"lobbies/history?regionId={mapRegion.Value}&mapId={mapRegion.Key}&profileHandle=PAX&orderDirection=desc&includeMapInfo=true&includeSlots=true&includeMatchResult=true&includeMatchPlayers=true";
                

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

                    var result = await httpClient.GetFromJsonAsync<LobbyHistoryResponse>(request);
                    if (result != null)
                    {
                        results.AddRange(result.Results);
                        next = result.Page.Next;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogInformation($"failed getting lobby result ({next}): {ex.Message}");
                    if (results.Any())
                    {
                        await ImportArcadeReplays(results, (mapRegion.Key == 231019 || mapRegion.Key == 231019));
                        if (results.Last().CreatedAt < tillTime)
                        {
                            break;
                        }
                        results.Clear();
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
                    await ImportArcadeReplays(results, (mapRegion.Key == 231019 || mapRegion.Key == 231019));

                    if (results.Last().CreatedAt < tillTime || i > fsBreak)
                    {
                        break;
                    }
                    results.Clear();
                }
            }

            await ImportArcadeReplays(results, (mapRegion.Key == 231019 || mapRegion.Key == 231019));
            results.Clear();
        }

        logger.LogWarning($"job done.");
    }
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
