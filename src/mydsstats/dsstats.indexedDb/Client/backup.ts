import { Dump } from "./migration";
import { openDB, migrateDump } from "./db-core";

export async function exportDb(): Promise<ArrayBuffer> {
    const database = await openDB();

    return new Promise((resolve, reject) => {
        const tx = database.transaction(Array.from(database.objectStoreNames), "readonly");
        const dump: Dump = { __meta: { dbVersion: database.version, date: new Date().toISOString() }, stores: {} };
        let pending = database.objectStoreNames.length;

        if (pending === 0) return resolve(new ArrayBuffer(0));

        for (const storeName of Array.from(database.objectStoreNames)) {
            const store = tx.objectStore(storeName);
            const req = store.getAll();

            req.onsuccess = () => {
                dump.stores[storeName] = req.result;
                if (--pending === 0) {
                    const json = JSON.stringify(dump);
                    const compressed = pako.gzip(json);
                    const binary = new Uint8Array(compressed);
                    resolve(binary.buffer);
                }
            };

            req.onerror = () => reject(req.error);
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
                    for (const item of items) store.put(item);
                };
            } else {
                for (const item of items) store.put(item);
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


