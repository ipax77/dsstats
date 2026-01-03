import 'fake-indexeddb/auto';

// pako global type is declared in global-pako.d.ts

(globalThis as any).pako = {
  gzip: (data: string | Uint8Array) => {
    // just return Uint8Array of string bytes
    if (typeof data === "string") {
      return new TextEncoder().encode(data);
    }
    return new Uint8Array(data);
  },
  ungzip: (data: Uint8Array, options?: any) => {
    if (options?.to === "string") {
      return new TextDecoder().decode(data);
    }
    return data;
  },
};
