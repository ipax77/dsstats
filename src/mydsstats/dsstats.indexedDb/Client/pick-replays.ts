import { FileInfo, FileInfoRecord } from "./dtos";
import { addDirectoryHandle, getDirectoryHandleFromUser, verifyDirectoryPermission } from "./file-handle-repository";

const fileHandleMap = new Map<string, File>();

export async function getReplaysFromFolder(
    regionId: number,
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
            rootKey = await addDirectoryHandle(dirHandle, dirHandle.name, regionId);
        } else {
            await verifyDirectoryPermission(dirHandle);
        }

        // Use the UUID (or dirHandle.name as fallback) as the path root so that
        // stored paths are globally unique across handles with the same folder name.
        const pathRoot = rootKey ?? dirHandle.name;
        const records = await getFilesFromFolderRecursive(dirHandle, existingSet, startName, true, pathRoot);

        // Filter + sort
        const filtered = records
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

async function getFilesFromFolderRecursive(
    dirHandle: FileSystemDirectoryHandle,
    existing: Set<string>,
    startName: string,
    recursive: boolean,
    currentPath: string // initial is the UUID or dirHandle.name
): Promise<FileInfo[]> {
    const out: FileInfo[] = [];

    for await (const [name, entry] of dirHandle.entries()) {
        const fullPath = currentPath ? `${currentPath}/${name}` : name;

        if (entry.kind === "directory") {
            if (recursive) {
                const nested = await getFilesFromFolderRecursive(
                    entry as FileSystemDirectoryHandle,
                    existing,
                    startName,
                    recursive,
                    fullPath
                );
                out.push(...nested);
            }
            continue;
        }
        else if (entry.kind === "file") {
            if (!name.startsWith(startName)) continue;
            if (existing.has(fullPath)) continue;

            const file = await (entry as FileSystemFileHandle).getFile();

            out.push({
                record: {
                    path: fullPath,     // important for uniqueness in recursion
                    name: file.name,
                    size: file.size,
                    lastModified: file.lastModified,
                },
                file,
            });
        }
    }

    return out;
}

export async function readFileContentStream(path: string): Promise<File> {
    const file = fileHandleMap.get(path);
    if (!file) throw new Error(`File not found: ${path}`);
    return file;
}
