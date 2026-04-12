// file-handle-repository.ts
import { openDB, STORES } from "./db-core";
import { DirectoryFingerprint, FileInfo, FileInfoRecord } from "./dtos";
import { getFilesFromFolderRecursive } from "./get-files";

export interface StoredDirHandle {
  handle: FileSystemDirectoryHandle | null;
  displayName: string;
  regionId: number;
  fingerprint: DirectoryFingerprint | null;
  status: "bound" | "unbound";
  lastBoundAt?: number;
  lastScannedAt?: number;
}

// ── internal helpers ──────────────────────────────────────────────────────────

async function getAllStoredEntries(): Promise<{ key: string; entry: StoredDirHandle }[]> {
  const db = await openDB();
  return new Promise((resolve) => {
    const tx = db.transaction(STORES.directoryHandles, "readonly");
    const store = tx.objectStore(STORES.directoryHandles);
    const results: { key: string; entry: StoredDirHandle }[] = [];
    const req = store.openCursor();
    req.onsuccess = (e) => {
      const cursor = (e.target as IDBRequest<IDBCursorWithValue>).result;
      if (!cursor) { resolve(results); return; }
      results.push({ key: cursor.key as string, entry: cursor.value as StoredDirHandle });
      cursor.continue();
    };
    req.onerror = () => resolve(results);
  });
}

// ── public API ────────────────────────────────────────────────────────────────

/**
 * Persist a new directory handle.
 * - Checks all existing handles with isSameEntry(); if a duplicate is found,
 *   returns its existing UUID instead of saving a second copy.
 * - Ensures displayName is unique; appends a numeric suffix if needed.
 * Returns the UUID key used for storage.
 */
export async function addDirectoryHandle(
  handle: FileSystemDirectoryHandle,
  startName: string,
  rootKey: string | undefined,
): Promise<string> {
  const existing = await getAllStoredEntries();

  // Duplicate-handle check
  for (const { key, entry } of existing) {
    if (await entry.handle?.isSameEntry(handle)) {
      return key;
    }
  }

  if (rootKey) {
    const unboundHandles = existing.filter((f) => f.entry.status == 'unbound');
    if (unboundHandles.length > 0) {
      const fileInfos: FileInfo[] = await getFilesFromFolderRecursive(handle, startName, true, rootKey);
      const fileIndex = new Map<string, FileInfoRecord[]>();
      for (const f of fileInfos) {
        const key = `${f.file.name}::${f.file.size}`;
        if (!fileIndex.has(key)) fileIndex.set(key, []);
        fileIndex.get(key)!.push({
          name: f.file.name,
          size: f.file.size,
          lastModified: f.file.lastModified,
          path: f.record.path
        });
      }
      for (const { key, entry } of unboundHandles) {
        if (matchesFingerprint(entry.fingerprint, fileIndex)) {
          await updateDirectoryHandle(key, {
            ...entry,
            handle,
            status: "bound",
            lastBoundAt: Date.now()
          });

          return key; // reuse existing UUID
        }
      }
    }
  }
  
  // Unique display-name
  let effectiveName = handle.name;
  let suffix = 2;
  while (existing.some((e) => e.entry.displayName === effectiveName)) {
    effectiveName = `${handle.name} ${suffix++}`;
  }

  const uuid = crypto.randomUUID();
  const db = await openDB();
  await new Promise<void>((resolve, reject) => {
    const tx = db.transaction(STORES.directoryHandles, "readwrite");
    tx.objectStore(STORES.directoryHandles).put({ handle, displayName: effectiveName }, uuid);
    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error);
  });
  return uuid;
}

async function updateDirectoryHandle(key: string, entry: StoredDirHandle) {
  const db = await openDB();

  return new Promise<void>((resolve, reject) => {
    const tx = db.transaction(STORES.directoryHandles, "readwrite");
    tx.objectStore(STORES.directoryHandles).put(entry, key);
    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error);
  });
}

export async function updateDirectoryFingerprint(rootKey: string, fingerprint: DirectoryFingerprint) {
    const existing = await getAllStoredEntries();
    
    const target = existing.find((e) => e.key === rootKey);
    if (!target) throw new Error(`Handle key "${rootKey}" not found.`);

    const db = await openDB();
    await new Promise<void>((resolve, reject) => {
      const tx = db.transaction(STORES.directoryHandles, "readwrite");
      tx.objectStore(STORES.directoryHandles).put(
        { ...target.entry, fingerprint: fingerprint },
        rootKey
      );
      tx.oncomplete = () => resolve();
      tx.onerror = () => reject(tx.error);
    });
}

export async function updateDirectoryScanState(rootKey: string, lastScannedAt: number) {
    const existing = await getAllStoredEntries();
    const target = existing.find((e) => e.key === rootKey);
    if (!target) throw new Error(`Handle key "${rootKey}" not found.`);

    await updateDirectoryHandle(rootKey, {
      ...target.entry,
      lastScannedAt
    });
}

/** Returns the full entry (handle + displayName) for a UUID key. */
export async function getDirectoryHandle(key: string): Promise<StoredDirHandle | null> {
  const db = await openDB();
  return new Promise((resolve) => {
    const tx = db.transaction(STORES.directoryHandles, "readonly");
    const req = tx.objectStore(STORES.directoryHandles).get(key);
    req.onsuccess = () => resolve((req.result as StoredDirHandle) ?? null);
    req.onerror = () => resolve(null);
  });
}

/** Returns all saved entries as plain objects (no FileSystemDirectoryHandle, safe to serialize). */
export async function getAllDirectoryHandleEntries(): Promise<{ key: string; displayName: string; }[]> {
  const entries = await getAllStoredEntries();
  return entries.map(({ key, entry }) => ({ key, displayName: entry.displayName }));
}

/** Returns just the UUID keys (kept for backward compatibility). */
export async function getAllDirectoryHandles(): Promise<string[]> {
  const db = await openDB();
  return new Promise((resolve) => {
    const tx = db.transaction(STORES.directoryHandles, "readonly");
    const req = tx.objectStore(STORES.directoryHandles).getAllKeys();
    req.onsuccess = () => resolve(req.result as string[]);
    req.onerror = () => resolve([]);
  });
}

/**
 * Rename a saved handle. Throws if newDisplayName is already used by another entry.
 */
export async function renameDirectoryHandle(key: string, newDisplayName: string): Promise<void> {
  const existing = await getAllStoredEntries();
  if (existing.some((e) => e.key !== key && e.entry.displayName === newDisplayName)) {
    throw new Error(`Display name "${newDisplayName}" is already in use.`);
  }
  const target = existing.find((e) => e.key === key);
  if (!target) throw new Error(`Handle key "${key}" not found.`);

  const db = await openDB();
  await new Promise<void>((resolve, reject) => {
    const tx = db.transaction(STORES.directoryHandles, "readwrite");
    tx.objectStore(STORES.directoryHandles).put(
      { ...target.entry, displayName: newDisplayName },
      key
    );
    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error);
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

export async function verifyDirectoryPermission(
  handle: FileSystemDirectoryHandle | null,
  mode: "read" | "readwrite" = "read"
): Promise<boolean> {
  if (!handle) return false;
  const opts = { mode };
  // @ts-ignore
  const q = await (handle as any).queryPermission?.(opts);
  if (q === "granted") return true;
  const r = await (handle as any).requestPermission?.(opts);
  return r === "granted";
}

export async function verifyAllDirectoryPermissions(keys: string[]): Promise<string[]> {
  const granted: string[] = [];
  for (const key of keys) {
    const entry = await getDirectoryHandle(key);
    if (!entry) continue;
    const ok = await verifyDirectoryPermission(entry.handle);
    if (ok) granted.push(key);
  }
  return granted;
}

export async function getDirectoryHandleFromUser(): Promise<FileSystemDirectoryHandle | null> {
  if (!("showDirectoryPicker" in window)) {
    throw new Error(
      "showDirectoryPicker is not supported in this browser. File selection requires a Chromium-based browser."
    );
  }
  try {
    return await (window as any).showDirectoryPicker();
  } catch (error: any) {
    if (error?.name === "AbortError") return null;
    throw new Error(`Failed to pick directory: ${error?.message ?? error}`);
  }
}

export function selectFingerprintFiles(files: FileInfo[], count = 12) {
  if (files.length <= count) return files;

  const sorted = files
    .slice()
    .sort((a, b) => a.record.lastModified - b.record.lastModified);

  const result: FileInfo[] = [];
  const step = sorted.length / count;

  for (let i = 0; i < count; i++) {
    result.push(sorted[Math.floor(i * step)]);
  }

  return result;
}

export function matchesFingerprint(
  fingerprint: DirectoryFingerprint | null,
  fileIndex: Map<string, FileInfoRecord[]>
): boolean {
  if (!fingerprint) return false;
  let matches = 0;

  for (const f of fingerprint.files) {
    const key = `${f.name}::${f.size}`;
    const candidates = fileIndex.get(key);

    if (!candidates) continue;

    if (candidates.some(c => c.lastModified === f.lastModified)) {
      matches++;
    }
  }

  // threshold tuning
  return matches >= Math.ceil(fingerprint.files.length * 0.5);
}
