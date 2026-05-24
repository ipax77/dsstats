import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { closeDB, DB_NAME, openDB } from '../db-core';
import { exportUnuploadedReplays, exportUnuploadedReplays10, getReplayByHash, markReplaysAsUploaded, saveReplayFull } from '../dsstatsDb';
import { getTestReplay, getTestReplayList, getTestReplayMeta } from './replays.test';

vi.mock("pako", () => ({
  gzip: (content: string | Uint8Array) =>
    typeof content === "string"
      ? new TextEncoder().encode(content)
      : new Uint8Array(content),
  ungzip: (binary: Uint8Array) => new TextDecoder().decode(binary),
}));

describe("dsstats IndexedDb Upload Flow", () => {
    beforeEach(async () => {
        await openDB();
    });

    afterEach(async () => {
        closeDB();
        await new Promise<void>((resolve, reject) => {
            const deleteRequest = indexedDB.deleteDatabase(DB_NAME);
            deleteRequest.onsuccess = () => resolve();
            deleteRequest.onerror = () => reject(deleteRequest.error);
            deleteRequest.onblocked = () => {
                console.warn("Database deletion blocked.");
                reject("Database deletion blocked");
            };
        });
    });


    it("should export only unuploaded replays", async () => {
        const replay1 = getTestReplay();
        const meta1 = getTestReplayMeta(replay1);

        // mark both as not uploaded
        meta1.uploaded = 0;

        await saveReplayFull(replay1.compatHash, replay1, getTestReplayList(replay1), meta1);

        const exported = await exportUnuploadedReplays();
        expect(exported.hashes.length).toBe(1);
        expect(exported.hashes).toContain(replay1.compatHash);
        expect(exported.payload).toBeDefined();
    });

    it("should preserve replay-player compat hashes in exported replays", async () => {
        const replay = getTestReplay();
        const meta = getTestReplayMeta(replay);
        meta.uploaded = 0;

        await saveReplayFull(replay.compatHash, replay, getTestReplayList(replay), meta);

        const exported = await exportUnuploadedReplays();
        const exportedReplays = JSON.parse(atob(exported.payload));

        expect(exportedReplays[0].players.map((player: { compatHash?: string }) => player.compatHash)).toEqual(
            replay.players.map(player => player.compatHash)
        );
    });

    it("should preserve replay-player compat hashes in upload request exports", async () => {
        const replay = getTestReplay();
        const meta = getTestReplayMeta(replay);
        meta.uploaded = 0;

        await saveReplayFull(replay.compatHash, replay, getTestReplayList(replay), meta);

        const exported = await exportUnuploadedReplays10({
            appGuid: "test-app",
            appVersion: "1.0.0",
            requestNames: [],
            replays: []
        });
        const uploadRequest = JSON.parse(new TextDecoder().decode(exported.payload));

        expect(uploadRequest.replays[0].players.map((player: { compatHash?: string }) => player.compatHash)).toEqual(
            replay.players.map(player => player.compatHash)
        );
    });

    it("should mark matching request name players as uploaders only in upload request exports", async () => {
        const replay = getTestReplay();
        const meta = getTestReplayMeta(replay);
        meta.uploaded = 0;

        await saveReplayFull(replay.compatHash, replay, getTestReplayList(replay), meta);

        const requestNames = [
            {
                name: "PlayerOne",
                toonId: replay.players[0].player.toonId.id,
                regionId: replay.players[0].player.toonId.region,
                realmId: replay.players[0].player.toonId.realm
            }
        ];
        const exported = await exportUnuploadedReplays10({
            appGuid: "test-app",
            appVersion: "1.0.0",
            requestNames,
            replays: []
        });
        const uploadRequest = JSON.parse(new TextDecoder().decode(exported.payload));
        const exportedReplay = uploadRequest.replays[0];

        expect(uploadRequest.requestNames).toEqual(requestNames);
        expect(exportedReplay.players[0].isUploader).toBe(true);
        expect(exportedReplay.players[1].isUploader).toBe(false);

        const storedReplay = await getReplayByHash(replay.compatHash);
        expect(storedReplay?.players.map(player => player.isUploader)).toEqual([false, false]);
    });

    it("should Brotli-compress raw spawn playback sidecars before upload request exports", async () => {
        const replay = getTestReplay();
        const meta = getTestReplayMeta(replay);
        const rawSidecar = new Uint8Array([0x44, 0x53, 0x50, 0x42, 1, 2, 3, 4]);
        meta.uploaded = 0;
        replay.spawnPlayback = {
            available: true,
            formatVersion: 3,
            compression: 1,
            compressedLength: rawSidecar.length,
            uncompressedLength: rawSidecar.length,
            unitCount: 1,
        };

        await saveReplayFull(replay.compatHash, replay, getTestReplayList(replay), meta, rawSidecar);

        const storedReplay = await getReplayByHash(replay.compatHash);
        expect(storedReplay?.spawnPlayback?.compression).toBe(1);
        expect(storedReplay?.spawnPlayback?.compressedLength).toBe(3);
        expect(storedReplay?.spawnPlayback?.uncompressedLength).toBe(rawSidecar.length);

        const exported = await exportUnuploadedReplays10({
            appGuid: "test-app",
            appVersion: "1.0.0",
            requestNames: [],
            replays: []
        });

        expect(exported.sidecars.length).toBe(1);
        expect(exported.sidecars[0].compression).toBe(1);
        expect(exported.sidecars[0].compressedLength).toBe(3);
        expect(exported.sidecars[0].uncompressedLength).toBe(rawSidecar.length);
        expect(Array.from(exported.sidecars[0].payload)).toEqual([7, 8, 9]);
    });

    it("should preserve existing gzip spawn playback sidecars in upload request exports", async () => {
        const replay = getTestReplay();
        const meta = getTestReplayMeta(replay);
        const gzipSidecar = new Uint8Array([9, 8, 7]);
        meta.uploaded = 0;
        replay.spawnPlayback = {
            available: true,
            formatVersion: 3,
            compression: 2,
            compressedLength: gzipSidecar.length,
            uncompressedLength: 8,
            unitCount: 1,
        };

        await saveReplayFull(replay.compatHash, replay, getTestReplayList(replay), meta, gzipSidecar);

        const exported = await exportUnuploadedReplays10({
            appGuid: "test-app",
            appVersion: "1.0.0",
            requestNames: [],
            replays: []
        });

        expect(exported.sidecars.length).toBe(1);
        expect(exported.sidecars[0].compression).toBe(2);
        expect(exported.sidecars[0].compressedLength).toBe(gzipSidecar.length);
        expect(exported.sidecars[0].uncompressedLength).toBe(8);
        expect(Array.from(exported.sidecars[0].payload)).toEqual([9, 8, 7]);
    });

    it("should export a bounded newest-first upload request with matching sidecars", async () => {
        for (let id = 1; id <= 3; id++) {
            const replay = getTestReplay(id);
            const meta = getTestReplayMeta(replay);
            const gzipSidecar = new Uint8Array([id, id + 1, id + 2]);
            meta.uploaded = 0;
            replay.spawnPlayback = {
                available: true,
                formatVersion: 3,
                compression: 2,
                compressedLength: gzipSidecar.length,
                uncompressedLength: 8,
                unitCount: id,
            };

            await saveReplayFull(replay.compatHash, replay, getTestReplayList(replay), meta, gzipSidecar);
        }

        const exported = await exportUnuploadedReplays10({
            appGuid: "test-app",
            appVersion: "1.0.0",
            requestNames: [],
            replays: []
        }, 2);
        const uploadRequest = JSON.parse(new TextDecoder().decode(exported.payload));

        expect(exported.hashes).toEqual([getTestReplay(3).compatHash, getTestReplay(2).compatHash]);
        expect(uploadRequest.replays.map((replay: { compatHash: string }) => replay.compatHash)).toEqual(exported.hashes);
        expect(exported.sidecars.map(sidecar => sidecar.replayHash)).toEqual(exported.hashes);
        expect(exported.sidecars.map(sidecar => sidecar.partName)).toEqual(["sidecar-0", "sidecar-1"]);
        expect(exported.sidecars.map(sidecar => Array.from(sidecar.payload))).toEqual([[3, 4, 5], [2, 3, 4]]);
    });

    it("should not export uploaded replays", async () => {
        const replay = getTestReplay();
        const meta = getTestReplayMeta(replay);
        meta.uploaded = 1; // already uploaded
        await saveReplayFull(replay.compatHash, replay, getTestReplayList(replay), meta);

        const exported = await exportUnuploadedReplays();
        expect(exported.hashes.length).toBe(0);
    });

    it("should mark replays as uploaded", async () => {
        const replay = getTestReplay();
        const meta = getTestReplayMeta(replay);
        meta.uploaded = 0;
        await saveReplayFull(replay.compatHash, replay, getTestReplayList(replay), meta);

        // Verify it's unuploaded first
        let exported = await exportUnuploadedReplays();
        expect(exported.hashes).toContain(replay.compatHash);

        // Mark as uploaded
        await markReplaysAsUploaded([replay.compatHash]);

        // Verify it's now gone from export list
        exported = await exportUnuploadedReplays();
        expect(exported.hashes).not.toContain(replay.compatHash);
    });
});
