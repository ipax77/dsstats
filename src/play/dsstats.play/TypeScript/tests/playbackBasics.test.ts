import { describe, expect, it } from "vitest";
import { clipSumLine, clamp, withAlpha } from "../canvasUtils";
import { normalizeMiddleControl, normalizeReplay } from "../normalization";
import { unitIconCatalog } from "../unitIcons";

describe("spawn playback basics", () => {
    it("normalizes PascalCase replay DTOs and sorts units by spawn time", () => {
        const replay = normalizeReplay({
            DurationGameloop: 500,
            StepGameloops: 112,
            Bounds: { MinX: 10, MinY: 20, MaxX: 200, MaxY: 220 },
            MiddleControl: { FirstTeamId: 1, ChangeGameloops: [100, 200] },
            Players: [
                {
                    Name: "Player One",
                    TeamId: 1,
                    GamePos: 2,
                    Commander: "Terran",
                    RefineryGameloops: [220, 100],
                    TierUpgradeGameloops: [450, 300],
                    Units: [
                        {
                            Name: "Marauder",
                            SpawnGameloop: 180,
                            SpawnX: 50,
                            SpawnY: 60,
                            TargetX: 150,
                            TargetY: 160,
                            ExpiresGameloop: 300,
                            Radius: 8,
                            Color: "#FFFFFF"
                        },
                        {
                            Name: "Marine",
                            SpawnGameloop: 120,
                            SpawnX: 40,
                            SpawnY: 50,
                            Radius: 6,
                            Color: "#AAAAAA"
                        }
                    ]
                }
            ]
        });

        expect(replay.durationGameloop).toBe(500);
        expect(replay.bounds).toEqual({ minX: 10, minY: 20, maxX: 200, maxY: 220 });
        expect(replay.middleControl).toEqual({ firstTeamId: 1, changeGameloops: [100, 200] });
        expect(replay.players[0].refineryGameloops).toEqual([100, 220]);
        expect(replay.players[0].tierUpgradeGameloops).toEqual([300, 450]);
        expect(replay.units.map(unit => unit.name)).toEqual(["Marine", "Marauder"]);
        expect(replay.units[0].deltaX).toBe(0);
        expect(replay.units[0].deltaY).toBe(0);
        expect(replay.units[1].deltaX).toBe(100);
        expect(replay.units[1].deltaY).toBe(100);
    });

    it("rejects invalid middle-control data", () => {
        expect(normalizeMiddleControl({
            middleControl: {
                firstTeamId: 3,
                changeGameloops: [100]
            }
        })).toEqual({ firstTeamId: 0, changeGameloops: [] });

        expect(normalizeMiddleControl({
            middleControl: {
                firstTeamId: 2,
                changeGameloops: [100, Number.NaN, "later"]
            }
        })).toEqual({ firstTeamId: 2, changeGameloops: [100] });
    });

    it("resolves catalog icons using normalized commander and unit aliases", () => {
        const marine = unitIconCatalog.resolve(" Terran ", "Marine Lightweight");
        const zergling = unitIconCatalog.resolve("zerg", "ZerglingLightweight");

        expect(marine?.id).toBe("terran.marine");
        expect(zergling?.id).toBe("zerg.zergling");
        expect(unitIconCatalog.resolve("protoss", "Zealot")).toBeNull();
    });

    it("keeps small math and color helpers predictable", () => {
        expect(clamp(12, 0, 10)).toBe(10);
        expect(clamp(-2, 0, 10)).toBe(0);
        expect(withAlpha("#112233", "AA")).toBe("#112233AA");
        expect(withAlpha("rgba(1, 2, 3, 1)", "AA")).toBe("rgba(1, 2, 3, 1)");

        const segment = clipSumLine({ minX: 0, minY: 0, maxX: 10, maxY: 10 }, 10);
        expect(segment).toEqual({
            start: { x: 0, y: 10 },
            end: { x: 10, y: 0 }
        });
    });
});
