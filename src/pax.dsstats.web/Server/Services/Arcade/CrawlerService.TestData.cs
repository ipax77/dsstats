using pax.dsstats.shared.Arcade;
using System.Dynamic;
using System.Text.Json;

namespace pax.dsstats.web.Server.Services.Arcade;

public partial class CrawlerService
{
    public async Task CreateTestData()
    {
        var httpClient = httpClientFactory.CreateClient("sc2arcardeClient");

        CrawlInfo crawlInfo = new(2, 140436, "2-S2-1-226401", false);

        string baseRequest =
        $"lobbies/history?regionId={crawlInfo.RegionId}&mapId={crawlInfo.MapId}&profileHandle={crawlInfo.Handle}&orderDirection=desc&includeMapInfo=false&includeSlots=true&includeSlotsProfile=true&includeMatchResult=true&includeMatchPlayers=true";

        for (int i = 0; i < 20; i++)
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
            }
        }

        var json = JsonSerializer.Serialize(crawlInfo);
        File.WriteAllText("/data/ds/arcadetestdata.json", json);
    }
}
