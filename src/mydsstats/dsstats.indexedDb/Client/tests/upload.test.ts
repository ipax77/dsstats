import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { closeDB, openDB } from '../db-core';
import { exportUnuploadedReplays, markReplaysAsUploaded, saveReplayFull } from '../dsstatsDb';
import { getTestReplay, getTestReplayList, getTestReplayMeta } from './replays.test';

vi.mock("pako", () => ({
  gzip: (content: string) => new TextEncoder().encode(content),
  ungzip: (binary: Uint8Array) => new TextDecoder().decode(binary),
}));

describe("dsstats IndexedDb Upload Flow", () => {
    beforeEach(async () => {
        await openDB();
    });

    afterEach(async () => {
        closeDB();
        await new Promise<void>((resolve, reject) => {
            const deleteRequest = indexedDB.deleteDatabase("DsstatsDB");
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
