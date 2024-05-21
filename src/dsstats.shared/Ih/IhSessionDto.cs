namespace dsstats.shared;

public record IhSessionListDto
{
    public Guid GroupId { get; set; }
    public RatingType RatingType { get; set; }
    public DateTime Created { get; set; }
    public int Players { get; set; }
    public int Games { get; set; }
    public bool Closed { get; set; }
}

public record IhSessionDto
{
    public Guid GroupId { get; set; }
    public RatingType RatingType { get; set; }
    public DateTime Created { get; set; }
    public int Players { get; set; }
    public int Games { get; set; }
    public bool Closed { get; set; }
    public List<IhSessionPlayerDto> IhSessionPlayers { get; set; } = [];
}

public record IhSessionPlayerDto
{
    public string Name { get; set; } = string.Empty;
    public int Games { get; set; }
    public int Wins { get; set; }
    public int Obs { get; set; }
    public int RatingStart { get; set; }
    public int RatingEnd { get; set; }
    public int Performance { get; set; }
}
