using System.Net.Http.Json;
using dsstats.shared;
using dsstats.shared.Interfaces;
using dsstats.shared.PatchNotes;

namespace dsstats.apiServices;

public sealed class PatchNotesService(IHttpClientFactory httpClientFactory) : IPatchNotesService
{
    private readonly HttpClient httpClient = httpClientFactory.CreateClient("api");

    public async Task<PatchNotesPage> GetPatchNotes(PatchNotesRequest request, CancellationToken token = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api10/PatchNotes", request, token);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PatchNotesPage>(cancellationToken: token)
            ?? new PatchNotesPage();
    }

    public async Task<IReadOnlyList<string>> GetUnitNames(Commander commander, CancellationToken token = default)
    {
        return await httpClient.GetFromJsonAsync<string[]>(
            $"api10/PatchNotes/units?commander={(int)commander}", token) ?? [];
    }
}
