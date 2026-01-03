import { getFilteredReplayLists } from '../dsstatsDb';
import { openDB, STORES } from "../db-core";
import { PlayerDto, ReplayDto, ReplayPlayerDto, ToonIdDto } from "../dtos";
import { CommanderStats, GameModeStats, MyPlayerStats, PlayerStats } from "./stats-dto";

function toonKey(toon: ToonIdDto): string {
    return `${toon.region}:${toon.realm}:${toon.id}`;
}

type GameModeStatsBuilder = {
    commanderStats: Map<number, CommanderStats>,
    teammateStats: Map<string, PlayerStats>,   // keyed by toonKey
    opponentStats: Map<string, PlayerStats>,   // keyed by toonKey
};

export class StatsService {
    
    public async generateStats(player: PlayerDto): Promise<MyPlayerStats> {
        const gameModeMap = new Map<number, GameModeStatsBuilder>();
        const take = 100;
        let afterKey: IDBValidKey | null = null;

        while (true) {
            const { replays, lastKey } = await this.getReplayChunk(afterKey, take);
            if (replays.length === 0) {
                break;
            }
            for (const replay of replays) {
                if (!gameModeMap.has(replay.gameMode)) {
                    gameModeMap.set(replay.gameMode, {
                        commanderStats: new Map<number, CommanderStats>(),
                        teammateStats: new Map<string, PlayerStats>(),
                        opponentStats: new Map<string, PlayerStats>(),
                    });
                }
                const gameModeStats = gameModeMap.get(replay.gameMode)!;
                this.setStats(player, gameModeStats, replay);
            }
            afterKey = lastKey;
        }

        const stats: GameModeStats[] = [];
        for (const [gameMode, builder] of gameModeMap.entries()) {
            stats.push({
                gameMode: gameMode,
                commanderStats: Array.from(builder.commanderStats.values()),
                teammateStats: Array.from(builder.teammateStats.values()).filter(s => s.count > 5),
                opponentStats: Array.from(builder.opponentStats.values()).filter(s => s.count > 5),
            });
        }

        const recentReplays = await getFilteredReplayLists({
            name: player.name,
            skip: 0,
            take: 10,
        });

        return {
            player: player,
            gameModeStats: stats,
            recentReplays: recentReplays,
        };
    }

    private setStats(player: PlayerDto, gameModeStats: GameModeStatsBuilder, replay: ReplayDto) {
        const myReplayPlayer = replay.players.find(
            f => toonKey(f.player.toonId) === toonKey(player.toonId)
        );
        if (!myReplayPlayer) {
            return;
        }

        const team = myReplayPlayer.teamId;
        const win = replay.winnerTeam === team;
        const commander = myReplayPlayer.race;
        const isMvp = this.isMvp(myReplayPlayer, replay.players);

        // Commander stats
        if (gameModeStats.commanderStats.has(commander)) {
            const commanderStats = gameModeStats.commanderStats.get(commander)!;
            commanderStats.count += 1;
            if (win) commanderStats.wins += 1;
            if (isMvp) commanderStats.mvp += 1;
        } else {
            gameModeStats.commanderStats.set(commander, {
                commander: commander,
                count: 1,
                wins: win ? 1 : 0,
                mvp: isMvp ? 1 : 0,
            });
        }

        // Teammate & opponent stats
        for (const replayPlayer of replay.players) {
            const key = toonKey(replayPlayer.player.toonId);
            if (key === toonKey(player.toonId)) {
                continue; // skip self
            }
            const isTeammate = replayPlayer.teamId === team;
            const playerStatsMap = isTeammate ? gameModeStats.teammateStats : gameModeStats.opponentStats;

            if (playerStatsMap.has(key)) {
                const playerStats = playerStatsMap.get(key)!;
                playerStats.count += 1;
                if (win) {
                    playerStats.wins += 1;
                }
            } else {
                playerStatsMap.set(key, {
                    player: replayPlayer.player,
                    count: 1,
                    wins: win ? 1 : 0,
                });
            }
        }
    }

    private isMvp(player: ReplayPlayerDto, replayPlayers: ReplayPlayerDto[]): boolean {
        let kills = 0;
        for (const s of player.spawns) {
            if (s.breakpoint === 4) {
                kills = s.killedValue;
                break;
            }
        }
        let maxKills = 0;
        for (const rp of replayPlayers) {
            for (const s of rp.spawns) {
                if (s.breakpoint === 4 && s.killedValue > maxKills) {
                    maxKills = s.killedValue;
                }
            }
        }
        return kills > 0 && kills === maxKills;
    }

    public async findMainPlayer(): Promise<PlayerDto | undefined> {
        const { replays: recentReplays } = await this.getReplayChunk(null, 10);
        const oldestReplays = await this.getReplayChunkFromStart(10);

        const allReplays = [...recentReplays, ...oldestReplays];
        const playerCounts = new Map<string, { player: PlayerDto, count: number }>();

        for (const replay of allReplays) {
            for (const replayPlayer of replay.players) {
                const key = toonKey(replayPlayer.player.toonId);
                if (playerCounts.has(key)) {
                    playerCounts.get(key)!.count++;
                } else {
                    playerCounts.set(key, { player: replayPlayer.player, count: 1 });
                }
            }
        }

        if (playerCounts.size === 0) {
            return undefined;
        }

        let mainPlayer: { player: PlayerDto, count: number } | undefined = undefined;
        for (const playerStat of playerCounts.values()) {
            if (!mainPlayer || playerStat.count > mainPlayer.count) {
                mainPlayer = playerStat;
            }
        }

        return mainPlayer?.player;
    }

    private async getReplayChunkFromStart(take: number): Promise<ReplayDto[]> {
        const db = await openDB();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORES.replays, "readonly");
            const store = tx.objectStore(STORES.replays);
            const index = store.index("gametime");

            const replays: ReplayDto[] = [];

            const request = index.openCursor(null, "next"); // oldest first

            request.onsuccess = (event) => {
                const cursor = (event.target as IDBRequest<IDBCursorWithValue>).result;
                if (cursor && replays.length < take) {
                    replays.push(cursor.value);
                    cursor.continue();
                } else {
                    resolve(replays);
                }
            };

            request.onerror = () => reject(request.error);
        });
    }

    private async getReplayChunk(afterKey: IDBValidKey | null, take: number)
     :Promise<{ replays: ReplayDto[], lastKey: IDBValidKey | null }> {
        const db = await openDB();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORES.replays, "readonly");
            const store = tx.objectStore(STORES.replays);
            const index = store.index("gametime");

            const replays: ReplayDto[] = [];
            let lastKey: IDBValidKey | null = null;

            // Start from "afterKey" if provided, otherwise from the end (descending order)
            const range = afterKey ? IDBKeyRange.upperBound(afterKey, true) : null;
            const request = index.openCursor(range, "prev"); // newest first

            request.onsuccess = (event) => {
                const cursor = (event.target as IDBRequest<IDBCursorWithValue>).result;
                if (cursor && replays.length < take) {
                    replays.push(cursor.value);
                    lastKey = cursor.key;
                    cursor.continue();
                } else {
                    resolve({ replays, lastKey });
                }
            };

            request.onerror = () => reject(request.error);
        });
    }

}
