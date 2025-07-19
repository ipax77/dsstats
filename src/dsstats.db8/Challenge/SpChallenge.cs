using dsstats.shared;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace dsstats.db8.Challenge;

public class SpChallenge
{
    public int SpChallengeId { get; set; }
    public GameMode GameMode { get; set; }
    public Commander Commander { get; set; }
    [MaxLength(1000)]
    public string Fen { get; set; } = string.Empty;
    public string Base64Image { get; set; } = string.Empty;
    public int Time { get; set; }
    public Player? Winner { get; set; }
    public bool Active { get; set; }
    [Precision(0)]
    public DateTime CreatedAt { get; set; }
    public ICollection<SpChallengeSubmission> SpChallengeSubmissions { get; set; } = [];
}

public class SpChallengeSubmission
{
    public int SpChallengeSubmissionId { get; set; }
    [Precision(0)]
    public DateTime Submitted { get; set; }
    [Precision(0)]
    public DateTime GameTime { get; set; }
    public Commander Commander { get; set; }
    [MaxLength(1000)]
    public string Fen { get; set; } = string.Empty;
    public int Time { get; set; }
    public int SpChallengeId { get; set; }
    public SpChallenge? SpChallenge { get; set; }
    public int PlayerId { get; set; }
    public Player? Player { get; set; }
}

