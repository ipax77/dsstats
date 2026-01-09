import { FileInfo, FileInfoRecord } from "./dtos";
import { getDirectoryHandleFromUser, saveDirectoryHandle, verifyDirectoryPermission } from "./file-handle-repository";

const fileHandleMap = new Map<string, File>();

export async function getReplaysFromFolder(
    regionId: number,
    startName: string,
    existingPaths: string[],
    count: number,
    dirHandle?: FileSystemDirectoryHandle | null,
): Promise<FileInfoRecord[]> {
    fileHandleMap.clear(); // reset

    const existingSet = new Set(existingPaths);
    try {
        if (dirHandle === null || dirHandle === undefined) {
            dirHandle = await getDirectoryHandleFromUser();
            if (dirHandle === null || dirHandle === undefined) {
                return [];
            }
            await saveDirectoryHandle(`${dirHandle.name}_${regionId}`, dirHandle);
        } else {
            await verifyDirectoryPermission(dirHandle);
        }

        const records = await getFilesFromFolderRecursive(dirHandle, existingSet, startName, true, dirHandle.name);

        // Filter + sort
        const filtered = records
            .sort((a, b) => b.record.lastModified - a.record.lastModified);

        // Take only {count}
        const top = filtered.slice(0, count);

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
    currentPath: string // initial is dirHandle.name
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
