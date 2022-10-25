namespace pax.dsstats.shared;

public record BuildResponse
{
    public Commander Interest { get; set; }
    public Commander Versus { get; set; }
    public int Count { get; set; }
    public int Duration { get; set; }
    public int Gas { get; set; }
    public int Wins { get; set; }
    public int Upgrades { get; set; }
    public List<BuildResponseBreakpoint> Breakpoints { get; set; } = new();
    public List<BuildResponseReplay> Replays { get; set; } = new();
}

public record BuildResponseBreakpoint
{
    public string Breakpoint { get; set; } = "ALL";
    public int Count { get; set; }
    public int Duration { get; set; }
    public int Gas { get; set; }
    public int Wins { get; set; }
    public int Upgrades { get; set; }
    public List<BuildResponseBreakpointUnit> Units { get; set; } = new();

}

public record BuildResponseBreakpointUnit
{
    public string Name { get; set; } = null!;
    public int Count { get; set; }
}

public record BuildResponseReplay
{
    public string Hash { get; set; } = null!;
    public DateTime Gametime { get; set; }
}
