// Workers/ReplayDecodeWorker.razor.js
// Boots the .NET 10 WASM runtime inside a dedicated Web Worker.
// Path '../_framework/dotnet.js' is one level up from Workers/ to the app root.

import { dotnet } from '../_framework/dotnet.js';

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
        self.postMessage({ type: 'result', jobId, resultJson });
    } catch (err) {
        self.postMessage({
            type: 'result',
            jobId,
            resultJson: JSON.stringify({ success: false, error: String(err) })
        });
    }
};
