namespace pax.dsstats.shared;

public record CrossTableResponse
{
    public List<TeamCrossTable> TeamCrossTables { get; set; } = new();
}

public record TeamCrossTable
{
    public TeamCmdrs Comp { get; set; } = new();
    public List<TeamResult> TeamResults { get; set; } = new();
}

public record TeamResult
{
    public TeamCmdrs Comp { get; set; } = new();
    public int Count { get; set; }
    public int Wins { get; set; }
}

public record TeamCmdrs
{
    public Commander[] Cmdrs { get; set; } = new Commander[3] { Commander.None, Commander.None, Commander.None };
}
