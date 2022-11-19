
using pax.dsstats.shared;

namespace dsstats.mmr;

public static class MmrService
{
    private static readonly double startMmr = 1000.0;

    public static (Dictionary<int, PlayerRating>, List<ReplayPlayerMmrChange>) GeneratePlayerRatings(List<ReplayDsRDto> replays)
    {
        Random random = new();
        Dictionary<int, PlayerRating> ratings = new();
        List<ReplayPlayerMmrChange> mmrChanges = new();
        for (int i = 0; i < replays.Count; i++)
        {
            var replay = replays[i];

            for (int j = 0; j < replay.ReplayPlayers.Count; j++)
            {
                var replayPlayer = replay.ReplayPlayers[j];
                var mmrId = GetMmrId(replayPlayer.Player);
                float mmrBefore = (float)startMmr;

                var mmrAfter = (float)(random.NextDouble() * 500);

                if (ratings.ContainsKey(mmrId))
                {
                    mmrBefore = ratings[mmrId].Mmr;
                }
                else
                {
                    ratings[mmrId] = new()
                    {
                        PlayerId = replayPlayer.Player.PlayerId,
                        Name = replayPlayer.Player.Name,
                        ToonId = replayPlayer.Player.ToonId,
                        RegionId = replayPlayer.Player.RegionId,
                        Main = Commander.None,
                        MainPercentage = 50.1f,
                        Games = random.Next(2, 1000),
                        Wins = 1,
                        Mvp = 1,
                        TeamGames = 0,
                        MmrGames = 1,
                        Mmr = mmrAfter,
                        Consistency = (float)random.NextDouble(),
                        Uncertainty = (float)random.NextDouble()
                    };
                }
                // todo: yield return
                mmrChanges.Add(new()
                {
                    ReplayPlayerId = replayPlayer.ReplayPlayerId,
                    MmrChange = mmrAfter - mmrBefore
                });
            }
        }
        return (ratings, mmrChanges);
    }

    private static int GetMmrId(PlayerDsRDto player)
    {
        return player.PlayerId;
    }

}