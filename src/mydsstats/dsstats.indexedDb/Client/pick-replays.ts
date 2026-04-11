import { DirectoryFingerprint, FileInfo, FileInfoRecord } from "./dtos";
import { addDirectoryHandle, getDirectoryHandleFromUser, selectFingerprintFiles, updateDirectoryFingerprint, verifyDirectoryPermission } from "./file-handle-repository";
import { getFilesFromFolderRecursive } from "./get-files";

const fileHandleMap = new Map<string, File>();

export async function getReplaysFromFolder(
    startName: string,
    existingPaths: string[],
    count: number,
    dirHandle?: FileSystemDirectoryHandle | null,
    rootKey?: string,
): Promise<FileInfoRecord[]> {
    fileHandleMap.clear(); // reset

    const existingSet = new Set(existingPaths);
    try {
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

        // Use the UUID (or dirHandle.name as fallback) as the path root so that
        // stored paths are globally unique across handles with the same folder name.
        const pathRoot = rootKey ?? dirHandle.name;
        const allRecords = await getFilesFromFolderRecursive(dirHandle, startName, true, pathRoot);
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
        }

        const todoRecords = [];
        for (let i = 0; i < allRecords.length; i++) {
            const record = allRecords[i];
            if (!existingSet.has(record.file.name)) {
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
