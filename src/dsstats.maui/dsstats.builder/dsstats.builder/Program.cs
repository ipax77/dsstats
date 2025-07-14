using dsstats.shared;
using dsstats.shared.DsFen;

namespace dsstats.builder;

class Program
{
    static void Main(string[] args)
    {
        Thread.Sleep(1000);
        BuildWithUpgrades();
    }

    private static void BuildFen()
    {
        string fen = "2:Terran;10q15/9q16/8q17/7q18/6q15q3/5q17w2/4q18qe1/3q19qqw/2q20qq1/1q20qq2/q20qq3/19eqq4/19qw5/18qq6/17qq7/16qq8/15qq9/14qq10/12eqq11/11qqw12/11qq13/5q4qq14/6wqqqq15/7eqq16/8w17|26/19z6/26/26/26/26/26/26/26/21d4/26/19d6/26/15f1d8/26/15d10/26/13d12/2z23/11d14/26/9d16/26/26/26";
        DsBuildRequest buildRequest = new();
        DsFen.ApplyFen(fen, out buildRequest);
        DsBuilder.Build(buildRequest, dry: true);

    }

    private static void BuildMany()
    {
        DsBuildRequest buildRequest = new()
        {
            Commander = shared.Commander.Protoss,
            Team = 2,
            Spawn = new()
            {
                Units = new List<SpawnUnitDto>()
               {
                   new () {
                       Count = 34,
                       Poss = "90,78,92,79,96,75,92,71,85,84,88,73,86,82,92,69,83,88,88,79,79,84,81,82,90,73,88,75,83,80,85,79,87,77,94,71,79,86,93,73,81,85,90,75,88,77,83,82,85,80,87,79,94,73,81,86,83,84,92,75,85,82,87,80,90,77,96,73,81,88,94,75,83,86,92,77,94,77,85,86,87,84,90,80,88,82,98,75,87,86,89,84,90,82,94,79,83,90,92,81",
                       Unit = new UnitDto { Name = "Immortal" }
                    }
               }
            }
        };
        DsBuilder.Build(buildRequest, dry: false);
    }

    private static void BuildWithUpgrades()
    {
        DsBuildRequest buildRequest = new()
        {
            Commander = Commander.Protoss,
            Team = 1,
            Spawn = new()
            {
                Units = new List<SpawnUnitDto>()
               {
                   new () {
                       Count = 1,
                       Poss = "160,160",
                       Unit = new UnitDto { Name = "Zealot" }
                    }
               }
            },
            Upgrades = new List<PlayerUpgradeDto>
            {
                new PlayerUpgradeDto { Upgrade = new UpgradeDto { Name = "Charge" }, Gameloop = 0 },
                new PlayerUpgradeDto { Upgrade = new UpgradeDto { Name = "ProtossGroundWeaponsLevel1" }, Gameloop = 0 },
            }
        };
        DsBuilder.Build(buildRequest);
    }
}
