using s2protocol.NET.Models;

namespace dsstats.challenge;

public static partial class Parse
{
    private static void SetPlayerUnits(DsReplay replay, List<SUnitBornEvent> bornEvents)
    {
        (Line planetaryLine, Line nexusLine) = GetLines(replay.Layout.Planetary, replay.Layout.Nexus);

        var playerTeam1 = replay.Players.FirstOrDefault(f => f.GamePos == 2)
                          ?? replay.Players.FirstOrDefault(x => x.Team == 1);
        if (playerTeam1 == null) return;

        var playerTeam2 = replay.Players.FirstOrDefault(f => f.GamePos == 4)
                          ?? replay.Players.FirstOrDefault(x => x.Team == 2);

        (Line buildTeam1, Line buildTeam2) = GetLines(playerTeam1.SpawnArea2.South,
                                                      playerTeam2?.SpawnArea2.North ?? new(0, 0));

        planetaryLine = MoveLine(planetaryLine, 3f);
        nexusLine = MoveLine(nexusLine, -3f);

        var playersByPos = replay.Players.ToDictionary(p => p.Pos);

        foreach (var bornEvent in bornEvents)
        {
            if (bornEvent.Gameloop == 0 || bornEvent.ControlPlayerId < 1 || bornEvent.ControlPlayerId > 6)
                continue;

            if (!playersByPos.TryGetValue(bornEvent.ControlPlayerId, out var player))
                continue;

            if (bornEvent.UnitTypeName.StartsWith("Worker"))
            {
                player.Race = bornEvent.UnitTypeName[6..];
                player.RaceInGameSelected = true;
                continue;
            }

            if (DoSkipUnit(bornEvent.UnitTypeName))
                continue;

            // Evaluate UnitType early
            var unitType = GetUnitType(bornEvent, player.Team, planetaryLine, nexusLine, buildTeam1, buildTeam2);
            if (unitType != UnitType.Spawn)
                continue;

            DsUnit unit = new()
            {
                Index = bornEvent.UnitIndex,
                Name = FixUnitName(bornEvent.UnitTypeName, player.Race),
                Gameloop = bornEvent.Gameloop,
                Position = new Position() { X = bornEvent.X, Y = bornEvent.Y },
                UnitType = unitType
            };

            if (bornEvent.SUnitDiedEvent != null)
            {
                unit.KillerPlayer = bornEvent.SUnitDiedEvent.KillerPlayerId ?? 0;
                unit.KillerUnit = bornEvent.SUnitDiedEvent.KillerUnitBornEvent?.UnitTypeName;
                unit.DiedGameloop = bornEvent.SUnitDiedEvent.Gameloop;
                unit.DiedPosition = new Position()
                {
                    X = bornEvent.SUnitDiedEvent.X,
                    Y = bornEvent.SUnitDiedEvent.Y
                };
            }

            player.Units.Add(unit);
        }
    }

    private static bool DoSkipUnit(string unitName)
    {
        if (IgnoreUnitsNg.Contains(unitName))
        {
            return true;
        }

        for (int j = 0; j < IgnoreUnitsContains.Count; j++)
        {
            if (unitName.Contains(IgnoreUnitsContains[j]))
            {
                return true;
            }
        }

        return false;
    }

    private static UnitType GetUnitType(SUnitBornEvent bornEvent, int team, Line planetaryLine, Line nexusLine, Line buildTeam1, Line buildTeam2)
    {
        if (bornEvent.UnitTypeName.StartsWith("Tier"))
        {
            return UnitType.Tier;
        }

        var line1 = team == 1 ? buildTeam1 : nexusLine;
        var line2 = team == 1 ? planetaryLine : buildTeam2;

        var isLeftOfLine1 = IsLeftOfLine(line1.A, line1.B, new(bornEvent.X, bornEvent.Y));
        var isLeftOfLine2 = IsLeftOfLine(line2.A, line2.B, new(bornEvent.X, bornEvent.Y));

        if (isLeftOfLine1 && !isLeftOfLine2)
        {
            return UnitType.Spawn;
        }

        else if (team == 1 && !isLeftOfLine1)
        {
            return UnitType.Build;
        }

        else if (team == 2 && isLeftOfLine2)
        {
            return UnitType.Build;
        }

        return UnitType.None;
    }

    private static bool IsLeftOfLine(Position lineA, Position lineB, Position p)
    {
        return ((lineB.X - lineA.X) * (p.Y - lineA.Y) - (lineB.Y - lineA.Y) * (p.X - lineA.X)) < 0;
    }

    private static List<string> IgnoreUnitsContains = new List<string>()
    {
        "Interceptor",
        "Worker",
        "Decoration",
        "Broodling",
        "ReaperLD9ClusterCharges",
        "KD8Charge",
        "Trophy",
        "StrikeWeaponry",
        "AdeptPhaseShift",
        "InfestedLiberatorViralSwarm",
        "Locust",
        "Railgun",
        "PrimalWurm",
        "UED",
    };

    private static readonly HashSet<string> IgnoreUnitsNg = new()
    {
        "TrainDummy",
        "ReaverStarlightDummy",
        "AiurCarrierRepairDrone",
        "StukovAleksanderPlaceholder",
        "AssaultDrone",
        "AutoTurret",
        "BioMechanicalRepairDrone",
        "Biomass",
        "ClolarionBomber",
        "CreepTumorBurrowed",
        "CreeperHostExplosiveCreeper",
        "DehakaCreeperHostExplosiveCreeper",
        "DisruptorPhased",
        "GuardianShell",
        "HornerAssaultDrone",
        "RaynorHyperionPointDefenseDrone",
        "InfestedTerransEgg",
        "InvisibleTargetDummy",
        "LurkerStetmannBurrowed",
        "ParasiticBombDummy",
        "ParasiticBombRelayDummy",
        "PrimalHostLocust",
        "PurifierAdeptShade",
        "PurifierTalisShade",
        "TychusRattlesnakeDeployRevitalizer",
        "SNARE_PLACEHOLDER",
        "TychusSiriusWarhoundTurret",
        "SplitterlingSpawn",
        "SprayDecal",
        "BelShirSapphire",
        "CharAgate",
        "InfestedDiamondbackSnarePlaceholder",
        "TerrazineAmethyst",
        "TridentMissiles",
        "UnitBirthBar",
    };


    private static (Line, Line) GetLines(Position p1, Position p2)
    {
        float x1t1 = p1.X + MathF.Cos(135 * MathF.PI / 180) * 50;
        float y1t1 = p1.Y + MathF.Sin(135 * MathF.PI / 180) * 50;
        float x2t1 = p1.X + MathF.Cos(315 * MathF.PI / 180) * 50;
        float y2t1 = p1.Y + MathF.Sin(315 * MathF.PI / 180) * 50;

        Line line1 = new(new Position(Convert.ToInt32(x1t1), Convert.ToInt32(y1t1)), new Position(Convert.ToInt32(x2t1), Convert.ToInt32(y2t1)));

        float x1t2 = p2.X + MathF.Cos(135 * MathF.PI / 180) * 50;
        float y1t2 = p2.Y + MathF.Sin(135 * MathF.PI / 180) * 50;
        float x2t2 = p2.X + MathF.Cos(315 * MathF.PI / 180) * 50;
        float y2t2 = p2.Y + MathF.Sin(315 * MathF.PI / 180) * 50;

        Line line2 = new(new Position(Convert.ToInt32(x1t2), Convert.ToInt32(y1t2)), new Position(Convert.ToInt32(x2t2), Convert.ToInt32(y2t2)));

        return (line1, line2);
    }

    private static Line MoveLine(Line line, float distance)
    {
        float r = MathF.Sqrt(MathF.Pow(line.B.X - line.A.X, 2) + MathF.Pow(line.B.Y - line.A.Y, 2));
        int xd = Convert.ToInt32(distance / r * (line.A.Y - line.B.Y));
        int yd = Convert.ToInt32(distance / r * (line.B.X - line.A.X));

        return new Line(new(line.A.X + xd, line.A.Y + yd), new(line.B.X + xd, line.B.Y + yd));
    }

    public record Line
    {
        public Line(Position a, Position b)
        {
            A = a;
            B = b;
        }

        public Position A { get; set; } = Position.Zero;
        public Position B { get; set; } = Position.Zero;
    }
}
