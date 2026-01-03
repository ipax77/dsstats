// file-handle-repository.ts
import { openDB, STORES } from "./db-core";

export async function saveDirectoryHandle(key: string, handle: FileSystemDirectoryHandle): Promise<void> {
  const db = await openDB();
  return new Promise<void>((resolve, reject) => {
    const tx = db.transaction(STORES.directoryHandles, "readwrite");
    const store = tx.objectStore(STORES.directoryHandles);
    const req = store.put(handle, key);
    req.onsuccess = () => resolve();
    req.onerror = () => reject(req.error);
  });
}

export async function getDirectoryHandle(key: string): Promise<FileSystemDirectoryHandle | null> {
  const db = await openDB();
  return new Promise((resolve) => {
    const tx = db.transaction(STORES.directoryHandles, "readonly");
    const store = tx.objectStore(STORES.directoryHandles);
    const req = store.get(key);
    req.onsuccess = () => resolve(req.result ?? null);
    req.onerror = () => resolve(null);
  });
}

export async function getAllDirectoryHandles(): Promise<string[]> {
  const db = await openDB();
  return new Promise((resolve) => {
    const tx = db.transaction(STORES.directoryHandles, "readonly");
    const store = tx.objectStore(STORES.directoryHandles);

    const reqKeys = store.getAllKeys();
    reqKeys.onsuccess = () => resolve(reqKeys.result as string[]);
    reqKeys.onerror = () => resolve([]);
  });
}

export async function deleteDirectoryHandle(key: string): Promise<boolean> {
  const db = await openDB();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(STORES.directoryHandles, "readwrite");
    tx.objectStore(STORES.directoryHandles).delete(key);
    tx.oncomplete = () => resolve(true);
    tx.onerror = () => reject(tx.error);
  });
}

export async function verifyDirectoryPermission(handle: FileSystemDirectoryHandle | null, mode: 'read' | 'readwrite' = 'read'): Promise<boolean> {
  if (!handle) return false;
  // FileSystemHandlePermissionDescriptor
  const opts = { mode };
  // queryPermission exists in Chromium's implementation
  // @ts-ignore - some typings may be missing
  const q = await (handle as any).queryPermission?.(opts);
  if (q === 'granted') return true;
  // requestPermission will show a prompt if needed
  const r = await (handle as any).requestPermission?.(opts);
  return r === 'granted';
}

export async function getDirectoryHandleFromUser(): Promise<FileSystemDirectoryHandle | null> {
  if ("showDirectoryPicker" in window) {
    try {
      const directoryHandle = await (window as any).showDirectoryPicker();
      return directoryHandle;
    } catch (error: any) {
      if (error.name === 'AbortError') {
        // User cancelled the picker
        return null;
      }
      console.error("Error picking directory:", error);
      return null;
    }
  } else {
    // Fallback for browsers that do not support showDirectoryPicker
    console.warn("showDirectoryPicker is not supported in this browser.");
    return null;
  }
}