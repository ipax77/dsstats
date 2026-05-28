import { MAP_HEIGHT, MAP_WIDTH, MAX_UNIT_LIFETIME_GAMELOOPS } from "./constants";
import type {
    Bounds,
    MiddleControl,
    NormalizedPlayer,
    NormalizedReplay,
    NormalizedUnit,
    PlaybackPlayerSummary,
    PlaybackSummary,
    PlaybackTopUnitSummary,
    RawObject,
    UnitLifeCost,
    UnitLifeCostEntry
} from "./types";

export function normalizeReplay(replayValue: unknown): NormalizedReplay {
    const replay = asObject(replayValue);
    const bounds = normalizeBounds(readObject(replay, "bounds", "Bounds"));
    const rawPlayers = readArray(replay, "players", "Players");
    const players: NormalizedPlayer[] = [];
    const units: NormalizedUnit[] = [];

    for (const rawPlayerValue of rawPlayers) {
        const rawPlayer = asObject(rawPlayerValue);
        const player: NormalizedPlayer = {
            name: readString(rawPlayer, "name", "Name"),
            teamId: readNumber(rawPlayer, "teamId", "TeamId"),
            gamePos: readNumber(rawPlayer, "gamePos", "GamePos"),
            commander: readString(rawPlayer, "commander", "Commander"),
            refineryGameloops: normalizeRefineryGameloops(rawPlayer),
            tierUpgradeGameloops: normalizeTierUpgradeGameloops(rawPlayer),
            units: []
        };

        const rawUnits = readArray(rawPlayer, "units", "Units");
        for (const rawUnitValue of rawUnits) {
            const rawUnit = asObject(rawUnitValue);
            const name = readString(rawUnit, "name", "Name");
            const spawnGameloop = readNumber(rawUnit, "spawnGameloop", "SpawnGameloop");
            const expiresGameloop = readOptionalNumber(rawUnit, "expiresGameloop", "ExpiresGameloop")
                ?? spawnGameloop + MAX_UNIT_LIFETIME_GAMELOOPS;
            const spawnX = readNumber(rawUnit, "spawnX", "SpawnX");
            const spawnY = readNumber(rawUnit, "spawnY", "SpawnY");
            const targetX = readOptionalNumber(rawUnit, "targetX", "TargetX") ?? spawnX;
            const targetY = readOptionalNumber(rawUnit, "targetY", "TargetY") ?? spawnY;
            const unit: NormalizedUnit = {
                name,
                playerName: player.name,
                gamePos: player.gamePos,
                commander: player.commander,
                aliveUnitHighlightKey: createAliveUnitHighlightKey(player.teamId, player.commander, name),
                spawnNumber: readNumber(rawUnit, "spawnNumber", "SpawnNumber"),
                spawnGameloop,
                expiresGameloop,
                spawnX,
                spawnY,
                deltaX: targetX - spawnX,
                deltaY: targetY - spawnY,
                inverseLifetime: 1 / Math.max(1, expiresGameloop - spawnGameloop),
                radius: readOptionalNumber(rawUnit, "radius", "Radius") ?? 8,
                color: readString(rawUnit, "color", "Color", "#EC7063"),
                teamId: player.teamId,
                iconDefinition: null,
                iconResolved: false,
                render: null
            };
            player.units.push(unit);
            units.push(unit);
        }

        players.push(player);
    }

    units.sort((left, right) => left.spawnGameloop - right.spawnGameloop || left.expiresGameloop - right.expiresGameloop);

    return {
        durationGameloop: readNumber(replay, "durationGameloop", "DurationGameloop"),
        stepGameloops: readOptionalNumber(replay, "stepGameloops", "StepGameloops") ?? 112,
        bounds,
        stats: replay.stats ?? replay.Stats,
        summary: normalizeSummary(replay),
        middleControl: normalizeMiddleControl(replay),
        landmarks: readArray(replay, "landmarks", "Landmarks").map(asObject),
        buildUnits: readArray(replay, "buildUnits", "BuildUnits"),
        snapshots: readArray(replay, "snapshots", "Snapshots"),
        players,
        units,
        ng: null
    };
}

export function normalizeUnitLifeCosts(value: unknown): Map<string, UnitLifeCost> {
    const result = new Map<string, UnitLifeCost>();
    const entries = Array.isArray(value) ? value : [];
    for (const entryValue of entries) {
        const entry = normalizeUnitLifeCostEntry(entryValue);
        if (entry !== null) {
            result.set(entry.key, { cost: entry.cost, life: entry.life });
        }
    }

    return result;
}

function normalizeUnitLifeCostEntry(value: unknown): UnitLifeCostEntry | null {
    const entry = asObject(value);
    const key = readString(entry, "key", "Key");
    const cost = readOptionalNumber(entry, "cost", "Cost");
    const life = readOptionalNumber(entry, "life", "Life");
    if (key.length === 0 || cost === null || life === null) {
        return null;
    }

    return {
        key,
        cost: Math.max(0, Math.round(cost)),
        life: Math.max(0, Math.round(life))
    };
}

export function createAliveUnitHighlightKey(teamId: number, commander: string, unitName: string): string {
    return `${teamId}|${commander.length}:${commander}|${unitName.length}:${unitName}`;
}

export function normalizeSummary(replayValue: unknown): PlaybackSummary {
    const replay = asObject(replayValue);
    const summary = asObject(replay.summary ?? replay.Summary);
    const players = readArray(summary, "players", "Players")
        .map(normalizePlayerSummary);
    const topUnits = readArray(summary, "topUnits", "TopUnits")
        .map(normalizeTopUnitSummary);

    return {
        totalKills: readNumber(summary, "totalKills", "TotalKills"),
        players,
        topUnits
    };
}

function normalizePlayerSummary(value: unknown): PlaybackPlayerSummary {
    const row = asObject(value);
    return {
        playerName: readString(row, "playerName", "PlayerName"),
        teamId: readNumber(row, "teamId", "TeamId"),
        gamePos: readNumber(row, "gamePos", "GamePos"),
        commander: readString(row, "commander", "Commander"),
        kills: readNumber(row, "kills", "Kills")
    };
}

function normalizeTopUnitSummary(value: unknown): PlaybackTopUnitSummary {
    const row = asObject(value);
    return {
        playerName: readString(row, "playerName", "PlayerName"),
        teamId: readNumber(row, "teamId", "TeamId"),
        gamePos: readNumber(row, "gamePos", "GamePos"),
        unitName: readString(row, "unitName", "UnitName"),
        kills: readNumber(row, "kills", "Kills")
    };
}

export function normalizeBounds(boundsValue: unknown): Bounds {
    const bounds = asObject(boundsValue);
    return {
        minX: readOptionalNumber(bounds, "minX", "MinX") ?? 0,
        minY: readOptionalNumber(bounds, "minY", "MinY") ?? 0,
        maxX: readOptionalNumber(bounds, "maxX", "MaxX") ?? MAP_WIDTH,
        maxY: readOptionalNumber(bounds, "maxY", "MaxY") ?? MAP_HEIGHT
    };
}

export function normalizeMiddleControl(replayValue: unknown): MiddleControl {
    const replay = asObject(replayValue);
    const middleControl = asObject(replay.middleControl ?? replay.MiddleControl);
    const firstTeamId = readNumber(middleControl, "firstTeamId", "FirstTeamId");
    const rawChangeGameloops = readArray(middleControl, "changeGameloops", "ChangeGameloops");
    const changeGameloops = firstTeamId === 1 || firstTeamId === 2
        ? rawChangeGameloops.filter(isFiniteNumber)
        : [];

    return {
        firstTeamId: changeGameloops.length > 0 ? firstTeamId : 0,
        changeGameloops
    };
}

export function normalizeRefineryGameloops(playerValue: unknown): number[] {
    const player = asObject(playerValue);
    return readArray(player, "refineryGameloops", "RefineryGameloops")
        .filter(isFiniteNumber)
        .sort(compareNumber);
}

export function normalizeTierUpgradeGameloops(playerValue: unknown): number[] {
    const player = asObject(playerValue);
    return readArray(player, "tierUpgradeGameloops", "TierUpgradeGameloops")
        .filter(isFiniteNumber)
        .sort(compareNumber);
}

export function readString(record: RawObject, camelName: string, pascalName: string, fallback = ""): string {
    const value = record[camelName] ?? record[pascalName];
    return typeof value === "string" ? value : fallback;
}

export function readNumber(record: RawObject, camelName: string, pascalName: string, fallback = 0): number {
    const value = readOptionalNumber(record, camelName, pascalName);
    return value ?? fallback;
}

export function readOptionalNumber(record: RawObject, camelName: string, pascalName: string): number | null {
    const value = record[camelName] ?? record[pascalName];
    return isFiniteNumber(value) ? value : null;
}

export function readArray(record: RawObject, camelName: string, pascalName: string): unknown[] {
    const value = record[camelName] ?? record[pascalName];
    return Array.isArray(value) ? value : [];
}

export function readObject(record: RawObject, camelName: string, pascalName: string): RawObject {
    return asObject(record[camelName] ?? record[pascalName]);
}

export function asObject(value: unknown): RawObject {
    return value !== null && typeof value === "object" ? value as RawObject : {};
}

function isFiniteNumber(value: unknown): value is number {
    return typeof value === "number" && Number.isFinite(value);
}

function compareNumber(left: number, right: number): number {
    return left - right;
}
