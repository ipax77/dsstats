import brotliPromise from "brotli-wasm";

const SPAWN_PLAYBACK_BROTLI_QUALITY = 5;

export async function compressSpawnPlaybackPayload(rawPayload: Uint8Array): Promise<Uint8Array> {
    const brotli = await brotliPromise;
    const compressed = brotli.compress(rawPayload, { quality: SPAWN_PLAYBACK_BROTLI_QUALITY });
    return normalizeCompressedPayload(compressed);
}

function normalizeCompressedPayload(value: Uint8Array | ArrayBuffer): Uint8Array {
    return value instanceof Uint8Array
        ? value
        : new Uint8Array(value);
}
