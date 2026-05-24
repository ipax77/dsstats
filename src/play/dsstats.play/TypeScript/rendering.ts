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
import { getState } from "./store";
import type {
    CanvasContext,
    LayerCanvas,
    MiddleControl,
    NormalizedUnit,
    PlayerGasBadge,
    Projection,
    Segment,
    SpawnAreaGeometry,
    SpawnPlaybackState,
    StaticGeometry
} from "./types";
import { unitIconCatalog } from "./unitIcons";

export function drawSpawnPlayback(canvas: HTMLCanvasElement, currentGameloop: number): void {
    const state = getState(canvas);
    if (!state?.replay) {
        return;
    }

    const resized = resizeCanvas(canvas);
    const ctx = getCanvasContext(canvas);
    const replay = state.replay;
    const bounds = replay.bounds;
    if (!ctx || !bounds) {
        return;
    }

    state.currentGameloop = clampGameloop(state, currentGameloop);
    if (resized
        || !state.staticGeometry
        || !state.renderCache
        || state.staticCanvasWidth !== canvas.width
        || state.staticCanvasHeight !== canvas.height) {
        state.renderCache = createRenderCache(bounds, canvas);
        state.staticGeometry = createStaticGeometry(replay, bounds, canvas);
        state.staticBackgroundCanvas = createStaticBackgroundCanvas(canvas, state.staticGeometry);
        state.staticCanvasWidth = canvas.width;
        state.staticCanvasHeight = canvas.height;
        state.activeUnits.length = 0;
        state.nextUnitIndex = 0;
        state.lastActiveGameloop = Number.NEGATIVE_INFINITY;
        prepareUnitSprites(state, canvas);
    }

    if (!state.staticBackgroundCanvas || !state.staticGeometry || !state.renderCache) {
        return;
    }

    ctx.drawImage(state.staticBackgroundCanvas, 0, 0);
    drawDynamicMapLayer(ctx, canvas, state.staticGeometry, state.currentGameloop);
    const activeUnits = getActiveUnits(state, state.currentGameloop);
    const drawnUnits = drawUnitLayer(ctx, state.renderCache.projection, activeUnits, state.currentGameloop);

    if (drawnUnits === 0) {
        drawEmptyState(ctx, canvas);
    }
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
    const key = iconDefinition
        ? `${iconDefinition.id}|${unit.commander}|${unit.name}|${teamId}|${color}|${Math.round(radius * 10)}`
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
            teamColor: color
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
    ctx.fillStyle = "#071015";
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    ctx.restore();

    drawGrid(ctx, canvas, geometry.gridLines);
    drawSpawnAreas(ctx, canvas, geometry.spawnAreas);
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
    ctx.lineWidth = Math.max(1.5, deviceScale(canvas) * 1.5);
    ctx.setLineDash([6 * deviceScale(canvas), 4 * deviceScale(canvas)]);

    for (const area of spawnAreas) {
        if (area.points.length === 0) {
            continue;
        }

        ctx.fillStyle = withAlpha(area.color, "20");
        ctx.strokeStyle = withAlpha(area.color, "CC");
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
    ctx.lineWidth = Math.max(1.5, deviceScale(canvas) * 1.5);
    ctx.strokeStyle = withAlpha(color, "EE");
    ctx.fillStyle = withAlpha(color, kind === "Base" ? "44" : "55");

    if (kind === "Base") {
        ctx.beginPath();
        ctx.moveTo(projected.x, projected.y - radius);
        ctx.lineTo(projected.x + radius, projected.y);
        ctx.lineTo(projected.x, projected.y + radius);
        ctx.lineTo(projected.x - radius, projected.y);
        ctx.closePath();
        ctx.fill();
        ctx.stroke();
    } else {
        ctx.beginPath();
        ctx.arc(projected.x, projected.y, radius, 0, Math.PI * 2);
        ctx.fill();
        ctx.stroke();

        ctx.beginPath();
        ctx.moveTo(projected.x - radius * 0.75, projected.y);
        ctx.lineTo(projected.x + radius * 0.75, projected.y);
        ctx.moveTo(projected.x, projected.y - radius * 0.75);
        ctx.lineTo(projected.x, projected.y + radius * 0.75);
        ctx.stroke();
    }

    ctx.fillStyle = "rgba(255, 255, 255, 0.82)";
    ctx.font = `${Math.max(10, 10 * deviceScale(canvas))}px sans-serif`;
    ctx.textAlign = "center";
    ctx.textBaseline = "top";
    ctx.fillText(label, projected.x, projected.y + radius + 3 * deviceScale(canvas));
    ctx.restore();
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

function drawEmptyState(ctx: CanvasContext, canvas: HTMLCanvasElement): void {
    ctx.save();
    ctx.fillStyle = "rgba(255, 255, 255, 0.72)";
    ctx.font = `${Math.max(14, 14 * deviceScale(canvas))}px sans-serif`;
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.fillText("No active units at this step", canvas.width / 2, canvas.height / 2);
    ctx.restore();
}
