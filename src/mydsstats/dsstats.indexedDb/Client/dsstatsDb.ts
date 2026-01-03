// dsstatsDb.ts v1.4
import { openDB, STORES } from "./db-core";
import { ExportedReplays, FileInfoRecord, PlayerDto, PwaConfig, ReplayDto, ReplayFilter, ReplayListDto, ReplayMeta, UploadRequestDto, ExportResult } from "./dtos";
import { getReplaysFromFolder, readFileContentStream } from "./pick-replays";
import { exportBackup, importBackup } from "./backup";
import { MyPlayerStats } from "./stats/stats-dto";
import { StatsService } from "./stats/stats";
import { deleteDirectoryHandle, getAllDirectoryHandles, getDirectoryHandle } from "./file-handle-repository";

// Save replay and its projection + meta in one transaction
export async function saveReplayFull(
    replayHash: string,
    replay: ReplayDto,
    list: ReplayListDto,
    meta: ReplayMeta
): Promise<void> {
    const database = await openDB();

    return new Promise((resolve, reject) => {
        const tx = database.transaction([STORES.replays, STORES.lists, STORES.meta], "readwrite");

        const replays = tx.objectStore(STORES.replays);
        const lists = tx.objectStore(STORES.lists);
        const metas = tx.objectStore(STORES.meta);

        replays.put({ ...replay, replayHash: replayHash, gametime: replay.gametime });
        lists.put({ ...list, gametime: list.gametime });
        metas.put(meta);

        tx.oncomplete = () => resolve();
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

/**
 * Get the latest N unuploaded replays and return their hashes + gzipped data.
 */
export async function exportUnuploadedReplays(limit: number = 1000): Promise<ExportedReplays> {
    const database = await openDB();

    return new Promise((resolve, reject) => {
        const metaTx = database.transaction(STORES.meta, "readonly");
        const metaStore = metaTx.objectStore(STORES.meta);
        const uploadedIndex = metaStore.index("uploaded");

        // Query all with uploaded == false
        const req = uploadedIndex.getAll(IDBKeyRange.only(0));

        req.onsuccess = async () => {
            const metas = req.result as { replayHash: string }[];

            if (metas.length === 0) {
                resolve({ hashes: [], payload: gzipString("[]") });
                return;
            }

            // Fetch full replays
            const replayTx = database.transaction(STORES.replays, "readonly");
            const replayStore = replayTx.objectStore(STORES.replays);

            const replayPromises = metas.map(
                (m) =>
                    new Promise<{ hash: string; replay?: ReplayDto }>((res, rej) => {
                        const r = replayStore.get(m.replayHash);
                        r.onsuccess = () => res({ hash: m.replayHash, replay: r.result as ReplayDto | undefined });
                        r.onerror = () => rej(r.error);
                    })
            );

            try {
                const all = await Promise.all(replayPromises);

                // Filter out undefined
                const valid = all.filter((x) => !!x.replay) as { hash: string; replay: ReplayDto }[];

                // Sort by gametime desc
                valid.sort(
                    (a, b) =>
                        new Date(b.replay.gametime).getTime() -
                        new Date(a.replay.gametime).getTime()
                );

                // Limit
                const selected = valid.slice(0, limit);

                // Collect
                const hashes = selected.map((x) => x.hash);
                const replays = selected.map((x) => x.replay);

                // Compress
                const payload = gzipString(JSON.stringify(replays));

                resolve({ hashes, payload });
            } catch (err) {
                reject(err);
            }
        };

        req.onerror = () => reject(req.error);
    });
}

/**
 * Get the latest N unuploaded replays and return their hashes + gzipped data.
 */
export async function exportUnuploadedReplays10(uploadRequest: UploadRequestDto, limit: number = 250): Promise<ExportResult> {
    const database = await openDB();

    return new Promise((resolve, reject) => {
        const metaTx = database.transaction(STORES.meta, "readonly");
        const metaStore = metaTx.objectStore(STORES.meta);
        const uploadedIndex = metaStore.index("uploaded");

        // Query all with uploaded == false
        const req = uploadedIndex.getAll(IDBKeyRange.only(0));

        req.onsuccess = async () => {
            const metas = req.result as { replayHash: string }[];

            if (metas.length === 0) {
                resolve({ hashes: [], payload: new Uint8Array()});
                return;
            }

            // Fetch full replays
            const replayTx = database.transaction(STORES.replays, "readonly");
            const replayStore = replayTx.objectStore(STORES.replays);

            const replayPromises = metas.map(
                (m) =>
                    new Promise<{ hash: string; replay?: ReplayDto }>((res, rej) => {
                        const r = replayStore.get(m.replayHash);
                        r.onsuccess = () => res({ hash: m.replayHash, replay: r.result as ReplayDto | undefined });
                        r.onerror = () => rej(r.error);
                    })
            );

            try {
                const all = await Promise.all(replayPromises);

                // Filter out undefined
                const valid = all.filter((x) => !!x.replay) as { hash: string; replay: ReplayDto }[];

                // Sort by gametime desc
                valid.sort(
                    (a, b) =>
                        new Date(b.replay.gametime).getTime() -
                        new Date(a.replay.gametime).getTime()
                );

                // Limit
                const selected = valid.slice(0, limit);

                // Collect
                const hashes = selected.map((x) => x.hash);
                const replays = selected.map((x) => x.replay);
                const request: UploadRequestDto = {
                    ...uploadRequest,
                    replays
                };

                // Compress
                const payload = gzipStringRaw(JSON.stringify(request));

                resolve({ hashes, payload });
            } catch (err) {
                reject(err);
            }
        };

        req.onerror = () => reject(req.error);
    });
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
    regionId: number,
    startName: string,
    dirKey?: string,
    count: number = 100
): Promise<FileInfoRecord[]> {
    const metas = await getAllReplayMatas();
    const paths = metas.filter(f => f.regionId === regionId).map(m => m.filePath);
    const dirHandle = !dirKey ? null : await getDirectoryHandle(dirKey);
    return await getReplaysFromFolder(regionId, startName, paths, count, dirHandle ?? undefined);
}

export async function getFileContentStream(path: string) {
    return await readFileContentStream(path);
}

export async function getConfig(): Promise<PwaConfig | undefined> {
    const database = await openDB();

    return new Promise((resolve, reject) => {
        const tx = database.transaction(STORES.config, "readonly");
        const store = tx.objectStore(STORES.config);

        const request = store.get("app");

        request.onsuccess = () => {
            resolve(request.result);
        };

        request.onerror = () => reject(request.error);
    });
}

export async function saveConfig(
    config: PwaConfig,
): Promise<void> {
    const database = await openDB();

    return new Promise((resolve, reject) => {
        const tx = database.transaction(STORES.config, "readwrite");

        const configs = tx.objectStore(STORES.config);
        configs.put(config, "app");

        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
}



async function _getFilteredReplayLists(filter: ReplayFilter): Promise<ReplayListDto[]> {
    const database = await openDB();

    const hasNameFilter = filter.name && filter.name.length > 0;
    const hasCommandersFilter = filter.commanders && filter.commanders.length > 0;

    const tx = database.transaction(STORES.lists, "readonly");
    const store = tx.objectStore(STORES.lists);

    let initialReplayLists: ReplayListDto[] = [];

    if (hasCommandersFilter) {
        const team1Index = store.index("commandersTeam1");
        const team2Index = store.index("commandersTeam2");
        const addedHashes = new Set<string>();

        const promises = filter.commanders!.map(commander => {
            return [
                new Promise<ReplayListDto[]>((res, rej) => {
                    const req = team1Index.getAll(IDBKeyRange.only(commander));
                    req.onsuccess = () => res(req.result);
                    req.onerror = () => rej(req.error);
                }),
                new Promise<ReplayListDto[]>((res, rej) => {
                    const req = team2Index.getAll(IDBKeyRange.only(commander));
                    req.onsuccess = () => res(req.result);
                    req.onerror = () => rej(req.error);
                })
            ];
        }).flat();

        const results = await Promise.all(promises);
        results.flat().forEach(replay => {
            if (!addedHashes.has(replay.replayHash)) {
                initialReplayLists.push(replay);
                addedHashes.add(replay.replayHash);
            }
        });
    } else {
        initialReplayLists = await new Promise<ReplayListDto[]>((res, rej) => {
            const req = store.getAll();
            req.onsuccess = () => res(req.result);
            req.onerror = () => rej(req.error);
        });
    }

    if (!hasNameFilter) {
        return initialReplayLists;
    }

    const searchNames = hasNameFilter ? filter.name!.toLowerCase().split(' ').filter(n => n) : [];

    return initialReplayLists.filter(replayList => {
        if (filter.linkCommanders && hasNameFilter && hasCommandersFilter) {
            const lowerCasePlayerNames = replayList.playerNames.map(name => name.toLowerCase());
            const allCommanders = [...replayList.commandersTeam1, ...replayList.commandersTeam2];

            return lowerCasePlayerNames.some((playerName, playerIndex) => {
                const nameMatch = searchNames.every(searchName => playerName.includes(searchName));
                if (nameMatch) {
                    const commander = allCommanders[playerIndex];
                    return filter.commanders!.includes(commander);
                }
                return false;
            });
        }

        if (hasNameFilter) {
            const lowerCasePlayerNames = replayList.playerNames.map(name => name.toLowerCase());
            return searchNames.every(searchName => lowerCasePlayerNames.some(playerName => playerName.includes(searchName)));
        }

        return true;
    });
}

export async function getFilteredReplayLists(
    filter: ReplayFilter
): Promise<ReplayListDto[]> {
    const filteredResults = await _getFilteredReplayLists(filter);

    const orders = (filter.tableOrders && filter.tableOrders.length > 0)
        ? filter.tableOrders
        : [{ name: 'gametime', ascending: false }];

    filteredResults.sort((a, b) => {
        for (const order of orders) {
            const aValue = a[order.name as keyof ReplayListDto];
            const bValue = b[order.name as keyof ReplayListDto];

            // Assuming array properties are not used for sorting
            if (Array.isArray(aValue) || Array.isArray(bValue)) {
                continue;
            }

            const valA = aValue === undefined ? 0 : aValue;
            const valB = bValue === undefined ? 0 : bValue;

            if (valA < valB) {
                return order.ascending ? -1 : 1;
            }
            if (valA > valB) {
                return order.ascending ? 1 : -1;
            }
        }
        return 0;
    });

    if (filter.skip !== undefined && filter.take !== undefined) {
        return filteredResults.slice(filter.skip, filter.skip + filter.take);
    }
    for (const result of filteredResults) {
        const json = JSON.stringify(result, null, 2);
        console.log(json);
    }
    return filteredResults;
}

export async function getFilteredReplayListsCount(filter: ReplayFilter): Promise<number> {
    const filteredResults = await _getFilteredReplayLists(filter);
    console.log("count: " + filteredResults.length);
    return filteredResults.length;
}

export async function downloadBackup() {
    await exportBackup();
}

export async function uploadBackup() {
    await importBackup();
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

export async function getPlayerStats(player: PlayerDto): Promise<MyPlayerStats> {
    const statsService = new StatsService();
    return await statsService.generateStats(player);
}

export async function exportAllDirectoryHandles(): Promise<string[]> {
    return await getAllDirectoryHandles();
}

export async function delDirectoryHandle(key: string): Promise<void> {
    await deleteDirectoryHandle(key);
}