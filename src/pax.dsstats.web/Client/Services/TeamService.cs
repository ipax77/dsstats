using pax.dsstats.shared;
using System.Net.Http.Json;

namespace pax.dsstats.web.Client.Services;

public class TeamService : ITeamService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<TeamService> logger;
    private readonly string statsController = "api/v6/Stats";

    public TeamService(HttpClient httpClient, ILogger<TeamService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<TeamCompResponse> GetTeamRating(TeamCompRequest request, CancellationToken token)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}/teamcomp", request, token);
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<TeamCompResponse>();

            if (data == null)
            {
                logger.LogError($"failed getting teamcomp");
            }
            else
            {
                return data;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"failed getting teamcomp: {ex.Message}");
        }
        return new();
    }
}
