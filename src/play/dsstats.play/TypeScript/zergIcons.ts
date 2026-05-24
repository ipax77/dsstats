import type { UnitIconDefinition } from "./types";

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

const zergRoach: UnitIconDefinition = {
    id: "zerg.roach",
    commander: "zerg",
    aliases: ["Roach", "RoachLightweight"],
    viewBox: { width: 100, height: 100 },
    tokens: {
        skinFill: "#7B4B83",
        skinMid: "#633769",
        skinShade: "#43234A",

        carapaceFill: "#A77CB7",
        carapaceMid: "#7C5A8A",
        carapaceShade: "#4E355B",

        acidFill: "#8EFF5A",
        acidCore: "#D7FF75",
        eyeFill: "#FF5A54",

        clawFill: "#E7D9B8",
        darkStroke: "#26142D"
    },
    layers: [
        // Rear tail / abdomen taper
        {
            type: "path",
            commands: [
                ["M", 17, 59],
                ["C", 9, 58, 7, 51, 13, 47],
                ["C", 20, 43, 29, 47, 34, 53],
                ["L", 31, 64],
                ["C", 26, 63, 21, 61, 17, 59],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Back left leg
        {
            type: "path",
            commands: [
                ["M", 31, 63],
                ["L", 22, 76],
                ["L", 14, 78],
                ["L", 21, 84],
                ["L", 33, 77],
                ["L", 39, 65],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Back right leg
        {
            type: "path",
            commands: [
                ["M", 67, 62],
                ["L", 78, 75],
                ["L", 87, 77],
                ["L", 80, 84],
                ["L", 67, 77],
                ["L", 61, 65],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Main low body silhouette
        {
            type: "path",
            commands: [
                ["M", 25, 58],
                ["C", 28, 43, 42, 34, 58, 35],
                ["C", 73, 36, 85, 45, 88, 57],
                ["C", 87, 68, 77, 75, 60, 77],
                ["C", 43, 79, 29, 72, 25, 58],
                ["Z"]
            ],
            fill: "skinFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Large armored shell / roach back
        {
            type: "path",
            commands: [
                ["M", 31, 55],
                ["C", 36, 41, 47, 34, 59, 36],
                ["C", 72, 38, 82, 46, 85, 56],
                ["C", 78, 61, 68, 64, 56, 64],
                ["C", 45, 64, 36, 61, 31, 55],
                ["Z"]
            ],
            fill: "carapaceFill",
            stroke: "darkStroke",
            strokeWidth: 3.5,
            lineJoin: "round"
        },

        // Shell segmented plates
        {
            type: "path",
            commands: [
                ["M", 43, 39],
                ["C", 39, 46, 38, 55, 42, 62],

                ["M", 56, 36],
                ["C", 52, 45, 52, 56, 57, 64],

                ["M", 70, 41],
                ["C", 66, 48, 66, 57, 72, 62]
            ],
            stroke: "carapaceShade",
            strokeWidth: 3,
            lineCap: "round",
            opacity: 0.9
        },

        // Front head
        {
            type: "path",
            commands: [
                ["M", 72, 54],
                ["C", 79, 48, 89, 49, 94, 56],
                ["C", 94, 63, 87, 68, 77, 67],
                ["L", 68, 62],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 3.5,
            lineJoin: "round"
        },

        // Head carapace / brow
        {
            type: "path",
            commands: [
                ["M", 77, 54],
                ["C", 83, 52, 89, 54, 92, 58],
                ["L", 86, 61],
                ["C", 82, 59, 78, 59, 75, 61],
                ["Z"]
            ],
            fill: "carapaceMid",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },

        // Eye
        {
            type: "circle",
            cx: 85,
            cy: 57,
            r: 2.5,
            fill: "eyeFill",
            stroke: "darkStroke",
            strokeWidth: 1.5
        },

        // Acid mouth sac
        {
            type: "path",
            commands: [
                ["M", 82, 63],
                ["C", 86, 62, 90, 64, 91, 67],
                ["C", 88, 71, 82, 71, 78, 67],
                ["Z"]
            ],
            fill: "acidFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 86,
            cy: 66,
            r: 2,
            fill: "acidCore",
            opacity: 0.85
        },

        // Front left leg
        {
            type: "path",
            commands: [
                ["M", 63, 67],
                ["L", 55, 82],
                ["L", 46, 86],
                ["L", 55, 91],
                ["L", 67, 82],
                ["L", 73, 69],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Front right leg / raised claw
        {
            type: "path",
            commands: [
                ["M", 75, 63],
                ["L", 86, 72],
                ["L", 94, 72],
                ["L", 90, 80],
                ["L", 79, 76],
                ["L", 70, 68],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Claw tips
        {
            type: "path",
            commands: [
                ["M", 14, 78],
                ["L", 7, 82],
                ["L", 20, 84],
                ["Z"],

                ["M", 87, 77],
                ["L", 96, 80],
                ["L", 80, 84],
                ["Z"],

                ["M", 46, 86],
                ["L", 39, 92],
                ["L", 55, 91],
                ["Z"],

                ["M", 94, 72],
                ["L", 99, 75],
                ["L", 90, 80],
                ["Z"]
            ],
            fill: "clawFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },

        // Acid sacs on back
        {
            type: "circle",
            cx: 46,
            cy: 52,
            r: 4,
            fill: "acidFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            opacity: 0.95
        },
        {
            type: "circle",
            cx: 60,
            cy: 50,
            r: 4.5,
            fill: "acidFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            opacity: 0.95
        },
        {
            type: "circle",
            cx: 72,
            cy: 53,
            r: 3.5,
            fill: "acidFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            opacity: 0.95
        },

        // Acid sac highlights
        {
            type: "path",
            commands: [
                ["M", 45, 50],
                ["L", 47, 49],
                ["M", 59, 48],
                ["L", 62, 47],
                ["M", 71, 51],
                ["L", 73, 50]
            ],
            stroke: "acidCore",
            strokeWidth: 2,
            lineCap: "round",
            opacity: 0.8
        },

        // Lower body shadow accent
        {
            type: "path",
            commands: [
                ["M", 31, 65],
                ["C", 42, 71, 62, 72, 78, 66]
            ],
            stroke: "skinShade",
            strokeWidth: 4,
            lineCap: "round",
            opacity: 0.65
        }
    ]
};

const zergQueen: UnitIconDefinition = {
    id: "zerg.queen",
    commander: "zerg",
    aliases: ["Queen", "QueenLightweight"],
    viewBox: { width: 100, height: 100 },
    tokens: {
        skinFill: "#8A4F8F",
        skinMid: "#6D3B73",
        skinShade: "#47224F",

        carapaceFill: "#B487C8",
        carapaceMid: "#7D5A8F",
        carapaceShade: "#4A315A",

        clawFill: "#E8D7B2",
        spineFill: "#C6A0D8",

        eyeFill: "#FF5A54",
        acidFill: "#8EFF5A",
        acidCore: "#D7FF75",

        darkStroke: "#24122C"
    },
    layers: [
        // Rear abdomen / organic base
        {
            type: "path",
            commands: [
                ["M", 32, 69],
                ["C", 32, 55, 40, 47, 50, 47],
                ["C", 60, 47, 68, 55, 68, 69],
                ["C", 68, 84, 60, 93, 50, 94],
                ["C", 40, 93, 32, 84, 32, 69],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Abdomen front plate
        {
            type: "path",
            commands: [
                ["M", 39, 67],
                ["C", 40, 58, 44, 53, 50, 53],
                ["C", 56, 53, 60, 58, 61, 67],
                ["C", 61, 80, 56, 88, 50, 90],
                ["C", 44, 88, 39, 80, 39, 67],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },

        // Back carapace / upper shell
        {
            type: "path",
            commands: [
                ["M", 26, 50],
                ["C", 30, 36, 39, 28, 50, 28],
                ["C", 61, 28, 70, 36, 74, 50],
                ["L", 65, 63],
                ["L", 35, 63],
                ["Z"]
            ],
            fill: "carapaceShade",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Main torso
        {
            type: "path",
            commands: [
                ["M", 34, 48],
                ["C", 36, 35, 43, 28, 50, 28],
                ["C", 57, 28, 64, 35, 66, 48],
                ["L", 62, 68],
                ["L", 50, 74],
                ["L", 38, 68],
                ["Z"]
            ],
            fill: "skinFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Chest carapace
        {
            type: "path",
            commands: [
                ["M", 38, 49],
                ["C", 41, 41, 45, 37, 50, 37],
                ["C", 55, 37, 59, 41, 62, 49],
                ["L", 58, 61],
                ["L", 50, 66],
                ["L", 42, 61],
                ["Z"]
            ],
            fill: "carapaceFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Head
        {
            type: "path",
            commands: [
                ["M", 38, 32],
                ["C", 39, 21, 44, 14, 50, 14],
                ["C", 56, 14, 61, 21, 62, 32],
                ["L", 59, 43],
                ["L", 50, 47],
                ["L", 41, 43],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Crown / queen crest
        {
            type: "path",
            commands: [
                ["M", 41, 24],
                ["L", 35, 9],
                ["L", 46, 20],

                ["M", 50, 18],
                ["L", 50, 4],
                ["L", 56, 20],

                ["M", 59, 24],
                ["L", 65, 9],
                ["L", 54, 20]
            ],
            fill: "spineFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round",
            lineCap: "round"
        },

        // Face plate / brow
        {
            type: "path",
            commands: [
                ["M", 41, 31],
                ["C", 44, 26, 47, 24, 50, 24],
                ["C", 53, 24, 56, 26, 59, 31],
                ["L", 56, 37],
                ["L", 44, 37],
                ["Z"]
            ],
            fill: "carapaceFill",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },

        // Eyes
        {
            type: "circle",
            cx: 46,
            cy: 33,
            r: 2.2,
            fill: "eyeFill",
            stroke: "darkStroke",
            strokeWidth: 1
        },
        {
            type: "circle",
            cx: 54,
            cy: 33,
            r: 2.2,
            fill: "eyeFill",
            stroke: "darkStroke",
            strokeWidth: 1
        },

        // Mouth / mandibles
        {
            type: "path",
            commands: [
                ["M", 45, 40],
                ["L", 39, 48],
                ["L", 47, 44],

                ["M", 55, 40],
                ["L", 61, 48],
                ["L", 53, 44]
            ],
            fill: "clawFill",
            stroke: "darkStroke",
            strokeWidth: 2.2,
            lineJoin: "round",
            lineCap: "round"
        },

        // Left upper arm
        {
            type: "path",
            commands: [
                ["M", 35, 52],
                ["L", 22, 42],
                ["L", 13, 47],
                ["L", 20, 55],
                ["L", 34, 61],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Right upper arm
        {
            type: "path",
            commands: [
                ["M", 65, 52],
                ["L", 78, 42],
                ["L", 87, 47],
                ["L", 80, 55],
                ["L", 66, 61],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Upper claw tips
        {
            type: "path",
            commands: [
                ["M", 13, 47],
                ["L", 5, 45],
                ["L", 13, 54],
                ["Z"],

                ["M", 87, 47],
                ["L", 95, 45],
                ["L", 87, 54],
                ["Z"]
            ],
            fill: "clawFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },

        // Left lower arm
        {
            type: "path",
            commands: [
                ["M", 39, 65],
                ["L", 28, 72],
                ["L", 23, 84],
                ["L", 32, 83],
                ["L", 43, 72],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Right lower arm
        {
            type: "path",
            commands: [
                ["M", 61, 65],
                ["L", 72, 72],
                ["L", 77, 84],
                ["L", 68, 83],
                ["L", 57, 72],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Lower claw tips
        {
            type: "path",
            commands: [
                ["M", 23, 84],
                ["L", 16, 91],
                ["L", 32, 83],
                ["Z"],

                ["M", 77, 84],
                ["L", 84, 91],
                ["L", 68, 83],
                ["Z"]
            ],
            fill: "clawFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },

        // Small side legs / stabilizers
        {
            type: "path",
            commands: [
                ["M", 35, 76],
                ["L", 25, 92],
                ["M", 65, 76],
                ["L", 75, 92]
            ],
            stroke: "skinShade",
            strokeWidth: 5,
            lineCap: "round"
        },

        // Spawn/acid sacs on abdomen
        {
            type: "circle",
            cx: 44,
            cy: 74,
            r: 3.5,
            fill: "acidFill",
            stroke: "darkStroke",
            strokeWidth: 1.5,
            opacity: 0.95
        },
        {
            type: "circle",
            cx: 56,
            cy: 74,
            r: 3.5,
            fill: "acidFill",
            stroke: "darkStroke",
            strokeWidth: 1.5,
            opacity: 0.95
        },
        {
            type: "circle",
            cx: 50,
            cy: 83,
            r: 3.8,
            fill: "acidFill",
            stroke: "darkStroke",
            strokeWidth: 1.5,
            opacity: 0.95
        },

        // Carapace seams
        {
            type: "path",
            commands: [
                ["M", 42, 52],
                ["L", 50, 58],
                ["L", 58, 52],

                ["M", 40, 68],
                ["C", 45, 71, 55, 71, 60, 68],

                ["M", 43, 78],
                ["C", 47, 81, 53, 81, 57, 78]
            ],
            stroke: "carapaceShade",
            strokeWidth: 2.3,
            lineCap: "round",
            lineJoin: "round",
            opacity: 0.85
        },

        // Acid highlights
        {
            type: "path",
            commands: [
                ["M", 43, 72],
                ["L", 45, 71],
                ["M", 55, 72],
                ["L", 57, 71],
                ["M", 49, 81],
                ["L", 52, 80]
            ],
            stroke: "acidCore",
            strokeWidth: 2,
            lineCap: "round",
            opacity: 0.8
        }
    ]
};

const zergHydralisk: UnitIconDefinition = {
    id: "zerg.hydralisk",
    commander: "zerg",
    aliases: ["Hydralisk", "HydraliskLightweight"],
    viewBox: { width: 100, height: 100 },
    tokens: {
        skinFill: "#83508B",
        skinMid: "#67396F",
        skinShade: "#43234A",

        carapaceFill: "#B98BCC",
        carapaceMid: "#835E92",
        carapaceShade: "#4D355C",

        clawFill: "#E8D7B2",
        spineFill: "#D6B4E5",

        eyeFill: "#FF5A54",
        acidFill: "#8EFF5A",
        acidCore: "#D7FF75",

        darkStroke: "#24122C"
    },
    layers: [
        // Coiled lower tail shadow
        {
            type: "path",
            commands: [
                ["M", 29, 82],
                ["C", 35, 72, 47, 69, 58, 73],
                ["C", 70, 77, 75, 87, 67, 93],
                ["C", 58, 99, 39, 97, 29, 89],
                ["C", 24, 86, 24, 84, 29, 82],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Main serpent body
        {
            type: "path",
            commands: [
                ["M", 47, 88],
                ["C", 43, 76, 43, 66, 47, 55],
                ["C", 51, 44, 52, 34, 49, 24],
                ["C", 46, 16, 50, 9, 57, 9],
                ["C", 67, 10, 74, 22, 72, 35],
                ["C", 70, 50, 63, 60, 62, 73],
                ["C", 62, 83, 67, 89, 75, 92],
                ["C", 65, 97, 53, 96, 47, 88],
                ["Z"]
            ],
            fill: "skinFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Chest / belly plate
        {
            type: "path",
            commands: [
                ["M", 53, 35],
                ["C", 58, 43, 58, 56, 55, 70],
                ["C", 53, 80, 55, 87, 61, 92],
                ["C", 55, 92, 51, 89, 50, 83],
                ["C", 48, 70, 51, 55, 50, 44],
                ["C", 50, 39, 50, 36, 53, 35],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },

        // Back carapace ridge
        {
            type: "path",
            commands: [
                ["M", 58, 12],
                ["C", 65, 16, 68, 25, 67, 36],
                ["C", 66, 50, 59, 61, 59, 74],
                ["C", 59, 82, 63, 88, 69, 91],
                ["C", 63, 92, 58, 90, 56, 84],
                ["C", 53, 72, 57, 57, 59, 44],
                ["C", 61, 31, 58, 21, 54, 14],
                ["Z"]
            ],
            fill: "carapaceMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round",
            opacity: 0.95
        },

        // Hood / head crest
        {
            type: "path",
            commands: [
                ["M", 39, 23],
                ["C", 41, 11, 49, 4, 59, 5],
                ["C", 72, 6, 83, 17, 84, 31],
                ["C", 75, 25, 66, 23, 58, 26],
                ["C", 51, 28, 45, 27, 39, 23],
                ["Z"]
            ],
            fill: "carapaceFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Face / snout
        {
            type: "path",
            commands: [
                ["M", 50, 25],
                ["C", 58, 20, 70, 23, 76, 32],
                ["C", 79, 38, 75, 45, 67, 47],
                ["C", 58, 48, 50, 42, 47, 34],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 3.5,
            lineJoin: "round"
        },

        // Face plate / brow
        {
            type: "path",
            commands: [
                ["M", 56, 28],
                ["C", 62, 26, 69, 29, 73, 34],
                ["L", 67, 38],
                ["C", 62, 35, 57, 35, 53, 37],
                ["Z"]
            ],
            fill: "carapaceShade",
            stroke: "darkStroke",
            strokeWidth: 2.3,
            lineJoin: "round"
        },

        // Eye
        {
            type: "circle",
            cx: 65,
            cy: 32,
            r: 2.6,
            fill: "eyeFill",
            stroke: "darkStroke",
            strokeWidth: 1.4
        },

        // Mouth acid glow
        {
            type: "path",
            commands: [
                ["M", 67, 42],
                ["C", 72, 41, 76, 44, 77, 48],
                ["C", 73, 51, 67, 50, 63, 46],
                ["Z"]
            ],
            fill: "acidFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 71,
            cy: 46,
            r: 2,
            fill: "acidCore",
            opacity: 0.9
        },

        // Left scythe arm
        {
            type: "path",
            commands: [
                ["M", 48, 47],
                ["C", 39, 49, 31, 57, 27, 68],
                ["L", 17, 73],
                ["L", 24, 80],
                ["L", 35, 72],
                ["C", 40, 65, 45, 58, 51, 54],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Left blade
        {
            type: "path",
            commands: [
                ["M", 17, 73],
                ["C", 10, 74, 6, 78, 4, 86],
                ["C", 12, 84, 19, 82, 24, 80],
                ["Z"]
            ],
            fill: "clawFill",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },

        // Right scythe arm
        {
            type: "path",
            commands: [
                ["M", 67, 48],
                ["C", 76, 51, 83, 59, 86, 70],
                ["L", 96, 75],
                ["L", 89, 82],
                ["L", 78, 74],
                ["C", 73, 66, 68, 60, 62, 55],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Right blade
        {
            type: "path",
            commands: [
                ["M", 96, 75],
                ["C", 101, 78, 104, 84, 104, 91],
                ["C", 97, 86, 92, 84, 89, 82],
                ["Z"]
            ],
            fill: "clawFill",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },

        // Dorsal quills / spines
        {
            type: "path",
            commands: [
                ["M", 45, 22],
                ["L", 35, 10],
                ["L", 50, 19],
                ["Z"],

                ["M", 53, 15],
                ["L", 51, 1],
                ["L", 60, 14],
                ["Z"],

                ["M", 66, 19],
                ["L", 76, 8],
                ["L", 72, 23],
                ["Z"],

                ["M", 58, 47],
                ["L", 70, 42],
                ["L", 61, 54],
                ["Z"],

                ["M", 56, 60],
                ["L", 68, 59],
                ["L", 58, 68],
                ["Z"]
            ],
            fill: "spineFill",
            stroke: "darkStroke",
            strokeWidth: 2.3,
            lineJoin: "round"
        },

        // Chest / body segmentation
        {
            type: "path",
            commands: [
                ["M", 51, 43],
                ["C", 56, 46, 61, 46, 66, 43],

                ["M", 50, 55],
                ["C", 54, 58, 59, 58, 63, 55],

                ["M", 49, 67],
                ["C", 53, 70, 58, 70, 62, 67],

                ["M", 50, 79],
                ["C", 54, 82, 60, 82, 65, 79]
            ],
            stroke: "carapaceShade",
            strokeWidth: 2.5,
            lineCap: "round",
            opacity: 0.85
        },

        // Lower tail highlight
        {
            type: "path",
            commands: [
                ["M", 34, 84],
                ["C", 43, 89, 57, 90, 68, 86]
            ],
            stroke: "skinMid",
            strokeWidth: 4,
            lineCap: "round",
            opacity: 0.75
        }
    ]
};

const zergInfestor: UnitIconDefinition = {
    id: "zerg.infestor",
    commander: "zerg",
    aliases: ["Infestor", "InfestorLightweight"],
    viewBox: { width: 100, height: 100 },
    tokens: {
        skinFill: "#7E4B87",
        skinMid: "#613568",
        skinShade: "#402247",

        carapaceFill: "#AA7BBC",
        carapaceMid: "#7D5A8F",
        carapaceShade: "#4B3158",

        fungalFill: "#7DFF5A",
        fungalCore: "#D7FF75",
        fungalDark: "#3B9E43",

        eyeFill: "#FF5A54",
        clawFill: "#E8D7B2",

        darkStroke: "#24122C"
    },
    layers: [
        // Rear bloated body shadow
        {
            type: "path",
            commands: [
                ["M", 19, 62],
                ["C", 19, 43, 34, 31, 53, 31],
                ["C", 73, 31, 87, 44, 87, 62],
                ["C", 87, 78, 72, 89, 52, 89],
                ["C", 32, 89, 19, 78, 19, 62],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Main bloated body
        {
            type: "path",
            commands: [
                ["M", 24, 60],
                ["C", 25, 45, 38, 35, 54, 35],
                ["C", 70, 35, 82, 46, 82, 61],
                ["C", 82, 74, 69, 83, 52, 83],
                ["C", 35, 83, 24, 73, 24, 60],
                ["Z"]
            ],
            fill: "skinFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Upper shell plate
        {
            type: "path",
            commands: [
                ["M", 30, 55],
                ["C", 34, 42, 44, 36, 56, 37],
                ["C", 68, 38, 77, 47, 79, 58],
                ["C", 69, 61, 53, 62, 39, 60],
                ["Z"]
            ],
            fill: "carapaceFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Shell plate segmentation
        {
            type: "path",
            commands: [
                ["M", 42, 39],
                ["C", 39, 45, 39, 54, 43, 60],

                ["M", 55, 37],
                ["C", 52, 45, 52, 54, 56, 61],

                ["M", 68, 43],
                ["C", 64, 49, 64, 56, 69, 60]
            ],
            stroke: "carapaceShade",
            strokeWidth: 2.8,
            lineCap: "round",
            opacity: 0.9
        },

        // Front head/mouth cluster
        {
            type: "path",
            commands: [
                ["M", 67, 54],
                ["C", 76, 49, 88, 52, 94, 62],
                ["C", 94, 70, 87, 75, 77, 73],
                ["L", 66, 67],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3.5,
            lineJoin: "round"
        },

        // Head brow / face plate
        {
            type: "path",
            commands: [
                ["M", 73, 56],
                ["C", 80, 54, 87, 57, 91, 62],
                ["L", 85, 66],
                ["C", 81, 63, 76, 63, 72, 65],
                ["Z"]
            ],
            fill: "carapaceMid",
            stroke: "darkStroke",
            strokeWidth: 2.3,
            lineJoin: "round"
        },

        // Eyes
        {
            type: "circle",
            cx: 82,
            cy: 60,
            r: 2.3,
            fill: "eyeFill",
            stroke: "darkStroke",
            strokeWidth: 1.3
        },
        {
            type: "circle",
            cx: 88,
            cy: 64,
            r: 1.8,
            fill: "eyeFill",
            stroke: "darkStroke",
            strokeWidth: 1
        },

        // Mouth / mandibles
        {
            type: "path",
            commands: [
                ["M", 80, 69],
                ["L", 75, 78],
                ["L", 84, 72],

                ["M", 88, 69],
                ["L", 94, 76],
                ["L", 85, 73]
            ],
            fill: "clawFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineCap: "round",
            lineJoin: "round"
        },

        // Left front tentacle
        {
            type: "path",
            commands: [
                ["M", 43, 74],
                ["C", 35, 80, 28, 88, 18, 90],
                ["C", 25, 82, 30, 75, 39, 68],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Right front tentacle
        {
            type: "path",
            commands: [
                ["M", 59, 75],
                ["C", 68, 80, 77, 87, 88, 88],
                ["C", 80, 82, 72, 75, 63, 68],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Left side tentacle
        {
            type: "path",
            commands: [
                ["M", 29, 65],
                ["C", 18, 66, 10, 72, 7, 82],
                ["C", 17, 79, 25, 75, 34, 68],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 2.8,
            lineJoin: "round"
        },

        // Rear right tentacle
        {
            type: "path",
            commands: [
                ["M", 70, 66],
                ["C", 80, 66, 90, 70, 97, 78],
                ["C", 86, 78, 76, 75, 67, 70],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 2.8,
            lineJoin: "round"
        },

        // Tentacle claw/tips
        {
            type: "path",
            commands: [
                ["M", 18, 90],
                ["L", 10, 94],
                ["L", 23, 93],
                ["Z"],

                ["M", 88, 88],
                ["L", 96, 92],
                ["L", 83, 92],
                ["Z"],

                ["M", 7, 82],
                ["L", 0, 86],
                ["L", 12, 85],
                ["Z"],

                ["M", 97, 78],
                ["L", 104, 81],
                ["L", 92, 82],
                ["Z"]
            ],
            fill: "clawFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },

        // Large fungal sacs
        {
            type: "circle",
            cx: 39,
            cy: 56,
            r: 5.2,
            fill: "fungalFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            opacity: 0.96
        },
        {
            type: "circle",
            cx: 53,
            cy: 52,
            r: 6,
            fill: "fungalFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            opacity: 0.96
        },
        {
            type: "circle",
            cx: 67,
            cy: 57,
            r: 5,
            fill: "fungalFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            opacity: 0.96
        },
        {
            type: "circle",
            cx: 46,
            cy: 70,
            r: 4.5,
            fill: "fungalFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            opacity: 0.96
        },
        {
            type: "circle",
            cx: 61,
            cy: 69,
            r: 4.2,
            fill: "fungalFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            opacity: 0.96
        },

        // Fungal sac cores / highlights
        {
            type: "path",
            commands: [
                ["M", 37, 54],
                ["L", 41, 53],
                ["M", 51, 49],
                ["L", 56, 48],
                ["M", 65, 55],
                ["L", 69, 54],
                ["M", 44, 68],
                ["L", 48, 67],
                ["M", 59, 67],
                ["L", 63, 66]
            ],
            stroke: "fungalCore",
            strokeWidth: 2,
            lineCap: "round",
            opacity: 0.85
        },

        // Belly shadow curve
        {
            type: "path",
            commands: [
                ["M", 30, 68],
                ["C", 42, 76, 63, 77, 77, 69]
            ],
            stroke: "skinShade",
            strokeWidth: 4,
            lineCap: "round",
            opacity: 0.6
        },

        // Small dark pores
        {
            type: "circle",
            cx: 33,
            cy: 62,
            r: 1.5,
            fill: "fungalDark",
            opacity: 0.9
        },
        {
            type: "circle",
            cx: 57,
            cy: 61,
            r: 1.4,
            fill: "fungalDark",
            opacity: 0.9
        },
        {
            type: "circle",
            cx: 72,
            cy: 64,
            r: 1.5,
            fill: "fungalDark",
            opacity: 0.9
        }
    ]
};

const zergCorruptor: UnitIconDefinition = {
    id: "zerg.corruptor",
    commander: "zerg",
    aliases: ["Corruptor", "CorruptorLightweight"],
    viewBox: { width: 100, height: 100 },
    tokens: {
        skinFill: "#7E4B87",
        skinMid: "#633769",
        skinShade: "#402247",

        carapaceFill: "#AD7FC0",
        carapaceMid: "#7C5A8A",
        carapaceShade: "#4B3158",

        wingFill: "#8D5A98",
        wingShade: "#56315F",

        corruptionFill: "#8EFF5A",
        corruptionCore: "#D7FF75",
        corruptionDark: "#3B9E43",

        eyeFill: "#FF5A54",
        clawFill: "#E8D7B2",

        darkStroke: "#24122C"
    },
    layers: [
        // Left wing membrane
        {
            type: "path",
            commands: [
                ["M", 43, 42],
                ["C", 31, 30, 17, 27, 6, 34],
                ["C", 14, 42, 21, 50, 25, 61],
                ["C", 31, 56, 38, 51, 47, 49],
                ["Z"]
            ],
            fill: "wingFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Right wing membrane
        {
            type: "path",
            commands: [
                ["M", 57, 42],
                ["C", 69, 30, 83, 27, 94, 34],
                ["C", 86, 42, 79, 50, 75, 61],
                ["C", 69, 56, 62, 51, 53, 49],
                ["Z"]
            ],
            fill: "wingFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Left wing dark inner fold
        {
            type: "path",
            commands: [
                ["M", 18, 38],
                ["C", 24, 43, 30, 49, 34, 56],
                ["M", 31, 33],
                ["C", 35, 39, 40, 44, 45, 48]
            ],
            stroke: "wingShade",
            strokeWidth: 3,
            lineCap: "round",
            opacity: 0.9
        },

        // Right wing dark inner fold
        {
            type: "path",
            commands: [
                ["M", 82, 38],
                ["C", 76, 43, 70, 49, 66, 56],
                ["M", 69, 33],
                ["C", 65, 39, 60, 44, 55, 48]
            ],
            stroke: "wingShade",
            strokeWidth: 3,
            lineCap: "round",
            opacity: 0.9
        },

        // Rear floating tail / stinger
        {
            type: "path",
            commands: [
                ["M", 43, 66],
                ["C", 43, 77, 47, 86, 50, 94],
                ["C", 54, 86, 57, 77, 57, 66],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Main manta body
        {
            type: "path",
            commands: [
                ["M", 30, 48],
                ["C", 33, 33, 42, 24, 50, 23],
                ["C", 58, 24, 67, 33, 70, 48],
                ["C", 72, 63, 63, 74, 50, 76],
                ["C", 37, 74, 28, 63, 30, 48],
                ["Z"]
            ],
            fill: "skinFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Upper carapace
        {
            type: "path",
            commands: [
                ["M", 35, 45],
                ["C", 38, 32, 44, 26, 50, 26],
                ["C", 56, 26, 62, 32, 65, 45],
                ["C", 60, 51, 54, 54, 50, 55],
                ["C", 46, 54, 40, 51, 35, 45],
                ["Z"]
            ],
            fill: "carapaceFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Hooked head / beak
        {
            type: "path",
            commands: [
                ["M", 41, 29],
                ["C", 44, 18, 50, 10, 57, 9],
                ["C", 66, 9, 72, 18, 69, 28],
                ["C", 67, 36, 59, 41, 50, 40],
                ["C", 45, 39, 42, 35, 41, 29],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Face plate
        {
            type: "path",
            commands: [
                ["M", 50, 24],
                ["C", 55, 20, 63, 22, 66, 28],
                ["L", 61, 33],
                ["C", 57, 30, 52, 30, 48, 33],
                ["Z"]
            ],
            fill: "carapaceMid",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },

        // Eye
        {
            type: "circle",
            cx: 59,
            cy: 27,
            r: 2.5,
            fill: "eyeFill",
            stroke: "darkStroke",
            strokeWidth: 1.4
        },

        // Corruption mouth sac
        {
            type: "path",
            commands: [
                ["M", 58, 36],
                ["C", 65, 35, 70, 39, 70, 45],
                ["C", 65, 48, 57, 46, 53, 41],
                ["Z"]
            ],
            fill: "corruptionFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 63,
            cy: 42,
            r: 2.2,
            fill: "corruptionCore",
            opacity: 0.9
        },

        // Underside body plate
        {
            type: "path",
            commands: [
                ["M", 38, 57],
                ["C", 45, 62, 55, 62, 62, 57],
                ["L", 58, 70],
                ["L", 50, 74],
                ["L", 42, 70],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Back corruption sacs
        {
            type: "circle",
            cx: 43,
            cy: 49,
            r: 4.2,
            fill: "corruptionFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            opacity: 0.96
        },
        {
            type: "circle",
            cx: 54,
            cy: 48,
            r: 4.8,
            fill: "corruptionFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            opacity: 0.96
        },
        {
            type: "circle",
            cx: 61,
            cy: 57,
            r: 3.7,
            fill: "corruptionFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            opacity: 0.96
        },

        // Sac highlights
        {
            type: "path",
            commands: [
                ["M", 42, 47],
                ["L", 45, 46],
                ["M", 53, 45],
                ["L", 57, 44],
                ["M", 60, 55],
                ["L", 63, 54]
            ],
            stroke: "corruptionCore",
            strokeWidth: 2,
            lineCap: "round",
            opacity: 0.8
        },

        // Lower tentacles
        {
            type: "path",
            commands: [
                ["M", 44, 70],
                ["C", 39, 76, 36, 84, 32, 91],
                ["M", 50, 73],
                ["C", 49, 81, 49, 88, 47, 96],
                ["M", 56, 70],
                ["C", 62, 77, 65, 84, 69, 91]
            ],
            stroke: "skinShade",
            strokeWidth: 4,
            lineCap: "round",
            opacity: 0.95
        },

        // Tentacle tips
        {
            type: "path",
            commands: [
                ["M", 32, 91],
                ["L", 26, 96],
                ["L", 36, 95],
                ["Z"],

                ["M", 47, 96],
                ["L", 43, 101],
                ["L", 52, 100],
                ["Z"],

                ["M", 69, 91],
                ["L", 75, 96],
                ["L", 65, 95],
                ["Z"]
            ],
            fill: "clawFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },

        // Outer wing claws/hooks
        {
            type: "path",
            commands: [
                ["M", 6, 34],
                ["L", 0, 29],
                ["L", 3, 42],
                ["Z"],

                ["M", 94, 34],
                ["L", 100, 29],
                ["L", 97, 42],
                ["Z"]
            ],
            fill: "clawFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },

        // Carapace seams
        {
            type: "path",
            commands: [
                ["M", 40, 41],
                ["C", 46, 44, 54, 44, 60, 41],
                ["M", 37, 53],
                ["C", 45, 57, 56, 57, 64, 53],
                ["M", 50, 29],
                ["L", 50, 53]
            ],
            stroke: "carapaceShade",
            strokeWidth: 2.4,
            lineCap: "round",
            opacity: 0.85
        }
    ]
};

export const zergUnits = {
    zergling: zergZergling,
    roach: zergRoach,
    queen: zergQueen,
    hydralisk: zergHydralisk,
    infestor: zergInfestor,
    corruptor: zergCorruptor,
};
