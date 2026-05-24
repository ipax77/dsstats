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

const zergLurker: UnitIconDefinition = {
    id: "zerg.lurker",
    commander: "zerg",
    aliases: ["Lurker", "LurkerLightweight", "LurkerMP"],
    viewBox: { width: 100, height: 100 },
    tokens: {
        skinFill: "#7B4B83",
        skinMid: "#633769",
        skinShade: "#402247",

        carapaceFill: "#A77CB7",
        carapaceMid: "#7C5A8A",
        carapaceShade: "#4E355B",

        spineFill: "#D6B4E5",
        spikeFill: "#E8D7B2",

        eyeFill: "#FF5A54",
        acidFill: "#8EFF5A",
        acidCore: "#D7FF75",

        darkStroke: "#24122C"
    },
    layers: [
        // Underground shadow / burrow mound
        {
            type: "path",
            commands: [
                ["M", 15, 73],
                ["C", 25, 63, 43, 58, 61, 60],
                ["C", 78, 61, 91, 68, 96, 78],
                ["C", 83, 86, 60, 90, 39, 87],
                ["C", 24, 85, 14, 80, 15, 73],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round",
            opacity: 0.95
        },

        // Rear abdomen taper
        {
            type: "path",
            commands: [
                ["M", 18, 59],
                ["C", 10, 57, 8, 50, 14, 46],
                ["C", 22, 41, 33, 47, 37, 55],
                ["L", 33, 67],
                ["C", 27, 65, 22, 62, 18, 59],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Main low body
        {
            type: "path",
            commands: [
                ["M", 25, 59],
                ["C", 30, 43, 44, 34, 60, 36],
                ["C", 75, 38, 87, 48, 90, 61],
                ["C", 88, 72, 76, 78, 59, 78],
                ["C", 41, 79, 29, 72, 25, 59],
                ["Z"]
            ],
            fill: "skinFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Heavy armored shell
        {
            type: "path",
            commands: [
                ["M", 30, 55],
                ["C", 35, 41, 47, 34, 61, 37],
                ["C", 74, 39, 84, 48, 87, 59],
                ["C", 77, 64, 64, 66, 50, 64],
                ["C", 40, 63, 34, 60, 30, 55],
                ["Z"]
            ],
            fill: "carapaceFill",
            stroke: "darkStroke",
            strokeWidth: 3.5,
            lineJoin: "round"
        },

        // Shell side plates
        {
            type: "path",
            commands: [
                ["M", 38, 47],
                ["C", 36, 53, 38, 59, 43, 63],

                ["M", 51, 39],
                ["C", 48, 47, 49, 57, 55, 65],

                ["M", 66, 41],
                ["C", 63, 49, 65, 58, 72, 63],

                ["M", 78, 49],
                ["C", 75, 54, 77, 59, 82, 61]
            ],
            stroke: "carapaceShade",
            strokeWidth: 3,
            lineCap: "round",
            opacity: 0.9
        },

        // Long dorsal lurker spines
        {
            type: "path",
            commands: [
                ["M", 34, 49],
                ["L", 27, 26],
                ["L", 43, 45],
                ["Z"],

                ["M", 45, 42],
                ["L", 43, 13],
                ["L", 55, 41],
                ["Z"],

                ["M", 58, 39],
                ["L", 64, 10],
                ["L", 67, 43],
                ["Z"],

                ["M", 71, 44],
                ["L", 84, 21],
                ["L", 80, 50],
                ["Z"]
            ],
            fill: "spineFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Front head
        {
            type: "path",
            commands: [
                ["M", 72, 55],
                ["C", 80, 50, 90, 52, 95, 60],
                ["C", 95, 67, 87, 72, 77, 70],
                ["L", 67, 64],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 3.5,
            lineJoin: "round"
        },

        // Head armor brow
        {
            type: "path",
            commands: [
                ["M", 77, 56],
                ["C", 83, 54, 90, 56, 93, 61],
                ["L", 87, 65],
                ["C", 83, 62, 78, 62, 74, 64],
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
            cx: 86,
            cy: 59,
            r: 2.5,
            fill: "eyeFill",
            stroke: "darkStroke",
            strokeWidth: 1.4
        },

        // Front digging claw left
        {
            type: "path",
            commands: [
                ["M", 69, 68],
                ["L", 80, 78],
                ["L", 94, 80],
                ["L", 88, 88],
                ["L", 75, 83],
                ["L", 63, 72],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Front digging claw right
        {
            type: "path",
            commands: [
                ["M", 58, 70],
                ["L", 50, 84],
                ["L", 39, 90],
                ["L", 52, 93],
                ["L", 65, 82],
                ["L", 70, 72],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Rear stabilizing leg
        {
            type: "path",
            commands: [
                ["M", 34, 67],
                ["L", 23, 80],
                ["L", 12, 82],
                ["L", 21, 88],
                ["L", 35, 80],
                ["L", 42, 69],
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
                ["M", 94, 80],
                ["L", 101, 84],
                ["L", 88, 88],
                ["Z"],

                ["M", 39, 90],
                ["L", 31, 96],
                ["L", 52, 93],
                ["Z"],

                ["M", 12, 82],
                ["L", 5, 86],
                ["L", 21, 88],
                ["Z"]
            ],
            fill: "spikeFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },

        // Burrow attack spikes emerging from ground
        {
            type: "path",
            commands: [
                ["M", 24, 82],
                ["L", 29, 63],
                ["L", 34, 83],
                ["Z"],

                ["M", 48, 87],
                ["L", 54, 67],
                ["L", 59, 88],
                ["Z"],

                ["M", 76, 83],
                ["L", 82, 65],
                ["L", 87, 84],
                ["Z"]
            ],
            fill: "spikeFill",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round",
            opacity: 0.95
        },

        // Acid / sensory sacs
        {
            type: "circle",
            cx: 47,
            cy: 55,
            r: 3.6,
            fill: "acidFill",
            stroke: "darkStroke",
            strokeWidth: 1.8,
            opacity: 0.95
        },
        {
            type: "circle",
            cx: 62,
            cy: 53,
            r: 4.2,
            fill: "acidFill",
            stroke: "darkStroke",
            strokeWidth: 1.8,
            opacity: 0.95
        },

        // Sac highlights
        {
            type: "path",
            commands: [
                ["M", 46, 53],
                ["L", 49, 52],
                ["M", 61, 51],
                ["L", 64, 50]
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
                ["M", 31, 67],
                ["C", 43, 73, 64, 74, 80, 67]
            ],
            stroke: "skinShade",
            strokeWidth: 4,
            lineCap: "round",
            opacity: 0.65
        }
    ]
};

const zergMutalisk: UnitIconDefinition = {
    id: "zerg.mutalisk",
    commander: "zerg",
    aliases: ["Mutalisk", "MutaliskLightweight", "ZergAir"],
    viewBox: { width: 100, height: 100 },
    tokens: {
        skinFill: "#7E4B87",
        skinMid: "#633769",
        skinShade: "#402247",

        wingFill: "#9A63A6",
        wingMid: "#7B4D87",
        wingShade: "#4D2B56",

        carapaceFill: "#B98BCC",
        carapaceMid: "#805A91",
        carapaceShade: "#4A3158",

        clawFill: "#E8D7B2",
        spineFill: "#D6B4E5",

        eyeFill: "#FF5A54",
        glaiveFill: "#8EFF5A",
        glaiveCore: "#D7FF75",

        darkStroke: "#24122C"
    },
    layers: [
        // Rear tail
        {
            type: "path",
            commands: [
                ["M", 42, 66],
                ["C", 36, 78, 32, 88, 25, 97],
                ["C", 38, 94, 48, 86, 53, 72],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Left wing outer crescent
        {
            type: "path",
            commands: [
                ["M", 44, 44],
                ["C", 31, 25, 15, 17, 2, 22],
                ["C", 8, 32, 14, 44, 18, 58],
                ["C", 26, 53, 35, 49, 48, 50],
                ["Z"]
            ],
            fill: "wingFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Right wing outer crescent
        {
            type: "path",
            commands: [
                ["M", 56, 44],
                ["C", 69, 25, 85, 17, 98, 22],
                ["C", 92, 32, 86, 44, 82, 58],
                ["C", 74, 53, 65, 49, 52, 50],
                ["Z"]
            ],
            fill: "wingFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Left inner wing fold
        {
            type: "path",
            commands: [
                ["M", 12, 27],
                ["C", 20, 35, 27, 43, 33, 53],

                ["M", 27, 24],
                ["C", 31, 33, 38, 41, 46, 48]
            ],
            stroke: "wingShade",
            strokeWidth: 3,
            lineCap: "round",
            opacity: 0.9
        },

        // Right inner wing fold
        {
            type: "path",
            commands: [
                ["M", 88, 27],
                ["C", 80, 35, 73, 43, 67, 53],

                ["M", 73, 24],
                ["C", 69, 33, 62, 41, 54, 48]
            ],
            stroke: "wingShade",
            strokeWidth: 3,
            lineCap: "round",
            opacity: 0.9
        },

        // Wing lower hooks
        {
            type: "path",
            commands: [
                ["M", 18, 58],
                ["L", 9, 66],
                ["L", 24, 64],
                ["Z"],

                ["M", 82, 58],
                ["L", 91, 66],
                ["L", 76, 64],
                ["Z"]
            ],
            fill: "clawFill",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },

        // Main body
        {
            type: "path",
            commands: [
                ["M", 34, 48],
                ["C", 36, 35, 44, 27, 53, 27],
                ["C", 63, 28, 70, 38, 69, 51],
                ["C", 68, 64, 59, 73, 48, 73],
                ["C", 39, 71, 33, 61, 34, 48],
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
                ["M", 39, 46],
                ["C", 42, 34, 48, 29, 54, 30],
                ["C", 61, 31, 66, 39, 65, 49],
                ["C", 60, 53, 52, 54, 44, 52],
                ["Z"]
            ],
            fill: "carapaceFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Head / snout
        {
            type: "path",
            commands: [
                ["M", 48, 30],
                ["C", 51, 18, 59, 10, 68, 11],
                ["C", 76, 13, 80, 22, 77, 31],
                ["C", 74, 40, 64, 43, 55, 39],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 3.5,
            lineJoin: "round"
        },

        // Head plate
        {
            type: "path",
            commands: [
                ["M", 58, 24],
                ["C", 63, 21, 70, 23, 74, 29],
                ["L", 69, 34],
                ["C", 65, 31, 60, 31, 56, 34],
                ["Z"]
            ],
            fill: "carapaceMid",
            stroke: "darkStroke",
            strokeWidth: 2.4,
            lineJoin: "round"
        },

        // Eye
        {
            type: "circle",
            cx: 67,
            cy: 28,
            r: 2.5,
            fill: "eyeFill",
            stroke: "darkStroke",
            strokeWidth: 1.4
        },

        // Mouth / projectile sac
        {
            type: "path",
            commands: [
                ["M", 67, 38],
                ["C", 73, 37, 78, 41, 78, 46],
                ["C", 73, 49, 66, 47, 62, 43],
                ["Z"]
            ],
            fill: "glaiveFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 72,
            cy: 43,
            r: 2,
            fill: "glaiveCore",
            opacity: 0.9
        },

        // Lower body / abdomen
        {
            type: "path",
            commands: [
                ["M", 40, 58],
                ["C", 46, 63, 56, 64, 63, 59],
                ["L", 58, 70],
                ["L", 49, 75],
                ["L", 42, 70],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Body spines
        {
            type: "path",
            commands: [
                ["M", 42, 42],
                ["L", 34, 30],
                ["L", 47, 39],
                ["Z"],

                ["M", 52, 31],
                ["L", 52, 16],
                ["L", 60, 31],
                ["Z"],

                ["M", 62, 38],
                ["L", 74, 27],
                ["L", 68, 42],
                ["Z"]
            ],
            fill: "spineFill",
            stroke: "darkStroke",
            strokeWidth: 2.3,
            lineJoin: "round"
        },

        // Wing claws / tips
        {
            type: "path",
            commands: [
                ["M", 2, 22],
                ["L", -4, 17],
                ["L", -1, 31],
                ["Z"],

                ["M", 98, 22],
                ["L", 104, 17],
                ["L", 101, 31],
                ["Z"]
            ],
            fill: "clawFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },

        // Small underside talons
        {
            type: "path",
            commands: [
                ["M", 45, 70],
                ["L", 39, 80],
                ["L", 48, 74],

                ["M", 56, 70],
                ["L", 62, 80],
                ["L", 53, 74]
            ],
            fill: "clawFill",
            stroke: "darkStroke",
            strokeWidth: 2.2,
            lineCap: "round",
            lineJoin: "round"
        },

        // Glaive sacs on body
        {
            type: "circle",
            cx: 47,
            cy: 52,
            r: 3.5,
            fill: "glaiveFill",
            stroke: "darkStroke",
            strokeWidth: 1.7,
            opacity: 0.95
        },
        {
            type: "circle",
            cx: 59,
            cy: 51,
            r: 3.8,
            fill: "glaiveFill",
            stroke: "darkStroke",
            strokeWidth: 1.7,
            opacity: 0.95
        },

        // Glaive sac highlights
        {
            type: "path",
            commands: [
                ["M", 46, 50],
                ["L", 49, 49],
                ["M", 58, 49],
                ["L", 61, 48]
            ],
            stroke: "glaiveCore",
            strokeWidth: 2,
            lineCap: "round",
            opacity: 0.8
        },

        // Body seams
        {
            type: "path",
            commands: [
                ["M", 43, 47],
                ["C", 49, 50, 58, 50, 63, 47],

                ["M", 42, 59],
                ["C", 48, 63, 57, 63, 63, 59],

                ["M", 51, 32],
                ["L", 51, 57]
            ],
            stroke: "carapaceShade",
            strokeWidth: 2.3,
            lineCap: "round",
            opacity: 0.85
        }
    ]
};

const zergSwarmHost: UnitIconDefinition = {
    id: "zerg.swarm_host",
    commander: "zerg",
    aliases: ["Swarm Host", "SwarmHost", "SwarmHostLightweight", "SwarmHostMP"],
    viewBox: { width: 100, height: 100 },
    tokens: {
        skinFill: "#7E4B87",
        skinMid: "#633769",
        skinShade: "#402247",

        carapaceFill: "#AA7BBC",
        carapaceMid: "#7D5A8F",
        carapaceShade: "#4B3158",

        spawnFill: "#8EFF5A",
        spawnCore: "#D7FF75",
        spawnDark: "#3B9E43",

        locustFill: "#9A63A6",
        clawFill: "#E8D7B2",
        eyeFill: "#FF5A54",

        darkStroke: "#24122C"
    },
    layers: [
        // Rear abdomen shadow / brood sac mass
        {
            type: "path",
            commands: [
                ["M", 15, 63],
                ["C", 15, 43, 31, 30, 53, 31],
                ["C", 77, 32, 92, 47, 91, 66],
                ["C", 90, 82, 74, 91, 52, 91],
                ["C", 30, 91, 15, 81, 15, 63],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Main swollen body
        {
            type: "path",
            commands: [
                ["M", 22, 61],
                ["C", 24, 45, 37, 35, 55, 36],
                ["C", 73, 37, 85, 49, 85, 64],
                ["C", 84, 77, 70, 85, 52, 85],
                ["C", 34, 85, 22, 75, 22, 61],
                ["Z"]
            ],
            fill: "skinFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Heavy upper carapace shell
        {
            type: "path",
            commands: [
                ["M", 27, 55],
                ["C", 32, 40, 44, 32, 59, 35],
                ["C", 73, 38, 82, 48, 84, 60],
                ["C", 74, 64, 59, 66, 44, 64],
                ["C", 35, 63, 30, 60, 27, 55],
                ["Z"]
            ],
            fill: "carapaceFill",
            stroke: "darkStroke",
            strokeWidth: 3.5,
            lineJoin: "round"
        },

        // Shell segmentation
        {
            type: "path",
            commands: [
                ["M", 38, 40],
                ["C", 35, 47, 36, 57, 41, 63],

                ["M", 52, 34],
                ["C", 49, 44, 50, 56, 56, 65],

                ["M", 67, 40],
                ["C", 63, 48, 65, 57, 72, 62]
            ],
            stroke: "carapaceShade",
            strokeWidth: 3,
            lineCap: "round",
            opacity: 0.9
        },

        // Head / front cluster
        {
            type: "path",
            commands: [
                ["M", 69, 55],
                ["C", 78, 50, 90, 53, 95, 62],
                ["C", 95, 70, 87, 76, 76, 74],
                ["L", 66, 67],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3.5,
            lineJoin: "round"
        },

        // Head brow
        {
            type: "path",
            commands: [
                ["M", 75, 57],
                ["C", 82, 55, 90, 58, 93, 63],
                ["L", 86, 67],
                ["C", 82, 64, 77, 64, 73, 66],
                ["Z"]
            ],
            fill: "carapaceMid",
            stroke: "darkStroke",
            strokeWidth: 2.3,
            lineJoin: "round"
        },

        // Eye
        {
            type: "circle",
            cx: 85,
            cy: 61,
            r: 2.4,
            fill: "eyeFill",
            stroke: "darkStroke",
            strokeWidth: 1.3
        },

        // Large spawning sacs
        {
            type: "circle",
            cx: 38,
            cy: 60,
            r: 5,
            fill: "spawnFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            opacity: 0.96
        },
        {
            type: "circle",
            cx: 52,
            cy: 56,
            r: 6,
            fill: "spawnFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            opacity: 0.96
        },
        {
            type: "circle",
            cx: 67,
            cy: 61,
            r: 5,
            fill: "spawnFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            opacity: 0.96
        },
        {
            type: "circle",
            cx: 45,
            cy: 73,
            r: 4.8,
            fill: "spawnFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            opacity: 0.96
        },
        {
            type: "circle",
            cx: 60,
            cy: 73,
            r: 4.5,
            fill: "spawnFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            opacity: 0.96
        },

        // Spawn sac highlights
        {
            type: "path",
            commands: [
                ["M", 36, 58],
                ["L", 40, 57],
                ["M", 50, 53],
                ["L", 55, 52],
                ["M", 65, 59],
                ["L", 69, 58],
                ["M", 43, 71],
                ["L", 47, 70],
                ["M", 58, 71],
                ["L", 62, 70]
            ],
            stroke: "spawnCore",
            strokeWidth: 2,
            lineCap: "round",
            opacity: 0.85
        },

        // Left rear leg
        {
            type: "path",
            commands: [
                ["M", 31, 70],
                ["L", 20, 81],
                ["L", 9, 83],
                ["L", 18, 90],
                ["L", 32, 82],
                ["L", 40, 72],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Right rear leg
        {
            type: "path",
            commands: [
                ["M", 70, 70],
                ["L", 81, 81],
                ["L", 92, 83],
                ["L", 83, 90],
                ["L", 69, 82],
                ["L", 61, 72],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Left front digging leg
        {
            type: "path",
            commands: [
                ["M", 61, 73],
                ["L", 50, 86],
                ["L", 38, 91],
                ["L", 51, 95],
                ["L", 65, 84],
                ["L", 72, 74],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Right front digging leg
        {
            type: "path",
            commands: [
                ["M", 75, 70],
                ["L", 87, 78],
                ["L", 98, 79],
                ["L", 91, 87],
                ["L", 78, 83],
                ["L", 68, 74],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Claw tips
        {
            type: "path",
            commands: [
                ["M", 9, 83],
                ["L", 2, 88],
                ["L", 18, 90],
                ["Z"],

                ["M", 92, 83],
                ["L", 99, 88],
                ["L", 83, 90],
                ["Z"],

                ["M", 38, 91],
                ["L", 30, 97],
                ["L", 51, 95],
                ["Z"],

                ["M", 98, 79],
                ["L", 105, 83],
                ["L", 91, 87],
                ["Z"]
            ],
            fill: "clawFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },

        // Dorsal spawn vents / small spines
        {
            type: "path",
            commands: [
                ["M", 34, 50],
                ["L", 30, 35],
                ["L", 41, 48],
                ["Z"],

                ["M", 49, 43],
                ["L", 50, 25],
                ["L", 58, 44],
                ["Z"],

                ["M", 65, 47],
                ["L", 75, 32],
                ["L", 72, 51],
                ["Z"]
            ],
            fill: "carapaceMid",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },

        // Tiny locust silhouette emerging from back
        {
            type: "path",
            commands: [
                ["M", 47, 30],
                ["C", 48, 22, 53, 18, 59, 19],
                ["C", 64, 20, 67, 25, 65, 31],
                ["L", 59, 36],
                ["L", 51, 34],
                ["Z"]
            ],
            fill: "locustFill",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round",
            opacity: 0.95
        },
        {
            type: "path",
            commands: [
                ["M", 52, 25],
                ["L", 43, 18],
                ["L", 49, 29],

                ["M", 62, 26],
                ["L", 73, 21],
                ["L", 64, 31]
            ],
            stroke: "darkStroke",
            strokeWidth: 2.2,
            lineCap: "round",
            lineJoin: "round"
        },

        // Belly shadow
        {
            type: "path",
            commands: [
                ["M", 28, 69],
                ["C", 41, 78, 64, 79, 80, 70]
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
            cy: 65,
            r: 1.4,
            fill: "spawnDark",
            opacity: 0.9
        },
        {
            type: "circle",
            cx: 56,
            cy: 64,
            r: 1.4,
            fill: "spawnDark",
            opacity: 0.9
        },
        {
            type: "circle",
            cx: 72,
            cy: 67,
            r: 1.4,
            fill: "spawnDark",
            opacity: 0.9
        }
    ]
};

const zergUltralisk: UnitIconDefinition = {
    id: "zerg.ultralisk",
    commander: "zerg",
    aliases: ["Ultralisk", "UltraliskLightweight"],
    viewBox: { width: 100, height: 100 },
    tokens: {
        skinFill: "#7E4B87",
        skinMid: "#633769",
        skinShade: "#402247",

        carapaceFill: "#AD7FC0",
        carapaceMid: "#7C5A8A",
        carapaceShade: "#4B3158",

        tuskFill: "#E8D7B2",
        tuskShade: "#BBA47F",

        eyeFill: "#FF5A54",
        acidFill: "#8EFF5A",
        acidCore: "#D7FF75",

        darkStroke: "#24122C"
    },
    layers: [
        // Rear massive body shadow
        {
            type: "path",
            commands: [
                ["M", 13, 61],
                ["C", 16, 41, 34, 28, 56, 29],
                ["C", 76, 30, 91, 44, 93, 62],
                ["C", 95, 78, 80, 90, 57, 91],
                ["C", 34, 92, 12, 80, 13, 61],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Main huge body
        {
            type: "path",
            commands: [
                ["M", 19, 60],
                ["C", 22, 43, 37, 33, 56, 34],
                ["C", 74, 35, 86, 47, 88, 62],
                ["C", 89, 75, 76, 84, 56, 85],
                ["C", 36, 86, 19, 75, 19, 60],
                ["Z"]
            ],
            fill: "skinFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Heavy top carapace shell
        {
            type: "path",
            commands: [
                ["M", 25, 55],
                ["C", 31, 39, 44, 31, 59, 34],
                ["C", 74, 37, 84, 48, 86, 60],
                ["C", 75, 65, 60, 67, 44, 64],
                ["C", 35, 63, 29, 60, 25, 55],
                ["Z"]
            ],
            fill: "carapaceFill",
            stroke: "darkStroke",
            strokeWidth: 3.5,
            lineJoin: "round"
        },

        // Carapace plates / segmentation
        {
            type: "path",
            commands: [
                ["M", 36, 41],
                ["C", 33, 48, 35, 57, 41, 63],

                ["M", 51, 34],
                ["C", 47, 44, 49, 57, 56, 65],

                ["M", 67, 40],
                ["C", 63, 48, 65, 57, 73, 62],

                ["M", 79, 50],
                ["C", 76, 55, 78, 59, 83, 61]
            ],
            stroke: "carapaceShade",
            strokeWidth: 3,
            lineCap: "round",
            opacity: 0.9
        },

        // Head / front skull mass
        {
            type: "path",
            commands: [
                ["M", 69, 51],
                ["C", 79, 45, 92, 49, 98, 60],
                ["C", 99, 70, 90, 77, 77, 75],
                ["L", 65, 68],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Head armor brow
        {
            type: "path",
            commands: [
                ["M", 74, 53],
                ["C", 82, 50, 91, 54, 95, 61],
                ["L", 88, 66],
                ["C", 83, 62, 78, 62, 73, 65],
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
            cx: 86,
            cy: 58,
            r: 2.6,
            fill: "eyeFill",
            stroke: "darkStroke",
            strokeWidth: 1.4
        },

        // Left giant tusk blade
        {
            type: "path",
            commands: [
                ["M", 74, 66],
                ["C", 62, 69, 45, 78, 31, 92],
                ["C", 51, 89, 68, 82, 82, 71],
                ["Z"]
            ],
            fill: "tuskFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Right giant tusk blade
        {
            type: "path",
            commands: [
                ["M", 84, 66],
                ["C", 93, 69, 103, 76, 109, 89],
                ["C", 96, 87, 85, 80, 78, 71],
                ["Z"]
            ],
            fill: "tuskFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Tusk shade lines
        {
            type: "path",
            commands: [
                ["M", 50, 85],
                ["C", 61, 80, 71, 75, 80, 69],
                ["M", 93, 82],
                ["C", 88, 77, 84, 72, 80, 68]
            ],
            stroke: "tuskShade",
            strokeWidth: 2,
            lineCap: "round",
            opacity: 0.8
        },

        // Front left massive leg
        {
            type: "path",
            commands: [
                ["M", 63, 74],
                ["L", 52, 88],
                ["L", 39, 93],
                ["L", 53, 97],
                ["L", 68, 86],
                ["L", 74, 76],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 3.5,
            lineJoin: "round"
        },

        // Front right massive leg
        {
            type: "path",
            commands: [
                ["M", 76, 71],
                ["L", 89, 80],
                ["L", 100, 80],
                ["L", 94, 89],
                ["L", 80, 85],
                ["L", 68, 75],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 3.5,
            lineJoin: "round"
        },

        // Rear left leg
        {
            type: "path",
            commands: [
                ["M", 32, 72],
                ["L", 21, 84],
                ["L", 9, 86],
                ["L", 19, 93],
                ["L", 34, 84],
                ["L", 42, 74],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3.5,
            lineJoin: "round"
        },

        // Rear right support leg
        {
            type: "path",
            commands: [
                ["M", 48, 77],
                ["L", 42, 91],
                ["L", 30, 96],
                ["L", 45, 98],
                ["L", 56, 87],
                ["L", 58, 78],
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
                ["M", 39, 93],
                ["L", 31, 99],
                ["L", 53, 97],
                ["Z"],

                ["M", 100, 80],
                ["L", 107, 85],
                ["L", 94, 89],
                ["Z"],

                ["M", 9, 86],
                ["L", 2, 91],
                ["L", 19, 93],
                ["Z"],

                ["M", 30, 96],
                ["L", 22, 101],
                ["L", 45, 98],
                ["Z"]
            ],
            fill: "tuskFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },

        // Dorsal armor spikes
        {
            type: "path",
            commands: [
                ["M", 31, 50],
                ["L", 26, 31],
                ["L", 40, 48],
                ["Z"],

                ["M", 45, 39],
                ["L", 47, 17],
                ["L", 55, 40],
                ["Z"],

                ["M", 62, 39],
                ["L", 72, 20],
                ["L", 70, 44],
                ["Z"],

                ["M", 76, 48],
                ["L", 90, 34],
                ["L", 82, 54],
                ["Z"]
            ],
            fill: "carapaceMid",
            stroke: "darkStroke",
            strokeWidth: 2.7,
            lineJoin: "round"
        },

        // Small acid/bio sacs for Zerg color identity
        {
            type: "circle",
            cx: 47,
            cy: 58,
            r: 3.8,
            fill: "acidFill",
            stroke: "darkStroke",
            strokeWidth: 1.8,
            opacity: 0.95
        },
        {
            type: "circle",
            cx: 63,
            cy: 56,
            r: 4.2,
            fill: "acidFill",
            stroke: "darkStroke",
            strokeWidth: 1.8,
            opacity: 0.95
        },

        // Acid highlights
        {
            type: "path",
            commands: [
                ["M", 46, 56],
                ["L", 49, 55],
                ["M", 62, 54],
                ["L", 65, 53]
            ],
            stroke: "acidCore",
            strokeWidth: 2,
            lineCap: "round",
            opacity: 0.8
        },

        // Lower body shadow curve
        {
            type: "path",
            commands: [
                ["M", 28, 68],
                ["C", 43, 77, 67, 78, 82, 68]
            ],
            stroke: "skinShade",
            strokeWidth: 4,
            lineCap: "round",
            opacity: 0.65
        }
    ]
};

const zergOverseer: UnitIconDefinition = {
    id: "zerg.overseer",
    commander: "zerg",
    aliases: ["Overseer"],
    viewBox: { width: 100, height: 100 },
    tokens: {
        skinFill: "#87518F",
        skinMid: "#6B3A73",
        skinShade: "#43234A",
        carapaceFill: "#B487C8",
        carapaceShade: "#5A3A66",
        eyeFill: "#FF5A54",
        bioGlow: "#8EFF5A",
        bioCore: "#D7FF75",
        clawFill: "#E8D7B2",
        darkStroke: "#24122C"
    },
    layers: [
        {
            type: "path",
            commands: [
                ["M", 50, 15],
                ["C", 69, 17, 83, 31, 85, 50],
                ["C", 83, 69, 68, 83, 50, 86],
                ["C", 32, 83, 17, 69, 15, 50],
                ["C", 17, 31, 31, 17, 50, 15],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 26, 48],
                ["C", 29, 34, 39, 26, 50, 26],
                ["C", 61, 26, 71, 34, 74, 48],
                ["C", 68, 56, 59, 60, 50, 60],
                ["C", 41, 60, 32, 56, 26, 48],
                ["Z"]
            ],
            fill: "carapaceFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 50,
            cy: 52,
            r: 15,
            fill: "bioGlow",
            stroke: "darkStroke",
            strokeWidth: 3,
            opacity: 0.9
        },
        {
            type: "circle",
            cx: 50,
            cy: 52,
            r: 7,
            fill: "bioCore",
            stroke: "darkStroke",
            strokeWidth: 1.5
        },
        {
            type: "path",
            commands: [
                ["M", 23, 49],
                ["L", 8, 37],
                ["L", 15, 55],
                ["M", 77, 49],
                ["L", 92, 37],
                ["L", 85, 55],
                ["M", 32, 70],
                ["L", 20, 87],
                ["M", 68, 70],
                ["L", 80, 87]
            ],
            stroke: "skinMid",
            strokeWidth: 5,
            lineCap: "round",
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 36, 26],
                ["L", 31, 10],
                ["L", 45, 24],
                ["M", 64, 26],
                ["L", 69, 10],
                ["L", 55, 24]
            ],
            fill: "carapaceShade",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 43,
            cy: 45,
            r: 2.6,
            fill: "eyeFill",
            stroke: "darkStroke",
            strokeWidth: 1
        },
        {
            type: "circle",
            cx: 57,
            cy: 45,
            r: 2.6,
            fill: "eyeFill",
            stroke: "darkStroke",
            strokeWidth: 1
        }
    ]
};

const zergRavager: UnitIconDefinition = {
    id: "zerg.ravager",
    commander: "zerg",
    aliases: ["Ravager"],
    viewBox: { width: 100, height: 100 },
    tokens: {
        skinFill: "#7B4B83",
        skinMid: "#633769",
        skinShade: "#43234A",
        carapaceFill: "#AA7BBC",
        carapaceShade: "#4E355B",
        bileFill: "#8EFF5A",
        bileCore: "#D7FF75",
        clawFill: "#E8D7B2",
        eyeFill: "#FF5A54",
        darkStroke: "#24122C"
    },
    layers: [
        {
            type: "path",
            commands: [
                ["M", 18, 61],
                ["C", 21, 43, 37, 32, 58, 34],
                ["C", 76, 36, 90, 48, 92, 63],
                ["C", 91, 78, 76, 88, 55, 88],
                ["C", 34, 88, 18, 77, 18, 61],
                ["Z"]
            ],
            fill: "skinFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 27, 55],
                ["C", 33, 39, 45, 31, 60, 35],
                ["C", 75, 39, 86, 50, 88, 62],
                ["C", 77, 66, 61, 67, 44, 64],
                ["C", 35, 62, 30, 59, 27, 55],
                ["Z"]
            ],
            fill: "carapaceFill",
            stroke: "darkStroke",
            strokeWidth: 3.5,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 40, 42],
                ["L", 45, 21],
                ["L", 52, 43],
                ["M", 57, 39],
                ["L", 68, 20],
                ["L", 66, 45],
                ["M", 72, 47],
                ["L", 88, 34],
                ["L", 80, 54]
            ],
            fill: "carapaceShade",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 52,
            cy: 53,
            r: 6,
            fill: "bileFill",
            stroke: "darkStroke",
            strokeWidth: 2
        },
        {
            type: "circle",
            cx: 68,
            cy: 56,
            r: 5,
            fill: "bileFill",
            stroke: "darkStroke",
            strokeWidth: 2
        },
        {
            type: "path",
            commands: [
                ["M", 70, 58],
                ["C", 79, 52, 91, 55, 96, 64],
                ["C", 94, 72, 84, 77, 73, 73],
                ["L", 64, 66],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 84,
            cy: 62,
            r: 2.5,
            fill: "eyeFill",
            stroke: "darkStroke",
            strokeWidth: 1
        },
        {
            type: "path",
            commands: [
                ["M", 32, 70],
                ["L", 20, 83],
                ["L", 8, 85],
                ["L", 18, 91],
                ["L", 34, 82],
                ["Z"],
                ["M", 68, 72],
                ["L", 81, 82],
                ["L", 94, 82],
                ["L", 85, 90],
                ["L", 70, 83],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        }
    ]
};

const zergBaneling: UnitIconDefinition = {
    id: "zerg.baneling",
    commander: "zerg",
    aliases: ["Baneling"],
    viewBox: { width: 100, height: 100 },
    tokens: {
        skinFill: "#7B4B83",
        skinShade: "#43234A",
        carapaceFill: "#A77CB7",
        acidFill: "#8EFF5A",
        acidCore: "#D7FF75",
        eyeFill: "#FF5A54",
        darkStroke: "#24122C"
    },
    layers: [
        {
            type: "circle",
            cx: 50,
            cy: 57,
            r: 31,
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 4
        },
        {
            type: "circle",
            cx: 50,
            cy: 54,
            r: 25,
            fill: "acidFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            opacity: 0.92
        },
        {
            type: "path",
            commands: [
                ["M", 28, 44],
                ["C", 36, 29, 56, 24, 72, 35],
                ["C", 65, 41, 56, 44, 45, 44],
                ["C", 38, 44, 32, 44, 28, 44],
                ["Z"]
            ],
            fill: "carapaceFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 45,
            cy: 51,
            r: 7,
            fill: "acidCore",
            opacity: 0.9
        },
        {
            type: "circle",
            cx: 60,
            cy: 61,
            r: 6,
            fill: "acidCore",
            opacity: 0.75
        },
        {
            type: "path",
            commands: [
                ["M", 28, 70],
                ["L", 14, 82],
                ["L", 24, 86],
                ["L", 37, 75],
                ["M", 72, 70],
                ["L", 86, 82],
                ["L", 76, 86],
                ["L", 63, 75]
            ],
            stroke: "skinFill",
            strokeWidth: 5,
            lineCap: "round",
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 66,
            cy: 42,
            r: 2.6,
            fill: "eyeFill",
            stroke: "darkStroke",
            strokeWidth: 1
        }
    ]
};

const zergViper: UnitIconDefinition = {
    id: "zerg.viper",
    commander: "zerg",
    aliases: ["Viper"],
    viewBox: { width: 100, height: 100 },
    tokens: {
        skinFill: "#86508E",
        skinMid: "#6B3A73",
        skinShade: "#43234A",
        carapaceFill: "#B487C8",
        carapaceShade: "#5A3A66",
        acidFill: "#8EFF5A",
        acidCore: "#D7FF75",
        eyeFill: "#FF5A54",
        darkStroke: "#24122C"
    },
    layers: [
        {
            type: "path",
            commands: [
                ["M", 50, 13],
                ["C", 62, 25, 70, 42, 69, 61],
                ["C", 64, 77, 56, 88, 50, 94],
                ["C", 44, 88, 36, 77, 31, 61],
                ["C", 30, 42, 38, 25, 50, 13],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 39, 30],
                ["C", 43, 21, 47, 16, 50, 13],
                ["C", 53, 16, 57, 21, 61, 30],
                ["L", 58, 56],
                ["L", 50, 68],
                ["L", 42, 56],
                ["Z"]
            ],
            fill: "carapaceFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 37, 45],
                ["C", 24, 38, 13, 34, 4, 36],
                ["C", 13, 49, 25, 58, 39, 60],
                ["Z"],
                ["M", 63, 45],
                ["C", 76, 38, 87, 34, 96, 36],
                ["C", 87, 49, 75, 58, 61, 60],
                ["Z"]
            ],
            fill: "skinFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 35, 62],
                ["L", 22, 75],
                ["L", 27, 83],
                ["L", 44, 69],
                ["M", 65, 62],
                ["L", 78, 75],
                ["L", 73, 83],
                ["L", 56, 69]
            ],
            stroke: "skinMid",
            strokeWidth: 5,
            lineCap: "round",
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 44,
            cy: 35,
            r: 2.5,
            fill: "eyeFill",
            stroke: "darkStroke",
            strokeWidth: 1
        },
        {
            type: "circle",
            cx: 56,
            cy: 35,
            r: 2.5,
            fill: "eyeFill",
            stroke: "darkStroke",
            strokeWidth: 1
        },
        {
            type: "path",
            commands: [
                ["M", 46, 56],
                ["C", 48, 64, 52, 64, 54, 56],
                ["M", 40, 72],
                ["C", 46, 80, 54, 80, 60, 72]
            ],
            stroke: "acidFill",
            strokeWidth: 3,
            lineCap: "round",
            opacity: 0.9
        }
    ]
};

const zergBroodLord: UnitIconDefinition = {
    id: "zerg.broodLord",
    commander: "zerg",
    aliases: ["Brood Lord", "BroodLord"],
    viewBox: { width: 100, height: 100 },
    tokens: {
        skinFill: "#80508A",
        skinMid: "#65376E",
        skinShade: "#402247",
        carapaceFill: "#AD7FC0",
        carapaceShade: "#4B3158",
        broodFill: "#8EFF5A",
        broodCore: "#D7FF75",
        clawFill: "#E8D7B2",
        eyeFill: "#FF5A54",
        darkStroke: "#24122C"
    },
    layers: [
        {
            type: "path",
            commands: [
                ["M", 50, 16],
                ["C", 68, 19, 83, 35, 85, 55],
                ["C", 82, 76, 67, 89, 50, 91],
                ["C", 33, 89, 18, 76, 15, 55],
                ["C", 17, 35, 32, 19, 50, 16],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 25, 51],
                ["C", 30, 34, 41, 25, 50, 25],
                ["C", 59, 25, 70, 34, 75, 51],
                ["C", 66, 58, 56, 61, 50, 61],
                ["C", 44, 61, 34, 58, 25, 51],
                ["Z"]
            ],
            fill: "carapaceFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 31, 48],
                ["C", 18, 42, 8, 38, 1, 42],
                ["C", 10, 55, 23, 65, 38, 66],
                ["Z"],
                ["M", 69, 48],
                ["C", 82, 42, 92, 38, 99, 42],
                ["C", 90, 55, 77, 65, 62, 66],
                ["Z"]
            ],
            fill: "skinFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 42,
            cy: 63,
            r: 6,
            fill: "broodFill",
            stroke: "darkStroke",
            strokeWidth: 2
        },
        {
            type: "circle",
            cx: 58,
            cy: 63,
            r: 6,
            fill: "broodFill",
            stroke: "darkStroke",
            strokeWidth: 2
        },
        {
            type: "path",
            commands: [
                ["M", 36, 76],
                ["L", 20, 90],
                ["L", 32, 93],
                ["L", 47, 79],
                ["M", 64, 76],
                ["L", 80, 90],
                ["L", 68, 93],
                ["L", 53, 79]
            ],
            stroke: "skinMid",
            strokeWidth: 5,
            lineCap: "round",
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 39, 33],
                ["L", 34, 15],
                ["L", 47, 31],
                ["M", 61, 33],
                ["L", 66, 15],
                ["L", 53, 31]
            ],
            fill: "carapaceShade",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 50,
            cy: 48,
            r: 3,
            fill: "eyeFill",
            stroke: "darkStroke",
            strokeWidth: 1
        }
    ]
};

const zergLocust: UnitIconDefinition = {
    id: "zerg.locust",
    commander: "zerg",
    aliases: ["Locust", "LocustMPPrecursor"],
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
        {
            type: "path",
            commands: [
                ["M", 25, 63],
                ["C", 29, 49, 43, 39, 59, 40],
                ["C", 74, 41, 86, 50, 88, 62],
                ["C", 86, 75, 72, 82, 54, 82],
                ["C", 38, 82, 27, 74, 25, 63],
                ["Z"]
            ],
            fill: "skinFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 33, 56],
                ["C", 41, 43, 55, 38, 70, 44],
                ["C", 76, 47, 82, 54, 85, 61],
                ["C", 72, 61, 58, 59, 43, 64],
                ["Z"]
            ],
            fill: "carapaceFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 67, 53],
                ["C", 76, 48, 88, 51, 94, 59],
                ["C", 93, 66, 84, 70, 73, 68],
                ["L", 63, 62],
                ["Z"]
            ],
            fill: "skinShade",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "circle",
            cx: 84,
            cy: 58,
            r: 2.5,
            fill: "eyeFill",
            stroke: "darkStroke",
            strokeWidth: 1
        },
        {
            type: "path",
            commands: [
                ["M", 35, 66],
                ["L", 22, 80],
                ["L", 12, 82],
                ["L", 20, 89],
                ["L", 36, 78],
                ["Z"],
                ["M", 62, 68],
                ["L", 76, 81],
                ["L", 88, 82],
                ["L", 80, 90],
                ["L", 64, 79],
                ["Z"]
            ],
            fill: "skinMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 39, 51],
                ["L", 36, 33],
                ["L", 48, 49],
                ["M", 56, 44],
                ["L", 60, 25],
                ["L", 65, 47],
                ["M", 72, 49],
                ["L", 84, 36],
                ["L", 78, 56]
            ],
            fill: "carapaceShade",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 12, 82],
                ["L", 5, 87],
                ["L", 20, 89],
                ["Z"],
                ["M", 88, 82],
                ["L", 96, 87],
                ["L", 80, 90],
                ["Z"]
            ],
            fill: "clawFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
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
    lurker: zergLurker,
    mutalisk: zergMutalisk,
    swarmhost: zergSwarmHost,
    ultralisk: zergUltralisk,
    overseer: zergOverseer,
    ravager: zergRavager,
    baneling: zergBaneling,
    viper: zergViper,
    broodLord: zergBroodLord,
    locust: zergLocust
};
