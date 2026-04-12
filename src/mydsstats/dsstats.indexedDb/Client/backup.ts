import { Dump } from "./migration";
import { openDB, migrateDump, STORES } from "./db-core";

type KeyedDumpItem = {
    __idbKey: IDBValidKey;
    __idbValue: unknown;
};

function isKeyedDumpItem(item: unknown): item is KeyedDumpItem {
    return !!item && typeof item === "object" && "__idbKey" in item && "__idbValue" in item;
}

async function exportStore(store: IDBObjectStore): Promise<unknown[]> {
    if (store.keyPath !== null) {
        return await new Promise((resolve, reject) => {
            const req = store.getAll();
            req.onsuccess = () => resolve(req.result);
            req.onerror = () => reject(req.error);
        });
    }

    return await new Promise((resolve, reject) => {
        const items: KeyedDumpItem[] = [];
        const req = store.openCursor();

        req.onsuccess = () => {
            const cursor = req.result;
            if (!cursor) {
                resolve(items);
                return;
            }

            items.push({
                __idbKey: cursor.key,
                __idbValue: cursor.value,
            });
            cursor.continue();
        };

        req.onerror = () => reject(req.error);
    });
}

function transformImportedItem(storeName: string, item: any) {
    if (storeName === STORES.directoryHandles) {
        return {
            ...item,
            handle: null,              // strip invalid handle
            status: "unbound",         // force rebinding
            lastBoundAt: undefined
        };
    }

    return item;
}

function getLegacyOutOfLineKey(storeName: string, item: unknown): IDBValidKey | undefined {
    if (storeName === STORES.config) {
        return "app";
    }

    if (storeName === STORES.directoryHandles) {
        console.warn("Skipping legacy directory handle backup entry without key; it cannot be restored reliably.");
        return undefined;
    }

    console.warn(`Skipping legacy backup entry for out-of-line store "${storeName}" without key.`);
    return undefined;
}

function putImportedItem(store: IDBObjectStore, storeName: string, item: unknown) {
    if (store.keyPath === null) {
        if (isKeyedDumpItem(item)) {
            store.put(transformImportedItem(storeName, item.__idbValue), item.__idbKey);
            return;
        }

        const legacyKey = getLegacyOutOfLineKey(storeName, item);
        if (legacyKey !== undefined) {
            store.put(transformImportedItem(storeName, item), legacyKey);
        }
        return;
    }

    store.put(transformImportedItem(storeName, item));
}

export async function exportDb(): Promise<ArrayBuffer> {
    const database = await openDB();

    return new Promise((resolve, reject) => {
        const tx = database.transaction(Array.from(database.objectStoreNames), "readonly");
        const dump: Dump = { __meta: { dbVersion: database.version, date: new Date().toISOString() }, stores: {} };
        let pending = database.objectStoreNames.length;

        if (pending === 0) return resolve(new ArrayBuffer(0));

        for (const storeName of Array.from(database.objectStoreNames)) {
            const store = tx.objectStore(storeName);
            exportStore(store)
                .then((items) => {
                    dump.stores[storeName] = items;
                    if (--pending === 0) {
                        const json = JSON.stringify(dump);
                        const compressed = pako.gzip(json);
                        const binary = new Uint8Array(compressed);
                        resolve(binary.buffer);
                    }
                })
                .catch(reject);
        }
    });
}

export async function importDb(json: string, replace = false): Promise<void> {
    let dump: Dump = JSON.parse(json);
    dump = migrateDump(dump);
    const db = await openDB();

    return new Promise((resolve, reject) => {
        const tx = db.transaction(Array.from(db.objectStoreNames), "readwrite");
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);

        for (const [storeName, items] of Object.entries(dump.stores)) {
            if (!db.objectStoreNames.contains(storeName)) continue;
            const store = tx.objectStore(storeName);

            if (replace) {
                const clearReq = store.clear();
                clearReq.onsuccess = () => {
                    for (const item of items) putImportedItem(store, storeName, item);
                };
            } else {
                for (const item of items) putImportedItem(store, storeName, item);
            }
        }
    });
}

/**
 * Export DB and trigger a download as .json.gz.txt with timestamped filename
 */
export async function exportBackup(): Promise<void> {
    const binary = await exportDb();

    const blob = new Blob([binary], { type: "application/gzip" });

    const timestamp = new Date().toISOString().replace(/[:.]/g, "-");
    const filename = `dsstatsdb-backup-${timestamp}.idb.json.gz`;

    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
}

/**
 * Open file picker, read backup file and import into DB
 */
export async function importBackup(replace: boolean = false): Promise<void> {
    return new Promise((resolve, reject) => {
        const input = document.createElement("input");
        input.type = "file";
        input.accept = ".gz,.json";

        input.onchange = async () => {
            if (!input.files || input.files.length === 0) {
                return reject("No file selected");
            }

            const file = input.files[0];
            const reader = new FileReader();

            reader.onload = async () => {
                try {
                    const buffer = reader.result as ArrayBuffer;
                    const binary = new Uint8Array(buffer); // for pako
                    const json = pako.ungzip(binary, { to: "string" });
                    await importDb(json, replace);
                    resolve();
                } catch (err) {
                    reject(err);
                }
            };

            reader.onerror = () => reject(reader.error);
            reader.readAsArrayBuffer(file);
        };

        input.click();
    });
}

