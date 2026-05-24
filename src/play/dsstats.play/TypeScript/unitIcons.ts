import { TEAM_COLORS } from "./constants";
import { getCanvasContext } from "./canvasUtils";
import { objectiveIconCatalog } from "./objectiveIcons";
import { protossUnits } from "./protossIcons";
import { terranUnits } from "./terranIcons";
import type {
    CanvasContext,
    UnitIconDefinition,
    UnitIconLayer,
    UnitIconPathCommand,
    UnitIconRenderOptions
} from "./types";
import { zergUnits } from "./zergIcons";

const definitions: UnitIconDefinition[] = [
    ...Object.values(protossUnits),
    ...Object.values(terranUnits),
    ...Object.values(zergUnits)
];

const aliases = new Map<string, UnitIconDefinition>();
const svgCache = new Map<string, string>();
const tokenCache = new Map<string, Record<string, string>>();
const OBJECTIVE_COMMANDER = "objective";

for (const definition of definitions) {
    for (const alias of definition.aliases) {
        aliases.set(getAliasKey(definition.commander, alias), definition);
    }
}

export const unitIconCatalog = {
    resolve(commander: string, unitName: string): UnitIconDefinition | null {
        return aliases.get(getAliasKey(commander, unitName)) ?? null;
    },

    render(ctx: CanvasContext, definition: UnitIconDefinition, options: UnitIconRenderOptions): void {
        renderIcon(ctx, definition, options);
    },

    toSvg(definition: UnitIconDefinition, options: UnitIconRenderOptions): string {
        return toSvg(definition, options);
    },

    hydrateUnitIcons(root: ParentNode = document): void {
        hydrateUnitIcons(root);
    }
};

export function hydrateUnitIcons(root: ParentNode = document): void {
    const hosts = root.querySelectorAll<HTMLElement>("[data-unit-icon]");

    for (const host of hosts) {
        const commander = host.dataset.unitCommander ?? "";
        const unitName = host.dataset.unitIcon ?? "";
        const size = normalizeSize(Number(host.dataset.unitSize ?? 20));
        const teamId = Number(host.dataset.teamId ?? 0);
        const teamColor = host.dataset.teamColor || colorForTeam(teamId);
        const unitColor = host.dataset.unitColor || undefined;
        const definition = unitIconCatalog.resolve(commander, unitName);
        const isObjective = isObjectiveIcon(commander);
        const renderKey = `${commander}|${unitName}|${size}|${teamColor ?? ""}|${unitColor ?? ""}|${definition?.id ?? ""}|${isObjective ? "objective" : ""}`;

        if (host.dataset.renderedIconKey === renderKey) {
            continue;
        }

        host.dataset.renderedIconKey = renderKey;
        if (definition) {
            host.innerHTML = toSvg(definition, { size, teamColor });
        } else if (isObjective && hydrateObjectiveIcon(host, unitName, size, teamColor ?? "#8a949e")) {
            continue;
        } else {
            host.innerHTML = fallbackSvg(size, unitColor ?? teamColor ?? "#8a949e");
        }
    }
}

function hydrateObjectiveIcon(host: HTMLElement, unitName: string, size: number, teamColor: string): boolean {
    const pixelSize = Math.ceil(size);
    const canvas = document.createElement("canvas");
    canvas.width = pixelSize;
    canvas.height = pixelSize;
    const ctx = getCanvasContext(canvas);
    if (!ctx) {
        return false;
    }

    const rendered = objectiveIconCatalog.render(ctx, {
        name: unitName,
        kind: unitName,
        teamColor,
        x: pixelSize / 2,
        y: pixelSize / 2,
        size: pixelSize * 0.72
    });
    if (!rendered) {
        return false;
    }

    canvas.style.width = `${pixelSize}px`;
    canvas.style.height = `${pixelSize}px`;
    host.replaceChildren(canvas);
    return true;
}

function renderIcon(ctx: CanvasContext, definition: UnitIconDefinition, options: UnitIconRenderOptions): void {
    const size = normalizeSize(options.size ?? 24);
    const x = options.x ?? size / 2;
    const y = options.y ?? size / 2;
    const scaleX = size / definition.viewBox.width;
    const scaleY = size / definition.viewBox.height;
    const tokens = resolveTokens(definition, options.teamColor);

    ctx.save();
    ctx.translate(x - size / 2, y - size / 2);
    ctx.scale(scaleX, scaleY);

    for (const layer of definition.layers) {
        drawLayer(ctx, layer, tokens, scaleX, scaleY);
    }

    ctx.restore();
}

function drawLayer(
    ctx: CanvasContext,
    layer: UnitIconLayer,
    tokens: Record<string, string>,
    scaleX: number,
    scaleY: number): void {
    const inheritedAlpha = ctx.globalAlpha;
    ctx.save();
    ctx.globalAlpha = inheritedAlpha * (layer.opacity ?? 1);

    if (layer.type === "circle") {
        ctx.beginPath();
        ctx.arc(layer.cx, layer.cy, layer.r, 0, Math.PI * 2);
    } else {
        applyPath(ctx, layer.commands);
        ctx.lineCap = layer.lineCap ?? "butt";
        ctx.lineJoin = layer.lineJoin ?? "miter";
    }

    if (layer.fill) {
        ctx.fillStyle = resolvePaint(layer.fill, tokens);
        ctx.fill();
    }

    if (layer.stroke && layer.strokeWidth) {
        ctx.strokeStyle = resolvePaint(layer.stroke, tokens);
        const inverseScale = 1 / Math.max(0.001, Math.min(scaleX, scaleY));
        ctx.lineWidth = Math.max(inverseScale, layer.strokeWidth);
        ctx.stroke();
    }

    ctx.restore();
}

function applyPath(ctx: CanvasContext, commands: UnitIconPathCommand[]): void {
    ctx.beginPath();
    for (const command of commands) {
        switch (command[0]) {
            case "M":
                ctx.moveTo(command[1], command[2]);
                break;
            case "L":
                ctx.lineTo(command[1], command[2]);
                break;
            case "C":
                ctx.bezierCurveTo(command[1], command[2], command[3], command[4], command[5], command[6]);
                break;
            case "Z":
                ctx.closePath();
                break;
        }
    }
}

function toSvg(definition: UnitIconDefinition, options: UnitIconRenderOptions): string {
    const size = normalizeSize(options.size ?? 20);
    const teamColor = options.teamColor ?? "";
    const cacheKey = `${definition.id}|${size}|${teamColor}`;
    const cached = svgCache.get(cacheKey);
    if (cached !== undefined) {
        return cached;
    }

    const tokens = resolveTokens(definition, options.teamColor);
    const layers = definition.layers.map(layer => toSvgLayer(layer, tokens)).join("");
    const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}" viewBox="0 0 ${definition.viewBox.width} ${definition.viewBox.height}" role="img" aria-hidden="true" focusable="false">${layers}</svg>`;
    svgCache.set(cacheKey, svg);
    return svg;
}

function toSvgLayer(layer: UnitIconLayer, tokens: Record<string, string>): string {
    const common = [
        attr("fill", layer.fill ? resolvePaint(layer.fill, tokens) : undefined),
        attr("stroke", layer.stroke ? resolvePaint(layer.stroke, tokens) : undefined),
        attr("stroke-width", layer.strokeWidth),
        attr("opacity", layer.opacity === undefined || layer.opacity === 1 ? undefined : layer.opacity),
        layer.type === "path" ? attr("stroke-linecap", layer.lineCap) : "",
        layer.type === "path" ? attr("stroke-linejoin", layer.lineJoin) : ""
    ].join("");

    if (layer.type === "circle") {
        return `<circle cx="${layer.cx}" cy="${layer.cy}" r="${layer.r}"${common}/>`;
    }

    return `<path d="${commandsToPath(layer.commands)}"${common}/>`;
}

function commandsToPath(commands: UnitIconPathCommand[]): string {
    return commands.map(command => command.join(" ")).join(" ");
}

function fallbackSvg(size: number, color: string): string {
    const safeColor = escapeAttribute(color);
    return `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}" viewBox="0 0 20 20" role="img" aria-hidden="true" focusable="false"><circle cx="10" cy="10" r="7" fill="${safeColor}99" stroke="${safeColor}" stroke-width="2"/></svg>`;
}

function resolveTokens(definition: UnitIconDefinition, teamColor: string | undefined): Record<string, string> {
    if (!teamColor) {
        return definition.tokens;
    }

    const cacheKey = `${definition.id}|${teamColor}`;
    const cached = tokenCache.get(cacheKey);
    if (cached !== undefined) {
        return cached;
    }

    const palette = createTeamPalette(teamColor);
    const tokens = {
        ...definition.tokens,
        badgeFill: teamColor,
        badgeGlow: palette.light,
        badgeStroke: palette.light,
        armorFill: palette.light,
        armorMid: teamColor,
        armorShade: palette.dark,
        armorDark: palette.deeper,
        rifleFill: palette.deeper,
        skinFill: teamColor,
        skinMid: palette.dark,
        skinShade: palette.deeper,
        carapaceFill: palette.light,
        carapaceShade: palette.dark
    };

    tokenCache.set(cacheKey, tokens);
    return tokens;
}

function createTeamPalette(color: string): { light: string; mid: string; dark: string; deeper: string } {
    return {
        light: mixHex(color, "#ffffff", 0.55),
        mid: mixHex(color, "#ffffff", 0.18),
        dark: mixHex(color, "#000000", 0.24),
        deeper: mixHex(color, "#000000", 0.48)
    };
}

function mixHex(left: string, right: string, weightRight: number): string {
    const leftRgb = parseHexColor(left);
    const rightRgb = parseHexColor(right);
    if (!leftRgb || !rightRgb) {
        return left;
    }

    const weightLeft = 1 - weightRight;
    return toHex(
        Math.round(leftRgb.r * weightLeft + rightRgb.r * weightRight),
        Math.round(leftRgb.g * weightLeft + rightRgb.g * weightRight),
        Math.round(leftRgb.b * weightLeft + rightRgb.b * weightRight));
}

function parseHexColor(color: string): { r: number; g: number; b: number } | null {
    const match = /^#?([0-9a-f]{6})$/i.exec(color.trim());
    if (!match) {
        return null;
    }

    const value = Number.parseInt(match[1], 16);
    return {
        r: (value >> 16) & 255,
        g: (value >> 8) & 255,
        b: value & 255
    };
}

function toHex(r: number, g: number, b: number): string {
    return `#${hexByte(r)}${hexByte(g)}${hexByte(b)}`;
}

function hexByte(value: number): string {
    return Math.max(0, Math.min(255, value)).toString(16).padStart(2, "0").toUpperCase();
}

function resolvePaint(paint: string, tokens: Record<string, string>): string {
    return tokens[paint] ?? paint;
}

function colorForTeam(teamId: number): string | undefined {
    return TEAM_COLORS[teamId];
}

function attr(name: string, value: string | number | undefined): string {
    return value === undefined ? "" : ` ${name}="${escapeAttribute(String(value))}"`;
}

function escapeAttribute(value: string): string {
    return value
        .replaceAll("&", "&amp;")
        .replaceAll('"', "&quot;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;");
}

function normalizeSize(value: number): number {
    return Number.isFinite(value) && value > 0 ? value : 20;
}

function getAliasKey(commander: string, unitName: string): string {
    return `${normalize(commander)}|${normalize(unitName)}`;
}

function isObjectiveIcon(commander: string): boolean {
    return normalize(commander) === OBJECTIVE_COMMANDER;
}

function normalize(value: string): string {
    return String(value ?? "").trim().toLowerCase().replace(/ /g, "");
}
