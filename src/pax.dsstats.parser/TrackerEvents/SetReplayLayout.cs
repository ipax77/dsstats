using pax.dsstats.shared;
using s2protocol.NET.Models;
using System.Numerics;

namespace pax.dsstats.parser;
public partial class Parse
{
    private static void SetReplayLayout(DsReplay replay, List<SUnitBornEvent> zeroBornEvents)
    {
        SUnitBornEvent? nexusBornEvent = null;
        SUnitBornEvent? planetaryBornEvent = null;
        SUnitBornEvent? cannonBornEvent = null;
        SUnitBornEvent? bunkerBornEvent = null;

        float[]? lineTeam1 = null;
        float[]? lineTeam2 = null;

        bool objectivesSet = false;

        for (int i = 0; i < zeroBornEvents.Count; i++)
        {
            var bornEvent = zeroBornEvents[i];

            if (!objectivesSet)
            {
                (nexusBornEvent, planetaryBornEvent, cannonBornEvent, bunkerBornEvent) =
                    CheckObjective(bornEvent, nexusBornEvent, planetaryBornEvent, cannonBornEvent, bunkerBornEvent);

                objectivesSet = CheckObjectivesSet(nexusBornEvent, planetaryBornEvent, cannonBornEvent, bunkerBornEvent);

                if (objectivesSet && nexusBornEvent != null && planetaryBornEvent != null && cannonBornEvent != null && bunkerBornEvent != null)
                {
                    (lineTeam1, lineTeam2) = GetLines(nexusBornEvent, planetaryBornEvent);
                    replay.Layout.Nexus = new Position(nexusBornEvent.X, nexusBornEvent.Y);
                    replay.Layout.Planetary = new Position(planetaryBornEvent.X, planetaryBornEvent.Y);
                    replay.Layout.Cannon = new Position(cannonBornEvent.X, cannonBornEvent.Y);
                    replay.Layout.Bunker = new Position(bunkerBornEvent.X, bunkerBornEvent.Y);

                    if (nexusBornEvent.SUnitDiedEvent != null)
                    {
                        replay.Duration = nexusBornEvent.SUnitDiedEvent.Gameloop;
                        replay.WinnerTeam = 1;
                    }
                    else if (planetaryBornEvent.SUnitDiedEvent != null)
                    {
                        replay.Duration = planetaryBornEvent.SUnitDiedEvent.Gameloop;
                        replay.WinnerTeam = 2;
                    }

                    if (cannonBornEvent.SUnitDiedEvent != null)
                    {
                        replay.Cannon = cannonBornEvent.SUnitDiedEvent.Gameloop;
                    }

                    if (bunkerBornEvent.SUnitDiedEvent != null)
                    {
                        replay.Bunker = bunkerBornEvent.SUnitDiedEvent.Gameloop;
                    }
                }
            }

            if (bornEvent.ControlPlayerId <= 0 || bornEvent.ControlPlayerId > 6)
            {
                continue;
            }

            DsPlayer? player = replay.Players.FirstOrDefault(f => f.Pos == bornEvent.ControlPlayerId);
            if (player == null)
            {
                continue;
            }

            if (bornEvent.UnitTypeName == "StagingAreaFootprintSouth" || bornEvent.UnitTypeName == "AreaMarkerSouth")
            {
                player.SpawnArea2.South = new Position(bornEvent.X, bornEvent.Y);

                var distance = Vector2.DistanceSquared(new Vector2(bornEvent.X, 0), new Vector2(bornEvent.X, bornEvent.Y));
                if (distance > 10000)
                {
                    player.Team = 1;
                }
                else
                {
                    player.Team = 2;
                }
            }

            else if (bornEvent.UnitTypeName == "StagingAreaFootprintWest" || bornEvent.UnitTypeName == "AreaMarkerWest")
            {
                player.SpawnArea2.West = new Position(bornEvent.X, bornEvent.Y);
            }

            else if (bornEvent.UnitTypeName == "StagingAreaFootprintNorth" || bornEvent.UnitTypeName == "AreaMarkerNorth")
            {
                player.SpawnArea2.North = new Position(bornEvent.X, bornEvent.Y);
            }

            else if (bornEvent.UnitTypeName == "StagingAreaFootprintEast" || bornEvent.UnitTypeName == "AreaMarkerEast")
            {
                player.SpawnArea2.East = new Position(bornEvent.X, bornEvent.Y);
            }

            else if (bornEvent.UnitTypeName.StartsWith("Worker"))
            {
                player.Race = bornEvent.UnitTypeName[6..];
            }
        }

        if (nexusBornEvent == null
            || planetaryBornEvent == null
            || bunkerBornEvent == null
            || cannonBornEvent == null
            || lineTeam1 == null
            || lineTeam2 == null)
        {
            throw new ArgumentNullException(nameof(nexusBornEvent));
        }
    }



    private static (float[], float[]) GetLines(SUnitBornEvent nexusBornEvent, SUnitBornEvent planetaryBornEvent)
    {
        float x1t1 = planetaryBornEvent.X + MathF.Cos(135 * MathF.PI / 180) * 100;
        float y1t1 = planetaryBornEvent.Y + MathF.Sin(135 * MathF.PI / 180) * 100;
        float x2t1 = planetaryBornEvent.X + MathF.Cos(315 * MathF.PI / 180) * 100;
        float y2t1 = planetaryBornEvent.Y + MathF.Sin(315 * MathF.PI / 180) * 100;

        var lineTeam1 = new float[4] { x1t1, y1t1, x2t1, y2t1 };

        float x1t2 = nexusBornEvent.X + MathF.Cos(135 * MathF.PI / 180) * 100;
        float y1t2 = nexusBornEvent.Y + MathF.Sin(135 * MathF.PI / 180) * 100;
        float x2t2 = nexusBornEvent.X + MathF.Cos(315 * MathF.PI / 180) * 100;
        float y2t2 = nexusBornEvent.Y + MathF.Sin(315 * MathF.PI / 180) * 100;

        var lineTeam2 = new float[4] { x1t2, y1t2, x2t2, y2t2 };

        return (lineTeam1, lineTeam2);
    }

    private static (SUnitBornEvent?, SUnitBornEvent?, SUnitBornEvent?, SUnitBornEvent?) CheckObjective(SUnitBornEvent bornEvent,
                                       SUnitBornEvent? nexusBornEvent,
                                       SUnitBornEvent? planetaryBornEvent,
                                       SUnitBornEvent? cannonBornEvent,
                                       SUnitBornEvent? bunkerBornEvent)
    {
        if (bornEvent.UnitTypeName == "ObjectiveNexus")
        {
            nexusBornEvent = bornEvent;
        }
        else if (bornEvent.UnitTypeName == "ObjectivePlanetaryFortress")
        {
            planetaryBornEvent = bornEvent;
        }
        else if (bornEvent.UnitTypeName == "ObjectivePhotonCannon")
        {
            cannonBornEvent = bornEvent;
        }
        else if (bornEvent.UnitTypeName == "ObjectiveBunker")
        {
            bunkerBornEvent = bornEvent;
        }
        return (nexusBornEvent, planetaryBornEvent, cannonBornEvent, bunkerBornEvent);
    }

    private static bool CheckObjectivesSet(SUnitBornEvent? nexusBornEvent,
                                           SUnitBornEvent? planetaryBornEvent,
                                           SUnitBornEvent? cannonBornEvent,
                                           SUnitBornEvent? bunkerBornEvent)
    {
        return nexusBornEvent != null
            && planetaryBornEvent != null
            && cannonBornEvent != null
            && bunkerBornEvent != null;
    }
}
