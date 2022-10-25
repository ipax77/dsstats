using pax.dsstats.shared;
using System.Numerics;

namespace pax.dsstats.parser;
public static partial class Parse
{
    public static void SetGamePos(DsReplay replay)
    {
        var playersTeam1 = replay.Players.Where(x => x.Team == 1).ToList();
        var playersTeam2 = replay.Players.Where(x => x.Team == 2).ToList();

        SetPos(replay, playersTeam1);
        SetPos(replay, playersTeam2);

        foreach (var pl in playersTeam2)
        {
            pl.GamePos += 3;
        }
    }

    private static void SetPos(DsReplay replay, List<DsPlayer> teamPlayers)
    {
        if (teamPlayers.Count == 1)
        {
            teamPlayers.First().GamePos = 1;
        }
        else if (teamPlayers.Count == 2)
        {
            var player1 = teamPlayers.First();
            var player2 = teamPlayers.Last();
            var d1 = Vector2.Distance(replay.Layout.Planetary.Vector2, player1.SpawnArea2.South.Vector2);
            var d2 = Vector2.Distance(replay.Layout.Planetary.Vector2, player2.SpawnArea2.South.Vector2);

            if (d1 > d2)
            {
                player1.GamePos = 1;
                player2.GamePos = 2;
            }
            else if (d2 > d1)
            {
                player1.GamePos = 2;
                player2.GamePos = 1;
            }
        }
        else if (teamPlayers.Count == 3)
        {
            var player1 = teamPlayers.First();
            var player2 = teamPlayers.Skip(1).First();
            var player3 = teamPlayers.Last();

            var d1 = Vector2.Distance(replay.Layout.Planetary.Vector2, player1.SpawnArea2.South.Vector2);
            var d2 = Vector2.Distance(replay.Layout.Planetary.Vector2, player2.SpawnArea2.South.Vector2);
            var d3 = Vector2.Distance(replay.Layout.Planetary.Vector2, player3.SpawnArea2.South.Vector2);

            DsPlayer middlePlayer;
            if (d1 < d2 && d1 < d3)
            {
                middlePlayer = player1;
                Set3ManPos(middlePlayer, player2, player3);

            }
            else if (d2 < d1 && d2 < d3)
            {
                middlePlayer = player2;
                Set3ManPos(middlePlayer, player1, player3);

            }
            else if (d3 < d1 && d3 < d2)
            {
                middlePlayer = player3;
                Set3ManPos(middlePlayer, player1, player2);
            }
        }
    }

    private static void Set3ManPos(DsPlayer middlePlayer, DsPlayer player1, DsPlayer player2)
    {
        var dm1 = Vector2.Distance(middlePlayer.SpawnArea2.West.Vector2, player1.SpawnArea2.South.Vector2);
        var dm2 = Vector2.Distance(middlePlayer.SpawnArea2.West.Vector2, player2.SpawnArea2.South.Vector2);

        if (dm1 < dm2)
        {
            middlePlayer.GamePos = 2;
            player1.GamePos = 1;
            player2.GamePos = 3;
        }
        else if (dm2 < dm1)
        {
            middlePlayer.GamePos = 2;
            player1.GamePos = 3;
            player2.GamePos = 1;
        }
    }

}

