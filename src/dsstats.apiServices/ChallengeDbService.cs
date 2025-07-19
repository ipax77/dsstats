using System.Net.Http.Json;
using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.Extensions.Logging;

public class ChallengeDbService(HttpClient httpClient, ILogger<ChallengeDbService> logger) : IChallengeDbService
{
    private readonly string challengeController = "api8/v1/challenge";

    public async Task<ChallengeDto?> GetActiveChallenge()
    {
        try
        {
            return await httpClient.GetFromJsonAsync<ChallengeDto?>($"{challengeController}/active");
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to get active challenge: {Message}", ex.Message);
            return null;
        }
    }

    public async Task<ChallengeSubmissionDto?> GetChallengeSubmission(int spChallengeSubmissionId)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<ChallengeSubmissionDto?>(
                $"{challengeController}/submission/{spChallengeSubmissionId}");
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to get submission {Id}: {Message}", spChallengeSubmissionId, ex.Message);
            return null;
        }
    }

    public async Task<List<ChallengeSubmissionListDto>> GetChallengeSubmissions(int spChallengeId)
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<List<ChallengeSubmissionListDto>>(
                $"{challengeController}/{spChallengeId}/submissions");

            return result ?? new();
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to get submissions for challenge {Id}: {Message}", spChallengeId, ex.Message);
            return new();
        }
    }

    public async Task<List<FinishedChallengeDto>> GetFinishedChallenges()
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<List<FinishedChallengeDto>>(
                $"{challengeController}/finished");

            return result ?? new();
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to get finished challenges: {Message}", ex.Message);
            return new();
        }
    }

    public async Task<List<PlayerRankingDto>> GetOverallPlayerRanking()
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<List<PlayerRankingDto>>(
                $"{challengeController}/rankings");

            return result ?? new();
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to get player rankings: {Message}", ex.Message);
            return new();
        }
    }

    public async Task<bool> SaveSubmission(ChallengeResponse response, int spChallengeId)
    {
        try
        {
            var res = await httpClient.PostAsJsonAsync($"{challengeController}/{spChallengeId}/submit", response);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to submit challenge response: {Message}", ex.Message);
            return false;
        }
    }

    public Task FinishChallenge(CancellationToken token)
    {
        throw new NotImplementedException();
    }
}
