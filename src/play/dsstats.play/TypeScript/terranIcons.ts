import type { UnitIconDefinition } from "./types";


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


export const terranUnits = {
    marine: terranMarine
};
