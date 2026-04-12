import { FileInfo } from "./dtos";

export async function getFilesFromFolderRecursive(
  dirHandle: FileSystemDirectoryHandle,
  startName: string,
  recursive: boolean,
  currentPath: string, // initial is the UUID or dirHandle.name
): Promise<FileInfo[]> {
  const out: FileInfo[] = [];

  for await (const [name, entry] of dirHandle.entries()) {
    const fullPath = currentPath ? `${currentPath}/${name}` : name;

    if (entry.kind === "directory") {
      if (recursive) {
        const nested = await getFilesFromFolderRecursive(
          entry as FileSystemDirectoryHandle,
          startName,
          recursive,
          fullPath,
        );
        out.push(...nested);
      }
      continue;
    } else if (entry.kind === "file") {
      if (!name.startsWith(startName)) continue;

      const file = await (entry as FileSystemFileHandle).getFile();

      out.push({
        record: {
          path: fullPath, // important for uniqueness in recursion
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
