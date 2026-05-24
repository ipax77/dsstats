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

export const zergUnits = {
    zergling: zergZergling
};
