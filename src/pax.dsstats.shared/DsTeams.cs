namespace pax.dsstats.shared;
public static class DsTeams
{
    public static readonly List<TeamData> Teams = new List<TeamData>()
    {
        new TeamData() { Name = "3Guys1PC", Group = 1 },
        new TeamData() { Name = "New Horde of Orcs", Group = 1 },
        new TeamData() { Name = "Siege Thanks", Group = 1 },
        new TeamData() { Name = "Team Vodka", Group = 1 },
        new TeamData() { Name = "Team Canada", Group = 1 },
        new TeamData() { Name = "Nekos of Korhal", Group = 1 },
        new TeamData() { Name = "Team BestDS", Group = 1 },
        new TeamData() { Name = "Team Macdonald", Group = 1 },
        new TeamData() { Name = "Give us Zeratul!", Group = 2 },
        new TeamData() { Name = "The Breakfast Club", Group = 2 },
        new TeamData() { Name = "ClanHs", Group = 2 },
        new TeamData() { Name = "SJW's For Life", Group = 2 },
        new TeamData() { Name = "Legendary Outlaws", Group = 2 },
        new TeamData() { Name = "Direkt Win", Group = 2 },
        new TeamData() { Name = "InsaneSmoker’s Ashes", Group = 2 },
        new TeamData() { Name = "The illustrious", Group = 3 },
        new TeamData() { Name = "Team Pliss", Group = 3 },
        new TeamData() { Name = "Florida Men", Group = 3 },
        new TeamData() { Name = "Team Co2", Group = 3 },
        new TeamData() { Name = "SibVir", Group = 3 },
        new TeamData() { Name = "Team Hawaii", Group = 3 },
        new TeamData() { Name = "Hungry hippos", Group = 3 },
        new TeamData() { Name = "Baja Blasted", Group = 4 },
        new TeamData() { Name = "Strike Boys", Group = 4 },
        new TeamData() { Name = "Team Toronto", Group = 4 },
        new TeamData() { Name = "Trooperz", Group = 4 },
        new TeamData() { Name = "Team Meow", Group = 4 },
        new TeamData() { Name = "Team Tychus", Group = 4 },
        new TeamData() { Name = "Peoples", Group = 4 },
    };
}


public enum Tournament
{
    None = 0,
    DSFTWA_Kragh_202202 = 1
}

public record TeamData
{
    public string Name { get; set; } = "";
    public int Round { get; set; }
    public int Group { get; set; }
}
