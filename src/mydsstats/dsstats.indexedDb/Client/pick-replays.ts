import { DirectoryFingerprint, FileInfo, FileInfoRecord, ReplayMeta } from "./dtos";
import { addDirectoryHandle, getDirectoryHandle, getDirectoryHandleFromUser, selectFingerprintFiles, updateDirectoryFingerprint, updateDirectoryScanState, verifyDirectoryPermission } from "./file-handle-repository";
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
        if (dirHandle === null || dirHandle === undefined) {
            dirHandle = await getDirectoryHandleFromUser();
            if (dirHandle === null || dirHandle === undefined) {
                return [];
            }
            // Save with unique UUID key; returns existing UUID if same folder was already saved.
            rootKey = await addDirectoryHandle(dirHandle, startName, rootKey);
        } else {
            await verifyDirectoryPermission(dirHandle);
        }

        if (rootKey) {
            const stored = await getDirectoryHandle(rootKey);
            previousScanTime = stored?.lastScannedAt ?? stored?.lastBoundAt ?? 0;
        }

        // Use the UUID (or dirHandle.name as fallback) as the path root so that
        // stored paths are globally unique across handles with the same folder name.
        const pathRoot = rootKey ?? dirHandle.name;
        const allRecords = await getFilesFromFolderRecursive(dirHandle, startName, true, pathRoot);

        const pathPrefixes = new Set<string>([pathRoot]);
        if (dirHandle.name && dirHandle.name !== pathRoot) {
            pathPrefixes.add(dirHandle.name);
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
            const legacyPath = dirHandle.name !== pathRoot
                ? toLegacyPath(currentPath, pathRoot, dirHandle.name)
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
