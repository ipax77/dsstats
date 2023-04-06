using dsstats.sc2arcade.api.Models;
using System.Text;
using System.Text.Json;

namespace dsstats.sc2arcade.api.Services;

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

    public async Task GetLobbyHistory(DateTime tillTime)
    {
        var httpClient = httpClientFactory.CreateClient("sc2arcardeClient");

        int waitTime = 40*1000 / 100; // 100 request per 40 sec

        List<LobbyResult> results = new List<LobbyResult>();
        string? next = null;
        string? current = null;

        var regions = new List<string>() { "NA", "EU" };

        foreach (var region in regions)
        {
            string baseRequest = region == "NA" ?
                "lobbies/history?regionId=1&mapId=208271&profileHandle=PAX&orderDirection=desc&includeMapInfo=true&includeSlots=true&includeMatchResult=true&includeMatchPlayers=true"
                : "lobbies/history?regionId=2&mapId=140436&profileHandle=PAX&orderDirection=desc&includeMapInfo=true&includeSlots=true&includeMatchResult=true&includeMatchPlayers=true";

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
                    logger.LogError($"failed getting lobby result ({next}): {ex.Message}");
                    if (results.Any())
                    {
                        await ImportArcadeReplays(results);
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
                    logger.LogWarning($"{i}/100");
                }
                if (results.Count > 10000)
                {
                    await ImportArcadeReplays(results);

                    if (results.Last().CreatedAt < tillTime)
                    {
                        break;
                    }
                    results.Clear();
                }
            }

            await ImportArcadeReplays(results);
        }
        //var json = JsonSerializer.Serialize(results, new JsonSerializerOptions() { WriteIndented = true });
        //File.WriteAllText("/data/ds/sc2arcardeLobbyResults.json", json);

        logger.LogInformation($"job done.");
    }

    public void AnalyizeLobbyHistory(string jsonFile)
    {
        List<LobbyResult> results = JsonSerializer.Deserialize<List<LobbyResult>>(File.ReadAllText(jsonFile)) ?? new();

        if (!results.Any())
        {
            return;
        }

        string gameMode = "3V3";

        results = results.Where(x => x.Match != null && x.Match.ProfileMatches.Count != 0 && x.MapVariantMode == gameMode).ToList();

        DateTime endTime = results.First().CreatedAt;
        DateTime startTime = results.Last().CreatedAt;

        Dictionary<PlayerId, PlayerSuccess> playerResults = new();

        for (int i = 0; i < results.Count; i++)
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            foreach (Models.PlayerResult playerResult in results[i].Match.ProfileMatches)
            {
                PlayerId playerId = new(playerResult.Profile.RegionId, playerResult.Profile.RealmId, playerResult.Profile.ProfileId);

                if (!playerResults.TryGetValue(playerId, out PlayerSuccess? playerSuccess))
                {
                    playerSuccess = new PlayerSuccess()
                    {
                        Name = playerResult.Profile.Name
                    };
                    playerResults[playerId] = playerSuccess;
                }
                playerSuccess.Games++;
                if (playerResult.Decision == "win")
                    playerSuccess.Wins++;
            }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }

        StringBuilder sb = new();
        sb.AppendLine(gameMode);
        sb.AppendLine($"{startTime.ToShortDateString()} - {endTime.ToShortDateString()} - {results.Count} games");
        
        foreach (var success in playerResults.OrderByDescending(o => o.Value.Winrate).Take(20))
        {
            sb.AppendLine($"{success.Value.Name} => {success.Value.Games} ({success.Value.Winrate}%)");
        }

        Console.WriteLine(sb.ToString());
    }

    public void DEBUGJson(string jsonFile)
    {
        var data = File.ReadAllText(jsonFile);
        try
        {
            var response = JsonSerializer.Deserialize<List<LobbyResult>>(data);
        } catch (Exception ex)
        {
            logger.LogError($"{ex.Message}");
        }
        Console.WriteLine("indahouse");
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
