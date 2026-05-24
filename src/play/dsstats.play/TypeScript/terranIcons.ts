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

const terranReaper: UnitIconDefinition = {
    id: "terran.reaper",
    commander: "terran",
    aliases: ["Reaper", "ReaperLightweight", "JetpackInfantry"],
    viewBox: { width: 100, height: 100 },
    tokens: {
        armorFill: "#D9E6EC",
        armorMid: "#93A9B5",
        armorShade: "#536C7A",
        armorDark: "#263E4C",

        visorFill: "#F2C84B",
        visorShade: "#C17A28",

        jetFill: "#405B69",
        jetGlow: "#70D9FF",
        flameFill: "#FF9B35",
        flameCore: "#FFE07A",

        pistolFill: "#253743",
        darkStroke: "#102634",
        redLight: "#FF5A4F"
    },
    layers: [
        // Left jetpack pod
        {
            type: "path",
            commands: [
                ["M", 29, 35],
                ["L", 20, 47],
                ["L", 24, 65],
                ["L", 34, 59],
                ["L", 37, 42],
                ["Z"]
            ],
            fill: "jetFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Right jetpack pod
        {
            type: "path",
            commands: [
                ["M", 71, 35],
                ["L", 80, 47],
                ["L", 76, 65],
                ["L", 66, 59],
                ["L", 63, 42],
                ["Z"]
            ],
            fill: "jetFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Jetpack top bridge
        {
            type: "path",
            commands: [
                ["M", 36, 38],
                ["C", 42, 33, 58, 33, 64, 38],
                ["L", 62, 50],
                ["L", 38, 50],
                ["Z"]
            ],
            fill: "armorDark",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Left thruster flame
        {
            type: "path",
            commands: [
                ["M", 25, 63],
                ["C", 20, 70, 22, 77, 27, 84],
                ["C", 29, 76, 34, 72, 32, 64],
                ["Z"]
            ],
            fill: "flameFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round",
            opacity: 0.9
        },
        {
            type: "path",
            commands: [
                ["M", 28, 66],
                ["C", 25, 71, 26, 76, 29, 80],
                ["C", 30, 74, 33, 71, 31, 66],
                ["Z"]
            ],
            fill: "flameCore",
            opacity: 0.85
        },

        // Right thruster flame
        {
            type: "path",
            commands: [
                ["M", 75, 63],
                ["C", 80, 70, 78, 77, 73, 84],
                ["C", 71, 76, 66, 72, 68, 64],
                ["Z"]
            ],
            fill: "flameFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round",
            opacity: 0.9
        },
        {
            type: "path",
            commands: [
                ["M", 72, 66],
                ["C", 75, 71, 74, 76, 71, 80],
                ["C", 70, 74, 67, 71, 69, 66],
                ["Z"]
            ],
            fill: "flameCore",
            opacity: 0.85
        },

        // Legs, spread/agile pose
        {
            type: "path",
            commands: [
                ["M", 42, 67],
                ["L", 35, 83],
                ["L", 27, 85],
                ["L", 34, 93],
                ["L", 44, 88],
                ["L", 49, 70],
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
                ["M", 58, 67],
                ["L", 65, 83],
                ["L", 73, 85],
                ["L", 66, 93],
                ["L", 56, 88],
                ["L", 51, 70],
                ["Z"]
            ],
            fill: "armorShade",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Torso
        {
            type: "path",
            commands: [
                ["M", 36, 47],
                ["C", 39, 40, 44, 37, 50, 37],
                ["C", 56, 37, 61, 40, 64, 47],
                ["L", 61, 68],
                ["L", 50, 74],
                ["L", 39, 68],
                ["Z"]
            ],
            fill: "armorFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Chest plate
        {
            type: "path",
            commands: [
                ["M", 41, 51],
                ["L", 59, 51],
                ["L", 57, 64],
                ["L", 50, 68],
                ["L", 43, 64],
                ["Z"]
            ],
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },

        // Head / helmet
        {
            type: "path",
            commands: [
                ["M", 36, 34],
                ["C", 37, 23, 43, 17, 50, 17],
                ["C", 57, 17, 63, 23, 64, 34],
                ["L", 60, 45],
                ["L", 40, 45],
                ["Z"]
            ],
            fill: "armorFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Helmet side caps
        {
            type: "path",
            commands: [
                ["M", 38, 31],
                ["L", 31, 36],
                ["L", 34, 45],
                ["L", 41, 42],
                ["Z"]
            ],
            fill: "armorShade",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },
        {
            type: "path",
            commands: [
                ["M", 62, 31],
                ["L", 69, 36],
                ["L", 66, 45],
                ["L", 59, 42],
                ["Z"]
            ],
            fill: "armorShade",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },

        // Visor
        {
            type: "path",
            commands: [
                ["M", 39, 33],
                ["C", 43, 29, 47, 27, 50, 27],
                ["C", 53, 27, 57, 29, 61, 33],
                ["L", 58, 39],
                ["L", 42, 39],
                ["Z"]
            ],
            fill: "visorFill",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },

        // Visor lower shadow
        {
            type: "path",
            commands: [
                ["M", 43, 37],
                ["C", 47, 35, 53, 35, 57, 37]
            ],
            stroke: "visorShade",
            strokeWidth: 2.5,
            lineCap: "round",
            opacity: 0.7
        },

        // Left arm
        {
            type: "path",
            commands: [
                ["M", 38, 51],
                ["L", 24, 55],
                ["L", 15, 50],
                ["L", 12, 57],
                ["L", 24, 65],
                ["L", 42, 61],
                ["Z"]
            ],
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Right arm
        {
            type: "path",
            commands: [
                ["M", 62, 51],
                ["L", 76, 55],
                ["L", 85, 50],
                ["L", 88, 57],
                ["L", 76, 65],
                ["L", 58, 61],
                ["Z"]
            ],
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Left pistol
        {
            type: "path",
            commands: [
                ["M", 13, 48],
                ["L", 5, 47],
                ["L", 4, 52],
                ["L", 13, 54],
                ["L", 17, 61],
                ["L", 21, 58],
                ["L", 18, 51],
                ["Z"]
            ],
            fill: "pistolFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },

        // Right pistol
        {
            type: "path",
            commands: [
                ["M", 87, 48],
                ["L", 95, 47],
                ["L", 96, 52],
                ["L", 87, 54],
                ["L", 83, 61],
                ["L", 79, 58],
                ["L", 82, 51],
                ["Z"]
            ],
            fill: "pistolFill",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },

        // Small armor lights
        {
            type: "circle",
            cx: 45,
            cy: 56,
            r: 2,
            fill: "redLight",
            stroke: "darkStroke",
            strokeWidth: 1
        },
        {
            type: "circle",
            cx: 55,
            cy: 56,
            r: 2,
            fill: "jetGlow",
            stroke: "darkStroke",
            strokeWidth: 1
        },

        // Motion-readable boot soles
        {
            type: "path",
            commands: [
                ["M", 27, 85],
                ["L", 20, 91],
                ["L", 34, 93],
                ["Z"],
                ["M", 73, 85],
                ["L", 80, 91],
                ["L", 66, 93],
                ["Z"]
            ],
            fill: "armorDark",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        }
    ]
};

const terranMarauder: UnitIconDefinition = {
    id: "terran.marauder",
    commander: "terran",
    aliases: ["Marauder", "MarauderLightweight", "HeavyInfantry"],
    viewBox: { width: 100, height: 100 },
    tokens: {
        armorFill: "#D7E3EA",
        armorMid: "#9CAFB9",
        armorShade: "#5E7480",
        armorDark: "#263E4A",

        visorFill: "#F2C84B",
        visorShade: "#B97828",

        launcherFill: "#324955",
        launcherDark: "#1C303A",
        muzzleFill: "#101F27",

        shellLight: "#FFB64D",
        redLight: "#FF5A4F",
        blueLight: "#76D6FF",

        darkStroke: "#102634"
    },
    layers: [
        // Back silhouette / heavy backpack
        {
            type: "path",
            commands: [
                ["M", 27, 39],
                ["C", 32, 28, 42, 23, 50, 23],
                ["C", 58, 23, 68, 28, 73, 39],
                ["L", 72, 66],
                ["L", 62, 74],
                ["L", 38, 74],
                ["L", 28, 66],
                ["Z"]
            ],
            fill: "armorDark",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Massive left shoulder
        {
            type: "path",
            commands: [
                ["M", 28, 42],
                ["C", 19, 43, 12, 50, 10, 61],
                ["C", 9, 70, 16, 77, 27, 77],
                ["L", 39, 68],
                ["L", 38, 50],
                ["Z"]
            ],
            fill: "armorShade",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Massive right shoulder
        {
            type: "path",
            commands: [
                ["M", 72, 42],
                ["C", 81, 43, 88, 50, 90, 61],
                ["C", 91, 70, 84, 77, 73, 77],
                ["L", 61, 68],
                ["L", 62, 50],
                ["Z"]
            ],
            fill: "armorShade",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Left shoulder front plate
        {
            type: "path",
            commands: [
                ["M", 18, 58],
                ["C", 21, 51, 27, 48, 34, 49],
                ["L", 37, 63],
                ["L", 29, 70],
                ["C", 22, 70, 18, 66, 18, 58],
                ["Z"]
            ],
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },

        // Right shoulder front plate
        {
            type: "path",
            commands: [
                ["M", 82, 58],
                ["C", 79, 51, 73, 48, 66, 49],
                ["L", 63, 63],
                ["L", 71, 70],
                ["C", 78, 70, 82, 66, 82, 58],
                ["Z"]
            ],
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },

        // Helmet
        {
            type: "path",
            commands: [
                ["M", 35, 43],
                ["C", 36, 31, 43, 25, 50, 25],
                ["C", 57, 25, 64, 31, 65, 43],
                ["L", 61, 54],
                ["L", 39, 54],
                ["Z"]
            ],
            fill: "armorFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Helmet brow
        {
            type: "path",
            commands: [
                ["M", 37, 39],
                ["C", 41, 35, 46, 33, 50, 33],
                ["C", 54, 33, 59, 35, 63, 39],
                ["L", 60, 45],
                ["L", 40, 45],
                ["Z"]
            ],
            fill: "armorDark",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },

        // Visor
        {
            type: "path",
            commands: [
                ["M", 39, 43],
                ["L", 61, 43],
                ["L", 58, 49],
                ["L", 42, 49],
                ["Z"]
            ],
            fill: "visorFill",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },

        // Visor shadow
        {
            type: "path",
            commands: [
                ["M", 43, 47],
                ["L", 57, 47]
            ],
            stroke: "visorShade",
            strokeWidth: 2,
            lineCap: "round",
            opacity: 0.7
        },

        // Thick torso
        {
            type: "path",
            commands: [
                ["M", 35, 53],
                ["L", 65, 53],
                ["L", 70, 75],
                ["L", 59, 87],
                ["L", 41, 87],
                ["L", 30, 75],
                ["Z"]
            ],
            fill: "armorFill",
            stroke: "darkStroke",
            strokeWidth: 4,
            lineJoin: "round"
        },

        // Central chest armor
        {
            type: "path",
            commands: [
                ["M", 40, 58],
                ["L", 60, 58],
                ["L", 62, 72],
                ["L", 55, 81],
                ["L", 45, 81],
                ["L", 38, 72],
                ["Z"]
            ],
            fill: "armorMid",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },

        // Abdomen dark plate
        {
            type: "path",
            commands: [
                ["M", 43, 70],
                ["L", 57, 70],
                ["L", 55, 80],
                ["L", 45, 80],
                ["Z"]
            ],
            fill: "armorDark",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },

        // Left arm cannon base
        {
            type: "path",
            commands: [
                ["M", 27, 66],
                ["L", 15, 70],
                ["L", 10, 83],
                ["L", 20, 88],
                ["L", 32, 77],
                ["Z"]
            ],
            fill: "launcherFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Right arm cannon base
        {
            type: "path",
            commands: [
                ["M", 73, 66],
                ["L", 85, 70],
                ["L", 90, 83],
                ["L", 80, 88],
                ["L", 68, 77],
                ["Z"]
            ],
            fill: "launcherFill",
            stroke: "darkStroke",
            strokeWidth: 3,
            lineJoin: "round"
        },

        // Left grenade launcher barrel
        {
            type: "path",
            commands: [
                ["M", 11, 76],
                ["L", 3, 78],
                ["L", 5, 87],
                ["L", 18, 85],
                ["L", 20, 78],
                ["Z"]
            ],
            fill: "launcherDark",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },

        // Right grenade launcher barrel
        {
            type: "path",
            commands: [
                ["M", 89, 76],
                ["L", 97, 78],
                ["L", 95, 87],
                ["L", 82, 85],
                ["L", 80, 78],
                ["Z"]
            ],
            fill: "launcherDark",
            stroke: "darkStroke",
            strokeWidth: 2,
            lineJoin: "round"
        },

        // Launcher muzzles
        {
            type: "circle",
            cx: 9,
            cy: 82,
            r: 4,
            fill: "muzzleFill",
            stroke: "darkStroke",
            strokeWidth: 2
        },
        {
            type: "circle",
            cx: 91,
            cy: 82,
            r: 4,
            fill: "muzzleFill",
            stroke: "darkStroke",
            strokeWidth: 2
        },

        // Heavy boots
        {
            type: "path",
            commands: [
                ["M", 40, 84],
                ["L", 33, 93],
                ["L", 47, 94],
                ["L", 50, 86],
                ["Z"],
                ["M", 60, 84],
                ["L", 67, 93],
                ["L", 53, 94],
                ["L", 50, 86],
                ["Z"]
            ],
            fill: "armorDark",
            stroke: "darkStroke",
            strokeWidth: 2.5,
            lineJoin: "round"
        },

        // Grenade/shell accents
        {
            type: "circle",
            cx: 28,
            cy: 54,
            r: 2.5,
            fill: "shellLight",
            stroke: "darkStroke",
            strokeWidth: 1
        },
        {
            type: "circle",
            cx: 72,
            cy: 54,
            r: 2.5,
            fill: "shellLight",
            stroke: "darkStroke",
            strokeWidth: 1
        },

        // Armor lights
        {
            type: "circle",
            cx: 45,
            cy: 63,
            r: 2,
            fill: "redLight",
            stroke: "darkStroke",
            strokeWidth: 1
        },
        {
            type: "circle",
            cx: 55,
            cy: 63,
            r: 2,
            fill: "blueLight",
            stroke: "darkStroke",
            strokeWidth: 1
        },

        // Panel seams
        {
            type: "path",
            commands: [
                ["M", 36, 65],
                ["L", 44, 69],
                ["M", 64, 65],
                ["L", 56, 69],
                ["M", 50, 58],
                ["L", 50, 68]
            ],
            stroke: "darkStroke",
            strokeWidth: 2,
            lineCap: "round",
            opacity: 0.8
        }
    ]
};


export const terranUnits = {
    marine: terranMarine,
    reaper: terranReaper,
    marauder: terranMarauder,
};
