import { DirectoryFingerprint, FileInfo, FileInfoRecord, ReplayMeta } from "./dtos";
import { addDirectoryHandle, addFallbackDirectoryFiles, DirectorySource, getDirectoryHandle, getDirectorySourceFromUser, getSessionFallbackFiles, selectFingerprintFiles, updateDirectoryFingerprint, updateDirectoryScanState, verifyDirectoryPermission } from "./file-handle-repository";
import { getFilesFromFolderRecursive } from "./get-files";

const fileHandleMap = new Map<string, File>();

function toLegacyPath(path: string, currentRoot: string, legacyRoot: string): string | null {
    if (!path.startsWith(`${currentRoot}/`)) {
        return null;
    }

    return `${legacyRoot}/${path.slice(currentRoot.length + 1)}`;
}

export async function getReplaysFromFolder(
    startName: string,
    existingPaths: string[],
    count: number,
    dirHandle?: FileSystemDirectoryHandle | null,
    rootKey?: string,
    metas: ReplayMeta[] = [],
): Promise<FileInfoRecord[]> {
    fileHandleMap.clear(); // reset
    try {
        let previousScanTime = 0;
        let source: DirectorySource | null = null;
        let sourceDisplayName = "";

        if (dirHandle === null || dirHandle === undefined) {
            if (rootKey) {
                const stored = await getDirectoryHandle(rootKey);
                if (stored?.handle) {
                    source = { kind: "handle", handle: stored.handle };
                    dirHandle = stored.handle;
                    sourceDisplayName = stored.displayName || stored.handle.name;
                } else {
                    const sessionFiles = getSessionFallbackFiles(rootKey);
                    if (sessionFiles) {
                        source = {
                            kind: "files",
                            displayName: stored?.displayName || "Replay folder",
                            files: sessionFiles
                        };
                        sourceDisplayName = source.displayName;
                    } else {
                        source = await getDirectorySourceFromUser();
                        if (!source) return [];

                        if (source.kind === "handle") {
                            rootKey = await addDirectoryHandle(source.handle, startName, rootKey);
                            dirHandle = source.handle;
                            sourceDisplayName = source.handle.name;
                        } else {
                            rootKey = await addFallbackDirectoryFiles(source.files, startName, rootKey, stored?.displayName);
                            sourceDisplayName = stored?.displayName || source.displayName;
                        }
                    }
                }
            } else {
                source = await getDirectorySourceFromUser();
                if (!source) return [];

                if (source.kind === "handle") {
                    dirHandle = source.handle;
                    rootKey = await addDirectoryHandle(source.handle, startName, rootKey);
                    sourceDisplayName = source.handle.name;
                } else {
                    rootKey = await addFallbackDirectoryFiles(source.files, startName);
                    sourceDisplayName = source.displayName;
                }
            }
        } else {
            await verifyDirectoryPermission(dirHandle);
            source = { kind: "handle", handle: dirHandle };
            sourceDisplayName = dirHandle.name;
        }

        if (rootKey) {
            const stored = await getDirectoryHandle(rootKey);
            previousScanTime = stored?.lastScannedAt ?? stored?.lastBoundAt ?? 0;
        }

        // Use the UUID (or dirHandle.name as fallback) as the path root so that
        // stored paths are globally unique across handles with the same folder name.
        if (!source) return [];

        const pathRoot = rootKey ?? sourceDisplayName;
        const allRecords = await getFileInfosFromDirectorySource(source, startName, pathRoot);

        const pathPrefixes = new Set<string>([pathRoot]);
        if (sourceDisplayName && sourceDisplayName !== pathRoot) {
            pathPrefixes.add(sourceDisplayName);
        }

        const relevantMetas = metas
            .filter((meta) =>
                Array.from(pathPrefixes).some((prefix) => meta.filePath.startsWith(`${prefix}/`))
            );
        const legacyPathSet = new Set([...existingPaths, ...relevantMetas.map((meta) => meta.filePath)]);
        const metaByPath = new Map(relevantMetas.map((meta) => [meta.filePath, meta]));
        if (rootKey) {
            const fingerprintRecords = selectFingerprintFiles(allRecords);
            const fingerprint: DirectoryFingerprint = {
                version: 1,
                files: fingerprintRecords.map((m) => ({
                    name: m.file.name,
                    size: m.file.size,
                    lastModified: m.file.lastModified
                    }))
                };
            await updateDirectoryFingerprint(rootKey, fingerprint);
            await updateDirectoryScanState(rootKey, Date.now());
        }

        const todoRecords = [];
        for (let i = 0; i < allRecords.length; i++) {
            const record = allRecords[i];
            const currentPath = record.record.path;
            const legacyPath = sourceDisplayName !== pathRoot
                ? toLegacyPath(currentPath, pathRoot, sourceDisplayName)
                : null;

            const currentMeta = metaByPath.get(currentPath) ?? (legacyPath ? metaByPath.get(legacyPath) : undefined);
            const hasExactMetadataMatch = currentMeta !== undefined
                && currentMeta.size === record.record.size
                && currentMeta.lastModified === record.record.lastModified;
            const legacyMetaNeedsRefresh = currentMeta !== undefined
                && (!currentMeta.size || !currentMeta.lastModified)
                && previousScanTime > 0
                && record.record.lastModified > previousScanTime;
            const pathAlreadyKnown = legacyPathSet.has(currentPath) || (!!legacyPath && legacyPathSet.has(legacyPath));

            if (!pathAlreadyKnown || legacyMetaNeedsRefresh || !hasExactMetadataMatch && currentMeta !== undefined && currentMeta.size !== undefined && currentMeta.lastModified !== undefined) {
                todoRecords.push(record);
            }
        }

        // Filter + sort
        const filtered = todoRecords
            .sort((a, b) => b.record.lastModified - a.record.lastModified);

        // Take only {count}; 0 means no limit (all remaining replays)
        const top = count > 0 ? filtered.slice(0, count) : filtered;

        // Store only those files in memory
        for (const { record, file } of top) {
            fileHandleMap.set(record.path, file);
        }

        return top.map(({ record }) => record);
    } catch (error: unknown) {
        console.log("Failed getting file infos: " + (error as Error).message);
        return [];
    }
}

export async function readFileContentStream(path: string): Promise<File> {
    const file = fileHandleMap.get(path);
    if (!file) throw new Error(`File not found: ${path}`);
    return file;
}

async function getFileInfosFromDirectorySource(
    source: DirectorySource,
    startName: string,
    pathRoot: string,
): Promise<FileInfo[]> {
    if (source.kind === "handle") {
        return await getFilesFromFolderRecursive(source.handle, startName, true, pathRoot);
    }

    const out: FileInfo[] = [];
    for (const file of source.files) {
        if (!file.name.startsWith(startName)) continue;

        out.push({
            record: {
                path: toFallbackPath(file, pathRoot),
                name: file.name,
                size: file.size,
                lastModified: file.lastModified,
            },
            file,
        });
    }

    return out;
}

function toFallbackPath(file: File, pathRoot: string): string {
    const relativePath = file.webkitRelativePath || file.name;
    const parts = relativePath.split("/").filter(Boolean);
    const pathUnderRoot = parts.length > 1 ? parts.slice(1).join("/") : parts.join("/");
    return `${pathRoot}/${pathUnderRoot}`;
}
