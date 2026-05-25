import {
    GAS_BADGE_HEIGHT,
    GAS_BADGE_WIDTH,
    MIN_CATALOG_ICON_CSS_SIZE,
    NEUTRAL_MIDDLE_LINE_COLOR,
    TEAM_COLORS
} from "./constants";
import {
    clamp,
    createLayerCanvas,
    deviceScale,
    drawRoundedRect,
    getCanvasContext,
    projectX,
    projectY,
    resizeCanvas,
    withAlpha
} from "./canvasUtils";
import { createRenderCache, createStaticGeometry } from "./geometry";
import { objectiveIconCatalog } from "./objectiveIcons";
import { getState } from "./store";
import type {
    CanvasContext,
    LandmarkGeometry,
    LayerCanvas,
    MiddleControl,
    NormalizedUnit,
    ObjectiveDeathAnnouncement,
    PlaybackSummary,
    PlayerGasBadge,
    Projection,
    Segment,
    SpawnAreaGeometry,
    SpawnPlaybackState,
    StaticGeometry
} from "./types";
import { unitIconCatalog } from "./unitIcons";

const MAP_BACKGROUND_BASE_COLOR = "#c1bda4";
const WATER_BACKGROUND_BASE_COLOR = "#56aeca";
const BACKGROUND_BASELINE_AREA = 960 * 560;
const OBJECTIVE_DEATH_ANNOUNCEMENT_SECONDS = 28;
const OBJECTIVE_DEATH_ANNOUNCEMENT_FADE_SECONDS = 7;
const OBJECTIVE_DEATH_ANNOUNCEMENT_HOLD_SECONDS = 14;
const OBJECTIVE_DEATH_LABELS = new Set(["Bunker", "Cannon"]);
const ALIVE_UNIT_HIGHLIGHT_COLOR = "#F8D34A";
const FOREST_CLUSTERS = [
    { x: 0.21, y: 0.19, width: 0.18, height: 0.12, trees: 18 },
    { x: 0.27, y: 0.36, width: 0.15, height: 0.12, trees: 16 },
    { x: 0.48, y: 0.17, width: 0.13, height: 0.10, trees: 12 },
    { x: 0.13, y: 0.61, width: 0.14, height: 0.17, trees: 18 },
    { x: 0.48, y: 0.84, width: 0.24, height: 0.13, trees: 28 },
    { x: 0.72, y: 0.62, width: 0.13, height: 0.17, trees: 18 },
    { x: 0.87, y: 0.41, width: 0.09, height: 0.12, trees: 10 }
];
const DRAW_DIAGNOSTIC_FIRST_DRAWS = 5;
const DRAW_DIAGNOSTIC_SLOW_MS = 16;
const drawDiagnosticCounts = new WeakMap<HTMLCanvasElement, number>();

export function drawSpawnPlayback(canvas: HTMLCanvasElement, currentGameloop: number, source = "unknown"): void {
    const drawStarted = performance.now();
    const stages: string[] = [];
    let stageStarted = drawStarted;
    let rebuiltStaticCache = false;
    const markStage = (name: string): void => {
        const now = performance.now();
        stages.push(`${name}=${(now - stageStarted).toFixed(1)}ms`);
        stageStarted = now;
    };

    const state = getState(canvas);
    if (!state?.replay) {
        return;
    }

    const resized = resizeCanvas(canvas, source);
    markStage("resizeCanvas");
    const ctx = getCanvasContext(canvas);
    markStage("getContext");
    const replay = state.replay;
    const bounds = replay.bounds;
    if (!ctx || !bounds) {
        logDrawDiagnostic(canvas, source, drawStarted, stages, resized, rebuiltStaticCache, 0, "missing context/bounds");
        return;
    }

    state.currentGameloop = clampGameloop(state, currentGameloop);
    markStage("clampGameloop");
    if (resized
        || !state.staticGeometry
        || !state.renderCache
        || state.staticCanvasWidth !== canvas.width
        || state.staticCanvasHeight !== canvas.height) {
        rebuiltStaticCache = true;
        state.renderCache = createRenderCache(bounds, canvas);
        markStage("createRenderCache");
        state.staticGeometry = createStaticGeometry(replay, bounds, canvas);
        markStage("createStaticGeometry");
        state.staticBackgroundCanvas = createStaticBackgroundCanvas(canvas, state.staticGeometry);
        markStage("createStaticBackgroundCanvas");
        state.objectiveDeathAnnouncements = createObjectiveDeathAnnouncements(
            state.staticGeometry.landmarks,
            replay.stepGameloops,
            state.gameloopsPerSecond);
        markStage("createObjectiveAnnouncements");
        state.staticCanvasWidth = canvas.width;
        state.staticCanvasHeight = canvas.height;
        state.activeUnits.length = 0;
        state.nextUnitIndex = 0;
        state.lastActiveGameloop = Number.NEGATIVE_INFINITY;
        prepareUnitSprites(state, canvas);
        markStage("prepareUnitSprites");
    }

    if (!state.staticBackgroundCanvas || !state.staticGeometry || !state.renderCache) {
        logDrawDiagnostic(canvas, source, drawStarted, stages, resized, rebuiltStaticCache, 0, "missing cache");
        return;
    }

    ctx.drawImage(state.staticBackgroundCanvas, 0, 0);
    markStage("drawStaticBackground");
    drawDynamicMapLayer(ctx, canvas, state.staticGeometry, state.currentGameloop);
    markStage("drawDynamicMapLayer");
    const activeUnits = getActiveUnits(state, state.currentGameloop);
    markStage("getActiveUnits");
    const drawnUnits = drawUnitLayer(ctx, state.renderCache.projection, activeUnits, state.currentGameloop);
    markStage("drawUnitLayer");
    if (state.highlightedAliveUnitKey !== null) {
        drawAliveUnitHighlightLayer(
            ctx,
            state.renderCache.projection,
            activeUnits,
            state.currentGameloop,
            state.highlightedAliveUnitKey);
        markStage("drawAliveUnitHighlightLayer");
    }

    if (drawnUnits === 0) {
        drawEmptyState(ctx, canvas);
        markStage("drawEmptyState");
    }

    drawObjectiveDeathAnnouncements(ctx, canvas, state.objectiveDeathAnnouncements, state.currentGameloop);
    markStage("drawObjectiveDeathAnnouncements");
    drawEndOfReplaySummary(ctx, canvas, replay.summary, state.currentGameloop, replay.durationGameloop);
    markStage("drawEndOfReplaySummary");
    logDrawDiagnostic(canvas, source, drawStarted, stages, resized, rebuiltStaticCache, activeUnits.length);
}

export function clampGameloop(state: SpawnPlaybackState, gameloop: number): number {
    const duration = state.replay.durationGameloop ?? 0;
    return clamp(Number.isFinite(gameloop) ? gameloop : 0, 0, duration);
}

function prepareUnitSprites(state: SpawnPlaybackState, canvas: HTMLCanvasElement): void {
    const scale = deviceScale(canvas);
    state.unitSpriteCache.clear();
    for (const unit of state.replay.units) {
        const radius = Math.max(3, unit.radius * scale * 0.55);
        if (!unit.iconResolved) {
            unit.iconDefinition = unitIconCatalog.resolve(unit.commander, unit.name);
            unit.iconResolved = true;
        }

        unit.render = {
            radius,
            sprite: getUnitSprite(state, unit, radius, scale)
        };
    }
}

function logDrawDiagnostic(
    canvas: HTMLCanvasElement,
    source: string,
    startedAt: number,
    stages: string[],
    resized: boolean,
    rebuiltStaticCache: boolean,
    activeUnitCount: number,
    note = ""): void {
    const elapsed = performance.now() - startedAt;
    const drawCount = (drawDiagnosticCounts.get(canvas) ?? 0) + 1;
    drawDiagnosticCounts.set(canvas, drawCount);
    if (!rebuiltStaticCache && drawCount > DRAW_DIAGNOSTIC_FIRST_DRAWS && elapsed < DRAW_DIAGNOSTIC_SLOW_MS) {
        return;
    }

    const suffix = note ? ` ${note}` : "";
    console.log(
        `spawnPlayback draw #${drawCount} source=${source} elapsed=${elapsed.toFixed(1)}ms resized=${resized} rebuilt=${rebuiltStaticCache} active=${activeUnitCount}${suffix} stages=[${stages.join(", ")}] - ${Date.now()}`);
}

function createStaticBackgroundCanvas(canvas: HTMLCanvasElement, geometry: StaticGeometry): LayerCanvas | null {
    const backgroundCanvas = createLayerCanvas(canvas.width, canvas.height);
    const ctx = getCanvasContext(backgroundCanvas);
    if (!ctx) {
        return null;
    }

    drawStaticBackgroundLayer(ctx, canvas, geometry);
    return backgroundCanvas;
}

function getUnitSprite(
    state: SpawnPlaybackState,
    unit: NormalizedUnit,
    radius: number,
    canvasScale: number): LayerCanvas {
    const color = unit.color;
    const teamId = unit.teamId;
    const iconDefinition = unit.iconDefinition;
    const iconColor = iconDefinition ? TEAM_COLORS[teamId] ?? color : color;
    const key = iconDefinition
        ? `${iconDefinition.id}|${unit.commander}|${unit.name}|${teamId}|${iconColor}|${Math.round(radius * 10)}`
        : `${teamId}|${color}|${Math.round(radius * 10)}`;
    const cached = state.unitSpriteCache.get(key);
    if (cached) {
        return cached;
    }

    const scale = Math.max(1, radius / 3);
    const padding = Math.ceil(3 * scale);
    const iconSize = iconDefinition
        ? Math.max(radius * 2.6, MIN_CATALOG_ICON_CSS_SIZE * canvasScale)
        : radius * 2;
    const size = Math.ceil(iconSize + padding * 2);
    const sprite = createLayerCanvas(size, size);
    const ctx = getCanvasContext(sprite);
    if (!ctx) {
        return sprite;
    }

    const center = size / 2;
    ctx.save();
    ctx.globalAlpha = teamId === 1 ? 0.92 : 0.78;
    if (iconDefinition) {
        unitIconCatalog.render(ctx, iconDefinition, {
            x: center,
            y: center,
            size: iconSize,
            teamColor: iconColor
        });
    } else {
        ctx.fillStyle = withAlpha(color, "99");
        ctx.strokeStyle = withAlpha(color, "EE");
        ctx.lineWidth = Math.max(1.5, 1.5 * scale);
        ctx.beginPath();
        ctx.arc(center, center, radius, 0, Math.PI * 2);
        ctx.fill();
        ctx.stroke();
    }

    ctx.restore();
    state.unitSpriteCache.set(key, sprite);
    return sprite;
}

function getActiveUnits(state: SpawnPlaybackState, currentGameloop: number): NormalizedUnit[] {
    if (currentGameloop < state.lastActiveGameloop) {
        rebuildActiveUnits(state, currentGameloop);
        return state.activeUnits;
    }

    const units = state.replay.units;
    while (state.nextUnitIndex < units.length && units[state.nextUnitIndex].spawnGameloop <= currentGameloop) {
        state.activeUnits.push(units[state.nextUnitIndex]);
        state.nextUnitIndex++;
    }

    compactActiveUnits(state, currentGameloop);
    state.lastActiveGameloop = currentGameloop;
    return state.activeUnits;
}

function rebuildActiveUnits(state: SpawnPlaybackState, currentGameloop: number): void {
    state.activeUnits.length = 0;
    const units = state.replay.units;
    let index = 0;
    while (index < units.length && units[index].spawnGameloop <= currentGameloop) {
        const unit = units[index];
        if (unit.expiresGameloop > currentGameloop) {
            state.activeUnits.push(unit);
        }

        index++;
    }

    state.nextUnitIndex = index;
    state.lastActiveGameloop = currentGameloop;
}

function compactActiveUnits(state: SpawnPlaybackState, currentGameloop: number): void {
    const activeUnits = state.activeUnits;
    let writeIndex = 0;
    for (let readIndex = 0; readIndex < activeUnits.length; readIndex++) {
        const unit = activeUnits[readIndex];
        if (unit.expiresGameloop > currentGameloop) {
            activeUnits[writeIndex] = unit;
            writeIndex++;
        }
    }

    activeUnits.length = writeIndex;
}

function drawStaticBackgroundLayer(ctx: CanvasContext, canvas: HTMLCanvasElement, geometry: StaticGeometry): void {
    ctx.save();
    drawCloudyBackground(ctx, canvas);
    drawForestAssets(ctx, canvas);
    ctx.restore();

    drawGrid(ctx, canvas, geometry.gridLines);
    drawSpawnAreas(ctx, canvas, geometry.spawnAreas);
}

function drawCloudyBackground(ctx: CanvasContext, canvas: HTMLCanvasElement): void {
    const scale = deviceScale(canvas);
    const random = createBackgroundRandom(canvas.width, canvas.height);
    const patchScale = getBackgroundAreaScale(canvas, scale);

    drawWaterBackground(ctx, canvas, random, scale, patchScale);
    drawShoreBands(ctx, canvas, scale);

    ctx.save();
    traceIslandPath(ctx, canvas, 0);
    ctx.clip();

    ctx.fillStyle = MAP_BACKGROUND_BASE_COLOR;
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    drawCloudPatches(ctx, canvas, random, Math.round(90 * patchScale), scale, {
        offset: 100,
        minRadius: 80,
        maxRadius: 260,
        centerColor: "255,255,245",
        minAlpha: 0.025,
        maxAlpha: 0.08
    });

    drawCloudPatches(ctx, canvas, random, Math.round(70 * patchScale), scale, {
        offset: 100,
        minRadius: 60,
        maxRadius: 220,
        centerColor: "80,90,80",
        minAlpha: 0.018,
        maxAlpha: 0.055
    });

    drawWispyBackgroundStrokes(ctx, canvas, random, Math.round(140 * patchScale), scale);
    drawFineGrain(ctx, canvas, random, scale);
    ctx.restore();
}

function drawWaterBackground(
    ctx: CanvasContext,
    canvas: HTMLCanvasElement,
    random: () => number,
    scale: number,
    patchScale: number): void {
    const gradient = ctx.createLinearGradient(0, 0, canvas.width, canvas.height);
    gradient.addColorStop(0, "#58b9d8");
    gradient.addColorStop(0.55, WATER_BACKGROUND_BASE_COLOR);
    gradient.addColorStop(1, "#3287b8");

    ctx.fillStyle = gradient;
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    ctx.save();
    ctx.globalAlpha = 0.25;
    ctx.lineCap = "round";
    for (let i = 0; i < Math.round(90 * patchScale); i++) {
        const x = rand(random, -40 * scale, canvas.width);
        const y = rand(random, 0, canvas.height);
        const length = rand(random, 36 * scale, 130 * scale);
        ctx.strokeStyle = random() > 0.45
            ? "rgba(210, 244, 240, 0.30)"
            : "rgba(24, 111, 158, 0.26)";
        ctx.lineWidth = rand(random, 1 * scale, 2.25 * scale);
        ctx.beginPath();
        ctx.moveTo(x, y);
        ctx.bezierCurveTo(
            x + length * 0.35,
            y + rand(random, -10 * scale, 10 * scale),
            x + length * 0.65,
            y + rand(random, -10 * scale, 10 * scale),
            x + length,
            y + rand(random, -8 * scale, 8 * scale));
        ctx.stroke();
    }

    ctx.restore();
}

function drawShoreBands(ctx: CanvasContext, canvas: HTMLCanvasElement, scale: number): void {
    ctx.save();
    traceIslandPath(ctx, canvas, 24 * scale);
    ctx.fillStyle = "rgba(218, 213, 169, 0.74)";
    ctx.fill();

    traceIslandPath(ctx, canvas, 12 * scale);
    ctx.fillStyle = "rgba(196, 190, 143, 0.70)";
    ctx.fill();

    traceIslandPath(ctx, canvas, 0);
    ctx.fillStyle = MAP_BACKGROUND_BASE_COLOR;
    ctx.fill();

    ctx.lineWidth = Math.max(2, 2 * scale);
    ctx.strokeStyle = "rgba(245, 238, 190, 0.42)";
    traceIslandPath(ctx, canvas, 24 * scale);
    ctx.stroke();

    ctx.lineWidth = Math.max(1, 1.25 * scale);
    ctx.strokeStyle = "rgba(65, 125, 136, 0.30)";
    traceIslandPath(ctx, canvas, 36 * scale);
    ctx.stroke();
    ctx.restore();
}

function traceIslandPath(ctx: CanvasContext, canvas: HTMLCanvasElement, expand: number): void {
    const width = canvas.width;
    const height = canvas.height;
    const left = 0.075 * width - expand;
    const right = 0.925 * width + expand;
    const top = 0.105 * height - expand;
    const bottom = 0.895 * height + expand;
    const upperRight = 0.985 * width + expand;
    const upperTop = 0.055 * height - expand;
    const lowerLeft = 0.02 * width - expand;
    const lowerBottom = 0.95 * height + expand;

    ctx.beginPath();
    ctx.moveTo(0.19 * width, top + 0.03 * height);
    ctx.bezierCurveTo(0.32 * width, top - 0.045 * height, 0.47 * width, top - 0.01 * height, 0.58 * width, top + 0.02 * height);
    ctx.bezierCurveTo(0.72 * width, upperTop - 0.035 * height, upperRight - 0.015 * width, upperTop + 0.02 * height, upperRight, 0.21 * height);
    ctx.bezierCurveTo(upperRight + 0.01 * width, 0.35 * height, 0.84 * width, 0.45 * height, 0.90 * width, 0.58 * height);
    ctx.bezierCurveTo(0.95 * width, 0.73 * height, 0.79 * width, bottom + 0.03 * height, 0.63 * width, bottom);
    ctx.bezierCurveTo(0.51 * width, lowerBottom + 0.035 * height, 0.39 * width, 0.93 * height, 0.28 * width, lowerBottom - 0.01 * height);
    ctx.bezierCurveTo(0.12 * width, lowerBottom + 0.03 * height, lowerLeft - 0.015 * width, 0.77 * height, lowerLeft, 0.61 * height);
    ctx.bezierCurveTo(left - 0.035 * width, 0.48 * height, 0.15 * width, 0.39 * height, left + 0.01 * width, 0.28 * height);
    ctx.bezierCurveTo(left + 0.035 * width, 0.18 * height, 0.10 * width, 0.15 * height, 0.19 * width, top + 0.03 * height);
    ctx.closePath();
}

function getBackgroundAreaScale(canvas: HTMLCanvasElement, scale: number): number {
    const cssWidth = canvas.width / Math.max(1, scale);
    const cssHeight = canvas.height / Math.max(1, scale);
    return clamp((cssWidth * cssHeight) / BACKGROUND_BASELINE_AREA, 0.65, 1.8);
}

function drawCloudPatches(
    ctx: CanvasContext,
    canvas: HTMLCanvasElement,
    random: () => number,
    count: number,
    scale: number,
    options: {
        offset: number;
        minRadius: number;
        maxRadius: number;
        centerColor: string;
        minAlpha: number;
        maxAlpha: number;
    }): void {
    const offset = options.offset * scale;
    for (let i = 0; i < count; i++) {
        const x = rand(random, -offset, canvas.width + offset);
        const y = rand(random, -offset, canvas.height + offset);
        const radius = rand(random, options.minRadius * scale, options.maxRadius * scale);
        const alpha = rand(random, options.minAlpha, options.maxAlpha);
        const gradient = ctx.createRadialGradient(x, y, 0, x, y, radius);
        gradient.addColorStop(0, `rgba(${options.centerColor},${alpha})`);
        gradient.addColorStop(1, `rgba(${options.centerColor},0)`);

        ctx.fillStyle = gradient;
        ctx.beginPath();
        ctx.arc(x, y, radius, 0, Math.PI * 2);
        ctx.fill();
    }
}

function drawWispyBackgroundStrokes(
    ctx: CanvasContext,
    canvas: HTMLCanvasElement,
    random: () => number,
    count: number,
    scale: number): void {
    ctx.save();
    ctx.globalAlpha = 0.22;
    ctx.lineCap = "round";

    for (let i = 0; i < count; i++) {
        const x = rand(random, -100 * scale, canvas.width);
        const y = rand(random, 0, canvas.height);
        const length = rand(random, 80 * scale, 260 * scale);

        ctx.strokeStyle = random() > 0.5
            ? "rgba(245,245,230,0.18)"
            : "rgba(85,95,85,0.12)";
        ctx.lineWidth = rand(random, 1 * scale, 4 * scale);

        ctx.beginPath();
        ctx.moveTo(x, y);
        ctx.bezierCurveTo(
            x + length * 0.25,
            y + rand(random, -20 * scale, 20 * scale),
            x + length * 0.7,
            y + rand(random, -30 * scale, 30 * scale),
            x + length,
            y + rand(random, -18 * scale, 18 * scale));
        ctx.stroke();
    }

    ctx.restore();
}

function drawFineGrain(
    ctx: CanvasContext,
    canvas: HTMLCanvasElement,
    random: () => number,
    scale: number): void {
    const tileSize = Math.max(96, Math.round(128 * Math.min(scale, 2)));
    const grainCanvas = createLayerCanvas(tileSize, tileSize);
    const grainCtx = getCanvasContext(grainCanvas);
    if (!grainCtx) {
        return;
    }

    const imageData = grainCtx.createImageData(tileSize, tileSize);
    const data = imageData.data;
    for (let i = 0; i < data.length; i += 4) {
        const noise = Math.round(rand(random, 0, 32));
        data[i] = noise;
        data[i + 1] = noise;
        data[i + 2] = noise;
        data[i + 3] = 34;
    }

    grainCtx.putImageData(imageData, 0, 0);
    const pattern = ctx.createPattern(grainCanvas, "repeat");
    if (!pattern) {
        return;
    }

    ctx.save();
    ctx.globalCompositeOperation = "overlay";
    ctx.fillStyle = pattern;
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    ctx.restore();
}

function drawForestAssets(ctx: CanvasContext, canvas: HTMLCanvasElement): void {
    const scale = deviceScale(canvas);
    const random = createBackgroundRandom(canvas.width + 17, canvas.height + 31);
    const areaScale = getBackgroundAreaScale(canvas, scale);

    ctx.save();
    traceIslandPath(ctx, canvas, 0);
    ctx.clip();
    for (const cluster of FOREST_CLUSTERS) {
        const centerX = cluster.x * canvas.width;
        const centerY = cluster.y * canvas.height;
        const width = cluster.width * canvas.width;
        const height = cluster.height * canvas.height;
        const treeCount = Math.round(cluster.trees * clamp(areaScale, 0.8, 1.35));

        drawForestGroundPatch(ctx, centerX, centerY, width, height, random);
        for (let i = 0; i < treeCount; i++) {
            const point = randomPointInEllipse(centerX, centerY, width * 0.45, height * 0.42, random);
            const size = rand(random, 5.5 * scale, 11 * scale);
            drawMinimalTree(ctx, point.x, point.y, size, random);
        }
    }

    ctx.restore();
}

function drawForestGroundPatch(
    ctx: CanvasContext,
    centerX: number,
    centerY: number,
    width: number,
    height: number,
    random: () => number): void {
    ctx.save();
    ctx.globalAlpha = 0.44;
    ctx.fillStyle = "rgba(28, 92, 42, 0.34)";
    ctx.beginPath();
    ctx.ellipse(centerX, centerY, width * rand(random, 0.42, 0.5), height * rand(random, 0.36, 0.48), rand(random, -0.45, 0.45), 0, Math.PI * 2);
    ctx.fill();

    ctx.globalAlpha = 0.28;
    ctx.fillStyle = "rgba(18, 73, 36, 0.30)";
    ctx.beginPath();
    ctx.ellipse(centerX + width * rand(random, -0.12, 0.12), centerY + height * rand(random, -0.14, 0.14), width * 0.36, height * 0.30, rand(random, -0.7, 0.7), 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();
}

function randomPointInEllipse(
    centerX: number,
    centerY: number,
    radiusX: number,
    radiusY: number,
    random: () => number): { x: number; y: number } {
    const angle = rand(random, 0, Math.PI * 2);
    const distance = Math.sqrt(random());
    return {
        x: centerX + Math.cos(angle) * radiusX * distance,
        y: centerY + Math.sin(angle) * radiusY * distance
    };
}

function drawMinimalTree(ctx: CanvasContext, x: number, y: number, size: number, random: () => number): void {
    const height = size * rand(random, 1.2, 1.8);
    const width = size * rand(random, 0.8, 1.15);
    const sway = rand(random, -0.12, 0.12);

    ctx.save();
    ctx.translate(x, y);
    ctx.rotate(sway);
    ctx.fillStyle = "rgba(42, 106, 49, 0.72)";
    drawTreeTriangle(ctx, 0, -height * 0.55, width * 0.58, height * 0.48);
    ctx.fillStyle = "rgba(23, 82, 38, 0.78)";
    drawTreeTriangle(ctx, 0, -height * 0.24, width * 0.72, height * 0.54);
    ctx.fillStyle = "rgba(17, 63, 32, 0.82)";
    drawTreeTriangle(ctx, 0, height * 0.08, width * 0.86, height * 0.58);
    ctx.strokeStyle = "rgba(77, 61, 35, 0.52)";
    ctx.lineWidth = Math.max(1, size * 0.13);
    ctx.beginPath();
    ctx.moveTo(0, height * 0.06);
    ctx.lineTo(0, height * 0.42);
    ctx.stroke();
    ctx.restore();
}

function drawTreeTriangle(ctx: CanvasContext, centerX: number, topY: number, width: number, height: number): void {
    ctx.beginPath();
    ctx.moveTo(centerX, topY);
    ctx.lineTo(centerX - width * 0.5, topY + height);
    ctx.lineTo(centerX + width * 0.5, topY + height);
    ctx.closePath();
    ctx.fill();
}

function createBackgroundRandom(width: number, height: number): () => number {
    let seed = (width * 73856093 ^ height * 19349663 ^ 0x6D2B79F5) >>> 0;
    return () => {
        seed = seed + 0x6D2B79F5 | 0;
        let value = Math.imul(seed ^ seed >>> 15, 1 | seed);
        value ^= value + Math.imul(value ^ value >>> 7, 61 | value);
        return ((value ^ value >>> 14) >>> 0) / 4294967296;
    };
}

function rand(random: () => number, min: number, max: number): number {
    return random() * (max - min) + min;
}

function drawDynamicMapLayer(
    ctx: CanvasContext,
    canvas: HTMLCanvasElement,
    geometry: StaticGeometry,
    currentGameloop: number): void {
    drawPlayerGasBadges(ctx, canvas, geometry.playerGasBadges, currentGameloop);
    drawMiddleLine(ctx, canvas, geometry.middleLine, geometry.middleControl, currentGameloop);

    for (const landmark of geometry.landmarks) {
        if (landmark.diedGameloop != null && landmark.diedGameloop <= currentGameloop) {
            continue;
        }

        drawLandmark(ctx, canvas, landmark.projected, landmark.radius, landmark.color, landmark.kind, landmark.label);
    }
}

export function createObjectiveDeathAnnouncements(
    landmarks: readonly LandmarkGeometry[],
    stepGameloops: number,
    gameloopsPerSecond: number): ObjectiveDeathAnnouncement[] {
    if (landmarks.length === 0) {
        return [];
    }

    const step = Math.max(1, Math.round(Number.isFinite(stepGameloops) ? stepGameloops : 1));
    const loopsPerSecond = Number.isFinite(gameloopsPerSecond) && gameloopsPerSecond > 0
        ? gameloopsPerSecond
        : 22.4;
    const fadeGameloops = Math.round(OBJECTIVE_DEATH_ANNOUNCEMENT_FADE_SECONDS * loopsPerSecond);
    const holdGameloops = Math.round(OBJECTIVE_DEATH_ANNOUNCEMENT_HOLD_SECONDS * loopsPerSecond);
    const durationGameloops = Math.round(OBJECTIVE_DEATH_ANNOUNCEMENT_SECONDS * loopsPerSecond);
    const announcements: ObjectiveDeathAnnouncement[] = [];

    for (const landmark of landmarks) {
        if (!OBJECTIVE_DEATH_LABELS.has(landmark.label) || landmark.diedGameloop == null) {
            continue;
        }

        const diedGameloop = landmark.diedGameloop;
        if (!Number.isFinite(diedGameloop)) {
            continue;
        }

        const anchorGameloop = Math.max(0, Math.round(diedGameloop / step) * step);
        const message = landmark.kills > 0
            ? `${landmark.label} down at ${formatGameloopClock(diedGameloop, loopsPerSecond)} with ${landmark.kills} kills`
            : `${landmark.label} down at ${formatGameloopClock(diedGameloop, loopsPerSecond)}`;

        announcements.push({
            message,
            accentColor: landmark.color,
            anchorGameloop,
            startGameloop: Math.max(0, anchorGameloop - fadeGameloops),
            holdEndGameloop: anchorGameloop + holdGameloops,
            endGameloop: anchorGameloop + durationGameloops - fadeGameloops
        });
    }

    return announcements;
}

function formatGameloopClock(gameloop: number, gameloopsPerSecond: number): string {
    const totalSeconds = Math.max(0, Math.round(gameloop / gameloopsPerSecond));
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    return `${minutes}:${seconds.toString().padStart(2, "0")}`;
}

function drawObjectiveDeathAnnouncements(
    ctx: CanvasContext,
    canvas: HTMLCanvasElement,
    announcements: readonly ObjectiveDeathAnnouncement[],
    currentGameloop: number): void {
    if (announcements.length === 0) {
        return;
    }

    for (const announcement of announcements) {
        if (currentGameloop < announcement.startGameloop || currentGameloop > announcement.endGameloop) {
            continue;
        }

        const alpha = getObjectiveDeathAnnouncementAlpha(announcement, currentGameloop);
        if (alpha <= 0) {
            continue;
        }

        drawObjectiveDeathAnnouncement(ctx, canvas, announcement, alpha);
    }
}

function getObjectiveDeathAnnouncementAlpha(
    announcement: ObjectiveDeathAnnouncement,
    currentGameloop: number): number {
    if (currentGameloop < announcement.anchorGameloop) {
        const fadeDuration = Math.max(1, announcement.anchorGameloop - announcement.startGameloop);
        return clamp((currentGameloop - announcement.startGameloop) / fadeDuration, 0, 1);
    }

    if (currentGameloop <= announcement.holdEndGameloop) {
        return 1;
    }

    const fadeDuration = Math.max(1, announcement.endGameloop - announcement.holdEndGameloop);
    return clamp(1 - (currentGameloop - announcement.holdEndGameloop) / fadeDuration, 0, 1);
}

function drawObjectiveDeathAnnouncement(
    ctx: CanvasContext,
    canvas: HTMLCanvasElement,
    announcement: ObjectiveDeathAnnouncement,
    alpha: number): void {
    const scale = deviceScale(canvas);
    const fontSize = Math.max(15, 16 * scale);
    const horizontalPadding = 18 * scale;
    const verticalPadding = 10 * scale;
    const accentWidth = 4 * scale;
    const y = Math.max(48 * scale, canvas.height * 0.18);

    ctx.save();
    ctx.globalAlpha = alpha;
    ctx.font = `700 ${fontSize}px sans-serif`;
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";

    const textWidth = ctx.measureText(announcement.message).width;
    const panelWidth = Math.min(canvas.width - 24 * scale, textWidth + horizontalPadding * 2 + accentWidth);
    const panelHeight = fontSize + verticalPadding * 2;
    const x = (canvas.width - panelWidth) / 2;
    const panelY = y - panelHeight / 2;
    const radius = 8 * scale;

    ctx.fillStyle = "rgba(7, 16, 21, 0.86)";
    drawRoundedRect(ctx, x, panelY, panelWidth, panelHeight, radius);
    ctx.fill();

    ctx.fillStyle = withAlpha(announcement.accentColor, "F2");
    drawRoundedRect(ctx, x, panelY, accentWidth, panelHeight, radius);
    ctx.fill();

    ctx.strokeStyle = "rgba(255, 255, 255, 0.18)";
    ctx.lineWidth = Math.max(1, scale);
    drawRoundedRect(ctx, x, panelY, panelWidth, panelHeight, radius);
    ctx.stroke();

    ctx.fillStyle = "rgba(255, 255, 255, 0.94)";
    ctx.fillText(announcement.message, canvas.width / 2 + accentWidth / 2, y, panelWidth - horizontalPadding * 2);
    ctx.restore();
}

export function isEndSummaryVisible(currentGameloop: number, durationGameloop: number): boolean {
    return Number.isFinite(currentGameloop)
        && Number.isFinite(durationGameloop)
        && durationGameloop > 0
        && currentGameloop >= durationGameloop;
}

function drawEndOfReplaySummary(
    ctx: CanvasContext,
    canvas: HTMLCanvasElement,
    summary: PlaybackSummary,
    currentGameloop: number,
    durationGameloop: number): void {
    if (!isEndSummaryVisible(currentGameloop, durationGameloop)
        || summary.players.length === 0 && summary.topUnits.length === 0) {
        return;
    }

    const scale = deviceScale(canvas);
    const panelWidth = Math.min(canvas.width - 28 * scale, 820 * scale);
    const stacked = panelWidth < 620 * scale;
    const padding = 16 * scale;
    const headerHeight = 34 * scale;
    const sectionTitleHeight = 24 * scale;
    const rowHeight = 18 * scale;
    const gap = 16 * scale;
    const playerRowsHeight = summary.players.length * rowHeight;
    const topRowsHeight = Math.max(1, summary.topUnits.length) * rowHeight;
    const sectionHeight = sectionTitleHeight + (stacked
        ? playerRowsHeight + topRowsHeight + sectionTitleHeight + gap
        : Math.max(playerRowsHeight, topRowsHeight));
    const panelHeight = Math.min(canvas.height - 28 * scale, padding * 2 + headerHeight + sectionHeight);
    const x = (canvas.width - panelWidth) / 2;
    const y = (canvas.height - panelHeight) / 2;
    const radius = 8 * scale;

    ctx.save();
    ctx.fillStyle = "rgba(7, 16, 21, 0.90)";
    drawRoundedRect(ctx, x, y, panelWidth, panelHeight, radius);
    ctx.fill();
    ctx.strokeStyle = "rgba(255, 255, 255, 0.18)";
    ctx.lineWidth = Math.max(1, scale);
    drawRoundedRect(ctx, x, y, panelWidth, panelHeight, radius);
    ctx.stroke();

    ctx.textAlign = "left";
    ctx.textBaseline = "middle";
    ctx.font = `700 ${Math.max(15, 16 * scale)}px sans-serif`;
    ctx.fillStyle = "rgba(255, 255, 255, 0.96)";
    ctx.fillText("Replay summary", x + padding, y + padding + 10 * scale);
    ctx.textAlign = "right";
    ctx.font = `700 ${Math.max(13, 14 * scale)}px sans-serif`;
    ctx.fillStyle = "rgba(255, 193, 7, 0.94)";
    ctx.fillText(`${formatCount(summary.totalKills)} total kills`, x + panelWidth - padding, y + padding + 10 * scale);

    const contentY = y + padding + headerHeight;
    if (stacked) {
        drawPlayerSummaryRows(ctx, summary, x + padding, contentY, panelWidth - padding * 2, rowHeight, scale);
        drawTopUnitSummaryRows(
            ctx,
            summary,
            x + padding,
            contentY + sectionTitleHeight + playerRowsHeight + gap,
            panelWidth - padding * 2,
            rowHeight,
            scale);
    } else {
        const columnWidth = (panelWidth - padding * 2 - gap) / 2;
        drawPlayerSummaryRows(ctx, summary, x + padding, contentY, columnWidth, rowHeight, scale);
        drawTopUnitSummaryRows(ctx, summary, x + padding + columnWidth + gap, contentY, columnWidth, rowHeight, scale);
    }

    ctx.restore();
}

function drawPlayerSummaryRows(
    ctx: CanvasContext,
    summary: PlaybackSummary,
    x: number,
    y: number,
    width: number,
    rowHeight: number,
    scale: number): void {
    drawSummarySectionTitle(ctx, "Player kills", x, y, width, scale);
    let rowY = y + 24 * scale;
    const killsWidth = 72 * scale;
    const labelWidth = Math.min(width - killsWidth - 16 * scale, 180 * scale);
    const killsX = x + 10 * scale + labelWidth + 14 * scale + killsWidth;
    for (const row of summary.players) {
        drawSummaryRowAccent(ctx, row.teamId, x, rowY, rowHeight, scale);
        ctx.textAlign = "left";
        ctx.font = `600 ${Math.max(10, 11 * scale)}px sans-serif`;
        ctx.fillStyle = "rgba(255, 255, 255, 0.90)";
        ctx.fillText(fitText(ctx, `P${row.gamePos} ${row.playerName}`, labelWidth), x + 10 * scale, rowY + rowHeight / 2);
        ctx.textAlign = "right";
        ctx.fillStyle = "rgba(255, 255, 255, 0.78)";
        ctx.fillText(formatCount(row.kills), killsX, rowY + rowHeight / 2);
        rowY += rowHeight;
    }
}

function drawTopUnitSummaryRows(
    ctx: CanvasContext,
    summary: PlaybackSummary,
    x: number,
    y: number,
    width: number,
    rowHeight: number,
    scale: number): void {
    drawSummarySectionTitle(ctx, "Top units", x, y, width, scale);
    let rowY = y + 24 * scale;
    if (summary.topUnits.length === 0) {
        ctx.textAlign = "left";
        ctx.font = `${Math.max(10, 11 * scale)}px sans-serif`;
        ctx.fillStyle = "rgba(255, 255, 255, 0.58)";
        ctx.fillText("No unit kill events", x, rowY + rowHeight / 2, width);
        return;
    }

    for (const row of summary.topUnits) {
        drawSummaryRowAccent(ctx, row.teamId, x, rowY, rowHeight, scale);
        ctx.textAlign = "left";
        ctx.font = `600 ${Math.max(10, 11 * scale)}px sans-serif`;
        ctx.fillStyle = "rgba(255, 255, 255, 0.90)";
        const playerWidth = width * 0.42;
        const unitWidth = width * 0.28;
        ctx.fillText(fitText(ctx, `P${row.gamePos} ${row.playerName}`, playerWidth), x + 10 * scale, rowY + rowHeight / 2);
        ctx.fillStyle = "rgba(255, 255, 255, 0.72)";
        ctx.fillText(fitText(ctx, row.unitName, unitWidth), x + width * 0.48, rowY + rowHeight / 2);
        ctx.textAlign = "right";
        ctx.fillStyle = "rgba(255, 255, 255, 0.78)";
        ctx.fillText(formatCount(row.kills), x + width, rowY + rowHeight / 2);
        rowY += rowHeight;
    }
}

export function fitText(ctx: Pick<CanvasContext, "measureText">, text: string, maxWidth: number): string {
    if (maxWidth <= 0 || text.length === 0) {
        return "";
    }

    if (ctx.measureText(text).width <= maxWidth) {
        return text;
    }

    const ellipsis = "...";
    if (ctx.measureText(ellipsis).width > maxWidth) {
        return "";
    }

    let left = 0;
    let right = text.length;
    while (left < right) {
        const middle = Math.ceil((left + right) / 2);
        const candidate = text.slice(0, middle) + ellipsis;
        if (ctx.measureText(candidate).width <= maxWidth) {
            left = middle;
        } else {
            right = middle - 1;
        }
    }

    return text.slice(0, left) + ellipsis;
}

function drawSummarySectionTitle(
    ctx: CanvasContext,
    title: string,
    x: number,
    y: number,
    width: number,
    scale: number): void {
    ctx.textAlign = "left";
    ctx.font = `700 ${Math.max(11, 12 * scale)}px sans-serif`;
    ctx.fillStyle = "rgba(255, 193, 7, 0.88)";
    ctx.fillText(title, x, y + 9 * scale, width);
}

function drawSummaryRowAccent(
    ctx: CanvasContext,
    teamId: number,
    x: number,
    y: number,
    rowHeight: number,
    scale: number): void {
    ctx.fillStyle = withAlpha(TEAM_COLORS[teamId] ?? "#FFFFFF", "CC");
    ctx.fillRect(x, y + 4 * scale, 3 * scale, rowHeight - 8 * scale);
}

function formatCount(value: number): string {
    return Math.max(0, Math.round(value)).toLocaleString("en-US");
}

function drawGrid(ctx: CanvasContext, canvas: HTMLCanvasElement, gridLines: Segment[]): void {
    ctx.save();
    ctx.strokeStyle = "rgba(255, 255, 255, 0.10)";
    ctx.lineWidth = Math.max(1, deviceScale(canvas));

    for (const line of gridLines) {
        ctx.beginPath();
        ctx.moveTo(line.start.x, line.start.y);
        ctx.lineTo(line.end.x, line.end.y);
        ctx.stroke();
    }

    ctx.restore();
}

function drawSpawnAreas(ctx: CanvasContext, canvas: HTMLCanvasElement, spawnAreas: SpawnAreaGeometry[]): void {
    ctx.save();
    ctx.lineWidth = Math.max(2.25, deviceScale(canvas) * 2.25);
    ctx.setLineDash([7 * deviceScale(canvas), 4 * deviceScale(canvas)]);

    for (const area of spawnAreas) {
        if (area.points.length === 0) {
            continue;
        }

        ctx.fillStyle = withAlpha(area.color, "34");
        ctx.strokeStyle = withAlpha(area.color, "F2");
        ctx.beginPath();
        ctx.moveTo(area.points[0].x, area.points[0].y);
        for (let i = 1; i < area.points.length; i++) {
            ctx.lineTo(area.points[i].x, area.points[i].y);
        }
        ctx.closePath();
        ctx.fill();
        ctx.stroke();
    }

    ctx.restore();

    drawSpawnAreaLabels(ctx, canvas, spawnAreas);
}

function drawSpawnAreaLabels(ctx: CanvasContext, canvas: HTMLCanvasElement, spawnAreas: SpawnAreaGeometry[]): void {
    ctx.save();
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.font = `${Math.max(11, 11 * deviceScale(canvas))}px sans-serif`;

    for (const area of spawnAreas) {
        const label = area.labelGeometry;
        if (!label) {
            continue;
        }

        ctx.save();
        ctx.translate(label.x, label.y);
        ctx.rotate(label.angle);
        ctx.lineWidth = Math.max(3, deviceScale(canvas) * 3);
        ctx.strokeStyle = "rgba(7, 16, 21, 0.92)";
        ctx.fillStyle = withAlpha(area.color, "EE");
        ctx.strokeText(area.label, 0, 0);
        ctx.fillText(area.label, 0, 0);
        ctx.restore();
    }

    ctx.restore();
}

function drawPlayerGasBadges(
    ctx: CanvasContext,
    canvas: HTMLCanvasElement,
    badges: PlayerGasBadge[],
    currentGameloop: number): void {
    if (badges.length === 0) {
        return;
    }

    const scale = deviceScale(canvas);
    const width = GAS_BADGE_WIDTH * scale;
    const height = GAS_BADGE_HEIGHT * scale;
    const radius = 6 * scale;
    const dotRadius = 3.5 * scale;
    const dotGap = 8 * scale;

    ctx.save();
    ctx.font = `${Math.max(10, 10 * scale)}px sans-serif`;
    ctx.textAlign = "left";
    ctx.textBaseline = "middle";

    for (const badge of badges) {
        const gasCount = getGasCountAtGameloop(badge.refineryGameloops, currentGameloop);
        const tierLevel = getTierLevelAtGameloop(badge.tierUpgradeGameloops, currentGameloop);
        const x = badge.x - width / 2;
        const y = badge.y - height / 2;

        ctx.fillStyle = "rgba(7, 16, 21, 0.82)";
        ctx.strokeStyle = withAlpha(badge.color, "DD");
        ctx.lineWidth = Math.max(1.25, 1.25 * scale);
        drawRoundedRect(ctx, x, y, width, height, radius);
        ctx.fill();
        ctx.stroke();

        ctx.fillStyle = "rgba(255, 255, 255, 0.90)";
        ctx.fillText(`P${badge.gamePos}`, x + 7 * scale, badge.y);

        ctx.fillStyle = "rgba(255, 193, 7, 0.92)";
        ctx.fillText(`T${tierLevel}`, x + 31 * scale, badge.y);

        if (gasCount === 0) {
            ctx.textAlign = "right";
            ctx.fillStyle = "rgba(255, 255, 255, 0.66)";
            ctx.fillText("0", x + width - 8 * scale, badge.y);
            ctx.textAlign = "left";
            continue;
        }

        const firstDotX = x + width - 8 * scale - (gasCount - 1) * dotGap;
        for (let i = 0; i < gasCount; i++) {
            const dotX = firstDotX + i * dotGap;
            ctx.beginPath();
            ctx.arc(dotX, badge.y, dotRadius, 0, Math.PI * 2);
            ctx.fillStyle = "#1D72F3";
            ctx.fill();
            ctx.lineWidth = Math.max(1.25, 1.25 * scale);
            ctx.strokeStyle = "rgba(255, 255, 255, 0.82)";
            ctx.stroke();
        }
    }

    ctx.restore();
}

function getGasCountAtGameloop(refineryGameloops: number[], currentGameloop: number): number {
    let gasCount = 0;
    while (gasCount < refineryGameloops.length && refineryGameloops[gasCount] <= currentGameloop) {
        gasCount++;
    }

    return gasCount;
}

function getTierLevelAtGameloop(tierUpgradeGameloops: number[], currentGameloop: number): number {
    let upgradeCount = 0;
    while (upgradeCount < tierUpgradeGameloops.length
        && upgradeCount < 2
        && tierUpgradeGameloops[upgradeCount] <= currentGameloop) {
        upgradeCount++;
    }

    return 1 + upgradeCount;
}

function drawMiddleLine(
    ctx: CanvasContext,
    canvas: HTMLCanvasElement,
    middleLine: Segment | null,
    middleControl: MiddleControl,
    currentGameloop: number): void {
    if (!middleLine) {
        return;
    }

    ctx.save();
    ctx.strokeStyle = getMiddleLineColor(middleControl, currentGameloop);
    ctx.lineWidth = Math.max(2, deviceScale(canvas) * 2);
    ctx.setLineDash([10 * deviceScale(canvas), 7 * deviceScale(canvas)]);
    ctx.beginPath();
    ctx.moveTo(middleLine.start.x, middleLine.start.y);
    ctx.lineTo(middleLine.end.x, middleLine.end.y);
    ctx.stroke();
    ctx.restore();
}

function getMiddleLineColor(middleControl: MiddleControl | null, currentGameloop: number): string {
    if (!middleControl || middleControl.firstTeamId !== 1 && middleControl.firstTeamId !== 2) {
        return NEUTRAL_MIDDLE_LINE_COLOR;
    }

    const changeGameloops = middleControl.changeGameloops;
    let reachedChanges = 0;
    while (reachedChanges < changeGameloops.length && currentGameloop >= changeGameloops[reachedChanges]) {
        reachedChanges++;
    }

    if (reachedChanges === 0) {
        return NEUTRAL_MIDDLE_LINE_COLOR;
    }

    const teamId = reachedChanges % 2 === 1
        ? middleControl.firstTeamId
        : getOtherTeamId(middleControl.firstTeamId);
    return withAlpha(TEAM_COLORS[teamId], "CC");
}

function getOtherTeamId(teamId: number): number {
    return teamId === 1 ? 2 : 1;
}

function drawLandmark(
    ctx: CanvasContext,
    canvas: HTMLCanvasElement,
    projected: { x: number; y: number } | null,
    radius: number,
    color: string,
    kind: string,
    label: string): void {
    if (!projected) {
        return;
    }

    ctx.save();
    const scale = deviceScale(canvas);
    const objectiveSize = objectiveIconCatalog.getSize(label, kind, radius, scale);
    const renderedIcon = objectiveIconCatalog.render(ctx, {
        name: label,
        kind,
        teamColor: color,
        x: projected.x,
        y: projected.y,
        size: objectiveSize
    });

    if (!renderedIcon) {
        drawFallbackLandmark(ctx, projected.x, projected.y, radius, color, kind, canvas);
    }

    ctx.fillStyle = "rgba(255, 255, 255, 0.82)";
    ctx.font = `${Math.max(10, 10 * scale)}px sans-serif`;
    ctx.textAlign = "center";
    ctx.textBaseline = "top";
    const labelOffset = renderedIcon ? objectiveSize / 2 : radius;
    ctx.fillText(label, projected.x, projected.y + labelOffset + 3 * scale);
    ctx.restore();
}

function drawFallbackLandmark(
    ctx: CanvasContext,
    x: number,
    y: number,
    radius: number,
    color: string,
    kind: string,
    canvas: HTMLCanvasElement): void {
    ctx.lineWidth = Math.max(2.25, deviceScale(canvas) * 2.25);
    ctx.strokeStyle = withAlpha(color, "FF");
    ctx.fillStyle = withAlpha(color, kind === "Base" ? "64" : "72");

    if (kind === "Base") {
        ctx.beginPath();
        ctx.moveTo(x, y - radius);
        ctx.lineTo(x + radius, y);
        ctx.lineTo(x, y + radius);
        ctx.lineTo(x - radius, y);
        ctx.closePath();
        ctx.fill();
        ctx.stroke();
        return;
    }

    ctx.beginPath();
    ctx.arc(x, y, radius, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();

    ctx.beginPath();
    ctx.moveTo(x - radius * 0.75, y);
    ctx.lineTo(x + radius * 0.75, y);
    ctx.moveTo(x, y - radius * 0.75);
    ctx.lineTo(x, y + radius * 0.75);
    ctx.stroke();
}

function drawUnitLayer(
    ctx: CanvasContext,
    projection: Projection,
    activeUnits: NormalizedUnit[],
    currentGameloop: number): number {
    let drawnUnits = 0;
    for (const unit of activeUnits) {
        if (drawUnit(ctx, projection, unit, currentGameloop)) {
            drawnUnits++;
        }
    }

    return drawnUnits;
}

function drawUnit(ctx: CanvasContext, projection: Projection, unit: NormalizedUnit, currentGameloop: number): boolean {
    if (currentGameloop < unit.spawnGameloop || unit.expiresGameloop <= currentGameloop) {
        return false;
    }

    const progress = clamp((currentGameloop - unit.spawnGameloop) * unit.inverseLifetime, 0, 1);
    const x = projectX(projection, unit.spawnX + unit.deltaX * progress);
    const y = projectY(projection, unit.spawnY + unit.deltaY * progress);
    const sprite = unit.render?.sprite;
    const radius = unit.render?.radius ?? 3;
    if (sprite) {
        ctx.drawImage(sprite, x - sprite.width / 2, y - sprite.height / 2);
    } else {
        ctx.beginPath();
        ctx.arc(x, y, radius, 0, Math.PI * 2);
        ctx.fillStyle = withAlpha(unit.color, "99");
        ctx.fill();
    }

    return true;
}

function drawAliveUnitHighlightLayer(
    ctx: CanvasContext,
    projection: Projection,
    activeUnits: NormalizedUnit[],
    currentGameloop: number,
    highlightedAliveUnitKey: string): void {
    ctx.save();
    ctx.strokeStyle = withAlpha(ALIVE_UNIT_HIGHLIGHT_COLOR, "EE");
    ctx.shadowColor = withAlpha(ALIVE_UNIT_HIGHLIGHT_COLOR, "AA");
    ctx.shadowBlur = 8;

    for (const unit of activeUnits) {
        if (unit.aliveUnitHighlightKey !== highlightedAliveUnitKey
            || currentGameloop < unit.spawnGameloop
            || unit.expiresGameloop <= currentGameloop) {
            continue;
        }

        const progress = clamp((currentGameloop - unit.spawnGameloop) * unit.inverseLifetime, 0, 1);
        const x = projectX(projection, unit.spawnX + unit.deltaX * progress);
        const y = projectY(projection, unit.spawnY + unit.deltaY * progress);
        const radius = unit.render?.radius ?? 3;

        ctx.lineWidth = Math.max(2, radius * 0.36);
        ctx.beginPath();
        ctx.arc(x, y, radius + Math.max(4, radius * 0.45), 0, Math.PI * 2);
        ctx.stroke();
    }

    ctx.restore();
}

function drawEmptyState(ctx: CanvasContext, canvas: HTMLCanvasElement): void {
    ctx.save();
    ctx.fillStyle = "rgba(255, 255, 255, 0.72)";
    ctx.font = `${Math.max(14, 14 * deviceScale(canvas))}px sans-serif`;
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.fillText("No active units at this step", canvas.width / 2, canvas.height / 2);
    ctx.restore();
}
