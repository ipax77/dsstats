namespace dsstats.shared.Interfaces;
public interface IChallengeDbService
{
    Task FinishChallenge(CancellationToken token);
    Task<ChallengeDto?> GetActiveChallenge();
    Task<ChallengeSubmissionDto?> GetChallengeSubmission(int spChallengeSubmissionId);
    Task<List<ChallengeSubmissionListDto>> GetChallengeSubmissions(int spChallengeId);
    Task<List<FinishedChallengeDto>> GetFinishedChallenges();
    Task<List<PlayerRankingDto>> GetOverallPlayerRanking();
    Task<bool> SaveSubmission(ChallengeResponse response, int spChallengeId);
}
