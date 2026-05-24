import {
    GAS_BADGE_CORNER_PADDING,
    GAS_BADGE_GAP,
    GAS_BADGE_HEIGHT,
    GAS_BADGE_WIDTH,
    GRID_INTERVAL,
    MAP_CENTER_SUM,
    TEAM_SPAWN_AREAS
} from "./constants";
import {
    clipDiffLine,
    clipSumLine,
    createProjection,
    deviceScale,
    project,
    projectSegment,
    roundUpToInterval
} from "./canvasUtils";
import {
    asObject,
    normalizeMiddleControl,
    normalizeRefineryGameloops,
    normalizeTierUpgradeGameloops,
    readNumber,
    readOptionalNumber,
    readString
} from "./normalization";
import type {
    Bounds,
    LandmarkGeometry,
    NormalizedPlayer,
    NormalizedReplay,
    PlayerGasBadge,
    Point,
    Segment,
    SpawnAreaGeometry,
    SpawnAreaLabelGeometry,
    StaticGeometry,
    TeamSpawnAreaSource
} from "./types";

export function createRenderCache(bounds: Bounds, canvas: HTMLCanvasElement) {
    return {
        projection: createProjection(bounds, canvas)
    };
}

export function createStaticGeometry(
    replay: NormalizedReplay,
    bounds: Bounds,
    canvas: HTMLCanvasElement): StaticGeometry {
    const spawnAreas = createSpawnAreas(bounds, canvas);

    return {
        gridLines: createGridLines(bounds, canvas),
        middleLine: createMiddleLine(bounds, canvas),
        middleControl: normalizeMiddleControl(replay),
        spawnAreas,
        playerGasBadges: createPlayerGasBadges(replay.players, spawnAreas, canvas),
        landmarks: replay.landmarks.map(landmark => normalizeLandmark(landmark, bounds, canvas))
    };
}

function createGridLines(bounds: Bounds, canvas: HTMLCanvasElement): Segment[] {
    const lines: Segment[] = [];
    const sumMin = bounds.minX + bounds.minY;
    const sumMax = bounds.maxX + bounds.maxY;
    const diffMin = bounds.minX - bounds.maxY;
    const diffMax = bounds.maxX - bounds.minY;

    for (let sum = roundUpToInterval(sumMin, GRID_INTERVAL); sum <= sumMax; sum += GRID_INTERVAL) {
        if (Math.abs(sum - MAP_CENTER_SUM) < 0.001) {
            continue;
        }

        const segment = clipSumLine(bounds, sum);
        if (segment) {
            lines.push(projectSegment(segment, bounds, canvas));
        }
    }

    for (let diff = roundUpToInterval(diffMin, GRID_INTERVAL); diff <= diffMax; diff += GRID_INTERVAL) {
        const segment = clipDiffLine(bounds, diff);
        if (segment) {
            lines.push(projectSegment(segment, bounds, canvas));
        }
    }

    return lines;
}

function createMiddleLine(bounds: Bounds, canvas: HTMLCanvasElement): Segment | null {
    const segment = clipSumLine(bounds, MAP_CENTER_SUM);
    return segment ? shortenSegment(projectSegment(segment, bounds, canvas), 1 / 3) : null;
}

function shortenSegment(segment: Segment, fraction: number): Segment {
    const clampedFraction = Math.max(0, Math.min(1, fraction));
    const midX = (segment.start.x + segment.end.x) / 2;
    const midY = (segment.start.y + segment.end.y) / 2;
    const halfX = (segment.end.x - segment.start.x) * clampedFraction / 2;
    const halfY = (segment.end.y - segment.start.y) * clampedFraction / 2;

    return {
        start: {
            x: midX - halfX,
            y: midY - halfY
        },
        end: {
            x: midX + halfX,
            y: midY + halfY
        }
    };
}

function createSpawnAreas(bounds: Bounds, canvas: HTMLCanvasElement): SpawnAreaGeometry[] {
    return TEAM_SPAWN_AREAS.map(area => {
        const points = area.points.map(point => project(point.x, point.y, bounds, canvas));
        return {
            teamId: area.teamId,
            label: area.label,
            color: area.color,
            points,
            labelGeometry: createSpawnAreaLabelGeometry(area, points, canvas)
        };
    });
}

function normalizeLandmark(landmarkValue: unknown, bounds: Bounds, canvas: HTMLCanvasElement): LandmarkGeometry {
    const landmark = asObject(landmarkValue);
    const x = readOptionalNumber(landmark, "x", "X");
    const y = readOptionalNumber(landmark, "y", "Y");
    const kind = readString(landmark, "kind", "Kind", "Defense");
    const teamId = readNumber(landmark, "teamId", "TeamId");
    const color = readString(landmark, "color", "Color", teamId === 1 ? "#5DADEC" : "#F87171");
    const kills = readNumber(landmark, "kills", "Kills");
    const label = readString(landmark, "name", "Name", kind);
    const radius = Math.max(7, readNumber(landmark, "radius", "Radius", 10) * deviceScale(canvas) * 0.7);
    const diedGameloop = readOptionalNumber(landmark, "diedGameloop", "DiedGameloop");

    return {
        x,
        y,
        kind,
        teamId,
        color,
        kills,
        label,
        radius,
        diedGameloop,
        projected: x == null || y == null ? null : project(x, y, bounds, canvas)
    };
}

function createSpawnAreaLabelGeometry(
    area: TeamSpawnAreaSource,
    points: Point[],
    canvas: HTMLCanvasElement): SpawnAreaLabelGeometry | null {
    if (points.length === 0) {
        return null;
    }

    const start = points[area.labelSegment[0]];
    const end = points[area.labelSegment[1]];
    if (!start || !end) {
        return null;
    }

    const midpointX = (start.x + end.x) / 2;
    const midpointY = (start.y + end.y) / 2;
    const centroid = getCentroid(points);
    const dx = end.x - start.x;
    const dy = end.y - start.y;
    const length = Math.sqrt((dx * dx) + (dy * dy));
    if (length <= 0) {
        return null;
    }

    let normalX = -dy / length;
    let normalY = dx / length;
    const outwardX = midpointX - centroid.x;
    const outwardY = midpointY - centroid.y;
    if ((normalX * outwardX) + (normalY * outwardY) < 0) {
        normalX = -normalX;
        normalY = -normalY;
    }
    if (area.labelSide === -1) {
        normalX = -normalX;
        normalY = -normalY;
    }

    let angle = Math.atan2(dy, dx);
    if (angle > Math.PI / 2 || angle < -Math.PI / 2) {
        angle += Math.PI;
    }

    const offset = 12 * deviceScale(canvas);
    return {
        x: midpointX + normalX * offset,
        y: midpointY + normalY * offset,
        angle
    };
}

function getCentroid(points: Point[]): Point {
    let x = 0;
    let y = 0;
    for (const point of points) {
        x += point.x;
        y += point.y;
    }

    return {
        x: x / points.length,
        y: y / points.length
    };
}

function createPlayerGasBadges(
    players: NormalizedPlayer[],
    spawnAreas: SpawnAreaGeometry[],
    canvas: HTMLCanvasElement): PlayerGasBadge[] {
    const badges: PlayerGasBadge[] = [];
    const scale = deviceScale(canvas);
    const width = GAS_BADGE_WIDTH * scale;
    const height = GAS_BADGE_HEIGHT * scale;
    const gap = GAS_BADGE_GAP * scale;
    const padding = GAS_BADGE_CORNER_PADDING * scale;

    for (const spawnArea of spawnAreas) {
        const teamPlayers = players
            .filter(player => player.teamId === spawnArea.teamId)
            .sort((left, right) => left.gamePos - right.gamePos);
        if (teamPlayers.length === 0) {
            continue;
        }

        const totalWidth = teamPlayers.length * width + Math.max(0, teamPlayers.length - 1) * gap;
        const startX = spawnArea.teamId === 1
            ? canvas.width - padding - totalWidth + width / 2
            : padding + width / 2;
        const y = spawnArea.teamId === 1
            ? padding + height / 2
            : canvas.height - padding - height / 2;

        for (let i = 0; i < teamPlayers.length; i++) {
            const player = teamPlayers[i];
            badges.push({
                x: startX + i * (width + gap),
                y,
                gamePos: player.gamePos,
                color: spawnArea.color,
                refineryGameloops: normalizeRefineryGameloops(player),
                tierUpgradeGameloops: normalizeTierUpgradeGameloops(player)
            });
        }
    }

    return badges;
}
