import type { UnitIconDefinition } from "./types";

const protossTokens = {
    armorFill: "#F4D372",
    armorMid: "#C99732",
    armorShade: "#76531C",
    armorDark: "#2D2417",
    ivoryFill: "#FFF1C2",
    psiFill: "#6FE7FF",
    psiMid: "#25A8E8",
    psiDark: "#0B4A76",
    bladeFill: "#A9F6FF",
    bladeCore: "#E8FFFF",
    shadowFill: "#171C28",
    darkStroke: "#18130C"
};

const protossZealot: UnitIconDefinition = {
    id: "protoss.zealot",
    commander: "protoss",
    aliases: ["Zealot"],
    viewBox: { width: 100, height: 100 },
    tokens: protossTokens,
    layers: [
        {
            type: "path",
            commands: [["M", 35, 34], ["C", 38, 22, 44, 15, 50, 13], ["C", 56, 15, 62, 22, 65, 34], ["L", 60, 48], ["L", 40, 48], ["Z"]],
            fill: "armorFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 40, 36], ["L", 60, 36], ["L", 57, 44], ["L", 43, 44], ["Z"]],
            fill: "psiFill",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 35, 48], ["L", 65, 48], ["L", 70, 73], ["L", 57, 88], ["L", 43, 88], ["L", 30, 73], ["Z"]],
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 39, 56], ["L", 61, 56], ["L", 56, 74], ["L", 50, 82], ["L", 44, 74], ["Z"]],
            fill: "ivoryFill",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 33, 55], ["L", 18, 65], ["L", 19, 75], ["L", 39, 66], ["Z"], ["M", 67, 55], ["L", 82, 65], ["L", 81, 75], ["L", 61, 66], ["Z"]],
            fill: "armorShade",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 18, 72], ["C", 10, 82, 9, 92, 16, 97], ["C", 21, 87, 28, 79, 27, 70], ["Z"], ["M", 82, 72], ["C", 90, 82, 91, 92, 84, 97], ["C", 79, 87, 72, 79, 73, 70], ["Z"]],
            fill: "bladeFill",
            stroke: "psiDark",
            strokeWidth: 2,
            lineJoin: "round",
            opacity: 0.9
        },
        {
            type: "path",
            commands: [["M", 15, 92], ["L", 25, 75], ["M", 85, 92], ["L", 75, 75]],
            stroke: "bladeCore",
            strokeWidth: 2.5,
            lineCap: "round",
            opacity: 0.8
        }
    ]
};

const protossSentry: UnitIconDefinition = {
    id: "protoss.sentry",
    commander: "protoss",
    aliases: ["Sentry"],
    viewBox: { width: 100, height: 100 },
    tokens: protossTokens,
    layers: [
        {
            type: "path",
            commands: [["M", 50, 14], ["C", 68, 15, 83, 30, 84, 49], ["C", 83, 69, 68, 84, 50, 86], ["C", 32, 84, 17, 69, 16, 49], ["C", 17, 30, 32, 15, 50, 14], ["Z"]],
            fill: "psiDark",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round",
            opacity: 0.65
        },
        {
            type: "path",
            commands: [["M", 50, 20], ["L", 64, 40], ["L", 58, 68], ["L", 50, 82], ["L", 42, 68], ["L", 36, 40], ["Z"]],
            fill: "armorFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 28, 43], ["L", 41, 34], ["L", 43, 54], ["L", 31, 64], ["Z"], ["M", 72, 43], ["L", 59, 34], ["L", 57, 54], ["L", 69, 64], ["Z"]],
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 50,
            cy: 51,
            r: 14,
            fill: "psiFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            opacity: 0.9
        },
        {
            type: "circle",
            cx: 50,
            cy: 51,
            r: 7,
            fill: "bladeCore",
            stroke: "psiDark",
            strokeWidth: 1.5
        },
        {
            type: "path",
            commands: [["M", 33, 76], ["L", 43, 64], ["M", 67, 76], ["L", 57, 64], ["M", 50, 68], ["L", 50, 89]],
            stroke: "armorShade",
            strokeWidth: 4,
            lineCap: "round"
        }
    ]
};

const protossStalker: UnitIconDefinition = {
    id: "protoss.stalker",
    commander: "protoss",
    aliases: ["Stalker"],
    viewBox: { width: 100, height: 100 },
    tokens: protossTokens,
    layers: [
        {
            type: "path",
            commands: [["M", 29, 45], ["L", 40, 29], ["L", 60, 29], ["L", 71, 45], ["L", 65, 67], ["L", 50, 76], ["L", 35, 67], ["Z"]],
            fill: "armorDark",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 37, 41], ["L", 63, 41], ["L", 60, 60], ["L", 50, 67], ["L", 40, 60], ["Z"]],
            fill: "armorFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 50,
            cy: 51,
            r: 8,
            fill: "psiFill",
            stroke: "darkStroke",
            strokeWidth: 2
        },
        {
            type: "path",
            commands: [["M", 35, 62], ["L", 20, 79], ["L", 27, 85], ["L", 44, 68], ["Z"], ["M", 65, 62], ["L", 80, 79], ["L", 73, 85], ["L", 56, 68], ["Z"]],
            fill: "armorShade",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 35, 50], ["L", 12, 44], ["L", 9, 52], ["L", 32, 59], ["Z"], ["M", 65, 50], ["L", 88, 44], ["L", 91, 52], ["L", 68, 59], ["Z"]],
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 41, 33], ["L", 50, 15], ["L", 59, 33]],
            stroke: "psiMid",
            strokeWidth: 4,
            lineCap: "round",
            lineJoin: "round"
        }
    ]
};

const protossAdept: UnitIconDefinition = {
    id: "protoss.adept",
    commander: "protoss",
    aliases: ["Adept"],
    viewBox: { width: 100, height: 100 },
    tokens: protossTokens,
    layers: [
        {
            type: "path",
            commands: [["M", 38, 32], ["C", 40, 21, 45, 15, 50, 13], ["C", 55, 15, 60, 21, 62, 32], ["L", 58, 46], ["L", 42, 46], ["Z"]],
            fill: "armorFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 41, 35], ["L", 59, 35], ["L", 56, 43], ["L", 44, 43], ["Z"]],
            fill: "psiFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 34, 48], ["L", 66, 48], ["L", 63, 73], ["L", 50, 88], ["L", 37, 73], ["Z"]],
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 39, 55], ["L", 61, 55], ["L", 56, 72], ["L", 50, 78], ["L", 44, 72], ["Z"]],
            fill: "ivoryFill",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 33, 56], ["L", 14, 54], ["L", 8, 64], ["L", 31, 66], ["Z"], ["M", 67, 56], ["L", 86, 54], ["L", 92, 64], ["L", 69, 66], ["Z"]],
            fill: "armorShade",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 14, 60], ["L", 5, 48], ["M", 86, 60], ["L", 95, 48]],
            stroke: "bladeFill",
            strokeWidth: 5,
            lineCap: "round"
        }
    ]
};

const protossHighTemplar: UnitIconDefinition = {
    id: "protoss.highTemplar",
    commander: "protoss",
    aliases: ["High Templar", "HighTemplar"],
    viewBox: { width: 100, height: 100 },
    tokens: protossTokens,
    layers: [
        {
            type: "path",
            commands: [["M", 35, 37], ["C", 38, 22, 44, 14, 50, 12], ["C", 56, 14, 62, 22, 65, 37], ["L", 59, 51], ["L", 41, 51], ["Z"]],
            fill: "ivoryFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 39, 39], ["L", 61, 39], ["L", 57, 48], ["L", 43, 48], ["Z"]],
            fill: "psiFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 34, 50], ["L", 66, 50], ["L", 78, 92], ["L", 22, 92], ["Z"]],
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 43, 55], ["L", 57, 55], ["L", 61, 87], ["L", 39, 87], ["Z"]],
            fill: "ivoryFill",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 35, 59], ["L", 16, 68], ["L", 24, 79], ["L", 42, 65], ["Z"], ["M", 65, 59], ["L", 84, 68], ["L", 76, 79], ["L", 58, 65], ["Z"]],
            fill: "armorShade",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 50,
            cy: 70,
            r: 9,
            fill: "psiFill",
            stroke: "psiDark",
            strokeWidth: 2,
            opacity: 0.75
        }
    ]
};

const protossDarkTemplar: UnitIconDefinition = {
    id: "protoss.darkTemplar",
    commander: "protoss",
    aliases: ["Dark Templar", "DarkTemplar"],
    viewBox: { width: 100, height: 100 },
    tokens: {
        ...protossTokens,
        armorFill: "#7462A8",
        armorMid: "#44336F",
        armorShade: "#271A44",
        bladeFill: "#8FF9FF",
        psiFill: "#45D7FF"
    },
    layers: [
        {
            type: "path",
            commands: [["M", 34, 35], ["C", 38, 20, 45, 13, 50, 11], ["C", 55, 13, 62, 20, 66, 35], ["L", 60, 50], ["L", 40, 50], ["Z"]],
            fill: "armorShade",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 40, 36], ["L", 60, 36], ["L", 56, 45], ["L", 44, 45], ["Z"]],
            fill: "psiFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 33, 49], ["L", 67, 49], ["L", 73, 82], ["L", 50, 93], ["L", 27, 82], ["Z"]],
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 38, 58], ["L", 62, 58], ["L", 56, 77], ["L", 50, 84], ["L", 44, 77], ["Z"]],
            fill: "shadowFill",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 31, 59], ["L", 13, 75], ["L", 20, 82], ["L", 40, 66], ["Z"], ["M", 69, 59], ["L", 87, 75], ["L", 80, 82], ["L", 60, 66], ["Z"]],
            fill: "armorFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 14, 80], ["C", 8, 88, 8, 96, 15, 99], ["C", 20, 91, 27, 84, 25, 77], ["Z"], ["M", 86, 80], ["C", 92, 88, 92, 96, 85, 99], ["C", 80, 91, 73, 84, 75, 77], ["Z"]],
            fill: "bladeFill",
            stroke: "psiDark",
            strokeWidth: 2,
            lineJoin: "round",
            opacity: 0.9
        }
    ]
};

const protossArchon: UnitIconDefinition = {
    id: "protoss.archon",
    commander: "protoss",
    aliases: ["Archon"],
    viewBox: { width: 100, height: 100 },
    tokens: protossTokens,
    layers: [
        {
            type: "path",
            commands: [["M", 50, 9], ["C", 72, 15, 84, 34, 82, 58], ["C", 78, 82, 65, 95, 50, 96], ["C", 35, 95, 22, 82, 18, 58], ["C", 16, 34, 28, 15, 50, 9], ["Z"]],
            fill: "psiDark",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 50, 18], ["C", 65, 24, 74, 40, 72, 58], ["C", 69, 76, 60, 87, 50, 88], ["C", 40, 87, 31, 76, 28, 58], ["C", 26, 40, 35, 24, 50, 18], ["Z"]],
            fill: "psiFill",
            stroke: "psiDark",
            strokeWidth: 3,
            opacity: 0.9
        },
        {
            type: "path",
            commands: [["M", 40, 35], ["L", 60, 35], ["L", 57, 48], ["L", 43, 48], ["Z"]],
            fill: "bladeCore",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 28, 56], ["L", 11, 72], ["L", 23, 80], ["L", 39, 62], ["Z"], ["M", 72, 56], ["L", 89, 72], ["L", 77, 80], ["L", 61, 62], ["Z"]],
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 37, 72], ["L", 28, 92], ["M", 63, 72], ["L", 72, 92], ["M", 50, 73], ["L", 50, 96]],
            stroke: "bladeCore",
            strokeWidth: 4,
            lineCap: "round",
            opacity: 0.8
        }
    ]
};

const protossImmortal: UnitIconDefinition = {
    id: "protoss.immortal",
    commander: "protoss",
    aliases: ["Immortal"],
    viewBox: { width: 100, height: 100 },
    tokens: protossTokens,
    layers: [
        {
            type: "path",
            commands: [["M", 25, 48], ["L", 36, 28], ["L", 64, 28], ["L", 75, 48], ["L", 70, 76], ["L", 50, 88], ["L", 30, 76], ["Z"]],
            fill: "armorDark",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 18, 37], ["L", 35, 27], ["L", 39, 43], ["L", 23, 56], ["Z"], ["M", 82, 37], ["L", 65, 27], ["L", 61, 43], ["L", 77, 56], ["Z"]],
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 36, 43], ["L", 64, 43], ["L", 61, 66], ["L", 50, 75], ["L", 39, 66], ["Z"]],
            fill: "armorFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 50,
            cy: 55,
            r: 8,
            fill: "psiFill",
            stroke: "darkStroke",
            strokeWidth: 2
        },
        {
            type: "path",
            commands: [["M", 21, 47], ["L", 5, 43], ["L", 4, 52], ["L", 21, 57], ["Z"], ["M", 79, 47], ["L", 95, 43], ["L", 96, 52], ["L", 79, 57], ["Z"]],
            fill: "ivoryFill",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 36, 76], ["L", 25, 91], ["L", 43, 92], ["L", 50, 80], ["Z"], ["M", 64, 76], ["L", 75, 91], ["L", 57, 92], ["L", 50, 80], ["Z"]],
            fill: "armorShade",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        }
    ]
};

const protossColossus: UnitIconDefinition = {
    id: "protoss.colossus",
    commander: "protoss",
    aliases: ["Colossus"],
    viewBox: { width: 100, height: 100 },
    tokens: protossTokens,
    layers: [
        {
            type: "path",
            commands: [["M", 34, 29], ["L", 44, 16], ["L", 56, 16], ["L", 66, 29], ["L", 62, 53], ["L", 50, 64], ["L", 38, 53], ["Z"]],
            fill: "armorFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 50,
            cy: 38,
            r: 8,
            fill: "psiFill",
            stroke: "darkStroke",
            strokeWidth: 2
        },
        {
            type: "path",
            commands: [["M", 38, 51], ["L", 25, 79], ["L", 17, 94], ["M", 44, 57], ["L", 39, 84], ["L", 35, 96], ["M", 62, 51], ["L", 75, 79], ["L", 83, 94], ["M", 56, 57], ["L", 61, 84], ["L", 65, 96]],
            stroke: "armorShade",
            strokeWidth: 6,
            lineCap: "round",
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 31, 28], ["L", 9, 19], ["L", 13, 27], ["L", 34, 35], ["Z"], ["M", 69, 28], ["L", 91, 19], ["L", 87, 27], ["L", 66, 35], ["Z"]],
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 14, 24], ["L", 5, 35], ["M", 86, 24], ["L", 95, 35]],
            stroke: "bladeFill",
            strokeWidth: 4,
            lineCap: "round"
        }
    ]
};

const protossDisruptor: UnitIconDefinition = {
    id: "protoss.disruptor",
    commander: "protoss",
    aliases: ["Disruptor"],
    viewBox: { width: 100, height: 100 },
    tokens: protossTokens,
    layers: [
        {
            type: "path",
            commands: [["M", 50, 18], ["L", 67, 33], ["L", 78, 50], ["L", 67, 67], ["L", 50, 82], ["L", 33, 67], ["L", 22, 50], ["L", 33, 33], ["Z"]],
            fill: "armorDark",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 50,
            cy: 50,
            r: 24,
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 3
        },
        {
            type: "circle",
            cx: 50,
            cy: 50,
            r: 15,
            fill: "psiFill",
            stroke: "psiDark",
            strokeWidth: 3
        },
        {
            type: "circle",
            cx: 50,
            cy: 50,
            r: 7,
            fill: "bladeCore",
            opacity: 0.9
        },
        {
            type: "path",
            commands: [["M", 50, 19], ["L", 50, 32], ["M", 50, 68], ["L", 50, 81], ["M", 19, 50], ["L", 32, 50], ["M", 68, 50], ["L", 81, 50]],
            stroke: "ivoryFill",
            strokeWidth: 5,
            lineCap: "round"
        }
    ]
};

const protossObserver: UnitIconDefinition = {
    id: "protoss.observer",
    commander: "protoss",
    aliases: ["Observer"],
    viewBox: { width: 100, height: 100 },
    tokens: protossTokens,
    layers: [
        {
            type: "path",
            commands: [["M", 50, 18], ["L", 67, 39], ["L", 82, 50], ["L", 67, 61], ["L", 50, 82], ["L", 33, 61], ["L", 18, 50], ["L", 33, 39], ["Z"]],
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 50,
            cy: 50,
            r: 18,
            fill: "armorFill",
            stroke: "darkStroke",
            strokeWidth: 3
        },
        {
            type: "circle",
            cx: 50,
            cy: 50,
            r: 10,
            fill: "psiFill",
            stroke: "psiDark",
            strokeWidth: 2.5
        },
        {
            type: "circle",
            cx: 50,
            cy: 50,
            r: 4,
            fill: "bladeCore",
            opacity: 0.9
        },
        {
            type: "path",
            commands: [["M", 32, 40], ["L", 20, 28], ["M", 68, 40], ["L", 80, 28], ["M", 32, 60], ["L", 20, 72], ["M", 68, 60], ["L", 80, 72]],
            stroke: "armorShade",
            strokeWidth: 4,
            lineCap: "round"
        }
    ]
};

const protossOracle: UnitIconDefinition = {
    id: "protoss.oracle",
    commander: "protoss",
    aliases: ["Oracle"],
    viewBox: { width: 100, height: 100 },
    tokens: protossTokens,
    layers: [
        {
            type: "path",
            commands: [["M", 50, 15], ["C", 70, 18, 84, 34, 85, 52], ["C", 72, 46, 59, 45, 50, 50], ["C", 41, 45, 28, 46, 15, 52], ["C", 16, 34, 30, 18, 50, 15], ["Z"]],
            fill: "armorFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 29, 53], ["C", 37, 68, 45, 80, 50, 90], ["C", 55, 80, 63, 68, 71, 53], ["C", 61, 58, 39, 58, 29, 53], ["Z"]],
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 50,
            cy: 51,
            r: 11,
            fill: "psiFill",
            stroke: "psiDark",
            strokeWidth: 2.5
        },
        {
            type: "path",
            commands: [["M", 23, 45], ["L", 7, 39], ["L", 18, 55], ["M", 77, 45], ["L", 93, 39], ["L", 82, 55]],
            stroke: "armorShade",
            strokeWidth: 5,
            lineCap: "round",
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 42, 27], ["L", 50, 15], ["L", 58, 27]],
            stroke: "psiMid",
            strokeWidth: 4,
            lineCap: "round"
        }
    ]
};

const protossPhoenix: UnitIconDefinition = {
    id: "protoss.phoenix",
    commander: "protoss",
    aliases: ["Phoenix"],
    viewBox: { width: 100, height: 100 },
    tokens: protossTokens,
    layers: [
        {
            type: "path",
            commands: [["M", 50, 12], ["L", 62, 37], ["L", 91, 48], ["L", 80, 63], ["L", 61, 58], ["L", 57, 81], ["L", 50, 91], ["L", 43, 81], ["L", 39, 58], ["L", 20, 63], ["L", 9, 48], ["L", 38, 37], ["Z"]],
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 41, 34], ["L", 59, 34], ["L", 62, 54], ["L", 55, 70], ["L", 45, 70], ["L", 38, 54], ["Z"]],
            fill: "armorFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 50,
            cy: 48,
            r: 7,
            fill: "psiFill",
            stroke: "darkStroke",
            strokeWidth: 2
        },
        {
            type: "path",
            commands: [["M", 20, 52], ["L", 4, 57], ["L", 10, 66], ["L", 27, 58], ["Z"], ["M", 80, 52], ["L", 96, 57], ["L", 90, 66], ["L", 73, 58], ["Z"]],
            fill: "ivoryFill",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 44, 76], ["L", 36, 92], ["M", 56, 76], ["L", 64, 92]],
            stroke: "psiMid",
            strokeWidth: 4,
            lineCap: "round"
        }
    ]
};

const protossVoidRay: UnitIconDefinition = {
    id: "protoss.voidRay",
    commander: "protoss",
    aliases: ["Void Ray", "VoidRay"],
    viewBox: { width: 100, height: 100 },
    tokens: protossTokens,
    layers: [
        {
            type: "path",
            commands: [["M", 50, 10], ["L", 62, 32], ["L", 83, 44], ["L", 72, 61], ["L", 60, 57], ["L", 57, 84], ["L", 50, 94], ["L", 43, 84], ["L", 40, 57], ["L", 28, 61], ["L", 17, 44], ["L", 38, 32], ["Z"]],
            fill: "armorDark",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 41, 31], ["L", 59, 31], ["L", 64, 57], ["L", 56, 75], ["L", 44, 75], ["L", 36, 57], ["Z"]],
            fill: "armorFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 46, 28], ["L", 54, 28], ["L", 54, 75], ["L", 46, 75], ["Z"]],
            fill: "psiFill",
            stroke: "psiDark",
            strokeWidth: 2,
            lineJoin: "round",
            opacity: 0.85
        },
        {
            type: "path",
            commands: [["M", 27, 47], ["L", 8, 41], ["L", 17, 55], ["M", 73, 47], ["L", 92, 41], ["L", 83, 55]],
            stroke: "armorMid",
            strokeWidth: 5,
            lineCap: "round",
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 50, 75], ["L", 50, 96]],
            stroke: "bladeCore",
            strokeWidth: 4,
            lineCap: "round"
        }
    ]
};

const protossCarrier: UnitIconDefinition = {
    id: "protoss.carrier",
    commander: "protoss",
    aliases: ["Carrier"],
    viewBox: { width: 100, height: 100 },
    tokens: protossTokens,
    layers: [
        {
            type: "path",
            commands: [["M", 50, 8], ["L", 67, 32], ["L", 90, 45], ["L", 80, 67], ["L", 62, 63], ["L", 59, 84], ["L", 50, 96], ["L", 41, 84], ["L", 38, 63], ["L", 20, 67], ["L", 10, 45], ["L", 33, 32], ["Z"]],
            fill: "armorDark",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 38, 29], ["L", 62, 29], ["L", 68, 57], ["L", 58, 78], ["L", 42, 78], ["L", 32, 57], ["Z"]],
            fill: "armorFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 39, 41], ["L", 61, 41], ["L", 64, 53], ["L", 36, 53], ["Z"]],
            fill: "psiFill",
            stroke: "psiDark",
            strokeWidth: 2,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 27,
            cy: 56,
            r: 4,
            fill: "bladeCore",
            stroke: "darkStroke",
            strokeWidth: 1
        },
        {
            type: "circle",
            cx: 73,
            cy: 56,
            r: 4,
            fill: "bladeCore",
            stroke: "darkStroke",
            strokeWidth: 1
        },
        {
            type: "path",
            commands: [["M", 41, 79], ["L", 35, 94], ["M", 50, 82], ["L", 50, 97], ["M", 59, 79], ["L", 65, 94]],
            stroke: "psiMid",
            strokeWidth: 4,
            lineCap: "round"
        }
    ]
};

const protossTempest: UnitIconDefinition = {
    id: "protoss.tempest",
    commander: "protoss",
    aliases: ["Tempest"],
    viewBox: { width: 100, height: 100 },
    tokens: protossTokens,
    layers: [
        {
            type: "path",
            commands: [["M", 50, 6], ["L", 60, 34], ["L", 83, 49], ["L", 73, 62], ["L", 59, 57], ["L", 55, 84], ["L", 50, 96], ["L", 45, 84], ["L", 41, 57], ["L", 27, 62], ["L", 17, 49], ["L", 40, 34], ["Z"]],
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 43, 27], ["L", 57, 27], ["L", 60, 69], ["L", 53, 88], ["L", 47, 88], ["L", 40, 69], ["Z"]],
            fill: "armorFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 47, 16], ["L", 53, 16], ["L", 53, 87], ["L", 47, 87], ["Z"]],
            fill: "psiFill",
            stroke: "psiDark",
            strokeWidth: 1.5,
            opacity: 0.85
        },
        {
            type: "path",
            commands: [["M", 27, 50], ["L", 9, 48], ["L", 15, 58], ["L", 33, 56], ["Z"], ["M", 73, 50], ["L", 91, 48], ["L", 85, 58], ["L", 67, 56], ["Z"]],
            fill: "ivoryFill",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 50,
            cy: 43,
            r: 5,
            fill: "bladeCore",
            stroke: "darkStroke",
            strokeWidth: 1.5
        }
    ]
};

const protossMothership: UnitIconDefinition = {
    id: "protoss.mothership",
    commander: "protoss",
    aliases: ["Mothership"],
    viewBox: { width: 100, height: 100 },
    tokens: protossTokens,
    layers: [
        {
            type: "path",
            commands: [["M", 50, 6], ["L", 64, 25], ["L", 88, 30], ["L", 76, 49], ["L", 88, 70], ["L", 64, 75], ["L", 50, 94], ["L", 36, 75], ["L", 12, 70], ["L", 24, 49], ["L", 12, 30], ["L", 36, 25], ["Z"]],
            fill: "armorDark",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [["M", 50, 18], ["L", 62, 35], ["L", 67, 58], ["L", 58, 78], ["L", 50, 86], ["L", 42, 78], ["L", 33, 58], ["L", 38, 35], ["Z"]],
            fill: "armorFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 50,
            cy: 54,
            r: 15,
            fill: "psiFill",
            stroke: "psiDark",
            strokeWidth: 3,
            opacity: 0.9
        },
        {
            type: "circle",
            cx: 50,
            cy: 54,
            r: 7,
            fill: "bladeCore",
            opacity: 0.9
        },
        {
            type: "path",
            commands: [["M", 25, 34], ["L", 7, 21], ["M", 75, 34], ["L", 93, 21], ["M", 24, 68], ["L", 6, 81], ["M", 76, 68], ["L", 94, 81], ["M", 50, 20], ["L", 50, 3], ["M", 50, 84], ["L", 50, 98]],
            stroke: "armorMid",
            strokeWidth: 5,
            lineCap: "round"
        },
        {
            type: "path",
            commands: [["M", 18, 49], ["L", 5, 49], ["M", 82, 49], ["L", 95, 49]],
            stroke: "psiMid",
            strokeWidth: 4,
            lineCap: "round"
        }
    ]
};

export const protossUnits = {
    zealot: protossZealot,
    sentry: protossSentry,
    stalker: protossStalker,
    adept: protossAdept,
    highTemplar: protossHighTemplar,
    darkTemplar: protossDarkTemplar,
    archon: protossArchon,
    immortal: protossImmortal,
    colossus: protossColossus,
    disruptor: protossDisruptor,
    observer: protossObserver,
    oracle: protossOracle,
    phoenix: protossPhoenix,
    voidRay: protossVoidRay,
    carrier: protossCarrier,
    tempest: protossTempest,
    mothership: protossMothership
};
