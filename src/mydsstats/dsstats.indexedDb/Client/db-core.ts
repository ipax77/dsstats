import { Dump, Migration } from "./migration.js";

export const DB_NAME = "ReplayDB";
export const DB_VERSION = 9;

export const STORES = {
    replays: "Replays",
    lists: "ReplayLists",
    meta: "ReplayMeta",
    config: "Config",
    directoryHandles: "DirectoryHandles",
    ratings: "ReplayRatings",
    spawnPlayback: "ReplaySpawnPlayback",
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
        if (!listsStore.indexNames.contains("duration")) {
            listsStore.createIndex("duration", "duration", { unique: false });
        }
        if (!listsStore.indexNames.contains("playerCount")) {
            listsStore.createIndex("playerCount", "playerCount", { unique: false });
        }
        if (!listsStore.indexNames.contains("tournamentEdition")) {
            listsStore.createIndex("tournamentEdition", "tournamentEdition", { unique: false });
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


// migration3: fix boolean 'uploaded' values that migration1 missed in live DBs.
// migration1 only ran via the dump/restore path; existing live records may still
// have uploaded: true/false. Convert them to 1/0 so getAllReplayMetas() can be
// deserialized as number (int) in C#.
const migration3: Migration = {
  schema: (_db, tx) => {
    const metaStore = tx.objectStore(STORES.meta);
    const cursorReq = metaStore.openCursor();
    cursorReq.onsuccess = (e) => {
      const cursor = (e.target as IDBRequest<IDBCursorWithValue>).result;
      if (!cursor) return;
      const record = cursor.value;
      if (typeof record.uploaded === "boolean") {
        record.uploaded = record.uploaded ? 1 : 0;
        cursor.update(record);
      }
      cursor.continue();
    };
  },
};

// migration4: migrate DirectoryHandles from {key: humanName, value: FileSystemDirectoryHandle}
// to {key: UUID, value: {handle, displayName, regionId}} for unique, user-editable handle names.
const migration4: Migration = {
  schema: (_db, tx) => {
    const store = tx.objectStore(STORES.directoryHandles);
    const req = store.openCursor();
    req.onsuccess = (e) => {
      const cursor = (e.target as IDBRequest<IDBCursorWithValue>).result;
      if (!cursor) return;
      const oldKey = cursor.key as string;
      const oldValue = cursor.value;
      // Skip if already migrated (value has a .handle property)
      if (oldValue && typeof oldValue === "object" && "handle" in oldValue) {
        cursor.continue();
        return;
      }
      // Extract regionId from key pattern like "Multiplayer_2" or "Multiplayer_2_3"
      let regionId = 0;
      const match = (oldKey as string).match(/_(\d)(?:_\d+)?$/);
      if (match) regionId = parseInt(match[1], 10);
      const uuid = crypto.randomUUID();
      store.put({ handle: oldValue, displayName: oldKey, regionId }, uuid);
      cursor.delete();
      cursor.continue();
    };
  },
};

// migration5: add ReplayRatings store for persisting server-fetched Elo ratings per replay.
const migration5: Migration = {
  schema: (db) => {
    if (!db.objectStoreNames.contains(STORES.ratings)) {
      db.createObjectStore(STORES.ratings, { keyPath: "replayHash" });
    }
  },
};

const migration6: Migration = {
  schema: (_db, tx) => {
    const store = tx.objectStore(STORES.directoryHandles);
    const req = store.openCursor();

    req.onsuccess = (e) => {
      const cursor = (e.target as IDBRequest<IDBCursorWithValue>).result;
      if (!cursor) return;

      const value = cursor.value;

      // Skip if already migrated
      if (value && typeof value === "object" && "status" in value) {
        cursor.continue();
        return;
      }

      const migrated = {
        ...value,
        handle: value.handle ?? null,   // keep existing handle
        fingerprint: value.fingerprint ?? null,
        status: "bound",                // existing entries are valid
        lastBoundAt: Date.now()
      };

      cursor.update(migrated);
      cursor.continue();
    };
  },
};

const migration7: Migration = {
  schema: (_db, tx) => {
    const listsStore = tx.objectStore(STORES.lists);
    if (!listsStore.indexNames.contains("duration")) {
      listsStore.createIndex("duration", "duration", { unique: false });
    }
    if (!listsStore.indexNames.contains("playerCount")) {
      listsStore.createIndex("playerCount", "playerCount", { unique: false });
    }
    if (!listsStore.indexNames.contains("tournamentEdition")) {
      listsStore.createIndex("tournamentEdition", "tournamentEdition", { unique: false });
    }

    backfillReplayListProjections(listsStore, tx.objectStore(STORES.replays));
  },
  data: (dump) => {
    backfillReplayListProjectionsInDump(dump);
    dump.__meta.dbVersion = 8;
    return dump;
  },
};

const migration8: Migration = {
  schema: (db) => {
    if (!db.objectStoreNames.contains(STORES.spawnPlayback)) {
      db.createObjectStore(STORES.spawnPlayback, { keyPath: "replayHash" });
    }
  },
};

const upgrades: Record<number, Migration> = {
  0: migration0, // initial schema
  1: migration1, // uploaded boolean → number (dump/restore path only)
  2: migration2, // new store for directory handles
  3: migration3, // fix boolean uploaded → number in live DB records
  4: migration4, // DirectoryHandles: humanName → UUID key, value wraps handle+displayName+regionId
  5: migration5, // new ReplayRatings store
  6: migration6, // add directory fingerprints
  7: migration7, // replay list query indexes and filter projections
  8: migration8, // compressed spawn playback sidecars
};

function backfillReplayListProjections(listsStore: IDBObjectStore, replaysStore: IDBObjectStore): void {
  const req = listsStore.openCursor();
  req.onsuccess = (e) => {
    const cursor = (e.target as IDBRequest<IDBCursorWithValue>).result;
    if (!cursor) return;

    const list = cursor.value;
    if (list.playerCount !== undefined && list.tournamentEdition !== undefined && hasReplayListGameMode(list) && list.duration !== undefined) {
      cursor.continue();
      return;
    }

    const replayReq = replaysStore.get(list.replayHash);
    replayReq.onsuccess = () => {
      const replay = replayReq.result;
      if (replay) {
        list.playerCount = replay.players?.length ?? list.playerNames?.length ?? 0;
        list.tournamentEdition = typeof replay.title === "string" && replay.title.endsWith("TE");
        if (!hasReplayListGameMode(list)) {
          list.gameMode = replay.gameMode;
        }
        list.duration = list.duration ?? replay.duration;
      } else {
        list.playerCount = list.playerCount ?? list.playerNames?.length ?? 0;
        list.tournamentEdition = list.tournamentEdition ?? false;
      }

      if (list.playerCount !== undefined || list.tournamentEdition !== undefined || hasReplayListGameMode(list) || list.duration !== undefined) {
        cursor.update(list);
      }
      cursor.continue();
    };
    replayReq.onerror = () => cursor.continue();
  };
}

function backfillReplayListProjectionsInDump(dump: Dump): void {
  const lists = dump.stores[STORES.lists] as any[] | undefined;
  const replays = dump.stores[STORES.replays] as any[] | undefined;
  if (!lists || !replays) {
    return;
  }

  const replayByHash = new Map<string, any>();
  for (const replay of replays) {
    if (replay?.replayHash) {
      replayByHash.set(replay.replayHash, replay);
    }
  }

  for (const list of lists) {
    const replay = replayByHash.get(list.replayHash);
    if (replay) {
      list.playerCount = replay.players?.length ?? list.playerNames?.length ?? 0;
      list.tournamentEdition = typeof replay.title === "string" && replay.title.endsWith("TE");
      list.duration = list.duration ?? replay.duration;
    } else {
      list.playerCount = list.playerCount ?? list.playerNames?.length ?? 0;
      list.tournamentEdition = list.tournamentEdition ?? false;
    }

    if (hasReplayListGameMode(list)) {
      continue;
    }

    if (replay?.gameMode && replay.gameMode !== 0) {
      list.gameMode = replay.gameMode;
    }
  }
}

function hasReplayListGameMode(list: { gameMode?: number }): boolean {
  return list.gameMode !== undefined && list.gameMode !== 0;
}
