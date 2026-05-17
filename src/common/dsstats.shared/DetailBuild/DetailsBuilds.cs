
namespace dsstats.shared.DetailBuild;

public static class DetailBuilds
{
    public static ReplayBuildDetails? DetectStandardBuild(ReplayDto replayDto)
    {
        if (replayDto.GameMode != GameMode.Standard || replayDto.Players.Count != 6 || replayDto.WinnerTeam == 0)
        {
            return null;
        }

        if (replayDto.Players.Any(a => a.Duration < 300))
        {
            return null;
        }

        var details = new ReplayBuildDetails();

        for (int lane = 1; lane <= 3; lane++)
        {
            var p1 = replayDto.Players.FirstOrDefault(f => f.GamePos == lane);
            var p2 = replayDto.Players.FirstOrDefault(f => f.GamePos == lane + 3);

            if (p1 is null || p2 is null)
            {
                return null;
            }

            var p1Build = DetectBuild(p1);
            if (p1Build == null)
            {
                return null;
            }

            var p2Build = DetectBuild(p2);
            if (p2Build is null)
            {
                return null;
            }

            details.Add(new MatchupInfo(
                Lane: lane,
                P1: p1Build,
                P2: p2Build,
                P1Won: p1.Result == PlayerResult.Win,
                P2Won: p2.Result == PlayerResult.Win
            ));
        }

        return details;
    }

    private static PlayerBuildInfo? DetectBuild(ReplayPlayerDto player)
    {
        var spawn = player.Spawns.FirstOrDefault(f => f.Breakpoint == Breakpoint.Min5);
        if (spawn is null)
        {
            return null;
        }

        return player.Race switch
        {
            Commander.Protoss => CreateProtossBuildInfo(player, spawn),
            Commander.Terran => CreateTerranBuildInfo(player, spawn),
            Commander.Zerg => CreateZergBuildInfo(player, spawn),
            _ => new PlayerBuildInfo(
                player.GamePos,
                player.Race,
                0,
                "None"
            )
        };
    }

    private static PlayerBuildInfo CreateTerranBuildInfo(ReplayPlayerDto player, SpawnDto spawn)
    {
        var build = DetectTerranBuild(spawn);

        return new PlayerBuildInfo(
            player.GamePos,
            player.Race,
            (int)build,
            build.ToString()
        );
    }

    private static PlayerBuildInfo CreateProtossBuildInfo(ReplayPlayerDto player, SpawnDto spawn)
    {
        var build = DetectProtossBuild(spawn);

        return new PlayerBuildInfo(
            player.GamePos,
            player.Race,
            (int)build,
            build.ToString()
        );
    }

    private static PlayerBuildInfo CreateZergBuildInfo(ReplayPlayerDto player, SpawnDto spawn)
    {
        var build = DetectZergBuild(spawn);

        return new PlayerBuildInfo(
            player.GamePos,
            player.Race,
            (int)build,
            build.ToString()
        );
    }

    private static int DetectProtossBuild(SpawnDto? spawnDto)
    {
        return (int)ProtossBuild.None;
    }

    private static int DetectTerranBuild(SpawnDto? spawnDto)
    {
        return (int)TerranBuild.None;
    }

    private static int DetectZergBuild(SpawnDto? spawnDto)
    {
        return (int)ZergBuild.None;
    }

    private sealed record PlayerMatchup(ReplayPlayerDto P1, ReplayPlayerDto P2);
}

public sealed class ReplayBuildDetails
{
    private readonly List<MatchupInfo> matchupInfos = [];

    public IReadOnlyList<MatchupInfo> MatchupInfos => matchupInfos;

    public void Add(MatchupInfo matchupInfo)
    {
        matchupInfos.Add(matchupInfo);
    }
}

public sealed record PlayerBuildInfo(
    int GamePos,
    Commander Commander,
    int Build,
    string BuildName
);

public sealed record MatchupInfo(
    int Lane,
    PlayerBuildInfo P1,
    PlayerBuildInfo P2,
    bool P1Won,
    bool P2Won
);

public enum ProtossBuild
{
    None = 0,
    Zealots = 1,
    Stalker = 2,
    AdeptStalker = 3,
    ZealotStalker = 4,
    Archons = 5,
    Immortals = 6,
    ArchonsImmortals = 7,
    Templar = 8,
    Carriers = 9,
}

public enum TerranBuild
{
    None = 0,
    Bio = 1,
    Libs = 2,
    Mech = 3,
    BC = 4,
}

public enum ZergBuild
{
    None = 0,
    Zerglings = 1,
    LingBanes = 2,
    Queens = 3,
    Roaches = 4,
    RoachQueen = 5,
    Mutas = 6,
    Hydras = 7,
    RoachQueenLurker = 8,
    QueenLurker = 9,
    Ultras = 10,
}