namespace sc2dsstats.shared
{
    public class DsBuildResponse
    {
        public string Interest { get; set; } = "Abathur";
        public string? Versus { get; set; }
        public int Count { get; set; }
        public int Duration { get; set; }
        public int Gas { get; set; }
        public int Wins { get; set; }
        public int Upgrades { get; set; }
        public List<DsBuildResponseBreakpoint> Breakpoints { get; set; } = new();
        public List<DsBuildResponseReplay> Replays { get; set; } = new();
    }

    public class DsBuildResponseBreakpoint
    {
        public string Breakpoint { get; set; } = "MIN5";
        public int Count { get; set; }
        public int Duration { get; set; }
        public int Gas { get; set; }
        public int Wins { get; set; }
        public int Upgrades { get; set; }
        public List<DsBuildResponseBreakpointUnit> Units { get; set; } = new();

    }

    public class DsBuildResponseBreakpointUnit
    {
        public string Name { get; set; } = "Marine";
        public int Count { get; set; }
    }

    public class DsBuildResponseReplay
    {
        public string Hash { get; set; } = "";
        public DateTime Gametime { get; set; }
    }
}
