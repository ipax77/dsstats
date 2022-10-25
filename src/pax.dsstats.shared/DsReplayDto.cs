namespace pax.dsstats.shared;

public record DsReplayListDto
{
    public string ReplayHash { get; set; } = "";
    public DateTime GameTime { get; set; }
    public int Duration { get; set; }
    public GameMode GameMode { get; set; }
    public int WinnerTeam { get; set; }
    public string? WinnerTeamName { get; set; }
    public string? RunnerTeamName { get; set; }
    public List<DsPlayerListDto> PlayerList { get; set; } = new List<DsPlayerListDto>();
    public DsTeamInfoDto? TeamInfo { get; set; }
}

public record DsPlayerListDto
{
    public int GamePos { get; set; }
    public Commander Commander { get; set; }
}

public record DsTeamInfoDto
{
    public string Team1 { get; set; } = "";
    public string Team2 { get; set; } = "";
    public int Round { get; set; }
    public int Group { get; set; }
    public List<Commander> Bans { get; set; } = new List<Commander>();
    public string RoundInfo()
    {
        if (Round > 0)
        {
            return Round switch
            {
                1 => "Ro16",
                2 => "Ro8",
                3 => "Ro4",
                4 => "Ro2",
                _ => "Ro"
            };
        }

        return Group switch
        {
            1 => "Group A",
            2 => "Group B",
            3 => "Group C",
            4 => "Group D",
            5 => "Group E",
            _ => ""
        };
    }

}


public record DsReplayDetailsDto
{
    public string ReplayHash { get; set; } = "";
    public DateTime GameTime { get; set; }
    public int Duration { get; set; }
    public GameMode GameMode { get; set; }
    public int WinnerTeam { get; set; }
    public string? WinnerTeamName { get; set; }
    public string? RunnerTeamName { get; set; }
    public int Cannon { get; set; }
    public int Bunker { get; set; }
    public string Middle { get; set; } = "";
    public DsTeamInfoDto? TeamInfo { get; set; }
    public List<DsPlayerDetailsDto> PlayerList { get; set; } = new List<DsPlayerDetailsDto>();
}

public record DsPlayerDetailsDto
{
    public int GamePos { get; set; }
    public Commander Commander { get; set; }
    public string Name { get; set; } = "";
    public int Duration { get; set; }
    public int APM { get; set; }
    public int Income { get; set; }
    public int Army { get; set; }
    public int Kills { get; set; }
    public int UpgradesSpent { get; set; }
    public string TierUpgrades { get; set; } = "";
    public string Refineries { get; set; } = "";
    public bool Build { get; set; }
    public List<DsBreakpointDto> Breakpoints { get; set; } = new List<DsBreakpointDto>();

    public int Team => GamePos <= 3 ? 1 : 2;
}

public record DsBreakpointDto
{
    public Breakpoint Breakpoint { get; set; }
    public int GasCount { get; set; }
    public int Tier { get; set; }
    public int Income { get; set; }
    public int Army { get; set; }
    public int Kills { get; set; }
    public int UpgradesSpent { get; set; }
    public List<DsUnitDto> Units { get; set; } = new List<DsUnitDto>();
    public List<DsUpgradeDto> Upgrades { get; set; } = new List<DsUpgradeDto>();

    public List<KeyValuePair<string, int>> GetUnitList()
    {
        Dictionary<string, int> unitList = new Dictionary<string, int>();
        foreach (var unit in this.Units)
        {
            unitList[unit.Name] = unit.Positions.Count;
        }
        return unitList.OrderByDescending(s => s.Value).ToList();
    }
}

public record DsUnitDto
{
    public string Name { get; set; } = "";
    public List<Position> Positions { get; set; } = new List<Position>();
}

public record DsUnitDtoBuilder
{
    public string Name { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
}

public record DsUpgradeDto
{
    public string Name { get; set; } = "";
}

