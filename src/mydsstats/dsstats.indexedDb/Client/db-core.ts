import { Dump, Migration } from "./migration.js";

export const DB_NAME = "ReplayDB";
export const DB_VERSION = 3;

export const STORES = {
    replays: "Replays",
    lists: "ReplayLists",
    meta: "ReplayMeta",
    config: "Config",
    directoryHandles: "DirectoryHandles",
};

let db: IDBDatabase | null = null;

export function openDB(): Promise<IDBDatabase> {
    return new Promise((resolve, reject) => {
        if (db) {
            resolve(db);
            return;
        }

        const request = indexedDB.open(DB_NAME, DB_VERSION);

        request.onupgradeneeded = (event) => {
            const database = (event.target as IDBOpenDBRequest).result;
            const tx = (event.target as IDBOpenDBRequest).transaction!;
            const oldVersion = event.oldVersion;

            // Apply migrations incrementally
            for (let v = oldVersion; v < DB_VERSION; v++) {
                const migration = upgrades[v];
                if (migration?.schema) {
                    migration.schema(database, tx);
                }
            }
        };

        request.onsuccess = () => {
            db = request.result;

            resolve(db);
        };

        request.onerror = () => reject(request.error);
    });
}

export function closeDB(): void {
    if (db) {
        db.close();
        db = null;
    }
}


export function migrateDump(dump: Dump): Dump {
  let version = dump.__meta.dbVersion;

  while (version < DB_VERSION) {
    const migration = upgrades[version];

    if (migration?.data) {
      dump = migration.data(dump); // can touch any store
    } else {
      dump.__meta.dbVersion = version + 1;
    }

    version = dump.__meta.dbVersion;
  }

  return dump;
}


export const migration0: Migration = {
    schema: (db, tx) => {
        const replayStore = db.objectStoreNames.contains(STORES.replays)
            ? tx.objectStore(STORES.replays)
            : db.createObjectStore(STORES.replays, { keyPath: "replayHash" });

        if (!replayStore.indexNames.contains("gametime")) {
            replayStore.createIndex("gametime", "gametime", { unique: false });
        }

        const listsStore = db.objectStoreNames.contains(STORES.lists)
            ? tx.objectStore(STORES.lists)
            : db.createObjectStore(STORES.lists, { keyPath: "replayHash" });

        if (!listsStore.indexNames.contains("gametime")) {
            listsStore.createIndex("gametime", "gametime", { unique: false });
        }
        if (!listsStore.indexNames.contains("gameMode")) {
            listsStore.createIndex("gameMode", "gameMode", { unique: false });
        }
        if (!listsStore.indexNames.contains("playerNames")) {
            listsStore.createIndex("playerNames", "playerNames", { multiEntry: true });
        }
        if (!listsStore.indexNames.contains("commandersTeam1")) {
            listsStore.createIndex("commandersTeam1", "commandersTeam1", { multiEntry: true });
        }
        if (!listsStore.indexNames.contains("commandersTeam2")) {
            listsStore.createIndex("commandersTeam2", "commandersTeam2", { multiEntry: true });
        }

        if (!db.objectStoreNames.contains(STORES.meta)) {
            const store = db.createObjectStore(STORES.meta, { keyPath: "replayHash" });
            store.createIndex("uploaded", "uploaded", { unique: false });
            store.createIndex("filePath", "filePath", { unique: false });
            store.createIndex("skip", "skip", { unique: false });
        }

        if (!db.objectStoreNames.contains(STORES.config)) {
            db.createObjectStore(STORES.config);
        }
    },
};

const migration1: Migration = {
  data: (dump) => {
    // Adjust ReplayMeta
    const meta = dump.stores[STORES.meta] as any[] | undefined;
    if (meta) {
      for (const record of meta) {
        if (typeof record.uploaded === "boolean") {
          record.uploaded = record.uploaded ? 1 : 0;
        }
      }
    }

    // Example: adjust ReplayLists
    const lists = dump.stores[STORES.lists] as any[] | undefined;
    if (lists) {
      for (const list of lists) {
        if (!list.playerNames) list.playerNames = [];
      }
    }

    dump.__meta.dbVersion = 2;
    return dump;
  },
};

const migration2: Migration = {
  schema: (db, tx) => {
    if (!db.objectStoreNames.contains(STORES.directoryHandles)) {
      // no keyPath: we'll use arbitrary string keys (e.g. 'replayDir')
      db.createObjectStore(STORES.directoryHandles);
    }
  },
};


const upgrades: Record<number, Migration> = {
  0: migration0, // initial schema
  1: migration1, // uploaded boolean → number, can modify other stores too
  2: migration2, // new store for directory handles
};