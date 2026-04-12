import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { exportDb, importDb } from '../backup';
import { closeDB, DB_NAME, openDB, STORES } from '../db-core';
import { Dump } from '../migration';

async function deleteTestDb() {
    closeDB();
    await new Promise<void>((resolve, reject) => {
        const deleteRequest = indexedDB.deleteDatabase(DB_NAME);
        deleteRequest.onsuccess = () => resolve();
        deleteRequest.onerror = () => reject(deleteRequest.error);
        deleteRequest.onblocked = () => reject(new Error('Database deletion blocked.'));
    });
}

async function seedOutOfLineStores() {
    const db = await openDB();

    await new Promise<void>((resolve, reject) => {
        const tx = db.transaction([STORES.config, STORES.directoryHandles], 'readwrite');
        tx.objectStore(STORES.config).put({ appGuid: 'cfg-1', replayStartname: 'replay' }, 'app');
        tx.objectStore(STORES.directoryHandles).put({
            handle: null,
            displayName: 'Saved Folder',
            regionId: 1,
            fingerprint: null,
            status: 'unbound',
        }, 'dir-1');
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
}

async function readOutOfLineStores() {
    const db = await openDB();

    return await new Promise<{ config: any; directoryHandle: any }>((resolve, reject) => {
        const tx = db.transaction([STORES.config, STORES.directoryHandles], 'readonly');
        const configReq = tx.objectStore(STORES.config).get('app');
        const dirReq = tx.objectStore(STORES.directoryHandles).get('dir-1');

        tx.oncomplete = () => resolve({
            config: configReq.result,
            directoryHandle: dirReq.result,
        });
        tx.onerror = () => reject(tx.error);
    });
}

describe('backup import/export', () => {
    beforeEach(async () => {
        await openDB();
    });

    afterEach(async () => {
        await deleteTestDb();
    });

    it('preserves explicit keys for out-of-line stores across backup and restore', async () => {
        await seedOutOfLineStores();

        const exported = await exportDb();
        const json = new TextDecoder().decode(new Uint8Array(exported));

        await deleteTestDb();
        await importDb(json, true);

        const restored = await readOutOfLineStores();
        expect(restored.config.appGuid).toBe('cfg-1');
        expect(restored.directoryHandle.displayName).toBe('Saved Folder');
        expect(restored.directoryHandle.status).toBe('unbound');
    });

    it('restores legacy keyless config backups and skips keyless directory-handle entries', async () => {
        const dump: Dump = {
            __meta: { dbVersion: 7, date: new Date().toISOString() },
            stores: {
                [STORES.config]: [
                    { appGuid: 'legacy-cfg', replayStartname: 'replay' },
                ],
                [STORES.directoryHandles]: [
                    {
                        handle: null,
                        displayName: 'Legacy Folder',
                        regionId: 1,
                        fingerprint: null,
                        status: 'bound',
                    },
                ],
            },
        };

        await expect(importDb(JSON.stringify(dump), true)).resolves.toBeUndefined();

        const db = await openDB();
        await new Promise<void>((resolve, reject) => {
            const tx = db.transaction([STORES.config, STORES.directoryHandles], 'readonly');
            const configReq = tx.objectStore(STORES.config).get('app');
            const dirReq = tx.objectStore(STORES.directoryHandles).getAll();

            tx.oncomplete = () => {
                expect(configReq.result.appGuid).toBe('legacy-cfg');
                expect(dirReq.result).toHaveLength(0);
                resolve();
            };
            tx.onerror = () => reject(tx.error);
        });
    });
});
