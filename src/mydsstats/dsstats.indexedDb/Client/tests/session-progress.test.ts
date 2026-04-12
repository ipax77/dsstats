import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { closeDB, DB_NAME, openDB } from '../db-core';
import {
    detectTrackedProfileCandidates,
    getRecentReplayHashes,
    getReplayHashesSince,
    getSessionWindowSettings,
    getTrackedProfiles,
    saveReplayFull,
    saveSessionWindowSettings,
    saveTrackedProfiles
} from '../dsstatsDb';
import { SessionWindowSettingsDto, TrackedProfileDto } from '../dtos';
import { getTestReplay, getTestReplayList, getTestReplayMeta } from './replays.test';

describe('session progress helpers', () => {
    beforeEach(async () => {
        await openDB();
    });

    afterEach(async () => {
        closeDB();
        await new Promise<void>((resolve, reject) => {
            const deleteRequest = indexedDB.deleteDatabase(DB_NAME);
            deleteRequest.onsuccess = () => resolve();
            deleteRequest.onerror = () => reject(deleteRequest.error);
            deleteRequest.onblocked = () => reject('Database deletion blocked');
        });
    });

    it('stores tracked profiles and session window settings', async () => {
        const profiles: TrackedProfileDto[] = [{
            name: 'Main',
            active: true,
            autoDetected: true,
            toonId: {
                region: 1,
                realm: 1,
                id: 123
            }
        }];

        const settings: SessionWindowSettingsDto = {
            mode: 1,
            hours: 12,
            replayCount: 30
        };

        await saveTrackedProfiles(profiles);
        await saveSessionWindowSettings(settings);

        expect(await getTrackedProfiles()).toEqual(profiles);
        expect(await getSessionWindowSettings()).toEqual(settings);
    });

    it('detects recent replay profile candidates and session replay hashes', async () => {
        const replay1 = getTestReplay(1);
        replay1.players[0].player.name = 'MainPlayer';
        replay1.players[0].player.toonId.id = 7;
        replay1.players[1].player.toonId.id = 101;
        replay1.gametime = '2026-04-12T11:00:00.000Z';

        const replay2 = getTestReplay(2);
        replay2.players[0].player.name = 'MainPlayer';
        replay2.players[0].player.toonId.id = 7;
        replay2.players[1].player.toonId.id = 102;
        replay2.gametime = '2026-04-12T10:30:00.000Z';

        const replay3 = getTestReplay(3);
        replay3.players[0].player.name = 'AltPlayer';
        replay3.players[0].player.toonId.id = 8;
        replay3.players[1].player.toonId.id = 103;
        replay3.gametime = '2026-04-12T08:00:00.000Z';

        for (const replay of [replay1, replay2, replay3]) {
            await saveReplayFull(replay.compatHash, replay, getTestReplayList(replay), getTestReplayMeta(replay));
        }

        const candidates = await detectTrackedProfileCandidates(10);
        expect(candidates[0].name).toBe('MainPlayer');
        expect(candidates[0].toonId.id).toBe(7);
        expect(candidates[0].count).toBe(2);

        const recentHashes = await getRecentReplayHashes(2);
        expect(recentHashes).toEqual([replay1.compatHash, replay2.compatHash]);

        const timeWindowHashes = await getReplayHashesSince('2026-04-12T09:00:00.000Z');
        expect(timeWindowHashes).toEqual([replay1.compatHash, replay2.compatHash]);
    });
});
