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
        List<SpawnPlaybackTopUnitSummary> topUnits = new(Math.Min(5, killsByPlayerUnit.Count));
        foreach (var pair in killsByPlayerUnit)
        {
            var player = players[pair.Key.PlayerIndex];
            var unitKind = unitKinds[pair.Key.UnitKindIndex];
            if (!ShouldInsertTopUnit(topUnits, player, unitKind, pair.Value))
            {
                continue;
            }

            int insertIndex = GetTopUnitInsertIndex(topUnits, player, unitKind, pair.Value);
            topUnits.Insert(insertIndex, new(
                player.Name,
                player.TeamId,
                player.GamePos,
                unitKind.Name,
                pair.Value));
            if (topUnits.Count > 5)
            {
                topUnits.RemoveAt(5);
            }
        }

        return topUnits;
    }

    private static bool ShouldInsertTopUnit(
        List<SpawnPlaybackTopUnitSummary> topUnits,
        SpawnPlaybackPlayerNg player,
        SpawnPlaybackUnitKindNg unitKind,
        int kills)
    {
        return topUnits.Count < 5
            || CompareTopUnit(kills, player.TeamId, player.GamePos, unitKind.Name, topUnits[^1]) < 0;
    }

    private static int GetTopUnitInsertIndex(
        List<SpawnPlaybackTopUnitSummary> topUnits,
        SpawnPlaybackPlayerNg player,
        SpawnPlaybackUnitKindNg unitKind,
        int kills)
    {
        int index = 0;
        while (index < topUnits.Count
            && CompareTopUnit(
                topUnits[index].Kills,
                topUnits[index].TeamId,
                topUnits[index].GamePos,
                topUnits[index].UnitName,
                kills,
                player.TeamId,
                player.GamePos,
                unitKind.Name) <= 0)
        {
            index++;
        }

        return index;
    }

    private static int CompareTopUnit(
        int leftKills,
        int leftTeamId,
        int leftGamePos,
        string leftUnitName,
        SpawnPlaybackTopUnitSummary right)
    {
        return CompareTopUnit(
            leftKills,
            leftTeamId,
            leftGamePos,
            leftUnitName,
            right.Kills,
            right.TeamId,
            right.GamePos,
            right.UnitName);
    }

    private static int CompareTopUnit(
        int leftKills,
        int leftTeamId,
        int leftGamePos,
        string leftUnitName,
        int rightKills,
        int rightTeamId,
        int rightGamePos,
        string rightUnitName)
    {
        int killsComparison = rightKills.CompareTo(leftKills);
        if (killsComparison != 0)
        {
            return killsComparison;
        }

        int teamComparison = leftTeamId.CompareTo(rightTeamId);
        if (teamComparison != 0)
        {
            return teamComparison;
        }

        int gamePosComparison = leftGamePos.CompareTo(rightGamePos);
        return gamePosComparison != 0
            ? gamePosComparison
            : string.Compare(leftUnitName, rightUnitName, StringComparison.Ordinal);
    }
}
