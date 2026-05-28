using dsstats.shared;

namespace dsstats.play;

public static partial class SpawnPlaybackFactoryNg
{
    private static SpawnPlaybackSummary GetSummary(
        ReplayDto replay,
        IReadOnlyList<SpawnPlaybackPlayerNg> players,
        IReadOnlyList<SpawnPlaybackUnitKindNg> unitKinds,
        Dictionary<PlayerUnitSummaryKey, int> killsByPlayerUnit)
    {
        ReplayPlayerDto[] replayPlayers = new ReplayPlayerDto[replay.Players.Count];
        for (int i = 0; i < replayPlayers.Length; i++)
        {
            replayPlayers[i] = replay.Players[i];
        }

        Array.Sort(replayPlayers, static (left, right) =>
        {
            int teamComparison = left.TeamId.CompareTo(right.TeamId);
            return teamComparison != 0
                ? teamComparison
                : left.GamePos.CompareTo(right.GamePos);
        });

        List<SpawnPlaybackPlayerSummary> playerSummaries = new(replayPlayers.Length);
        int totalKills = 0;
        for (int i = 0; i < replayPlayers.Length; i++)
        {
            var replayPlayer = replayPlayers[i];
            int kills = GetAllKills(replayPlayer);
            totalKills += kills;
            playerSummaries.Add(new(
                replayPlayer.Name,
                replayPlayer.TeamId,
                replayPlayer.GamePos,
                replayPlayer.Race.ToString(),
                kills));
        }

        List<SpawnPlaybackTopUnitSummary> topUnits = GetTopUnitSummaries(players, unitKinds, killsByPlayerUnit);
        return new(totalKills, playerSummaries, topUnits);
    }

    private static int GetAllKills(ReplayPlayerDto replayPlayer)
    {
        for (int i = 0; i < replayPlayer.Spawns.Count; i++)
        {
            var spawn = replayPlayer.Spawns[i];
            if (spawn.Breakpoint == Breakpoint.All)
            {
                return spawn.KilledValue;
            }
        }

        return 0;
    }

    private static List<SpawnPlaybackTopUnitSummary> GetTopUnitSummaries(
        IReadOnlyList<SpawnPlaybackPlayerNg> players,
        IReadOnlyList<SpawnPlaybackUnitKindNg> unitKinds,
        Dictionary<PlayerUnitSummaryKey, int> killsByPlayerUnit)
    {
        List<SpawnPlaybackTopUnitSummary> topUnits = new(killsByPlayerUnit.Count);
        foreach (var pair in killsByPlayerUnit)
        {
            var player = players[pair.Key.PlayerIndex];
            var unitKind = unitKinds[pair.Key.UnitKindIndex];
            topUnits.Add(new(
                player.Name,
                player.TeamId,
                player.GamePos,
                unitKind.Name,
                pair.Value));
        }

        topUnits.Sort(static (left, right) =>
        {
            int killsComparison = right.Kills.CompareTo(left.Kills);
            if (killsComparison != 0)
            {
                return killsComparison;
            }

            int teamComparison = left.TeamId.CompareTo(right.TeamId);
            if (teamComparison != 0)
            {
                return teamComparison;
            }

            int gamePosComparison = left.GamePos.CompareTo(right.GamePos);
            return gamePosComparison != 0
                ? gamePosComparison
                : string.Compare(left.UnitName, right.UnitName, StringComparison.Ordinal);
        });

        if (topUnits.Count > 5)
        {
            topUnits.RemoveRange(5, topUnits.Count - 5);
        }

        return topUnits;
    }
}
