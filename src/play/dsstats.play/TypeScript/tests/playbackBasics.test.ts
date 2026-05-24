import { describe, expect, it } from "vitest";
import { clipSumLine, clamp, withAlpha } from "../canvasUtils";
import { normalizeMiddleControl, normalizeReplay } from "../normalization";
import { hydrateUnitIcons, unitIconCatalog } from "../unitIcons";

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
        const reaper = unitIconCatalog.resolve("Terran", "ReaperLightweight");
        const marauder = unitIconCatalog.resolve("Terran", "Marauder Lightweight");
        const zergling = unitIconCatalog.resolve("zerg", "ZerglingLightweight");
        const terranUnits = [
            "Marine",
            "Marauder",
            "Reaper",
            "Ghost",
            "Hellbat",
            "Hellion",
            "Medivac",
            "Banshee",
            "Viking",
            "Raven",
            "Siege Tank",
            "Cyclone",
            "Widow Mine",
            "Liberator",
            "Thor",
            "Battlecruiser"
        ];
        const terranAliases = [
            ["GhostAlternate", "terran.ghost"],
            ["GhostNova", "terran.ghost"],
            ["HellionTank", "terran.hellion"],
            ["VikingFighter", "terran.viking"],
            ["VikingAssault", "terran.viking"],
            ["SiegeTank", "terran.siegeTank"],
            ["WidowMine", "terran.widowMine"],
            ["ThorAP", "terran.thor"]
        ] as const;

        expect(marine?.id).toBe("terran.marine");
        expect(reaper?.id).toBe("terran.reaper");
        expect(marauder?.id).toBe("terran.marauder");
        for (const unit of terranUnits) {
            expect(unitIconCatalog.resolve("Terran", unit), unit).not.toBeNull();
        }
        for (const [alias, id] of terranAliases) {
            expect(unitIconCatalog.resolve("Terran", alias)?.id).toBe(id);
        }
        expect(zergling?.id).toBe("zerg.zergling");
        expect(unitIconCatalog.resolve("protoss", "Zealot")).toBeNull();
    });

    it("renders team-colored SVG for catalog icons", () => {
        const marine = unitIconCatalog.resolve("terran", "Marine");
        const zergling = unitIconCatalog.resolve("zerg", "Zergling");

        expect(marine).not.toBeNull();
        expect(zergling).not.toBeNull();

        const team1Marine = unitIconCatalog.toSvg(marine!, { size: 20, teamColor: "#5DADEC" });
        const team2Zergling = unitIconCatalog.toSvg(zergling!, { size: 20, teamColor: "#F87171" });

        expect(team1Marine).toContain("<svg");
        expect(team1Marine).toContain("#5DADEC");
        expect(team1Marine).toContain("#B6DAF6");
        expect(team2Zergling).toContain("<svg");
        expect(team2Zergling).toContain("#F87171");
        expect(team2Zergling).toContain("#FCBFBF");
    });

    it("hydrates unit icon hosts once per render key", () => {
        const hosts: Array<{ dataset: Record<string, string>; innerHTML: string }> = [
            {
                dataset: {
                    unitIcon: "Zergling",
                    unitCommander: "Zerg",
                    unitSize: "20",
                    teamId: "1",
                    teamColor: "#5DADEC"
                },
                innerHTML: ""
            },
            {
                dataset: {
                    unitIcon: "Zergling",
                    unitCommander: "Zerg",
                    unitSize: "20",
                    teamId: "2",
                    teamColor: "#F87171"
                },
                innerHTML: ""
            }
        ];
        const root = {
            querySelectorAll: () => hosts
        } as unknown as ParentNode;

        hydrateUnitIcons(root);
        const firstHtml = hosts.map(host => host.innerHTML);

        expect(firstHtml[0]).toContain("#5DADEC");
        expect(firstHtml[1]).toContain("#F87171");
        expect(hosts[0].dataset.renderedIconKey).not.toBe(hosts[1].dataset.renderedIconKey);

        hosts[0].innerHTML = "unchanged";
        hydrateUnitIcons(root);

        expect(hosts[0].innerHTML).toBe("unchanged");
        expect(hosts[1].innerHTML).toBe(firstHtml[1]);
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
