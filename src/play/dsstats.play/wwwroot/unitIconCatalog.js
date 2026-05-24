const terranMarine = {
    id: "terran.marine",
    commander: "terran",
    aliases: ["Marine", "MarineLightweight"],
    viewBox: { width: 100, height: 100 },
    tokens: {
        badgeFill: "#5DADEC",
        badgeStroke: "#B9E1FF",
        armorFill: "#D8E7F0",
        armorShade: "#6E8290",
        visorFill: "#F5D35D",
        darkStroke: "#1B3445"
    },
    layers: [
        { type: "circle", cx: 50, cy: 50, r: 38, fill: "badgeFill", opacity: 0.95 },
        { type: "circle", cx: 50, cy: 50, r: 38, stroke: "badgeStroke", strokeWidth: 5, opacity: 0.9 },
        {
            type: "path",
            commands: [
                ["M", 29, 56],
                ["C", 30, 39, 39, 28, 50, 28],
                ["C", 61, 28, 70, 39, 71, 56],
                ["L", 63, 71],
                ["L", 37, 71],
                ["Z"]
            ],
            fill: "armorFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 34, 51],
                ["C", 39, 45, 45, 42, 50, 42],
                ["C", 55, 42, 61, 45, 66, 51],
                ["L", 62, 60],
                ["L", 38, 60],
                ["Z"]
            ],
            fill: "visorFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        { type: "circle", cx: 37, cy: 63, r: 5, fill: "armorShade", stroke: "darkStroke", strokeWidth: 2 },
        { type: "circle", cx: 63, cy: 63, r: 5, fill: "armorShade", stroke: "darkStroke", strokeWidth: 2 },
        {
            type: "path",
            commands: [
                ["M", 28, 35],
                ["L", 20, 27],
                ["M", 72, 35],
                ["L", 80, 27],
                ["M", 43, 72],
                ["L", 57, 72]
            ],
            stroke: "darkStroke",
            strokeWidth: 4,
            lineCap: "round"
        }
    ]
};

const zergZergling = {
    id: "zerg.zergling",
    commander: "zerg",
    aliases: ["Zergling", "ZerglingLightweight"],
    viewBox: { width: 100, height: 100 },
    tokens: {
        badgeFill: "#8BC34A",
        badgeStroke: "#D3F5A5",
        bodyFill: "#6D3C8D",
        bodyShade: "#B66FD2",
        clawFill: "#E7F2D0",
        darkStroke: "#281B36"
    },
    layers: [
        { type: "circle", cx: 50, cy: 50, r: 38, fill: "badgeFill", opacity: 0.92 },
        { type: "circle", cx: 50, cy: 50, r: 38, stroke: "badgeStroke", strokeWidth: 5, opacity: 0.82 },
        {
            type: "path",
            commands: [
                ["M", 31, 59],
                ["C", 34, 41, 43, 30, 51, 28],
                ["C", 62, 32, 69, 45, 69, 60],
                ["C", 61, 72, 41, 72, 31, 59],
                ["Z"]
            ],
            fill: "bodyFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 42, 40],
                ["L", 31, 28],
                ["L", 35, 47],
                ["M", 57, 40],
                ["L", 72, 28],
                ["L", 64, 47],
                ["M", 38, 63],
                ["L", 22, 73],
                ["M", 62, 63],
                ["L", 78, 73]
            ],
            stroke: "clawFill",
            strokeWidth: 5,
            lineCap: "round",
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 43, 52],
                ["C", 46, 49, 53, 49, 57, 52],
                ["M", 45, 62],
                ["C", 49, 65, 54, 65, 58, 62]
            ],
            stroke: "bodyShade",
            strokeWidth: 4,
            lineCap: "round"
        },
        { type: "circle", cx: 41, cy: 49, r: 3, fill: "clawFill" },
        { type: "circle", cx: 59, cy: 49, r: 3, fill: "clawFill" }
    ]
};

export const terranUnits = {
    marine: terranMarine
};

export const zergUnits = {
    zergling: zergZergling
};

const definitions = [
    terranUnits.marine,
    zergUnits.zergling
];

const aliases = new Map();

for (const definition of definitions) {
    for (const alias of definition.aliases) {
        aliases.set(getAliasKey(definition.commander, alias), definition);
    }
}

export const unitIconCatalog = {
    resolve(commander, unitName) {
        return aliases.get(getAliasKey(commander, unitName)) ?? null;
    },

    render(ctx, definition, options) {
        renderIcon(ctx, definition, options);
    }
};

function renderIcon(ctx, definition, options) {
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

function drawLayer(ctx, layer, tokens, scaleX, scaleY) {
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

function applyPath(ctx, commands) {
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

function resolveTokens(definition, teamColor) {
    if (typeof teamColor !== "string" || teamColor.length === 0) {
        return definition.tokens;
    }

    return {
        ...definition.tokens,
        badgeFill: teamColor
    };
}

function resolvePaint(paint, tokens) {
    return tokens[paint] ?? paint;
}

function getAliasKey(commander, unitName) {
    return `${normalize(commander)}|${normalize(unitName)}`;
}

function normalize(value) {
    return String(value ?? "").trim().toLowerCase().replaceAll(" ", "");
}
