import { createAliveUnitHighlightKey, normalizeBounds, normalizeMiddleControl, normalizeRefineryGameloops, normalizeSummary, normalizeTierUpgradeGameloops, readArray, readNumber, readObject, readOptionalNumber, readString, asObject } from "./normalization";
import type { NormalizedPlayer, NormalizedReplay, NormalizedReplayNg, NormalizedUnitKind, Point, RawObject } from "./types";

const UNIT_ROW_STRIDE = 11;
const UNIT_ROW_PLAYER_INDEX = 1;
const UNIT_ROW_UNIT_KIND_INDEX = 2;
const UNIT_ROW_SPAWN_NUMBER = 3;
const UNIT_ROW_SPAWN_GAMELOOP = 4;
const UNIT_ROW_EXPIRES_GAMELOOP = 5;
const UNIT_ROW_PATH_INDEX = 6;

const PATH_ROW_STRIDE = 3;
const PATH_ROW_POINT_OFFSET = 0;
const PATH_ROW_POINT_COUNT = 1;

const PATH_POINT_STRIDE = 3;
const PATH_POINT_X = 0;
const PATH_POINT_Y = 1;
const PATH_POINT_GAMELOOP_OFFSET = 2;

export function normalizeReplayNg(
    replayValue: unknown,
    unitRowsBytes: Uint8Array,
    pathRowsBytes: Uint8Array,
    pathPointsBytes: Uint8Array,
    killGameloopsBytes: Uint8Array): NormalizedReplay {
    const replay = asObject(replayValue);
    const rawPlayers = readArray(replay, "players", "Players");
    const players: NormalizedPlayer[] = rawPlayers.map(rawPlayerValue => {
        const rawPlayer = asObject(rawPlayerValue);
        return {
            name: readString(rawPlayer, "name", "Name"),
            teamId: readNumber(rawPlayer, "teamId", "TeamId"),
            gamePos: readNumber(rawPlayer, "gamePos", "GamePos"),
            commander: readString(rawPlayer, "commander", "Commander"),
            refineryGameloops: normalizeRefineryGameloops(rawPlayer),
            tierUpgradeGameloops: normalizeTierUpgradeGameloops(rawPlayer),
            units: []
        };
    });

    const unitKinds = readArray(replay, "unitKinds", "UnitKinds").map(normalizeUnitKind);

    return {
        durationGameloop: readNumber(replay, "durationGameloop", "DurationGameloop"),
        stepGameloops: readOptionalNumber(replay, "stepGameloops", "StepGameloops") ?? 112,
        bounds: normalizeBounds(readObject(replay, "bounds", "Bounds")),
        stats: replay.stats ?? replay.Stats,
        summary: normalizeSummary(replay),
        middleControl: normalizeMiddleControl(replay),
        landmarks: readArray(replay, "landmarks", "Landmarks").map(asObject),
        buildUnits: [],
        snapshots: readArray(replay, "snapshots", "Snapshots"),
        players,
        units: [],
        ng: {
            unitKinds,
            unitRows: decodeInt32Rows(unitRowsBytes),
            pathRows: decodeInt32Rows(pathRowsBytes),
            pathPoints: decodeInt32Rows(pathPointsBytes),
            killGameloops: decodeInt32Rows(killGameloopsBytes)
        }
    };
}

export function getReplayUnitCount(replay: NormalizedReplay): number {
    return replay.ng ? replay.ng.unitRows.length / UNIT_ROW_STRIDE : replay.units.length;
}

export function getUnitSpawnGameloop(replay: NormalizedReplay, unitIndex: number): number {
    return replay.ng
        ? getUnitRowValue(replay.ng, unitIndex, UNIT_ROW_SPAWN_GAMELOOP)
        : replay.units[unitIndex]?.spawnGameloop ?? 0;
}

export function getUnitExpiresGameloop(replay: NormalizedReplay, unitIndex: number): number {
    return replay.ng
        ? getUnitRowValue(replay.ng, unitIndex, UNIT_ROW_EXPIRES_GAMELOOP)
        : replay.units[unitIndex]?.expiresGameloop ?? 0;
}

export function getUnitSpawnNumber(replay: NormalizedReplay, unitIndex: number): number {
    return replay.ng
        ? getUnitRowValue(replay.ng, unitIndex, UNIT_ROW_SPAWN_NUMBER)
        : replay.units[unitIndex]?.spawnNumber ?? 0;
}

export function getUnitPlayer(replay: NormalizedReplay, unitIndex: number): NormalizedPlayer | null {
    if (!replay.ng) {
        const unit = replay.units[unitIndex];
        return unit
            ? {
                name: unit.playerName,
                teamId: unit.teamId,
                gamePos: unit.gamePos,
                commander: unit.commander,
                refineryGameloops: [],
                tierUpgradeGameloops: [],
                units: []
            }
            : null;
    }

    return replay.players[getUnitRowValue(replay.ng, unitIndex, UNIT_ROW_PLAYER_INDEX)] ?? null;
}

export function getUnitKind(replay: NormalizedReplay, unitIndex: number): NormalizedUnitKind | null {
    if (!replay.ng) {
        const unit = replay.units[unitIndex];
        return unit
            ? {
                name: unit.name,
                commander: unit.commander,
                radius: unit.radius,
                color: unit.color,
                iconDefinition: unit.iconDefinition,
                iconResolved: unit.iconResolved
            }
            : null;
    }

    return replay.ng.unitKinds[getUnitRowValue(replay.ng, unitIndex, UNIT_ROW_UNIT_KIND_INDEX)] ?? null;
}

export function getUnitTeamId(replay: NormalizedReplay, unitIndex: number): number {
    return getUnitPlayer(replay, unitIndex)?.teamId ?? 0;
}

export function getUnitGamePos(replay: NormalizedReplay, unitIndex: number): number {
    return getUnitPlayer(replay, unitIndex)?.gamePos ?? 0;
}

export function getUnitPlayerName(replay: NormalizedReplay, unitIndex: number): string {
    return getUnitPlayer(replay, unitIndex)?.name ?? "";
}

export function getUnitName(replay: NormalizedReplay, unitIndex: number): string {
    return getUnitKind(replay, unitIndex)?.name ?? "";
}

export function getUnitCommander(replay: NormalizedReplay, unitIndex: number): string {
    return getUnitKind(replay, unitIndex)?.commander ?? "";
}

export function getUnitColor(replay: NormalizedReplay, unitIndex: number): string {
    return getUnitKind(replay, unitIndex)?.color ?? "#EC7063";
}

export function getUnitRadius(replay: NormalizedReplay, unitIndex: number): number {
    return getUnitKind(replay, unitIndex)?.radius ?? 8;
}

export function getUnitAliveHighlightKey(replay: NormalizedReplay, unitIndex: number): string {
    return createAliveUnitHighlightKey(
        getUnitTeamId(replay, unitIndex),
        getUnitCommander(replay, unitIndex),
        getUnitName(replay, unitIndex));
}

export function resolveUnitPosition(replay: NormalizedReplay, unitIndex: number, currentGameloop: number): Point {
    if (!replay.ng) {
        const unit = replay.units[unitIndex];
        if (!unit) {
            return { x: 0, y: 0 };
        }

        const progress = Math.max(0, Math.min(1, (currentGameloop - unit.spawnGameloop) * unit.inverseLifetime));
        return {
            x: unit.spawnX + unit.deltaX * progress,
            y: unit.spawnY + unit.deltaY * progress
        };
    }

    const ng = replay.ng;
    const pathIndex = getUnitRowValue(ng, unitIndex, UNIT_ROW_PATH_INDEX);
    const pointOffset = getPathRowValue(ng, pathIndex, PATH_ROW_POINT_OFFSET);
    const pointCount = getPathRowValue(ng, pathIndex, PATH_ROW_POINT_COUNT);
    if (pointCount <= 0) {
        return { x: 0, y: 0 };
    }

    const localGameloop = Math.max(0, currentGameloop - getUnitSpawnGameloop(replay, unitIndex));
    let leftPoint = pointOffset;
    let rightPoint = pointOffset + pointCount - 1;
    if (localGameloop <= getPathPointValue(ng, leftPoint, PATH_POINT_GAMELOOP_OFFSET)) {
        return getPathPoint(ng, leftPoint);
    }
    if (localGameloop >= getPathPointValue(ng, rightPoint, PATH_POINT_GAMELOOP_OFFSET)) {
        return getPathPoint(ng, rightPoint);
    }

    while (rightPoint - leftPoint > 1) {
        const middle = leftPoint + Math.floor((rightPoint - leftPoint) / 2);
        if (getPathPointValue(ng, middle, PATH_POINT_GAMELOOP_OFFSET) <= localGameloop) {
            leftPoint = middle;
        } else {
            rightPoint = middle;
        }
    }

    const leftOffset = getPathPointValue(ng, leftPoint, PATH_POINT_GAMELOOP_OFFSET);
    const rightOffset = getPathPointValue(ng, rightPoint, PATH_POINT_GAMELOOP_OFFSET);
    const left = getPathPoint(ng, leftPoint);
    const right = getPathPoint(ng, rightPoint);
    if (rightOffset <= leftOffset) {
        return right;
    }

    const progress = Math.max(0, Math.min(1, (localGameloop - leftOffset) / (rightOffset - leftOffset)));
    return {
        x: left.x + (right.x - left.x) * progress,
        y: left.y + (right.y - left.y) * progress
    };
}

function normalizeUnitKind(value: unknown): NormalizedUnitKind {
    const unitKind = asObject(value);
    return {
        name: readString(unitKind, "name", "Name"),
        commander: readString(unitKind, "commander", "Commander"),
        radius: readNumber(unitKind, "radius", "Radius", 8),
        color: readString(unitKind, "color", "Color", "#EC7063"),
        iconDefinition: null,
        iconResolved: false
    };
}

function decodeInt32Rows(bytes: Uint8Array): Int32Array {
    if (bytes.byteLength === 0) {
        return new Int32Array(0);
    }

    const count = Math.floor(bytes.byteLength / Int32Array.BYTES_PER_ELEMENT);
    const values = new Int32Array(count);
    const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
    for (let i = 0; i < count; i++) {
        values[i] = view.getInt32(i * Int32Array.BYTES_PER_ELEMENT, true);
    }

    return values;
}

function getUnitRowValue(ng: NormalizedReplayNg, unitIndex: number, offset: number): number {
    return ng.unitRows[unitIndex * UNIT_ROW_STRIDE + offset] ?? 0;
}

function getPathRowValue(ng: NormalizedReplayNg, pathIndex: number, offset: number): number {
    return ng.pathRows[pathIndex * PATH_ROW_STRIDE + offset] ?? 0;
}

function getPathPointValue(ng: NormalizedReplayNg, pointIndex: number, offset: number): number {
    return ng.pathPoints[pointIndex * PATH_POINT_STRIDE + offset] ?? 0;
}

function getPathPoint(ng: NormalizedReplayNg, pointIndex: number): Point {
    return {
        x: getPathPointValue(ng, pointIndex, PATH_POINT_X),
        y: getPathPointValue(ng, pointIndex, PATH_POINT_Y)
    };
}
