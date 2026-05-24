// dsstatsDb.ts v1.5
import { openDB, STORES } from "./db-core";
import { ExportedReplays, FileInfoRecord, PlayerDto, ProfileCandidateDto, PwaConfig, ReplayDto, ReplayFilter, ReplayListDto, ReplayMeta, TrackedProfileDto, UploadRequestDto, ExportResult, ReplayRatingDto, SessionWindowSettingsDto, TableOrder, RequestNames, SpawnPlaybackExportDto } from "./dtos";
import { getReplaysFromFolder, readFileContentStream } from "./pick-replays";
import { exportBackup, importBackup } from "./backup";
import { MyPlayerStats } from "./stats/stats-dto";
import { StatsService } from "./stats/stats";
import { addDirectoryHandle, deleteDirectoryHandle, getAllDirectoryHandleEntries, getAllDirectoryHandles, getDirectoryHandle, getDirectoryHandleFromUser, renameDirectoryHandle as renameDirHandle, verifyAllDirectoryPermissions as verifyAllDirPerms } from "./file-handle-repository";
import { replayListMatchesDetailProjection, replayListNeedsFullDetailCheck, replayMatchesDetailFilter } from "./replay-detail-filter";

const CONFIG_KEYS = {
    app: "app",
    trackedProfiles: "trackedProfiles",
    sessionWindowSettings: "sessionWindowSettings",
    inHouseSession: "inHouseSession"
} as const;

const QUERY_CACHE_LIMIT = 8;
const queryCache = new Map<string, string[]>();

type UploadReplaySelection = {
    hash: string;
    replay: ReplayDto;
    spawnPlayback?: Uint8Array;
};

// Save replay and its projection + meta in one transaction
export async function saveReplayFull(
    replayHash: string,
    replay: ReplayDto,
    list: ReplayListDto,
    meta: ReplayMeta,
    spawnPlaybackPayload?: unknown
): Promise<void> {
    const database = await openDB();
    const normalizedSpawnPlaybackPayload = normalizeByteArray(spawnPlaybackPayload);
    const preparedSpawnPlaybackPayload = await prepareSpawnPlaybackPayload(replay, normalizedSpawnPlaybackPayload);
    const hasSpawnPlayback = !!preparedSpawnPlaybackPayload && preparedSpawnPlaybackPayload.length > 0;

    if (hasSpawnPlayback) {
        console.debug(`saveReplayFull: storing spawn playback sidecar for ${replayHash} (${preparedSpawnPlaybackPayload!.length} bytes).`, {
            compression: replay.spawnPlayback?.compression,
            payloadType: getPayloadType(spawnPlaybackPayload),
        });
    }

    return new Promise((resolve, reject) => {
        const storeNames = hasSpawnPlayback
            ? [STORES.replays, STORES.lists, STORES.meta, STORES.spawnPlayback]
            : [STORES.replays, STORES.lists, STORES.meta];
        const tx = database.transaction(storeNames, "readwrite");

        const replays = tx.objectStore(STORES.replays);
        const lists = tx.objectStore(STORES.lists);
        const metas = tx.objectStore(STORES.meta);

        replays.put({ ...replay, replayHash: replayHash, gametime: replay.gametime });
        lists.put({ ...list, gametime: list.gametime });
        metas.put(meta);
        if (hasSpawnPlayback) {
            tx.objectStore(STORES.spawnPlayback).put({ replayHash, payload: preparedSpawnPlaybackPayload });
        }

        tx.oncomplete = () => {
            clearReplayQueryCache();
            resolve();
        };
        tx.onerror = () => reject(tx.error);
    });
}

// Query for overview table
export async function getAllReplayLists(): Promise<ReplayListDto[]> {
    const database = await openDB();

    return new Promise((resolve, reject) => {
        const tx = database.transaction(STORES.lists, "readonly");
        const store = tx.objectStore(STORES.lists);

        const request = store.getAll();

        request.onsuccess = () => {
            const results = request.result.map((r: ReplayListDto) => ({
                ...r,
                gameTime: r.gametime,
            }));
            resolve(results);
        };

        request.onerror = () => reject(request.error);
    });
}

export async function getAllReplayMatas(): Promise<ReplayMeta[]> {
    const database = await openDB();

    return new Promise((resolve, reject) => {
        const tx = database.transaction(STORES.meta, "readonly");
        const store = tx.objectStore(STORES.meta);

        const request = store.getAll();

        request.onsuccess = () => {
            resolve(request.result);
        };

        request.onerror = () => reject(request.error);
    });
}

/**
 * Get a full ReplayDto by replayHash
 */
export async function getReplayByHash(hash: string): Promise<ReplayDto | undefined> {
    const database = await openDB();

    return new Promise((resolve, reject) => {
        const tx = database.transaction(STORES.replays, "readonly");
        const store = tx.objectStore(STORES.replays);

        const request = store.get(hash);

        request.onsuccess = () => {
            const result = request.result as ReplayDto | undefined;
            if (result) {
                result.gametime = new Date(result.gametime).toISOString();
            }
            resolve(result);
        };

        request.onerror = () => reject(request.error);
    });
}

export async function saveReplaySpawnPlayback(replayHash: string, payload: Uint8Array): Promise<void> {
    const database = await openDB();

    return new Promise((resolve, reject) => {
        const tx = database.transaction(STORES.spawnPlayback, "readwrite");
        tx.objectStore(STORES.spawnPlayback).put({ replayHash, payload });
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
}

export async function getReplaySpawnPlayback(replayHash: string): Promise<Uint8Array | undefined> {
    const database = await openDB();

    return new Promise((resolve, reject) => {
        const tx = database.transaction(STORES.spawnPlayback, "readonly");
        const request = tx.objectStore(STORES.spawnPlayback).get(replayHash);
        request.onsuccess = () => resolve(request.result?.payload as Uint8Array | undefined);
        request.onerror = () => reject(request.error);
    });
}

export async function getStorageEstimate(): Promise<{ usage: number; quota: number } | undefined> {
    if (!navigator.storage?.estimate) {
        return undefined;
    }

    const estimate = await navigator.storage.estimate();
    return {
        usage: estimate.usage ?? 0,
        quota: estimate.quota ?? 0,
    };
}

/**
 * Get the latest N unuploaded replays and return their hashes + gzipped data.
 */
export async function exportUnuploadedReplays(limit: number = 1000): Promise<ExportedReplays> {
    const database = await openDB();

    const selected = await getLatestUnuploadedReplays(database, limit, false);
    if (selected.length === 0) {
        return { hashes: [], payload: gzipString("[]") };
    }

    const hashes = selected.map((x) => x.hash);
    const replays = selected.map((x) => x.replay);
    const payload = gzipString(JSON.stringify(replays));

    return { hashes, payload };
}

/**
 * Get the latest N unuploaded replays and return their hashes + gzipped data.
 */
export async function exportUnuploadedReplays10(uploadRequest: UploadRequestDto, limit: number = 250): Promise<ExportResult> {
    const database = await openDB();

    try {
        const selected = await getLatestUnuploadedReplays(database, limit, true);

        if (selected.length === 0) {
            return { hashes: [], payload: new Uint8Array(0), sidecars: [] };
        }

        const hashes = selected.map((x) => x.hash);
        // Create a plain JS object from the .NET proxy object to avoid stack overflow issues
        // when stringifying an object that contains a .NET proxy.
        const plainUploadRequest = JSON.parse(JSON.stringify(uploadRequest));
        const requestNames = (plainUploadRequest.requestNames ?? []) as RequestNames[];
        const uploaderToonKeys = new Set(
            requestNames
                .filter(requestName => requestName.toonId > 0)
                .map(requestName => requestToonKey(requestName))
        );
        const replays = selected.map((x) => createUploadReplay(x.replay, uploaderToonKeys));

        const request: UploadRequestDto = {
            ...plainUploadRequest,
            requestNames,
            replays
        };

        const payload = gzipStringRaw(JSON.stringify(request));
        const sidecars = createSpawnPlaybackExports(selected);

        return { hashes, payload, sidecars };
    } catch (err) {
        console.error("exportUnuploadedReplays10 failed:", err);
        throw err;
    }
}

async function getLatestUnuploadedReplays(
    database: IDBDatabase,
    limit: number,
    includeSpawnPlayback: boolean
): Promise<UploadReplaySelection[]> {
    if (limit <= 0) {
        return [];
    }

    return new Promise((resolve, reject) => {
        const storeNames = includeSpawnPlayback
            ? [STORES.lists, STORES.meta, STORES.replays, STORES.spawnPlayback]
            : [STORES.lists, STORES.meta, STORES.replays];
        const tx = database.transaction(storeNames, "readonly");
        const listIndex = tx.objectStore(STORES.lists).index("gametime");
        const metaStore = tx.objectStore(STORES.meta);
        const replayStore = tx.objectStore(STORES.replays);
        const spawnPlaybackStore = includeSpawnPlayback
            ? tx.objectStore(STORES.spawnPlayback)
            : undefined;
        const selected: UploadReplaySelection[] = [];

        const cursorRequest = listIndex.openCursor(null, "prev");
        cursorRequest.onsuccess = (event) => {
            const cursor = (event.target as IDBRequest<IDBCursorWithValue>).result;
            if (!cursor || selected.length >= limit) {
                resolve(selected);
                return;
            }

            const list = cursor.value as ReplayListDto;
            const hash = list.replayHash;
            const metaRequest = metaStore.get(hash);

            metaRequest.onsuccess = () => {
                const meta = metaRequest.result as ReplayMeta | undefined;
                if (!meta || meta.uploaded !== 0) {
                    cursor.continue();
                    return;
                }

                const replayRequest = replayStore.get(hash);
                replayRequest.onsuccess = () => {
                    const replay = replayRequest.result as ReplayDto | undefined;
                    if (!replay) {
                        cursor.continue();
                        return;
                    }

                    if (!includeSpawnPlayback || !replay.spawnPlayback?.available || !spawnPlaybackStore) {
                        selected.push({ hash, replay });
                        cursor.continue();
                        return;
                    }

                    const sidecarRequest = spawnPlaybackStore.get(hash);
                    sidecarRequest.onsuccess = () => {
                        selected.push({
                            hash,
                            replay,
                            spawnPlayback: normalizeByteArray(sidecarRequest.result?.payload),
                        });
                        cursor.continue();
                    };
                    sidecarRequest.onerror = () => reject(sidecarRequest.error);
                };
                replayRequest.onerror = () => reject(replayRequest.error);
            };
            metaRequest.onerror = () => reject(metaRequest.error);
        };

        cursorRequest.onerror = () => reject(cursorRequest.error);
        tx.onerror = () => reject(tx.error);
    });
}

function createSpawnPlaybackExports(
    selected: { hash: string; replay: ReplayDto; spawnPlayback?: Uint8Array }[]
): SpawnPlaybackExportDto[] {
    const sidecars: SpawnPlaybackExportDto[] = [];
    for (const entry of selected) {
        const info = entry.replay.spawnPlayback;
        const payload = entry.spawnPlayback;
        if (!info?.available || !payload || payload.length === 0) {
            continue;
        }

        sidecars.push({
            replayHash: entry.hash,
            partName: `sidecar-${sidecars.length}`,
            payload,
            formatVersion: info.formatVersion,
            compression: info.compression ?? 1,
            compressedLength: info.compressedLength,
            uncompressedLength: info.uncompressedLength,
            unitCount: info.unitCount,
        });
    }

    return sidecars;
}

async function prepareSpawnPlaybackPayload(replay: ReplayDto, payload: Uint8Array | undefined): Promise<Uint8Array | undefined> {
    if (!payload || payload.length === 0) {
        replay.spawnPlayback = undefined;
        return undefined;
    }

    const info = replay.spawnPlayback;
    if (info?.compression === 1 && isRawSpawnPlaybackPayload(payload)) {
        try {
            const { compressSpawnPlaybackPayload } = await import("./spawn-playback-compression");
            const compressed = await compressSpawnPlaybackPayload(payload);
            replay.spawnPlayback = {
                ...info,
                compressedLength: compressed.length,
                uncompressedLength: payload.length,
            };
            return compressed;
        } catch (error) {
            console.warn("Failed to Brotli-compress spawn playback sidecar payload:", error);
            replay.spawnPlayback = undefined;
            return undefined;
        }
    }

    return payload;
}

function isRawSpawnPlaybackPayload(payload: Uint8Array): boolean {
    return payload.length >= 4
        && payload[0] === 0x44
        && payload[1] === 0x53
        && payload[2] === 0x50
        && payload[3] === 0x42;
}

function normalizeByteArray(value: unknown): Uint8Array | undefined {
    if (value instanceof Uint8Array) {
        return value;
    }
    if (ArrayBuffer.isView(value)) {
        return new Uint8Array(value.buffer, value.byteOffset, value.byteLength);
    }
    if (value instanceof ArrayBuffer) {
        return new Uint8Array(value);
    }
    if (Array.isArray(value)) {
        return new Uint8Array(value);
    }
    if (typeof value === "string" && value.length > 0) {
        const binary = atob(value);
        const bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }
        return bytes;
    }

    return undefined;
}

function getPayloadType(value: unknown): string {
    if (value === undefined) {
        return "undefined";
    }
    if (value === null) {
        return "null";
    }
    if (ArrayBuffer.isView(value)) {
        return value.constructor.name;
    }
    if (value instanceof ArrayBuffer) {
        return "ArrayBuffer";
    }
    if (Array.isArray(value)) {
        return "Array";
    }
    return typeof value;
}

export async function markReplaysAsUploaded(hashes: string[]): Promise<void> {
    const database = await openDB();
    const tx = database.transaction(STORES.meta, "readwrite");
    const store = tx.objectStore(STORES.meta);

    await Promise.all(
        hashes.map(
            (hash) =>
                new Promise<void>((res, rej) => {
                    const getReq = store.get(hash);
                    getReq.onsuccess = () => {
                        const record = getReq.result;
                        if (record) {
                            record.uploaded = 1;
                            const putReq = store.put(record);
                            putReq.onsuccess = () => res();
                            putReq.onerror = () => rej(putReq.error);
                        } else {
                            res(); // nothing to update
                        }
                    };
                    getReq.onerror = () => rej(getReq.error);
                })
        )
    );
}

export async function pickDirectoryInit(
    startName: string,
    dirKey?: string,
    count: number = 100
): Promise<FileInfoRecord[]> {
    const metas = await getAllReplayMatas();

    let dirHandle: FileSystemDirectoryHandle | null = null;
    let rootKey: string | undefined = undefined;

    if (dirKey) {
        const entry = await getDirectoryHandle(dirKey);
        if (entry) {
            dirHandle = entry.handle;
            rootKey = dirKey;
        }
    }

    return await getReplaysFromFolder(startName, [], count, dirHandle, rootKey, metas);
}

export async function pickDirectoryHandle(startName: string): Promise<string | null> {
    const dirHandle = await getDirectoryHandleFromUser();
    if (!dirHandle) {
        return null;
    }

    return await addDirectoryHandle(dirHandle, startName, undefined);
}

export async function getFileContentStream(path: string) {
    return await readFileContentStream(path);
}

export async function verifyAllDirectoryPermissions(keys: string[]): Promise<string[]> {
    return await verifyAllDirPerms(keys);
}

export async function getConfig(): Promise<PwaConfig | undefined> {
    return await getConfigEntry<PwaConfig>(CONFIG_KEYS.app);
}

export async function saveConfig(
    config: PwaConfig,
): Promise<void> {
    await saveConfigEntry(CONFIG_KEYS.app, config);
}

export async function getTrackedProfiles(): Promise<TrackedProfileDto[]> {
    return await getConfigEntry<TrackedProfileDto[]>(CONFIG_KEYS.trackedProfiles) ?? [];
}

export async function saveTrackedProfiles(
    profiles: TrackedProfileDto[],
): Promise<void> {
    await saveConfigEntry(CONFIG_KEYS.trackedProfiles, profiles);
}

export async function getSessionWindowSettings(): Promise<SessionWindowSettingsDto | undefined> {
    return await getConfigEntry<SessionWindowSettingsDto>(CONFIG_KEYS.sessionWindowSettings);
}

export async function saveSessionWindowSettings(
    settings: SessionWindowSettingsDto,
): Promise<void> {
    await saveConfigEntry(CONFIG_KEYS.sessionWindowSettings, settings);
}

export async function getInHouseSession<T>(): Promise<T | undefined> {
    return await getConfigEntry<T>(CONFIG_KEYS.inHouseSession);
}

export async function saveInHouseSession<T>(
    session: T,
): Promise<void> {
    await saveConfigEntry(CONFIG_KEYS.inHouseSession, session);
}

export async function clearInHouseSession(): Promise<void> {
    const database = await openDB();

    return new Promise((resolve, reject) => {
        const tx = database.transaction(STORES.config, "readwrite");
        const store = tx.objectStore(STORES.config);
        store.delete(CONFIG_KEYS.inHouseSession);

        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
}

async function getConfigEntry<T>(key: string): Promise<T | undefined> {
    const database = await openDB();

    return new Promise((resolve, reject) => {
        const tx = database.transaction(STORES.config, "readonly");
        const store = tx.objectStore(STORES.config);
        const request = store.get(key);

        request.onsuccess = () => {
            resolve(request.result as T | undefined);
        };

        request.onerror = () => reject(request.error);
    });
}

async function saveConfigEntry<T>(key: string, value: T): Promise<void> {
    const database = await openDB();

    return new Promise((resolve, reject) => {
        const tx = database.transaction(STORES.config, "readwrite");
        const store = tx.objectStore(STORES.config);

        store.put(value, key);

        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
}

async function getMatchingReplayHashes(filter: ReplayFilter): Promise<string[]> {
    const cacheKey = getReplayQueryCacheKey(filter);
    const cached = queryCache.get(cacheKey);
    if (cached) {
        queryCache.delete(cacheKey);
        queryCache.set(cacheKey, cached);
        return cached;
    }

    const database = await openDB();
    const orders = getReplayListOrders(filter);
    const hashes = canUseIndexedOrder(orders)
        ? await getMatchingReplayHashesByCursor(database, filter, orders[0])
        : await getMatchingReplayHashesBySort(database, filter, orders);

    setReplayQueryCache(cacheKey, hashes);
    return hashes;
}

async function getMatchingReplayHashesByCursor(database: IDBDatabase, filter: ReplayFilter, order: TableOrder): Promise<string[]> {
    return new Promise((resolve, reject) => {
        const tx = database.transaction([STORES.lists, STORES.replays], "readonly");
        const listStore = tx.objectStore(STORES.lists);
        const replayStore = tx.objectStore(STORES.replays);
        const source = listStore.index(order.name);
        const direction: IDBCursorDirection = order.ascending ? "next" : "prev";
        const request = source.openCursor(null, direction);
        const hashes: string[] = [];

        request.onsuccess = (event) => {
            const cursor = (event.target as IDBRequest<IDBCursorWithValue>).result;
            if (!cursor) {
                resolve(hashes);
                return;
            }

            const replayList = cursor.value as ReplayListDto;
            if (!replayListMatchesProjectionFilters(replayList, filter)) {
                cursor.continue();
                return;
            }

            if (!replayListNeedsFullDetailCheck(replayList, filter.detailFilter)) {
                hashes.push(replayList.replayHash);
                cursor.continue();
                return;
            }

            const replayRequest = replayStore.get(replayList.replayHash);
            replayRequest.onsuccess = () => {
                const replay = replayRequest.result as ReplayDto | undefined;
                if (replay && replayMatchesDetailFilter(replay, filter.detailFilter)) {
                    hashes.push(replayList.replayHash);
                }
                cursor.continue();
            };
            replayRequest.onerror = () => reject(replayRequest.error);
        };

        request.onerror = () => reject(request.error);
        tx.onerror = () => reject(tx.error);
    });
}

async function getMatchingReplayHashesBySort(database: IDBDatabase, filter: ReplayFilter, orders: TableOrder[]): Promise<string[]> {
    const replayLists = await getAllReplayListRecords(database);
    const matches: ReplayListDto[] = [];

    for (const replayList of replayLists) {
        if (!replayListMatchesProjectionFilters(replayList, filter)) {
            continue;
        }

        if (replayListNeedsFullDetailCheck(replayList, filter.detailFilter)) {
            const replay = await getReplayByHashFromDatabase(database, replayList.replayHash);
            if (!replay || !replayMatchesDetailFilter(replay, filter.detailFilter)) {
                continue;
            }
        }

        matches.push(replayList);
    }

    matches.sort((left, right) => compareReplayLists(left, right, orders));
    return matches.map(replayList => replayList.replayHash);
}

function replayListMatchesProjectionFilters(replayList: ReplayListDto, filter: ReplayFilter): boolean {
    if (!replayListMatchesDetailProjection(replayList, filter.detailFilter)) {
        return false;
    }

    const searchNames = getSearchNames(filter);
    const commanders = filter.commanders ?? [];
    const hasNameFilter = searchNames.length > 0;
    const hasCommandersFilter = commanders.length > 0;

    if (filter.linkCommanders && hasNameFilter && hasCommandersFilter) {
        const allCommanders = [...replayList.commandersTeam1, ...replayList.commandersTeam2];
        const lowerCasePlayerNames = (replayList.playerNames ?? []).map(name => name.toLowerCase());

        return lowerCasePlayerNames.some((playerName, playerIndex) => {
            const nameMatch = searchNames.every(searchName => playerName.includes(searchName));
            return nameMatch && commanders.includes(allCommanders[playerIndex]);
        });
    }

    if (hasNameFilter) {
        const lowerCasePlayerNames = (replayList.playerNames ?? []).map(name => name.toLowerCase());
        if (!searchNames.every(searchName => lowerCasePlayerNames.some(playerName => playerName.includes(searchName)))) {
            return false;
        }
    }

    if (hasCommandersFilter) {
        const allCommanders = [...replayList.commandersTeam1, ...replayList.commandersTeam2];
        if (!commanders.some(commander => allCommanders.includes(commander))) {
            return false;
        }
    }

    return true;
}

async function getAllReplayListRecords(database: IDBDatabase): Promise<ReplayListDto[]> {
    return new Promise((resolve, reject) => {
        const tx = database.transaction(STORES.lists, "readonly");
        const store = tx.objectStore(STORES.lists);
        const request = store.getAll();
        request.onsuccess = () => resolve(request.result as ReplayListDto[]);
        request.onerror = () => reject(request.error);
    });
}

async function getReplayListsByHashes(hashes: string[]): Promise<ReplayListDto[]> {
    if (hashes.length === 0) {
        return [];
    }

    const database = await openDB();

    return new Promise((resolve, reject) => {
        const tx = database.transaction(STORES.lists, "readonly");
        const store = tx.objectStore(STORES.lists);

        const requests = hashes.map(hash => new Promise<ReplayListDto | undefined>((res, rej) => {
            const request = store.get(hash);
            request.onsuccess = () => res(request.result as ReplayListDto | undefined);
            request.onerror = () => rej(request.error);
        }));

        Promise.all(requests)
            .then(replayLists => resolve(replayLists.filter((replayList): replayList is ReplayListDto => !!replayList)))
            .catch(reject);
    });
}

async function getReplayByHashFromDatabase(database: IDBDatabase, hash: string): Promise<ReplayDto | undefined> {
    return new Promise((resolve, reject) => {
        const tx = database.transaction(STORES.replays, "readonly");
        const store = tx.objectStore(STORES.replays);
        const request = store.get(hash);
        request.onsuccess = () => resolve(request.result as ReplayDto | undefined);
        request.onerror = () => reject(request.error);
    });
}

function getReplayListOrders(filter: ReplayFilter): TableOrder[] {
    const orders = (filter.tableOrders?.length ?? 0) > 0
        ? filter.tableOrders!
        : [{ name: "gametime", ascending: false }];

    return orders.map(order => ({
        name: normalizeReplayListOrderName(order.name),
        ascending: order.ascending,
    }));
}

function canUseIndexedOrder(orders: TableOrder[]): boolean {
    return orders.length === 1 && ["gametime", "gameMode", "duration"].includes(orders[0].name);
}

function compareReplayLists(left: ReplayListDto, right: ReplayListDto, orders: TableOrder[]): number {
    for (const order of orders) {
        const leftValue = left[order.name as keyof ReplayListDto];
        const rightValue = right[order.name as keyof ReplayListDto];

        if (Array.isArray(leftValue) || Array.isArray(rightValue)) {
            continue;
        }

        const result = compareReplayListValues(leftValue, rightValue);
        if (result !== 0) {
            return order.ascending ? result : -result;
        }
    }

    return compareReplayListValues(right.gametime, left.gametime);
}

function compareReplayListValues(left: string | number | boolean | undefined, right: string | number | boolean | undefined): number {
    const leftValue = left ?? 0;
    const rightValue = right ?? 0;

    if (leftValue < rightValue) {
        return -1;
    }

    if (leftValue > rightValue) {
        return 1;
    }

    return 0;
}

function getSearchNames(filter: ReplayFilter): string[] {
    return filter.name?.toLowerCase().split(" ").map(name => name.trim()).filter(Boolean) ?? [];
}

function normalizeReplayListOrderName(name: string): string {
    switch (name) {
        case "GameTime":
            return "gametime";
        case "GameMode":
            return "gameMode";
        case "Duration":
            return "duration";
        case "WinnerTeam":
            return "winnerTeam";
        case "Exp2Win":
            return "exp2Win";
        case "AvgRating":
            return "avgRating";
        case "LeaverType":
            return "leaverType";
        case "PlayerPos":
            return "playerPos";
        default:
            return name;
    }
}

function getReplayQueryCacheKey(filter: ReplayFilter): string {
    return JSON.stringify({
        name: filter.name ?? "",
        commanders: filter.commanders ?? [],
        linkCommanders: filter.linkCommanders ?? false,
        tableOrders: getReplayListOrders(filter),
        detailFilter: filter.detailFilter ?? null,
    });
}

function setReplayQueryCache(key: string, hashes: string[]): void {
    queryCache.set(key, hashes);
    while (queryCache.size > QUERY_CACHE_LIMIT) {
        const oldestKey = queryCache.keys().next().value;
        if (oldestKey === undefined) {
            break;
        }
        queryCache.delete(oldestKey);
    }
}

function clearReplayQueryCache(): void {
    queryCache.clear();
}

export async function getFilteredReplayLists(
    filter: ReplayFilter
): Promise<ReplayListDto[]> {
    const hashes = await getMatchingReplayHashes(filter);
    const skip = Math.max(filter.skip ?? 0, 0);
    const take = filter.take ?? hashes.length;
    const pageHashes = take <= 0 ? [] : hashes.slice(skip, skip + take);
    return await getReplayListsByHashes(pageHashes);
}

export async function getFilteredReplayListsCount(filter: ReplayFilter): Promise<number> {
    const hashes = await getMatchingReplayHashes(filter);
    return hashes.length;
}

export async function downloadBackup() {
    await exportBackup();
}

export async function uploadBackup() {
    await importBackup();
    clearReplayQueryCache();
}

export function gzipString(content: string): string {
    const binary = pako.gzip(content);
    return btoa(String.fromCharCode(...binary));
}

export function gzipStringRaw(content: string): Uint8Array {
    return pako.gzip(content);
}

export function ungzipString(base64: string): string {
    const binary = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
    const text = pako.ungzip(binary, { to: "string" });
    return text;
}

export async function saveReplayRating(replayHash: string, rating: ReplayRatingDto): Promise<void> {
    const database = await openDB();
    return new Promise((resolve, reject) => {
        const tx = database.transaction(STORES.ratings, "readwrite");
        const store = tx.objectStore(STORES.ratings);
        store.put({ ...rating, replayHash });
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
}

export async function getReplayRating(replayHash: string): Promise<ReplayRatingDto | undefined> {
    const database = await openDB();
    return new Promise((resolve, reject) => {
        const tx = database.transaction(STORES.ratings, "readonly");
        const store = tx.objectStore(STORES.ratings);
        const request = store.get(replayHash);
        request.onsuccess = () => resolve(request.result as ReplayRatingDto | undefined);
        request.onerror = () => reject(request.error);
    });
}

export async function getPlayerStats(player: PlayerDto): Promise<MyPlayerStats> {
    const statsService = new StatsService();
    return await statsService.generateStats(player);
}

export async function detectTrackedProfileCandidates(limit: number = 10): Promise<ProfileCandidateDto[]> {
    const database = await openDB();

    return new Promise((resolve, reject) => {
        const tx = database.transaction(STORES.replays, "readonly");
        const store = tx.objectStore(STORES.replays);
        const index = store.index("gametime");
        const request = index.openCursor(null, "prev");
        const counts = new Map<string, ProfileCandidateDto>();
        let replaysVisited = 0;

        request.onsuccess = (event) => {
            const cursor = (event.target as IDBRequest<IDBCursorWithValue>).result;
            if (!cursor || replaysVisited >= limit) {
                const candidates = Array.from(counts.values())
                    .sort((left, right) => right.count - left.count || left.name.localeCompare(right.name));
                resolve(candidates);
                return;
            }

            const replay = cursor.value as ReplayDto;
            replaysVisited += 1;

            for (const replayPlayer of replay.players) {
                const toonId = replayPlayer.player?.toonId;
                if (!toonId || toonId.id <= 0) {
                    continue;
                }

                const key = toonKey(toonId);
                const existing = counts.get(key);

                if (existing) {
                    existing.count += 1;
                    continue;
                }

                counts.set(key, {
                    count: 1,
                    name: replayPlayer.player?.name || replayPlayer.name,
                    toonId: { ...toonId }
                });
            }

            cursor.continue();
        };

        request.onerror = () => reject(request.error);
    });
}

export async function getRecentReplayHashes(limit: number = 10): Promise<string[]> {
    const database = await openDB();

    return new Promise((resolve, reject) => {
        const tx = database.transaction(STORES.lists, "readonly");
        const store = tx.objectStore(STORES.lists);
        const index = store.index("gametime");
        const request = index.openCursor(null, "prev");
        const hashes: string[] = [];

        request.onsuccess = (event) => {
            const cursor = (event.target as IDBRequest<IDBCursorWithValue>).result;
            if (!cursor || hashes.length >= limit) {
                resolve(hashes);
                return;
            }

            const replay = cursor.value as ReplayListDto;
            hashes.push(replay.replayHash);
            cursor.continue();
        };

        request.onerror = () => reject(request.error);
    });
}

export async function getReplayHashesSince(isoUtc: string): Promise<string[]> {
    const cutoff = new Date(isoUtc).getTime();
    const database = await openDB();

    return new Promise((resolve, reject) => {
        const tx = database.transaction(STORES.lists, "readonly");
        const store = tx.objectStore(STORES.lists);
        const index = store.index("gametime");
        const request = index.openCursor(null, "prev");
        const hashes: string[] = [];

        request.onsuccess = (event) => {
            const cursor = (event.target as IDBRequest<IDBCursorWithValue>).result;
            if (!cursor) {
                resolve(hashes);
                return;
            }

            const replay = cursor.value as ReplayListDto;
            if (new Date(replay.gametime).getTime() < cutoff) {
                resolve(hashes);
                return;
            }

            hashes.push(replay.replayHash);
            cursor.continue();
        };

        request.onerror = () => reject(request.error);
    });
}

export async function exportAllDirectoryHandles(): Promise<string[]> {
    return await getAllDirectoryHandles();
}

export async function exportAllDirectoryHandleEntries(): Promise<{ key: string; displayName: string; }[]> {
    return await getAllDirectoryHandleEntries();
}

export async function renameDirectoryHandle(key: string, newDisplayName: string): Promise<void> {
    await renameDirHandle(key, newDisplayName);
}

export async function delDirectoryHandle(key: string): Promise<boolean> {
    return await deleteDirectoryHandle(key);
}

function toonKey(toonId: { region: number; realm: number; id: number }): string {
    return `${toonId.region}:${toonId.realm}:${toonId.id}`;
}

function requestToonKey(requestName: RequestNames): string {
    return `${requestName.regionId}:${requestName.realmId}:${requestName.toonId}`;
}

function createUploadReplay(replay: ReplayDto, uploaderToonKeys: Set<string>): ReplayDto {
    const uploadReplay = JSON.parse(JSON.stringify(replay)) as ReplayDto;

    for (const replayPlayer of uploadReplay.players) {
        const playerToonId = replayPlayer.player?.toonId;
        if (playerToonId && uploaderToonKeys.has(toonKey(playerToonId))) {
            replayPlayer.isUploader = true;
        }
    }

    return uploadReplay;
}
