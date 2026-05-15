namespace dsstats.shared.InHouse;

public sealed class InHouseGameSessionListDto
{
    public Guid SessionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public string CreatedByDisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int Games { get; set; }
    public int Players { get; set; }
    public bool IsClosed => ClosedAt is not null;
}

public sealed class InHouseGameSessionDetailDto
{
    public Guid SessionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public string CreatedByDisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool CanClose { get; set; }
    public List<InHouseRosterPlayerDto> RosterPlayers { get; set; } = [];
    public List<InHouseGameSessionPlayerSummaryDto> Players { get; set; } = [];
    public List<InHouseGameSessionReplayDto> Replays { get; set; } = [];
}

public sealed class InHouseCreateGameSessionRequest
{
    public string Name { get; set; } = string.Empty;
}

public sealed class InHouseReplayUploadRequest
{
    public ReplayDto Replay { get; set; } = new();
    public List<InHouseReplayObserverDto> Observers { get; set; } = [];
}

public sealed class InHouseReplayObserverDto
{
    public string Name { get; set; } = string.Empty;
    public string? Clan { get; set; }
    public ToonIdDto ToonId { get; set; } = new();
    public int SlotId { get; set; }
}

public sealed class InHouseRosterPlayerDto
{
    public Guid RosterPlayerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ToonIdDto ToonId { get; set; } = new();
    public int? PlayerId { get; set; }
    public double InitialRating { get; set; } = 1000;
    public int JoinedReplayCount { get; set; }
    public bool IsSitter { get; set; }
    public bool IsManual { get; set; }
    public string AddSource { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Observes { get; set; }
    public double Winrate { get; set; }
    public bool PlayedLatestGame { get; set; }
    public bool ObservedLatestGame { get; set; }
    public bool RatingsPending { get; set; }
}

public sealed class InHouseRosterPlayerUpsertRequest
{
    public string Name { get; set; } = string.Empty;
    public ToonIdDto ToonId { get; set; } = new();
    public int? PlayerId { get; set; }
    public double? InitialRating { get; set; }
    public bool IsSitter { get; set; }
}

public sealed class InHouseRosterPlayerSitterRequest
{
    public bool IsSitter { get; set; }
}

public sealed class InHouseGameSessionPlayerSummaryDto
{
    public string Name { get; set; } = string.Empty;
    public ToonIdDto ToonId { get; set; } = new();
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Observes { get; set; }
    public double Winrate { get; set; }
    public double? RatingStart { get; set; }
    public double? RatingEnd { get; set; }
    public double? RatingDelta { get; set; }
    public double? AverageGain { get; set; }
    public bool PlayedLatestGame { get; set; }
    public bool ObservedLatestGame { get; set; }
    public bool RatingsPending { get; set; }
}

public sealed class InHouseGameSessionReplayDto
{
    public string ReplayHash { get; set; } = string.Empty;
    public DateTime Gametime { get; set; }
    public GameMode GameMode { get; set; }
    public int Duration { get; set; }
    public int WinnerTeam { get; set; }
    public List<Commander> CommandersTeam1 { get; set; } = [];
    public List<Commander> CommandersTeam2 { get; set; } = [];
    public double? ExpectedWinProbability { get; set; }
    public int? AvgRating { get; set; }
    public bool RatingsPending { get; set; }
    public List<InHouseGameSessionReplayPlayerDto> Players { get; set; } = [];
}

public sealed class InHouseGameSessionReplayPlayerDto
{
    public string Name { get; set; } = string.Empty;
    public ToonIdDto ToonId { get; set; } = new();
    public bool Observer { get; set; }
    public int TeamId { get; set; }
    public int GamePos { get; set; }
}

public sealed class InHouseParsedReplayDto
{
    public ReplayDto Replay { get; set; } = new();
    public List<InHouseReplayObserverDto> Observers { get; set; } = [];
}

public static class InHouseGameSessionReplayDtoExtensions
{
    public static ReplayListDto ToReplayListDto(this InHouseGameSessionReplayDto ihReplay)
    {
        return new()
        {
            ReplayHash = ihReplay.ReplayHash,
            Gametime = ihReplay.Gametime,
            GameMode = ihReplay.GameMode,
            Duration = ihReplay.Duration,
            WinnerTeam = ihReplay.WinnerTeam,
            CommandersTeam1 = ihReplay.CommandersTeam1,
            CommandersTeam2 = ihReplay.CommandersTeam2,
            Exp2Win = ihReplay.ExpectedWinProbability,
            AvgRating = ihReplay.AvgRating,
        };
    }
}
