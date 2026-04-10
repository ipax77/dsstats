// file-handle-repository.ts
import { openDB, STORES } from "./db-core";

export interface StoredDirHandle {
  handle: FileSystemDirectoryHandle;
  displayName: string;
  regionId: number;
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
  displayName: string,
  regionId: number
): Promise<string> {
  const existing = await getAllStoredEntries();

  // Duplicate-handle check
  for (const { key, entry } of existing) {
    if (await entry.handle.isSameEntry(handle)) {
      return key;
    }
  }

  // Unique display-name
  let effectiveName = displayName;
  let suffix = 2;
  while (existing.some((e) => e.entry.displayName === effectiveName)) {
    effectiveName = `${displayName} ${suffix++}`;
  }

  const uuid = crypto.randomUUID();
  const db = await openDB();
  await new Promise<void>((resolve, reject) => {
    const tx = db.transaction(STORES.directoryHandles, "readwrite");
    tx.objectStore(STORES.directoryHandles).put({ handle, displayName: effectiveName, regionId }, uuid);
    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error);
  });
  return uuid;
}

/** Returns the full entry (handle + displayName + regionId) for a UUID key. */
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
export async function getAllDirectoryHandleEntries(): Promise<{ key: string; displayName: string; regionId: number }[]> {
  const entries = await getAllStoredEntries();
  return entries.map(({ key, entry }) => ({ key, displayName: entry.displayName, regionId: entry.regionId }));
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
