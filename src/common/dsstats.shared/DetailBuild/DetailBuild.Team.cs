namespace dsstats.shared.DetailBuild;

public static partial class DetailBuilds
{
    private static void DetectTeamBuilds(ReplayBuildDetails details)
    {
        var p1 = GetBuild(details, 1);
        var p2 = GetBuild(details, 2);
        var p3 = GetBuild(details, 3);
        var p4 = GetBuild(details, 4);
        var p5 = GetBuild(details, 5);
        var p6 = GetBuild(details, 6);

        AddFirstTeamBuild(details, 1, p1, p2, p3);
        AddFirstTeamBuild(details, 2, p4, p5, p6);
    }

    private static void AddFirstTeamBuild(
        ReplayBuildDetails details,
        int teamId,
        PlayerBuildInfo p1,
        PlayerBuildInfo p2,
        PlayerBuildInfo p3)
    {
        var teamBuild = DetectTeamBuild(p1, p2);
        if (teamBuild != TeamBuild.None)
        {
            details.Add(CreateTeamBuildInfo(teamId, p1, p2, teamBuild));
            return;
        }

        teamBuild = DetectTeamBuild(p2, p3);
        if (teamBuild != TeamBuild.None)
        {
            details.Add(CreateTeamBuildInfo(teamId, p2, p3, teamBuild));
            return;
        }

        teamBuild = DetectTeamBuild(p3, p1);
        if (teamBuild != TeamBuild.None)
        {
            details.Add(CreateTeamBuildInfo(teamId, p3, p1, teamBuild));
        }
    }

    private static PlayerBuildInfo GetBuild(ReplayBuildDetails details, int gamePos)
    {
        foreach (var matchup in details.MatchupInfos)
        {
            if (matchup.P1.GamePos == gamePos)
            {
                return matchup.P1;
            }

            if (matchup.P2.GamePos == gamePos)
            {
                return matchup.P2;
            }
        }

        throw new InvalidOperationException($"Missing build info for game position {gamePos}.");
    }

    private static TeamBuildInfo CreateTeamBuildInfo(
        int teamId,
        PlayerBuildInfo leader,
        PlayerBuildInfo follower,
        TeamBuild teamBuild) =>
        new(
            TeamId: teamId,
            LeaderGamePos: leader.GamePos,
            FollowerGamePos: follower.GamePos,
            TeamBuild: teamBuild,
            TeamBuildName: teamBuild.ToString());

    private static TeamBuild DetectTeamBuild(PlayerBuildInfo leader, PlayerBuildInfo follower)
    {
        if (leader.Commander == Commander.Protoss
            && leader.Build == (int)ProtossBuild.Stalker
            && follower.Commander == Commander.Terran
            && follower.Build == (int)TerranBuild.Bio)
        {
            return TeamBuild.PTStack;
        }

        if (leader.Commander == Commander.Zerg
            && leader.Build == (int)ZergBuild.RoachQueen
            && follower.Commander == Commander.Zerg
            && follower.Build == (int)ZergBuild.Hydras)
        {
            return TeamBuild.ZZStack;
        }

        if (leader.Commander == Commander.Protoss
            && leader.Build == (int)ProtossBuild.Stalker
            && follower.Commander == Commander.Zerg
            && follower.Build == (int)ZergBuild.Zerglings)
        {
            return TeamBuild.PZStack;
        }

        if (leader.Commander == Commander.Terran
            && leader.Build == (int)TerranBuild.Libs
            && follower.Commander == Commander.Zerg
            && follower.Build == (int)ZergBuild.Mutas)
        {
            return TeamBuild.TZStack;
        }

        return TeamBuild.None;
    }
}
