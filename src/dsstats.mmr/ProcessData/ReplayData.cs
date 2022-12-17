using pax.dsstats.shared;

namespace dsstats.mmr.ProcessData;

public record ReplayData
{
    public ReplayData(ReplayDsRDto replay)
    {
        GameTime = replay.GameTime;
        Duration = replay.Duration;
        Maxleaver = replay.Maxleaver;
        Maxkillsum= replay.Maxkillsum;

        WinnerTeamData = new(replay, replay.ReplayPlayers.Where(x => x.Team == replay.WinnerTeam), true);
        LoserTeamData = new(replay, replay.ReplayPlayers.Where(x => x.Team != replay.WinnerTeam), false);
    }
    public ReplayData(DateTime gameTime,
                      int duration,
                      int maxLeaver,
                      int maxkillsum,
                      double confidence,
                      TeamData winnerTeamData,
                      TeamData loserTeamData)
    {
        GameTime = gameTime;
        Duration = duration;
        Maxleaver = maxLeaver;
        Maxkillsum = maxkillsum;
        Confidence = confidence;
        WinnerTeamData = winnerTeamData;
        LoserTeamData = loserTeamData;
    }

    public DateTime GameTime { get; init; }
    public int Duration { get; init; }
    public int Maxleaver { get; init; }
    public int Maxkillsum { get; init; }

    public TeamData WinnerTeamData { get; init; }
    public TeamData LoserTeamData { get; init; }

    public double Confidence { get; set; }
}
