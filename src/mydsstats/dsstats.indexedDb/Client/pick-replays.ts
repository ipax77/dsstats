import { FileInfoRecord } from "./dtos";
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
    const allRecords: { rec: FileInfoRecord; file: File }[] = [];

    try {
        if ("showDirectoryPicker" in window) {
            if (dirHandle === null || dirHandle === undefined) {
                dirHandle = await getDirectoryHandleFromUser();
                if (dirHandle === null || dirHandle === undefined) {
                    return [];
                }
                await saveDirectoryHandle(`${dirHandle.name}_${regionId}`, dirHandle);
            } else {
                await verifyDirectoryPermission(dirHandle);
            }

            async function walkDir(
                handle: FileSystemDirectoryHandle | undefined,
                prefix: string = ""
            ) {
                for await (const [name, entry] of (handle as any).entries()) {
                    const newPath = prefix ? `${prefix}/${name}` : name;

                    if (entry.kind === "file") {
                        const file = await (entry as FileSystemFileHandle).getFile();
                        allRecords.push({
                            rec: {
                                path: newPath,
                                name: file.name,
                                size: file.size,
                                lastModified: file.lastModified,
                            },
                            file,
                        });
                    } else if (entry.kind === "directory") {
                        await walkDir(entry as FileSystemDirectoryHandle, newPath);
                    }
                }
            }

            await walkDir(dirHandle, dirHandle?.name);
        } else {
            // Firefox / Safari fallback
            const files = await new Promise<File[]>((resolve) => {
                const input = document.createElement("input");
                input.type = "file";
                (input as any).webkitdirectory = true;
                input.multiple = true;
                input.onchange = () => resolve(Array.from(input.files || []));
                input.click();
            });

            for (const file of files) {
                const path = (file as any).webkitRelativePath || file.name;
                allRecords.push({
                    rec: {
                        path,
                        name: file.name,
                        size: file.size,
                        lastModified: file.lastModified,
                    },
                    file,
                });
            }
        }

        // Filter + sort
        const filtered = allRecords
            .filter(({ rec }) => rec.name.startsWith(startName))
            .filter(({ rec }) => !existingSet.has(rec.path))
            .sort((a, b) => b.rec.lastModified - a.rec.lastModified);

        // Take only {count}
        const top = filtered.slice(0, count);

        // Store only those files in memory
        for (const { rec, file } of top) {
            fileHandleMap.set(rec.path, file);
        }

        return top.map(({ rec }) => rec);
    } catch (error: unknown) {
        console.log("Failed getting file infos: " + (error as Error).message);
        return [];
    }
}

export async function readFileContentStream(path: string) {
    const file = fileHandleMap.get(path);
    if (!file) throw new Error(`File not found: ${path}`);
    return file;
}