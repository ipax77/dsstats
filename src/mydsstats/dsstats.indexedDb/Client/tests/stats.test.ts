import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { closeDB, DB_NAME, openDB } from '../db-core';
import { saveReplayFull } from '../dsstatsDb';
import { PlayerDto } from '../dtos';
import { StatsService } from '../stats/stats';
import { getTestReplay, getTestReplayList, getTestReplayMeta } from './replays.test';

const player1: PlayerDto = {
    playerId: 1,
    name: 'PlayerOne',
    toonId: {
        region: 1,
        realm: 1,
        id: 1
    }
};

const player2: PlayerDto = {
    playerId: 2,
    name: 'PlayerTwo',
    toonId: {
        region: 1,
        realm: 1,
        id: 2
    }
};

const player3: PlayerDto = {
    playerId: 3,
    name: 'PlayerThree',
    toonId: {
        region: 1,
        realm: 1,
        id: 3
    }
};

describe('StatsService', () => {
    beforeEach(async () => {
        // Ensure the DB is open before each test
        await openDB();
    });

    afterEach(async () => {
        closeDB();
        await new Promise<void>((resolve, reject) => {
            const deleteRequest = indexedDB.deleteDatabase(DB_NAME);
            deleteRequest.onsuccess = () => resolve();
            deleteRequest.onerror = () => reject(deleteRequest.error);
            deleteRequest.onblocked = () => {
                console.warn('Database deletion blocked.');
                reject('Database deletion blocked');
            };
        });
    });

    describe('findMainPlayer', () => {
        it('should return undefined when there are no replays', async () => {
            const statsService = new StatsService();
            const mainPlayer = await statsService.findMainPlayer();
            expect(mainPlayer).toBeUndefined();
        });

        it('should find the most frequent player as the main player', async () => {
            const replay1 = getTestReplay(1);
            replay1.players[0].player = player1;
            replay1.players[1].player = player2;
            await saveReplayFull(replay1.compatHash, replay1, getTestReplayList(replay1), getTestReplayMeta(replay1));

            const replay2 = getTestReplay(2);
            replay2.players[0].player = player1;
            replay2.players[1].player = player3;
            await saveReplayFull(replay2.compatHash, replay2, getTestReplayList(replay2), getTestReplayMeta(replay2));
            
            const replay3 = getTestReplay(3);
            replay3.players[0].player = player1;
            replay3.players[1].player = player3;
            await saveReplayFull(replay3.compatHash, replay3, getTestReplayList(replay3), getTestReplayMeta(replay3));

            const statsService = new StatsService();
            const mainPlayer = await statsService.findMainPlayer();

            expect(mainPlayer).toBeDefined();
            expect(mainPlayer?.name).toEqual(player1.name);
        });
    });

    describe('generateStats', () => {
        it('should generate correct stats for a player', async () => {
            // Create 6 replays for Player1 vs Player2 to pass the count > 5 filter
            for (let i = 1; i <= 6; i++) {
                const replay = getTestReplay(i);
                replay.players = [
                    { ...replay.players[0], player: player1, race: 1, teamId: 1, spawns: [{
                        breakpoint: 4, killedValue: 100,
                        income: 0,
                        gasCount: 0,
                        armyValue: 0,
                        upgradeSpent: 0,
                        units: []
                    }] },
                    { ...replay.players[1], player: player2, race: 1, teamId: 2, spawns: [{
                        breakpoint: 4, killedValue: 50,
                        income: 0,
                        gasCount: 0,
                        armyValue: 0,
                        upgradeSpent: 0,
                        units: []
                    }] }
                ];
                replay.winnerTeam = 1;
                await saveReplayFull(replay.compatHash, replay, getTestReplayList(replay), getTestReplayMeta(replay));
            }

            // Replay with Player3 (will be filtered out)
            const replay7 = getTestReplay(7);
            replay7.players = [
                { ...replay7.players[0], player: player1, race: 2, teamId: 1, spawns: [{
                    breakpoint: 4, killedValue: 50,
                    income: 0,
                    gasCount: 0,
                    armyValue: 0,
                    upgradeSpent: 0,
                    units: []
                }] },
                { ...replay7.players[1], player: player3, race: 2, teamId: 2, spawns: [{
                    breakpoint: 4, killedValue: 100,
                    income: 0,
                    gasCount: 0,
                    armyValue: 0,
                    upgradeSpent: 0,
                    units: []
                }] }
            ];
            replay7.winnerTeam = 2;
            await saveReplayFull(replay7.compatHash, replay7, getTestReplayList(replay7), getTestReplayMeta(replay7));
            
            const statsService = new StatsService();
            const stats = await statsService.generateStats(player1);

            expect(stats).toBeDefined();
            expect(stats.player.name).toEqual(player1.name);
            expect(stats.gameModeStats.length).toBe(1);

            const gameModeStats = stats.gameModeStats[0];
            expect(gameModeStats.gameMode).toBe(7);

            // Commander Stats
            expect(gameModeStats.commanderStats.length).toBe(2);
            const commander1Stats = gameModeStats.commanderStats.find(c => c.commander === 1);
            expect(commander1Stats).toEqual({ commander: 1, count: 6, wins: 6, mvp: 6 });
            const commander2Stats = gameModeStats.commanderStats.find(c => c.commander === 2);
            expect(commander2Stats).toEqual({ commander: 2, count: 1, wins: 0, mvp: 0 });

            // Opponent Stats
            expect(gameModeStats.opponentStats.length).toBe(1);
            const opponent2Stats = gameModeStats.opponentStats.find(o => o.player.name === 'PlayerTwo');
            expect(opponent2Stats).toEqual({ player: player2, count: 6, wins: 6 });

            // Teammate Stats should be empty
            expect(gameModeStats.teammateStats.length).toBe(0);

            // Recent Replays
            expect(stats.recentReplays).toBeDefined();
            expect(stats.recentReplays.length).toBe(7);
            expect(stats.recentReplays[0].playerNames).toContain(player1.name);
        });
    });
});
