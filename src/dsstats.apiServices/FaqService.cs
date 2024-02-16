using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace dsstats.apiServices;

public class FaqService(HttpClient httpCLient, ILogger<FaqService> logger) : IFaqService
{
    private readonly string faqController = "api8/v1/faq";

    public async Task<int> CreateFaq(FaqDto faqDto, string? name)
    {
        try
        {
            var response = await httpCLient.PostAsJsonAsync($"{faqController}/create", new { faqDto, name });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<int>();
        }
        catch (Exception ex)
        {
            logger.LogError("failed creating faq: {error}", ex.Message);
        }
        return 0;
    }

    public async Task<bool> DeleteFaq(int faqId)
    {
        try
        {
            var response = await httpCLient.DeleteAsync($"{faqController}/{faqId}");
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError("Failed deleting FAQ: {error}", ex.Message);
        }
        return false;
    }

    public async Task<int> GetCount(FaqRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpCLient.GetAsync($"{faqController}/count");
            return await response.Content.ReadFromJsonAsync<int>();
        }
        catch (Exception ex)
        {
            logger.LogError("Failed getting FAQ count: {error}", ex.Message);
            throw;
        }
    }

    public async Task<List<FaqDto>> GetList(FaqRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpCLient.PostAsJsonAsync($"{faqController}", request, token);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<FaqDto>>(token) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError("Failed getting FAQ list: {error}", ex.Message);
            throw;
        }
    }

    public async Task<bool> UpdateFaq(FaqDto faqDto, string? name)
    {
        try
        {
            var response = await httpCLient.PutAsJsonAsync($"{faqController}", new { faqDto, name });
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError("Failed updating FAQ: {error}", ex.Message);
        }
        return false;
    }

    public async Task<bool> Upvote(int faqId, string clientIp)
    {
        try
        {
            var response = await httpCLient.GetAsync($"{faqController}/upvote/{faqId}");
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError("Failed upvoting FAQ: {error}", ex.Message);
        }
        return false;
    }
}
