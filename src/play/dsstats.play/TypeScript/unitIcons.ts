import type {
    CanvasContext,
    UnitIconDefinition,
    UnitIconLayer,
    UnitIconPathCommand,
    UnitIconRenderOptions
} from "./types";

const terranMarine: UnitIconDefinition = {
    id: "terran.marine",
    commander: "terran",
    aliases: ["Marine", "MarineLightweight"],
    viewBox: { width: 100, height: 100 },
    tokens: {
        badgeFill: "#234A68",
        badgeGlow: "#5DADEC",
        badgeStroke: "#B9E1FF",

        armorFill: "#D8E7F0",
        armorMid: "#9FB3C0",
        armorShade: "#5E7280",
        armorDark: "#2E4756",

        visorFill: "#F5D35D",
        visorShade: "#D9962B",

        redLight: "#FF5A4F",
        blueLight: "#76D6FF",

        darkStroke: "#102838",
        rifleFill: "#243946"
    },
    layers: [
        // Badge base
        // {
        //     type: "circle",
        //     cx: 50,
        //     cy: 50,
        //     r: 39,
        //     fill: "badgeFill",
        //     opacity: 0.98
        // },
        // {
        //     type: "circle",
        //     cx: 50,
        //     cy: 50,
        //     r: 39,
        //     stroke: "badgeStroke",
        //     strokeWidth: 4,
        //     opacity: 0.9
        // },
        // {
        //     type: "circle",
        //     cx: 50,
        //     cy: 50,
        //     r: 33,
        //     stroke: "badgeGlow",
        //     strokeWidth: 2,
        //     opacity: 0.35
        // },

        // Back shoulder silhouette
        {
            type: "path",
            commands: [
                ["M", 19, 67],
                ["C", 21, 55, 29, 47, 38, 47],
                ["L", 44, 70],
                ["L", 31, 79],
                ["C", 24, 78, 20, 74, 19, 67],
                ["Z"]
            ],
            fill: "armorShade",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 81, 67],
                ["C", 79, 55, 71, 47, 62, 47],
                ["L", 56, 70],
                ["L", 69, 79],
                ["C", 76, 78, 80, 74, 81, 67],
                ["Z"]
            ],
            fill: "armorShade",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Helmet dome
        {
            type: "path",
            commands: [
                ["M", 28, 55],
                ["C", 28, 37, 37, 25, 50, 23],
                ["C", 63, 25, 72, 37, 72, 55],
                ["L", 66, 67],
                ["L", 34, 67],
                ["Z"]
            ],
            fill: "armorFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Helmet side panels
        {
            type: "path",
            commands: [
                ["M", 29, 50],
                ["L", 21, 55],
                ["L", 24, 68],
                ["L", 34, 66],
                ["L", 36, 54],
                ["Z"]
            ],
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 71, 50],
                ["L", 79, 55],
                ["L", 76, 68],
                ["L", 66, 66],
                ["L", 64, 54],
                ["Z"]
            ],
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Brow plate
        {
            type: "path",
            commands: [
                ["M", 32, 43],
                ["C", 38, 36, 44, 33, 50, 33],
                ["C", 56, 33, 62, 36, 68, 43],
                ["L", 64, 50],
                ["C", 59, 46, 55, 44, 50, 44],
                ["C", 45, 44, 41, 46, 36, 50],
                ["Z"]
            ],
            fill: "armorDark",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },

        // Visor
        {
            type: "path",
            commands: [
                ["M", 34, 50],
                ["C", 39, 45, 45, 42, 50, 42],
                ["C", 55, 42, 61, 45, 66, 50],
                ["L", 62, 59],
                ["L", 38, 59],
                ["Z"]
            ],
            fill: "visorFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 39, 55],
                ["C", 45, 52, 55, 52, 61, 55]
            ],
            stroke: "visorShade",
            strokeWidth: 3,
            lineCap: "round",
            opacity: 0.65
        },

        // Respirator / mouth guard
        {
            type: "path",
            commands: [
                ["M", 39, 61],
                ["L", 61, 61],
                ["L", 58, 72],
                ["L", 42, 72],
                ["Z"]
            ],
            fill: "armorDark",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 44, 64],
                ["L", 44, 70],
                ["M", 50, 64],
                ["L", 50, 71],
                ["M", 56, 64],
                ["L", 56, 70]
            ],
            stroke: "armorMid",
            strokeWidth: 2,
            lineCap: "round"
        },

        // Chest plate
        {
            type: "path",
            commands: [
                ["M", 36, 72],
                ["L", 64, 72],
                ["L", 70, 84],
                ["L", 30, 84],
                ["Z"]
            ],
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 43, 75],
                ["L", 57, 75],
                ["L", 54, 82],
                ["L", 46, 82],
                ["Z"]
            ],
            fill: "armorFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },

        // Helmet lights
        {
            type: "circle",
            cx: 35,
            cy: 38,
            r: 3,
            fill: "redLight",
            stroke: "darkStroke",
            strokeWidth: 1.5
        },
        {
            type: "circle",
            cx: 65,
            cy: 38,
            r: 3,
            fill: "blueLight",
            stroke: "darkStroke",
            strokeWidth: 1.5
        },

        // Small rifle silhouette across lower badge
        {
            type: "path",
            commands: [
                ["M", 24, 77],
                ["L", 58, 66],
                ["L", 61, 70],
                ["L", 78, 65],
                ["L", 80, 70],
                ["L", 63, 75],
                ["L", 59, 72],
                ["L", 29, 82],
                ["Z"]
            ],
            fill: "rifleFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round",
            opacity: 0.9
        },

        // Decorative antenna / comms
        {
            type: "path",
            commands: [
                ["M", 31, 33],
                ["L", 22, 22],
                ["M", 69, 33],
                ["L", 78, 22],
                ["M", 45, 25],
                ["L", 50, 17],
                ["L", 55, 25]
            ],
            stroke: "darkStroke",
            strokeWidth: 3.5,
            lineCap: "round",
            lineJoin: "round"
        }
    ]
};

const zergZergling: UnitIconDefinition = {
    id: "zerg.zergling",
    commander: "zerg",
    aliases: ["Zergling", "ZerglingLightweight"],
    viewBox: { width: 100, height: 100 },
    tokens: {
        skinFill: "#8C5A92",
        skinMid: "#6F4374",
        skinShade: "#4F2D56",
        carapaceFill: "#B78AC8",
        carapaceShade: "#7C5A8A",
        clawFill: "#E7D9B8",
        eyeFill: "#FF5A54",
        darkStroke: "#2A1630"
    },
    layers: [
        // Tail
        {
            type: "path",
            commands: [
                ["M", 18, 66],
                ["C", 10, 64, 8, 58, 14, 54],
                ["C", 20, 51, 27, 54, 31, 59],
                ["L", 29, 66],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Rear leg
        {
            type: "path",
            commands: [
                ["M", 30, 66],
                ["L", 24, 82],
                ["L", 31, 88],
                ["L", 41, 76],
                ["L", 39, 66],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Rear claw
        {
            type: "path",
            commands: [
                ["M", 23, 84],
                ["L", 18, 91],
                ["L", 27, 88],
                ["Z"]
            ],
            fill: "clawFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },

        // Main body
        {
            type: "path",
            commands: [
                ["M", 27, 61],
                ["C", 32, 49, 44, 42, 58, 42],
                ["C", 68, 42, 76, 46, 82, 52],
                ["C", 84, 58, 81, 64, 74, 67],
                ["L", 52, 70],
                ["C", 41, 72, 32, 69, 27, 61],
                ["Z"]
            ],
            fill: "skinFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Belly / lower body
        {
            type: "path",
            commands: [
                ["M", 33, 61],
                ["C", 40, 66, 49, 68, 60, 67],
                ["C", 67, 67, 72, 65, 75, 62],
                ["L", 70, 72],
                ["C", 59, 75, 46, 75, 36, 70],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },

        // Carapace top plate
        {
            type: "path",
            commands: [
                ["M", 34, 54],
                ["C", 43, 45, 55, 41, 67, 43],
                ["C", 73, 44, 78, 47, 81, 51],
                ["L", 72, 58],
                ["C", 62, 55, 50, 55, 39, 60],
                ["Z"]
            ],
            fill: "carapaceFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Dorsal spikes
        {
            type: "path",
            commands: [
                ["M", 38, 49],
                ["L", 35, 36],
                ["L", 44, 46],
                ["Z"],

                ["M", 50, 44],
                ["L", 49, 29],
                ["L", 57, 42],
                ["Z"],

                ["M", 63, 45],
                ["L", 67, 31],
                ["L", 69, 46],
                ["Z"]
            ],
            fill: "carapaceShade",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },

        // Head
        {
            type: "path",
            commands: [
                ["M", 61, 46],
                ["C", 72, 40, 82, 41, 89, 47],
                ["C", 92, 50, 92, 54, 88, 57],
                ["C", 82, 62, 72, 62, 63, 58],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 3.5,
            lineJoin: "round"
        },

        // Upper jaw highlight / face plate
        {
            type: "path",
            commands: [
                ["M", 68, 48],
                ["C", 75, 45, 82, 45, 87, 49],
                ["L", 81, 53],
                ["C", 77, 51, 72, 51, 68, 53],
                ["Z"]
            ],
            fill: "carapaceFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },

        // Eye
        {
            type: "circle",
            cx: 78,
            cy: 50,
            r: 2.8,
            fill: "eyeFill",
            stroke: "darkStroke",
            strokeWidth: 1.5
        },

        // Front arm / main scythe
        {
            type: "path",
            commands: [
                ["M", 60, 61],
                ["C", 66, 62, 72, 66, 74, 72],
                ["L", 86, 78],
                ["L", 78, 83],
                ["L", 69, 75],
                ["C", 66, 73, 63, 69, 60, 65],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Front scythe claw tip
        {
            type: "path",
            commands: [
                ["M", 84, 77],
                ["L", 93, 79],
                ["L", 82, 84],
                ["Z"]
            ],
            fill: "clawFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },

        // Lower forelimb / secondary claw
        {
            type: "path",
            commands: [
                ["M", 53, 63],
                ["C", 58, 66, 61, 71, 60, 76],
                ["L", 52, 80],
                ["L", 49, 73],
                ["L", 49, 65],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },

        // Secondary claw tip
        {
            type: "path",
            commands: [
                ["M", 51, 79],
                ["L", 47, 87],
                ["L", 56, 82],
                ["Z"]
            ],
            fill: "clawFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },

        // Little leg separation accents
        {
            type: "path",
            commands: [
                ["M", 35, 67],
                ["L", 40, 79],
                ["M", 44, 66],
                ["L", 48, 76]
            ],
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineCap: "round"
        }
    ]
};

export const terranUnits = {
    marine: terranMarine
};

export const zergUnits = {
    zergling: zergZergling
};

const definitions: UnitIconDefinition[] = [
    terranUnits.marine,
    zergUnits.zergling
];

const aliases = new Map<string, UnitIconDefinition>();

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
    }
};

function renderIcon(ctx: CanvasContext, definition: UnitIconDefinition, options: UnitIconRenderOptions): void {
    const size = Math.max(1, options.size ?? 24);
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

function resolveTokens(definition: UnitIconDefinition, teamColor: string | undefined): Record<string, string> {
    if (!teamColor) {
        return definition.tokens;
    }

    return {
        ...definition.tokens,
        badgeFill: teamColor
    };
}

function resolvePaint(paint: string, tokens: Record<string, string>): string {
    return tokens[paint] ?? paint;
}

function getAliasKey(commander: string, unitName: string): string {
    return `${normalize(commander)}|${normalize(unitName)}`;
}

function normalize(value: string): string {
    return String(value ?? "").trim().toLowerCase().replace(/ /g, "");
}
