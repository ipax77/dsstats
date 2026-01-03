import { PlayerDto, ReplayListDto } from "../dtos";

export interface MyPlayerStats {
    player: PlayerDto;
    gameModeStats: GameModeStats[];
    recentReplays: ReplayListDto[];
}

export interface GameModeStats {
    gameMode: number;
    commanderStats: CommanderStats[];
    teammateStats: PlayerStats[];
    opponentStats: PlayerStats[];
}

export interface CommanderStats {
    commander: number;
    count: number;
    wins: number;
    mvp: number;
}

export interface PlayerStats {
    player: PlayerDto;
    count: number;
    wins: number;
}
