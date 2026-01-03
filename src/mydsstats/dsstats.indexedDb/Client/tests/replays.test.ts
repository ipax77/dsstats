import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { closeDB, DB_NAME, openDB } from '../db-core';
import { getFilteredReplayLists, getFilteredReplayListsCount, getReplayByHash, saveReplayFull } from '../dsstatsDb';
import { ReplayDto, ReplayListDto, ReplayMeta } from '../dtos';

export const getTestReplay = (id: number = 1): ReplayDto => ({
    fileName: 'test/path',
    compatHash: `43BEE0CF165250AB9CE1B25B641B8C8F9F6146A0D34DB290B63E731B3E2B93B${id}`,
    title: 'Direct Strike',
    version: '5.0.14.94137',
    gameMode: 7,
    regionId: 1,
    gametime: `2025-08-19T21:30:${44 + id}.536566Z`,
    baseBuild: 94137,
    duration: 789 + id,
    cannon: 0,
    bunker: 285,
    winnerTeam: (id % 2) + 1,
    middleChanges: [],
    players: [
        {
            name: 'PlayerOne',
            race: 1,
            selectedRace: 1,
            teamId: 1,
            gamePos: 1,
            result: 0,
            duration: 796,
            apm: 9,
            messages: 0,
            pings: 0,
            isMvp: false,
            isUploader: false,
            spawns: [],
            upgrades: [],
            tierUpgrades: [],
            refineries: [],
            player: {
                playerId: 1,
                name: 'PlayerOne',
                toonId: {
                    region: 1,
                    realm: 1,
                    id: 1
                }
            }
        },
        {
            name: 'PlayerTwo_' + id,
            race: id,
            selectedRace: 2,
            teamId: 2,
            gamePos: 4,
            result: 1,
            duration: 796,
            apm: 9,
            messages: 0,
            pings: 0,
            isMvp: true,
            isUploader: false,
            spawns: [],
            upgrades: [],
            tierUpgrades: [],
            refineries: [],
            player: {
                playerId: 2,
                name: 'PlayerTwo_',
                toonId: {
                    region: 1,
                    realm: 1,
                    id: 2
                }
            }
        },
    ]
});

export const getTestReplayList = (replay: ReplayDto): ReplayListDto => ({
    replayHash: replay.compatHash,
    gametime: replay.gametime,
    gameMode: replay.gameMode,
    duration: replay.duration,
    winnerTeam: replay.winnerTeam,
    commandersTeam1: replay.players.filter(f => f.teamId === 1).map(m => m.race),
    commandersTeam2: replay.players.filter(f => f.teamId === 2).map(m => m.race),
    playerNames: replay.players.sort((a, b) => a.gamePos - b.gamePos).map(m => m.name),
    leaverType: 0,
    playerPos: 0
});

export const getTestReplayMeta = (replay: ReplayDto): ReplayMeta => ({
    replayHash: replay.compatHash,
    filePath: replay.fileName,
    regionId: replay.regionId,
    uploaded: 0,
    skip: false
});

describe('dsstats IndexedDb', () => {
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
                // Handle blocked event if necessary
                console.warn('Database deletion blocked.');
                reject('Database deletion blocked');
            };
        });
    });

    it('should create and retrieve a replay', async () => {
        const replay = getTestReplay();
        const replayList = getTestReplayList(replay);
        const replayMeta = getTestReplayMeta(replay);
        await saveReplayFull(replay.compatHash, replay, replayList, replayMeta);

        const savedReplay = await getReplayByHash(replay.compatHash);
        expect(savedReplay).toBeDefined();
        expect(savedReplay?.compatHash).toEqual(replay.compatHash);
    });

    describe('getFilteredReplayLists', () => {
        beforeEach(async () => {
            // Save some test data
            for (let i = 1; i <= 3; i++) {
                const replay = getTestReplay(i);
                const replayList = getTestReplayList(replay);
                const replayMeta = getTestReplayMeta(replay);
                await saveReplayFull(replay.compatHash, replay, replayList, replayMeta);
            }
        });

        it('should return all replays sorted by gameTime descending by default', async () => {
            const result = await getFilteredReplayLists({});
            expect(result.length).toBe(3);
            expect(result[0].duration).toBe(792);
            expect(result[1].duration).toBe(791);
            expect(result[2].duration).toBe(790);
        });

        it('should filter by player name and sort by gameTime descending by default', async () => {
            const result = await getFilteredReplayLists({ name: 'PlayerOne' });
            expect(result.length).toBe(3);
            expect(result[0].duration).toBe(792);
            expect(result[1].duration).toBe(791);
            expect(result[2].duration).toBe(790);
        });

        it('should filter by partial player name', async () => {
            const result = await getFilteredReplayLists({ name: 'PlayerTwo_2' });
            expect(result.length).toBe(1);
            expect(result[0].playerNames).toContain('PlayerTwo_2');
        });

        it('should return empty array for non-matching name', async () => {
            const result = await getFilteredReplayLists({ name: 'NonExistentPlayer' });
            expect(result.length).toBe(0);
        });

        it('should filter by multiple player names', async () => {
            const result = await getFilteredReplayLists({ name: 'PlayerOne PlayerTwo_2' });
            expect(result.length).toBe(1);
            expect(result[0].playerNames).toContain('PlayerOne');
            expect(result[0].playerNames).toContain('PlayerTwo_2');
        });

        it('should filter by commander', async () => {
            const result = await getFilteredReplayLists({ commanders: [2] });
            expect(result.length).toBe(1);
            expect(result[0].commandersTeam2).toContain(2);
        });

        it('should filter by multiple commanders and sort by gameTime descending by default', async () => {
            const result = await getFilteredReplayLists({ commanders: [1, 3] });
            expect(result.length).toBe(3);
            expect(result[0].duration).toBe(792);
            expect(result[1].duration).toBe(791);
            expect(result[2].duration).toBe(790);
        });

        it('should return empty array for non-matching commander', async () => {
            const result = await getFilteredReplayLists({ commanders: [4] });
            expect(result.length).toBe(0);
        });

        it('should filter by name and commander', async () => {
            const result = await getFilteredReplayLists({ name: 'PlayerTwo_3', commanders: [3] });
            expect(result.length).toBe(1);
            expect(result[0].playerNames).toContain('PlayerTwo_3');
            expect(result[0].commandersTeam2).toContain(3);
        });

        it('should filter by name and commander with linkCommanders and sort by gameTime descending by default', async () => {
            // PlayerOne always has race 1, so should return all 3
            let result = await getFilteredReplayLists({ name: 'PlayerOne', commanders: [1], linkCommanders: true });
            expect(result.length).toBe(3);
            expect(result[0].duration).toBe(792);
            expect(result[1].duration).toBe(791);
            expect(result[2].duration).toBe(790);

            // PlayerOne never has race 2
            result = await getFilteredReplayLists({ name: 'PlayerOne', commanders: [2], linkCommanders: true });
            expect(result.length).toBe(0);

            // PlayerTwo_2 has race 2
            result = await getFilteredReplayLists({ name: 'PlayerTwo_2', commanders: [2], linkCommanders: true });
            expect(result.length).toBe(1);
            expect(result[0].playerNames).toContain('PlayerTwo_2');
            expect(result[0].commandersTeam2).toContain(2);

            // PlayerTwo_2 never has race 1
            result = await getFilteredReplayLists({ name: 'PlayerTwo_2', commanders: [1], linkCommanders: true });
            expect(result.length).toBe(0);
        });

        it('should sort by duration ascending', async () => {
            const result = await getFilteredReplayLists({ tableOrders: [{ name: 'duration', ascending: true }] });
            expect(result.length).toBe(3);
            expect(result[0].duration).toBe(790);
            expect(result[1].duration).toBe(791);
            expect(result[2].duration).toBe(792);
        });

        it('should sort by duration descending', async () => {
            const result = await getFilteredReplayLists({ tableOrders: [{ name: 'duration', ascending: false }] });
            expect(result.length).toBe(3);
            expect(result[0].duration).toBe(792);
            expect(result[1].duration).toBe(791);
            expect(result[2].duration).toBe(790);
        });

        it('should sort by winnerTeam then duration', async () => {
            const result = await getFilteredReplayLists({ tableOrders: [{ name: 'winnerTeam', ascending: true }, { name: 'duration', ascending: true }] });
            expect(result.length).toBe(3);
            // winnerTeam 1, duration 791
            expect(result[0].winnerTeam).toBe(1);
            expect(result[0].duration).toBe(791);
            // winnerTeam 2, duration 790
            expect(result[1].winnerTeam).toBe(2);
            expect(result[1].duration).toBe(790);
            // winnerTeam 2, duration 792
            expect(result[2].winnerTeam).toBe(2);
            expect(result[2].duration).toBe(792);
        });

        it('should paginate results with skip and take', async () => {
            // Default sort is gameTime descending, so we expect replay 3, 2, 1
            let result = await getFilteredReplayLists({ skip: 0, take: 1 });
            expect(result.length).toBe(1);
            expect(result[0].duration).toBe(792); // Replay 3

            result = await getFilteredReplayLists({ skip: 1, take: 1 });
            expect(result.length).toBe(1);
            expect(result[0].duration).toBe(791); // Replay 2

            result = await getFilteredReplayLists({ skip: 1, take: 2 });
            expect(result.length).toBe(2);
            expect(result[0].duration).toBe(791); // Replay 2
            expect(result[1].duration).toBe(790); // Replay 1
        });
    });

    describe('getFilteredReplayListsCount', () => {
        beforeEach(async () => {
            // Save some test data
            for (let i = 1; i <= 3; i++) {
                const replay = getTestReplay(i);
                const replayList = getTestReplayList(replay);
                const replayMeta = getTestReplayMeta(replay);
                await saveReplayFull(replay.compatHash, replay, replayList, replayMeta);
            }
        });

        it('should return the total count of replays with no filter', async () => {
            const count = await getFilteredReplayListsCount({});
            expect(count).toBe(3);
        });

        it('should return the correct count with a filter', async () => {
            const count = await getFilteredReplayListsCount({ name: 'PlayerTwo_2' });
            expect(count).toBe(1);
        });

        it('should return 0 for a non-matching filter', async () => {
            const count = await getFilteredReplayListsCount({ name: 'NonExistentPlayer' });
            expect(count).toBe(0);
        });
    });
});