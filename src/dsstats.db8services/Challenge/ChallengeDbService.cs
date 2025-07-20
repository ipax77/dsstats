using dsstats.db8;
using dsstats.db8.Challenge;
using dsstats.shared;
using dsstats.shared.DsFen;
using dsstats.shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace dsstats.db8services.Challenge;

public class ChallengeDbService(ReplayContext context, IImportService importService) : IChallengeDbService
{
    public async Task<ChallengeDto?> GetActiveChallenge()
    {
        return await context.SpChallenges
            .Where(c => c.Active)
            .Select(c => new ChallengeDto
            {
                SpChallengeId = c.SpChallengeId,
                GameMode = c.GameMode,
                Commander = c.Commander,
                Fen = c.Fen,
                Base64Image = c.Base64Image,
                Time = c.Time,
                Active = c.Active,
                ArmyValue = c.ArmyValue,
                Desc = c.Desc,
                CreatedAt = c.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<ChallengeSubmissionListDto>> GetChallengeSubmissions(int spChallengeId)
    {
        return await context.SpChallengeSubmissions
            .Where(s => s.SpChallengeId == spChallengeId)
            .Select(s => new ChallengeSubmissionListDto
            {
                SpChallengeSubmissionId = s.SpChallengeSubmissionId,
                Submitted = s.Submitted,
                GameTime = s.GameTime,
                Commander = s.Commander,
                Time = s.Time,
                PlayerName = s.Player!.Name
            })
            .OrderBy(s => s.Time)
            .ToListAsync();
    }

    public async Task<ChallengeSubmissionDto?> GetChallengeSubmission(int spChallengeSubmissionId)
    {
        return await context.SpChallengeSubmissions
            .Where(s => s.SpChallengeSubmissionId == spChallengeSubmissionId)
            .Select(s => new ChallengeSubmissionDto
            {
                Submitted = s.Submitted,
                GameTime = s.GameTime,
                Commander = s.Commander,
                Fen = s.Fen,
                Time = s.Time,
                PlayerName = s.Player!.Name
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> SaveSubmission(ChallengeResponse response, int spChallengeId)
    {
        var challenge = await context.SpChallenges
            .FirstOrDefaultAsync(c => c.SpChallengeId == spChallengeId);

        if (challenge == null)
        {
            return false;
        }

        DsBuildRequest challengeRequest = new();
        DsBuildRequest playerChallengeRequest = new();

        DsFen.ApplyFen(challenge.Fen, out challengeRequest);
        DsFen.ApplyFen(response.ChallengeFen, out playerChallengeRequest);
        if (!FenIsReasonable(challengeRequest, playerChallengeRequest))
        {
            return false;
        }

        var playerId = await importService
        .GetPlayerIdAsync(new(response.RequestName.ToonId, response.RequestName.RealmId, response.RequestName.ToonId), response.RequestName.Name);
        var submission = await context.SpChallengeSubmissions
            .FirstOrDefaultAsync(s => s.SpChallengeId == spChallengeId && s.PlayerId == playerId)
;
        if (submission == null || submission.Commander == response.Commander)
        {
            submission = new SpChallengeSubmission
            {
                Submitted = DateTime.UtcNow,
                GameTime = response.GameTime,
                Commander = response.Commander,
                Fen = response.PlayerFen,
                Time = response.TimeTillVictory,
                SpChallengeId = spChallengeId,
                PlayerId = playerId
            };
            context.SpChallengeSubmissions.Add(submission);
        }
        else
        {
            submission.Submitted = DateTime.UtcNow;
            submission.GameTime = response.GameTime;
            submission.Commander = response.Commander;
            submission.Fen = response.PlayerFen;
            submission.Time = response.TimeTillVictory;
        }
        await context.SaveChangesAsync();
        return true;
    }

    private bool FenIsReasonable(DsBuildRequest challengeRequest, DsBuildRequest playerChallengeRequest)
    {
        foreach (var unit in challengeRequest.Spawn.Units)
        {
            var playerUnit = playerChallengeRequest.Spawn.Units
                .FirstOrDefault(u => u.Unit.Name == unit.Unit.Name);
            if (playerUnit == null || playerUnit.Count != unit.Count)
            {
                return false;
            }
        }
        return true;
    }

    public async Task FinishChallenge(CancellationToken token)
    {
        var inactiveWithoutWinner = await context.SpChallenges
            .Where(c => !c.Active && c.Winner == null)
            .ToListAsync(token);
        foreach (var challenge in inactiveWithoutWinner)
        {
            var bestSubmission = await context.SpChallengeSubmissions
                .Where(s => s.SpChallengeId == challenge.SpChallengeId)
                .OrderBy(s => s.Time)
                    .ThenBy(s => s.GameTime)
                .FirstOrDefaultAsync(token);

            if (bestSubmission != null)
            {
                challenge.Winner = bestSubmission.Player;
            }
        }
        await context.SaveChangesAsync(token);
    }

    public async Task<List<FinishedChallengeDto>> GetFinishedChallenges()
    {
        return await context.SpChallenges
            .Where(c => !c.Active && c.Winner != null)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new FinishedChallengeDto
            {
                SpChallengeId = c.SpChallengeId,
                GameMode = c.GameMode,
                Commander = c.Commander,
                Fen = c.Fen,
                Base64Image = c.Base64Image,
                Time = c.Time,
                CreatedAt = c.CreatedAt,
                WinnerName = c.Winner!.Name,
                WinnerTime = c.SpChallengeSubmissions
                    .OrderBy(s => s.Time)
                    .Select(s => s.Time)
                    .FirstOrDefault(),
                TopSubmissions = c.SpChallengeSubmissions
                    .OrderBy(s => s.Time)
                    .Take(3)
                    .Select(s => new ChallengeSubmissionListDto
                    {
                        SpChallengeSubmissionId = s.SpChallengeSubmissionId,
                        Submitted = s.Submitted,
                        GameTime = s.GameTime,
                        Commander = s.Commander,
                        Time = s.Time,
                        PlayerName = s.Player!.Name
                    })
                    .ToList()
            })
            .ToListAsync();
    }

    public async Task<List<PlayerRankingDto>> GetOverallPlayerRanking()
    {
        const int participationPoints = 1;
        const int winPoints = 10;
        const int secondPlacePoints = 7;
        const int thirdPlacePoints = 5;

        var challenges = await context.SpChallenges
            .Include(c => c.SpChallengeSubmissions)
                .ThenInclude(s => s.Player)
            .Where(x => x.Active == false && x.Winner != null)
            .ToListAsync();

        var playerPoints = new Dictionary<int, PlayerRankingDto>();

        foreach (var challenge in challenges)
        {
            var ordered = challenge.SpChallengeSubmissions.OrderBy(s => s.Time).ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                var sub = ordered[i];
                if (!playerPoints.TryGetValue(sub.PlayerId, out var entry))
                {
                    entry = new PlayerRankingDto
                    {
                        PlayerId = sub.PlayerId,
                        PlayerName = sub.Player!.Name
                    };
                    playerPoints[sub.PlayerId] = entry;
                }

                entry.Submissions += 1;
                entry.TotalPoints += participationPoints;

                if (i == 0)
                {
                    entry.TotalPoints += winPoints;
                    entry.Wins += 1;
                }
                else if (i == 1)
                {
                    entry.TotalPoints += secondPlacePoints;
                    entry.Seconds += 1;
                }
                else if (i == 2)
                {
                    entry.TotalPoints += thirdPlacePoints;
                    entry.Thirds += 1;
                }
            }
        }

        var ranked = playerPoints.Values
            .OrderByDescending(p => p.TotalPoints)
            .ThenBy(p => p.PlayerName)
            .ToList();

        for (int i = 0; i < ranked.Count; i++)
        {
            ranked[i].Rank = i + 1;
        }

        return ranked;
    }

}


