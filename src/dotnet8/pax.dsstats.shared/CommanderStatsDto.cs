namespace pax.dsstats.shared;

public record CommanderStatsDto
{
    public Commander Commander { get; set; }
    public int Matchups { get; set; }
    public int Wins { get; set; }
    public int Bans { get; set; }
    public float Winrate => Matchups == 0 ? 0 : MathF.Round(Wins * 100f / (float)Matchups, 2);
}
