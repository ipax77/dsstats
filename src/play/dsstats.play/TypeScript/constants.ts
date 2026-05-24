import type { TeamSpawnAreaSource } from "./types";

export const MAP_WIDTH = 256;
export const MAP_HEIGHT = 240;
export const MAP_CENTER_SUM = (MAP_WIDTH / 2) + (MAP_HEIGHT / 2);
export const GRID_INTERVAL = 16;
export const NEUTRAL_MIDDLE_LINE_COLOR = "rgba(255, 193, 7, 0.70)";
export const GAS_BADGE_WIDTH = 94;
export const GAS_BADGE_HEIGHT = 24;
export const GAS_BADGE_GAP = 8;
export const GAS_BADGE_CORNER_PADDING = 20;
export const MAX_UNIT_LIFETIME_GAMELOOPS = 2096;
export const MIN_CATALOG_ICON_CSS_SIZE = 18;

export const TEAM_COLORS: Record<number, string> = {
    1: "#5DADEC",
    2: "#F87171"
};

export const TEAM_SPAWN_AREAS: TeamSpawnAreaSource[] = [
    {
        teamId: 1,
        label: "Team 1",
        color: "#5DADEC",
        labelSegment: [0, 1],
        points: [
            { x: 165, y: 174 },
            { x: 182, y: 157 },
            { x: 171, y: 146 },
            { x: 154, y: 163 }
        ]
    },
    {
        teamId: 2,
        label: "Team 2",
        color: "#F87171",
        labelSegment: [2, 3],
        points: [
            { x: 84, y: 93 },
            { x: 101, y: 76 },
            { x: 90, y: 65 },
            { x: 73, y: 82 }
        ]
    }
];
