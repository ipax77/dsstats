using dsstats.db;
using dsstats.shared;

namespace dsstats.dbServices;

internal sealed class PlayerReplayData
{
    public string ReplayHash { get; init; } = string.Empty;
    public int ReplayId { get; init; }
    public DateTime Gametime { get; init; }
    public GameMode GameMode { get; init; }
    public int Duration { get; init; }
    public int WinnerTeam { get; init; }
    public List<PlayerReplayParticipantData> Players { get; init; } = [];

}

internal sealed class PlayerReplayRatingData
{
    public int ReplayId { get; init; }
    public LeaverType LeaverType { get; init; }
    public double ExpectedWinProbability { get; init; }
    public int AvgRating { get; init; }
    public List<PlayerReplayParticipantRatingData> PlayerRatings { get; init; } = [];
}

internal sealed class PlayerReplayParticipantData
{
    public int GamePos { get; init; }
    public Commander Race { get; init; }
    public int PlayerId { get; init; }
    public ToonId ToonId { get; init; } = new();
    public int TeamId { get; init; }
}

internal sealed class PlayerReplayParticipantRatingData
{
    public int PlayerId { get; init; }
    public double RatingBefore { get; init; }
    public double RatingDelta { get; init; }
    public int Games { get; init; }
}
