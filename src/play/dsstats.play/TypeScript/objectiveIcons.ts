import { createLayerCanvas, getCanvasContext, withAlpha } from "./canvasUtils";
import type { CanvasContext, LayerCanvas } from "./types";

interface ObjectiveIconRenderOptions {
    name: string;
    kind: string;
    teamColor: string;
    x: number;
    y: number;
    size: number;
}

type ObjectiveIconKind = "planetary" | "nexus" | "bunker" | "cannon";

const spriteCache = new Map<string, LayerCanvas>();

export const objectiveIconCatalog = {
    getSize(name: string, kind: string, radius: number, scale: number): number {
        return isLargeObjective(name, kind)
            ? Math.max(30 * scale, radius * 3.2)
            : Math.max(20 * scale, radius * 2.5);
    },

    render(ctx: CanvasContext, options: ObjectiveIconRenderOptions): boolean {
        const objectiveKind = resolveObjectiveKind(options.name, options.kind);
        if (!objectiveKind) {
            return false;
        }

        const sprite = getObjectiveSprite(objectiveKind, options.teamColor, options.size);
        ctx.drawImage(sprite, options.x - sprite.width / 2, options.y - sprite.height / 2);
        return true;
    }
};

function getObjectiveSprite(kind: ObjectiveIconKind, teamColor: string, size: number): LayerCanvas {
    const pixelSize = Math.max(1, Math.ceil(size));
    const key = `${kind}|${teamColor}|${pixelSize}`;
    const cached = spriteCache.get(key);
    if (cached) {
        return cached;
    }

    const padding = Math.ceil(pixelSize * 0.18);
    const spriteSize = pixelSize + padding * 2;
    const sprite = createLayerCanvas(spriteSize, spriteSize);
    const ctx = getCanvasContext(sprite);
    if (!ctx) {
        return sprite;
    }

    ctx.save();
    ctx.translate(spriteSize / 2, spriteSize / 2);
    switch (kind) {
        case "planetary":
            drawPlanetary(ctx, pixelSize, teamColor);
            break;
        case "nexus":
            drawNexus(ctx, pixelSize, teamColor);
            break;
        case "bunker":
            drawBunker(ctx, pixelSize, teamColor);
            break;
        case "cannon":
            drawCannon(ctx, pixelSize, teamColor);
            break;
    }

    ctx.restore();
    spriteCache.set(key, sprite);
    return sprite;
}

function drawPlanetary(ctx: CanvasContext, size: number, teamColor: string): void {
    const half = size / 2;
    const dark = "#20272d";
    const hull = "#3d474d";
    const plate = "#657078";
    const light = "#c4b482";
    const glow = withAlpha(teamColor, "E6");

    drawSoftShadow(ctx, size);

    ctx.fillStyle = "#2b343a";
    ctx.strokeStyle = dark;
    ctx.lineWidth = size * 0.06;
    drawOctagon(ctx, 0, 0, half * 0.78, half * 0.62);
    ctx.fill();
    ctx.stroke();

    drawPlanetaryPod(ctx, -half * 0.55, half * 0.28, size, hull, dark);
    drawPlanetaryPod(ctx, half * 0.48, half * 0.22, size, hull, dark);
    drawPlanetaryPod(ctx, -half * 0.45, -half * 0.28, size, hull, dark);
    drawPlanetaryPod(ctx, half * 0.43, -half * 0.28, size, hull, dark);

    ctx.fillStyle = hull;
    ctx.strokeStyle = dark;
    drawOctagon(ctx, 0, -half * 0.06, half * 0.50, half * 0.40);
    ctx.fill();
    ctx.stroke();

    ctx.fillStyle = plate;
    ctx.beginPath();
    ctx.ellipse(0, -half * 0.10, half * 0.30, half * 0.18, 0, 0, Math.PI * 2);
    ctx.fill();

    ctx.fillStyle = light;
    drawRoundedBox(ctx, -half * 0.16, -half * 0.02, half * 0.32, half * 0.13, size * 0.025);
    ctx.fill();

    ctx.fillStyle = glow;
    ctx.strokeStyle = withAlpha(teamColor, "88");
    ctx.lineWidth = size * 0.035;
    ctx.beginPath();
    ctx.arc(-half * 0.36, half * 0.14, size * 0.07, 0, Math.PI * 2);
    ctx.arc(half * 0.34, half * 0.10, size * 0.06, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();
}

function drawNexus(ctx: CanvasContext, size: number, teamColor: string): void {
    const half = size / 2;
    const gold = "#d7bd6a";
    const shade = "#806b33";
    const crystal = withAlpha(teamColor, "E8");

    drawSoftShadow(ctx, size);
    ctx.fillStyle = withAlpha(gold, "D8");
    ctx.strokeStyle = shade;
    ctx.lineWidth = size * 0.06;
    drawDiamond(ctx, 0, 0, half * 0.86, half * 0.66);
    ctx.fill();
    ctx.stroke();

    ctx.fillStyle = "#efe3a5";
    drawDiamond(ctx, 0, -half * 0.05, half * 0.46, half * 0.36);
    ctx.fill();

    ctx.fillStyle = crystal;
    ctx.strokeStyle = "#f7fbff";
    ctx.lineWidth = size * 0.035;
    drawDiamond(ctx, 0, -half * 0.20, half * 0.22, half * 0.42);
    ctx.fill();
    ctx.stroke();
}

function drawBunker(ctx: CanvasContext, size: number, teamColor: string): void {
    const half = size / 2;
    const dark = "#232930";
    const panel = "#59636d";
    const roof = "#d8d4cf";
    const glow = withAlpha(teamColor, "E8");

    drawSoftShadow(ctx, size);
    ctx.fillStyle = "#36414a";
    ctx.strokeStyle = dark;
    ctx.lineWidth = size * 0.055;
    drawOctagon(ctx, 0, half * 0.03, half * 0.82, half * 0.54);
    ctx.fill();
    ctx.stroke();

    ctx.fillStyle = panel;
    drawSlantedPanel(ctx, -half * 0.54, half * 0.06, half * 0.40, half * 0.28, -1);
    ctx.fill();
    drawSlantedPanel(ctx, half * 0.54, half * 0.06, half * 0.40, half * 0.28, 1);
    ctx.fill();

    ctx.strokeStyle = "#1f252b";
    ctx.lineWidth = size * 0.025;
    drawPanelGrid(ctx, -half * 0.54, half * 0.06, half * 0.32, 3, -1);
    drawPanelGrid(ctx, half * 0.54, half * 0.06, half * 0.32, 3, 1);

    ctx.fillStyle = roof;
    ctx.strokeStyle = "#59616a";
    ctx.lineWidth = size * 0.045;
    ctx.beginPath();
    ctx.ellipse(0, -half * 0.08, half * 0.46, half * 0.34, 0, Math.PI, Math.PI * 2);
    ctx.lineTo(half * 0.36, half * 0.08);
    ctx.quadraticCurveTo(0, half * 0.24, -half * 0.36, half * 0.08);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    ctx.fillStyle = "#272d34";
    drawRoundedBox(ctx, -half * 0.18, -half * 0.02, half * 0.36, half * 0.14, size * 0.025);
    ctx.fill();

    ctx.fillStyle = glow;
    for (let i = -2; i <= 2; i++) {
        ctx.beginPath();
        ctx.arc(i * half * 0.13, -half * 0.16 + Math.abs(i) * half * 0.015, size * 0.028, 0, Math.PI * 2);
        ctx.fill();
    }
}

function drawCannon(ctx: CanvasContext, size: number, teamColor: string): void {
    const half = size / 2;

    drawSoftShadow(ctx, size);
    ctx.strokeStyle = withAlpha(teamColor, "F2");
    ctx.lineWidth = size * 0.09;
    ctx.beginPath();
    ctx.arc(0, 0, half * 0.46, 0, Math.PI * 2);
    ctx.stroke();

    ctx.fillStyle = "#76623a";
    ctx.strokeStyle = "#42351c";
    ctx.lineWidth = size * 0.06;
    drawDiamond(ctx, 0, 0, half * 0.58, half * 0.58);
    ctx.fill();
    ctx.stroke();

    ctx.strokeStyle = withAlpha(teamColor, "E8");
    ctx.lineWidth = size * 0.08;
    ctx.beginPath();
    ctx.moveTo(0, -half * 0.52);
    ctx.lineTo(0, half * 0.52);
    ctx.moveTo(-half * 0.52, 0);
    ctx.lineTo(half * 0.52, 0);
    ctx.stroke();
}

function drawSoftShadow(ctx: CanvasContext, size: number): void {
    ctx.fillStyle = "rgba(0, 0, 0, 0.24)";
    ctx.beginPath();
    ctx.ellipse(0, size * 0.16, size * 0.42, size * 0.22, 0, 0, Math.PI * 2);
    ctx.fill();
}

function drawRoundedBox(ctx: CanvasContext, x: number, y: number, width: number, height: number, radius: number): void {
    const right = x + width;
    const bottom = y + height;
    ctx.beginPath();
    ctx.moveTo(x + radius, y);
    ctx.lineTo(right - radius, y);
    ctx.quadraticCurveTo(right, y, right, y + radius);
    ctx.lineTo(right, bottom - radius);
    ctx.quadraticCurveTo(right, bottom, right - radius, bottom);
    ctx.lineTo(x + radius, bottom);
    ctx.quadraticCurveTo(x, bottom, x, bottom - radius);
    ctx.lineTo(x, y + radius);
    ctx.quadraticCurveTo(x, y, x + radius, y);
    ctx.closePath();
}

function drawOctagon(ctx: CanvasContext, x: number, y: number, radiusX: number, radiusY: number): void {
    ctx.beginPath();
    ctx.moveTo(x - radiusX * 0.42, y - radiusY);
    ctx.lineTo(x + radiusX * 0.42, y - radiusY);
    ctx.lineTo(x + radiusX, y - radiusY * 0.38);
    ctx.lineTo(x + radiusX, y + radiusY * 0.36);
    ctx.lineTo(x + radiusX * 0.42, y + radiusY);
    ctx.lineTo(x - radiusX * 0.42, y + radiusY);
    ctx.lineTo(x - radiusX, y + radiusY * 0.36);
    ctx.lineTo(x - radiusX, y - radiusY * 0.38);
    ctx.closePath();
}

function drawPlanetaryPod(
    ctx: CanvasContext,
    x: number,
    y: number,
    size: number,
    fill: string,
    stroke: string): void {
    ctx.fillStyle = fill;
    ctx.strokeStyle = stroke;
    drawRoundedBox(ctx, x - size * 0.09, y - size * 0.10, size * 0.18, size * 0.20, size * 0.03);
    ctx.fill();
    ctx.stroke();
}

function drawSlantedPanel(
    ctx: CanvasContext,
    x: number,
    y: number,
    width: number,
    height: number,
    side: -1 | 1): void {
    ctx.beginPath();
    ctx.moveTo(x - side * width * 0.5, y - height * 0.45);
    ctx.lineTo(x + side * width * 0.36, y - height * 0.32);
    ctx.lineTo(x + side * width * 0.50, y + height * 0.48);
    ctx.lineTo(x - side * width * 0.40, y + height * 0.34);
    ctx.closePath();
}

function drawPanelGrid(ctx: CanvasContext, x: number, y: number, width: number, lines: number, side: -1 | 1): void {
    for (let i = 1; i <= lines; i++) {
        const offset = (i / (lines + 1) - 0.5) * width;
        ctx.beginPath();
        ctx.moveTo(x + side * offset, y - width * 0.22);
        ctx.lineTo(x + side * (offset + width * 0.10), y + width * 0.20);
        ctx.stroke();
    }
}

function drawDiamond(ctx: CanvasContext, x: number, y: number, radiusX: number, radiusY: number): void {
    ctx.beginPath();
    ctx.moveTo(x, y - radiusY);
    ctx.lineTo(x + radiusX, y);
    ctx.lineTo(x, y + radiusY);
    ctx.lineTo(x - radiusX, y);
    ctx.closePath();
}

function resolveObjectiveKind(name: string, kind: string): ObjectiveIconKind | null {
    const key = normalizeObjectiveKey(name || kind);
    if (key.includes("planetary")) {
        return "planetary";
    }

    if (key.includes("nexus")) {
        return "nexus";
    }

    if (key.includes("bunker")) {
        return "bunker";
    }

    if (key.includes("cannon")) {
        return "cannon";
    }

    return null;
}

function isLargeObjective(name: string, kind: string): boolean {
    const key = normalizeObjectiveKey(name || kind);
    return key.includes("planetary") || key.includes("nexus") || normalizeObjectiveKey(kind) === "base";
}

function normalizeObjectiveKey(value: string): string {
    return value.trim().toLowerCase().replaceAll(/[^a-z0-9]+/g, "");
}
