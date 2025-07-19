using dsstats.shared;
using dsstats.shared.Interfaces;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace dsstats.api.Controllers;

[EnableCors("dsstatsOrigin")]
[ApiController]
[Route("api8/v1/[controller]")]
public class ChallengeController(IChallengeDbService challengeDbService) : Controller
{
    [HttpGet("active")]
    public async Task<ActionResult<ChallengeDto>> GetActiveChallenge()
    {
        var challenge = await challengeDbService.GetActiveChallenge();
        if (challenge == null)
            return NotFound();

        return Ok(challenge);
    }

    [HttpGet("finished")]
    public async Task<ActionResult<List<FinishedChallengeDto>>> GetFinishedChallenges()
    {
        var challenges = await challengeDbService.GetFinishedChallenges();
        return Ok(challenges);
    }

    [HttpGet("rankings")]
    public async Task<ActionResult<List<PlayerRankingDto>>> GetPlayerRankings()
    {
        var rankings = await challengeDbService.GetOverallPlayerRanking();
        return Ok(rankings);
    }

    [HttpGet("{spChallengeId}/submissions")]
    public async Task<ActionResult<List<ChallengeSubmissionListDto>>> GetChallengeSubmissions(int spChallengeId)
    {
        var submissions = await challengeDbService.GetChallengeSubmissions(spChallengeId);
        return Ok(submissions);
    }

    [HttpGet("submission/{spChallengeSubmissionId}")]
    public async Task<ActionResult<ChallengeSubmissionDto>> GetChallengeSubmission(int spChallengeSubmissionId)
    {
        var submission = await challengeDbService.GetChallengeSubmission(spChallengeSubmissionId);
        if (submission == null)
            return NotFound();

        return Ok(submission);
    }

    [HttpPost("{spChallengeId}/submit")]
    public async Task<ActionResult> SubmitChallengeResponse(int spChallengeId, [FromBody] ChallengeResponse response)
    {
        var success = await challengeDbService.SaveSubmission(response, spChallengeId);
        if (!success)
            return BadRequest("Submission could not be saved.");

        return Ok();
    }

    [HttpPost("finish")]
    public async Task<ActionResult> FinishChallenge(CancellationToken token)
    {
        await challengeDbService.FinishChallenge(token);
        return Ok("Challenge evaluation complete.");
    }
}
