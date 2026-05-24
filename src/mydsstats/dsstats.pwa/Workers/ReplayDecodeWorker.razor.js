// Workers/ReplayDecodeWorker.razor.js
// Boots the .NET 10 WASM runtime inside a dedicated Web Worker.
// Path '../_framework/dotnet.js' is one level up from Workers/ to the app root.

import { dotnet } from '../_framework/dotnet.js';
import { compressSpawnPlaybackPayload } from '../_content/dsstats.indexedDb/js/spawn-playback-compression.js';

const { getAssemblyExports, getConfig } = await dotnet.create();

const config = getConfig();
const assemblyExports = await getAssemblyExports(config.mainAssemblyName);
const workerApi = assemblyExports.dsstats.pwa.Workers.ReplayDecodeWorker;

// Signal to the pool manager that this worker is ready to accept jobs.
self.postMessage({ type: 'ready' });

self.onmessage = async (e) => {
    const { jobId, bytes } = e.data;
    try {
        const resultJson = await workerApi.DecodeReplayAsync(bytes);
        self.postMessage({ type: 'result', jobId, resultJson: await compressSpawnPlaybackSidecar(resultJson) });
    } catch (err) {
        self.postMessage({
            type: 'result',
            jobId,
            resultJson: JSON.stringify({ success: false, error: String(err) })
        });
    }
};

async function compressSpawnPlaybackSidecar(resultJson) {
    let result;
    try {
        result = JSON.parse(resultJson);
    } catch {
        return resultJson;
    }

    if (!result?.success || !result.spawnPlaybackPayload) {
        return resultJson;
    }

    try {
        const rawPayload = base64ToBytes(result.spawnPlaybackPayload);
        const compressedPayload = await compressSpawnPlaybackPayload(rawPayload);
        result.spawnPlaybackPayload = bytesToBase64(compressedPayload);
        result.spawnPlaybackCompressedLength = compressedPayload.length;
        result.spawnPlaybackUncompressedLength = rawPayload.length;
        result.spawnPlaybackCompression = 1;
        result.spawnPlaybackError = null;
    } catch (error) {
        result.spawnPlaybackPayload = null;
        result.spawnPlaybackCompressedLength = 0;
        result.spawnPlaybackUncompressedLength = 0;
        result.spawnPlaybackUnitCount = 0;
        result.spawnPlaybackError = `JS Brotli failed: ${error?.message ?? error}`;
    }

    return JSON.stringify(result);
}

function base64ToBytes(base64) {
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
        bytes[i] = binary.charCodeAt(i);
    }
    return bytes;
}

function bytesToBase64(bytes) {
    const chunkSize = 0x8000;
    let binary = '';
    for (let offset = 0; offset < bytes.length; offset += chunkSize) {
        const chunk = bytes.subarray(offset, offset + chunkSize);
        binary += String.fromCharCode(...chunk);
    }
    return btoa(binary);
}
