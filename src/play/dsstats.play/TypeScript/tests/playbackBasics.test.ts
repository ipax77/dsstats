import { describe, expect, it } from "vitest";
import { clipSumLine, clamp, withAlpha } from "../canvasUtils";
import { createAliveUnitHighlightKey, normalizeMiddleControl, normalizeReplay, normalizeSummary } from "../normalization";
import { createObjectiveDeathAnnouncements, createSpawnWaveTable, fitText, getActiveSpawnWaveEvents, getSpawnWaveEventAlpha, isEndSummaryVisible } from "../rendering";
import { resolveAliveUnitHighlightToggle, syncAliveUnitHighlightSelection } from "../state";
import { deleteState, setState } from "../store";
import type { LandmarkGeometry } from "../types";
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
        expect(replay.units.map(unit => unit.spawnNumber)).toEqual([0, 0]);
        expect(replay.units[0].playerName).toBe("Player One");
        expect(replay.units[0].gamePos).toBe(2);
        expect(replay.units[0].aliveUnitHighlightKey).toBe(createAliveUnitHighlightKey(1, "Terran", "Marine"));
        expect(replay.units[0].deltaX).toBe(0);
        expect(replay.units[0].deltaY).toBe(0);
        expect(replay.units[1].deltaX).toBe(100);
        expect(replay.units[1].deltaY).toBe(100);
        expect(replay.summary).toEqual({ totalKills: 0, players: [], topUnits: [] });
    });

    it("selects active per-team spawn events only inside their fade windows", () => {
        const events = [
            createSpawnEvent("t1-old", 1, 1, "Alpha", 1, 100),
            createSpawnEvent("t2", 2, 4, "Bravo", 2, 300),
            createSpawnEvent("t1-new", 1, 2, "Charlie", 3, 520)
        ];

        expect(getActiveSpawnWaveEvents(events, 49)).toEqual({ team1: null, team2: null });
        expect(getActiveSpawnWaveEvents(events, 100).team1?.key).toBe("t1-old");
        expect(getActiveSpawnWaveEvents(events, 340).team2?.key).toBe("t2");
        expect(getActiveSpawnWaveEvents(events, 520).team1?.key).toBe("t1-new");
        expect(getActiveSpawnWaveEvents(events, 900)).toEqual({ team1: null, team2: null });
    });

    it("groups one player spawn table and totals latest-patch cost and life", () => {
        const replay = normalizeReplay({
            DurationGameloop: 800,
            Players: [
                {
                    Name: "Alpha",
                    TeamId: 1,
                    GamePos: 1,
                    Commander: "Terran",
                    Units: [
                        createUnit("Marine", 2, 200),
                        createUnit("Marine", 2, 201),
                        createUnit("Marauder", 2, 202),
                        createUnit("Ghost", 1, 100)
                    ]
                },
                {
                    Name: "Bravo",
                    TeamId: 2,
                    GamePos: 4,
                    Commander: "Terran",
                    Units: [
                        createUnit("Marine", 2, 205),
                        createUnit("Marauder", 2, 206)
                    ]
                }
            ]
        });
        const unitLifeCostByKey = new Map([
            [createAliveUnitHighlightKey(1, "Terran", "Marine"), { cost: 50, life: 45 }],
            [createAliveUnitHighlightKey(2, "Terran", "Marine"), { cost: 60, life: 55 }],
            [createAliveUnitHighlightKey(2, "Terran", "Marauder"), { cost: 100, life: 125 }]
        ]);

        const table = createSpawnWaveTable(
            { replay, unitLifeCostByKey },
            createSpawnEvent("alpha-2", 1, 1, "Alpha", 2, 200));

        expect(table?.spawnNumber).toBe(2);
        expect(table?.teamId).toBe(1);
        expect(table?.playerName).toBe("Alpha");
        expect(table?.gamePos).toBe(1);
        expect(table?.rows).toEqual([
            {
                teamId: 1,
                unitName: "Marine",
                count: 2,
                cost: 50,
                life: 45,
                totalCost: 100,
                totalLife: 90
            },
            {
                teamId: 1,
                unitName: "Marauder",
                count: 1,
                cost: null,
                life: null,
                totalCost: null,
                totalLife: null
            }
        ]);
        expect(table?.totalCount).toBe(3);
        expect(table?.totalCost).toBe(100);
        expect(table?.totalLife).toBe(90);
    });

    it("keeps same-team spawn tables separated by player name", () => {
        const replay = normalizeReplay({
            DurationGameloop: 800,
            Players: [
                {
                    Name: "Alpha",
                    TeamId: 1,
                    GamePos: 1,
                    Commander: "Terran",
                    Units: [
                        createUnit("Marine", 2, 200),
                        createUnit("Marine", 2, 201)
                    ]
                },
                {
                    Name: "Charlie",
                    TeamId: 1,
                    GamePos: 2,
                    Commander: "Terran",
                    Units: [
                        createUnit("Marine", 2, 205)
                    ]
                }
            ]
        });
        const unitLifeCostByKey = new Map([
            [createAliveUnitHighlightKey(1, "Terran", "Marine"), { cost: 50, life: 45 }]
        ]);

        const alphaTable = createSpawnWaveTable(
            { replay, unitLifeCostByKey },
            createSpawnEvent("alpha-2", 1, 1, "Alpha", 2, 200));
        const charlieTable = createSpawnWaveTable(
            { replay, unitLifeCostByKey },
            createSpawnEvent("charlie-2", 1, 2, "Charlie", 2, 205));

        expect(alphaTable?.rows.map(row => [row.unitName, row.count, row.totalCost, row.totalLife])).toEqual([
            ["Marine", 2, 100, 90]
        ]);
        expect(charlieTable?.rows.map(row => [row.unitName, row.count, row.totalCost, row.totalLife])).toEqual([
            ["Marine", 1, 50, 45]
        ]);
    });

    it("fades spawn wave tables around the spawn event", () => {
        const event = createSpawnEvent("alpha-2", 1, 1, "Alpha", 2, 200);

        expect(getSpawnWaveEventAlpha(event, 149)).toBe(0);
        expect(getSpawnWaveEventAlpha(event, 175)).toBeCloseTo(0.5);
        expect(getSpawnWaveEventAlpha(event, 200)).toBe(1);
        expect(getSpawnWaveEventAlpha(event, 450)).toBe(1);
        expect(getSpawnWaveEventAlpha(event, 475)).toBeCloseTo(0.5);
        expect(getSpawnWaveEventAlpha(event, 501)).toBe(0);
    });

    it("does not create spawn wave overlay rows without life-cost metadata", () => {
        const replay = normalizeReplay({
            DurationGameloop: 400,
            Players: [
                {
                    Name: "Alpha",
                    TeamId: 1,
                    GamePos: 1,
                    Commander: "Terran",
                    Units: [createUnit("Marine", 1, 100)]
                }
            ]
        });

        expect(createSpawnWaveTable(
            { replay, unitLifeCostByKey: new Map() },
            createSpawnEvent("alpha-1", 1, 1, "Alpha", 1, 100))).toBeNull();
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
        const zealot = unitIconCatalog.resolve("protoss", "Zealot");
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
        const protossUnits = [
            "Zealot",
            "Sentry",
            "Stalker",
            "Adept",
            "High Templar",
            "Dark Templar",
            "Archon",
            "Immortal",
            "Colossus",
            "Disruptor",
            "Observer",
            "Oracle",
            "Phoenix",
            "Void Ray",
            "Carrier",
            "Tempest",
            "Mothership"
        ];
        const protossAliases = [
            ["HighTemplar", "protoss.highTemplar"],
            ["DarkTemplar", "protoss.darkTemplar"],
            ["VoidRay", "protoss.voidRay"]
        ] as const;
        const zergUnits = [
            "Zergling",
            "Hydralisk",
            "Infestor",
            "Overseer",
            "Corruptor",
            "Lurker",
            "Roach",
            "Queen",
            "Mutalisk",
            "Swarm Host",
            "Ultralisk",
            "Ravager",
            "Baneling",
            "Viper",
            "Brood Lord",
            "Locust"
        ];
        const zergAliases = [
            ["ZerglingLightweight", "zerg.zergling"],
            ["LurkerMP", "zerg.lurker"],
            ["SwarmHost", "zerg.swarm_host"],
            ["SwarmHostMP", "zerg.swarm_host"],
            ["BroodLord", "zerg.broodLord"],
            ["LocustMPPrecursor", "zerg.locust"]
        ] as const;

        expect(marine?.id).toBe("terran.marine");
        expect(reaper?.id).toBe("terran.reaper");
        expect(marauder?.id).toBe("terran.marauder");
        expect(zealot?.id).toBe("protoss.zealot");
        for (const unit of terranUnits) {
            expect(unitIconCatalog.resolve("Terran", unit), unit).not.toBeNull();
        }
        for (const [alias, id] of terranAliases) {
            expect(unitIconCatalog.resolve("Terran", alias)?.id).toBe(id);
        }
        for (const unit of protossUnits) {
            expect(unitIconCatalog.resolve("Protoss", unit), unit).not.toBeNull();
        }
        for (const [alias, id] of protossAliases) {
            expect(unitIconCatalog.resolve("Protoss", alias)?.id).toBe(id);
        }
        expect(zergling?.id).toBe("zerg.zergling");
        for (const unit of zergUnits) {
            expect(unitIconCatalog.resolve("Zerg", unit), unit).not.toBeNull();
        }
        for (const [alias, id] of zergAliases) {
            expect(unitIconCatalog.resolve("Zerg", alias)?.id).toBe(id);
        }
        expect(unitIconCatalog.resolve("protoss", "Probe")).toBeNull();
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
                    teamColor: "#5DADEC",
                    unitColor: "#FDBA2D"
                },
                innerHTML: ""
            },
            {
                dataset: {
                    unitIcon: "Zergling",
                    unitCommander: "Zerg",
                    unitSize: "20",
                    teamId: "2",
                    teamColor: "#F87171",
                    unitColor: "#34D399"
                },
                innerHTML: ""
            },
            {
                dataset: {
                    unitIcon: "Supplicant",
                    unitCommander: "Alarak",
                    unitSize: "20",
                    teamId: "1",
                    teamColor: "#5DADEC",
                    unitColor: "#FDBA2D"
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
        expect(firstHtml[0]).not.toContain("#FDBA2D");
        expect(firstHtml[1]).toContain("#F87171");
        expect(firstHtml[1]).not.toContain("#34D399");
        expect(firstHtml[2]).toContain("#FDBA2D");
        expect(firstHtml[2]).not.toContain("#5DADEC");
        expect(hosts[0].dataset.renderedIconKey).not.toBe(hosts[1].dataset.renderedIconKey);

        hosts[0].innerHTML = "unchanged";
        hosts[2].dataset.unitColor = "#A78BFA";
        hydrateUnitIcons(root);

        expect(hosts[0].innerHTML).toBe("unchanged");
        expect(hosts[1].innerHTML).toBe(firstHtml[1]);
        expect(hosts[2].innerHTML).toContain("#A78BFA");
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

    it("creates objective death announcements for dead bunker and cannon landmarks", () => {
        const announcements = createObjectiveDeathAnnouncements([
            createLandmark("Bunker", 137, 3),
            createLandmark("Cannon", 351),
            createLandmark("Planetary", 200, 12),
            createLandmark("Bunker", null)
        ], 112, 22.4);

        expect(announcements).toHaveLength(2);
        expect(announcements[0].message).toBe("Bunker down at 0:06 with 3 kills");
        expect(announcements[0].anchorGameloop).toBe(112);
        expect(announcements[0].startGameloop).toBe(0);
        expect(announcements[0].holdEndGameloop).toBe(426);
        expect(announcements[0].endGameloop).toBe(582);
        expect(announcements[1].message).toBe("Cannon down at 0:16");
        expect(announcements[1].anchorGameloop).toBe(336);
    });

    it("normalizes PascalCase and camelCase replay summaries", () => {
        expect(normalizeSummary({
            Summary: {
                TotalKills: 42,
                Players: [
                    { PlayerName: "Alpha", TeamId: 1, GamePos: 1, Commander: "Terran", Kills: 30 }
                ],
                TopUnits: [
                    { PlayerName: "Alpha", TeamId: 1, GamePos: 1, UnitName: "Marine", Kills: 5 }
                ]
            }
        })).toEqual({
            totalKills: 42,
            players: [
                { playerName: "Alpha", teamId: 1, gamePos: 1, commander: "Terran", kills: 30 }
            ],
            topUnits: [
                { playerName: "Alpha", teamId: 1, gamePos: 1, unitName: "Marine", kills: 5 }
            ]
        });

        expect(normalizeSummary({
            summary: {
                totalKills: 7,
                players: [
                    { playerName: "Bravo", teamId: 2, gamePos: 4, commander: "Zerg", kills: 7 }
                ],
                topUnits: []
            }
        }).players[0].playerName).toBe("Bravo");
    });

    it("shows the end summary only at replay duration", () => {
        expect(isEndSummaryVisible(499, 500)).toBe(false);
        expect(isEndSummaryVisible(500, 500)).toBe(true);
        expect(isEndSummaryVisible(520, 500)).toBe(true);
        expect(isEndSummaryVisible(Number.NaN, 500)).toBe(false);
    });

    it("fits canvas text with stable ellipsis behavior", () => {
        const ctx = createMeasureContext();

        expect(fitText(ctx, "P1 Nova", 70)).toBe("P1 Nova");
        expect(fitText(ctx, "P2 PepperDome", 80)).toBe("P2 Pepp...");
        expect(fitText(ctx, "P3 NexusDS", 10)).toBe("");
        expect(fitText(ctx, "", 100)).toBe("");
    });

    it("creates unambiguous alive-unit highlight keys", () => {
        const key = createAliveUnitHighlightKey(2, "Terran|Prime", "10:Marine");

        expect(key).toBe("2|12:Terran|Prime|9:10:Marine");
    });

    it("toggles an already selected alive-unit highlight off", () => {
        const marineKey = createAliveUnitHighlightKey(1, "Terran", "Marine");
        const zerglingKey = createAliveUnitHighlightKey(2, "Zerg", "Zergling");

        expect(resolveAliveUnitHighlightToggle(null, marineKey)).toBe(marineKey);
        expect(resolveAliveUnitHighlightToggle(marineKey, zerglingKey)).toBe(zerglingKey);
        expect(resolveAliveUnitHighlightToggle(marineKey, marineKey)).toBeNull();
    });

    it("syncs alive-unit row selection from playback state", () => {
        const selectedKey = createAliveUnitHighlightKey(1, "Terran", "Marine");
        const rows = [
            createHighlightRow(selectedKey),
            createHighlightRow(createAliveUnitHighlightKey(2, "Zerg", "Zergling"))
        ];
        const root = {
            querySelectorAll: () => rows
        };
        const canvas = {} as HTMLCanvasElement;

        setState(canvas, {
            rootElement: root,
            highlightedAliveUnitKey: selectedKey
        } as never);

        syncAliveUnitHighlightSelection(canvas);

        expect(rows[0].selected).toBe(true);
        expect(rows[0].attributes["aria-pressed"]).toBe("true");
        expect(rows[1].selected).toBe(false);
        expect(rows[1].attributes["aria-pressed"]).toBe("false");

        deleteState(canvas);
    });
});

function createLandmark(label: string, diedGameloop: number | null, kills = 0): LandmarkGeometry {
    return {
        x: 0,
        y: 0,
        kind: "Defense",
        teamId: 1,
        color: "#F8D34A",
        kills,
        label,
        radius: 9,
        diedGameloop,
        projected: { x: 0, y: 0 }
    };
}

function createUnit(name: string, spawnNumber: number, spawnGameloop: number): Record<string, unknown> {
    return {
        Name: name,
        SpawnNumber: spawnNumber,
        SpawnGameloop: spawnGameloop,
        SpawnX: 40,
        SpawnY: 50,
        ExpiresGameloop: spawnGameloop + 400,
        Radius: 6,
        Color: "#AAAAAA"
    };
}

function createSpawnEvent(
    key: string,
    teamId: number,
    gamePos: number,
    playerName: string,
    spawnNumber: number,
    anchorGameloop: number) {
    return {
        key,
        teamId,
        gamePos,
        playerName,
        spawnNumber,
        anchorGameloop,
        startGameloop: anchorGameloop - 50,
        holdEndGameloop: anchorGameloop + 250,
        endGameloop: anchorGameloop + 300
    };
}

function createMeasureContext() {
    return {
        measureText: (text: string) => ({ width: text.length * 8 }) as TextMetrics
    };
}

function createHighlightRow(key: string) {
    const row = {
        dataset: {
            spawnPlaybackHighlightKey: key
        },
        selected: false,
        attributes: {} as Record<string, string>,
        classList: {
            toggle: (_className: string, selected: boolean) => {
                row.selected = selected;
            }
        },
        setAttribute: (name: string, value: string) => {
            row.attributes[name] = value;
        }
    };

    return row;
}
