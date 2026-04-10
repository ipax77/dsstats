// Clients/DecodeClient.razor.js
// Worker pool manager - runs on the main thread.
// Manages N decoder workers, queues jobs, and routes results back to callers.

const workers = [];      // array of { worker, ready, busy }
const pending = new Map(); // jobId -> { resolve, reject }
const queue   = [];      // queued jobs waiting for a free worker
let   nextId  = 1;
let   readyResolve = null;

function createWorker() {
    const state = {
        // Path resolved from the app's base URL (not relative to this file).
        worker: new Worker('./Workers/ReplayDecodeWorker.razor.js', { type: 'module' }),
        ready: false,
        busy:  false,
    };
    state.worker.onmessage = (e) => onMessage(state, e);
    state.worker.onerror   = (e) => console.error('[DecodeClient] Worker error:', e.message);
    return state;
}

function onMessage(state, e) {
    const { type, jobId, resultJson } = e.data;

    if (type === 'ready') {
        state.ready = true;
        if (readyResolve && workers.every(w => w.ready)) {
            readyResolve();
            readyResolve = null;
        }
        return;
    }

    if (type === 'result') {
        state.busy = false;
        const job = pending.get(jobId);
        if (job) {
            pending.delete(jobId);
            job.resolve(resultJson);
        }
        drain(state);
    }
}

function dispatch(state, job) {
    state.busy = true;
    pending.set(job.id, { resolve: job.resolve, reject: job.reject });
    // Transfer the ArrayBuffer for zero-copy hand-off to the worker.
    state.worker.postMessage({ jobId: job.id, bytes: job.bytes }, [job.bytes.buffer]);
}

function drain(state) {
    if (!state.busy && queue.length > 0) {
        dispatch(state, queue.shift());
    }
}

export function initWorkerPool(count) {
    for (let i = 0; i < count; i++) {
        workers.push(createWorker());
    }
}

export function waitForAllReady() {
    if (workers.length > 0 && workers.every(w => w.ready)) {
        return Promise.resolve();
    }
    return new Promise(r => { readyResolve = r; });
}

export function terminateWorkerPool() {
    workers.forEach(w => w.worker.terminate());
    workers.length = 0;
    pending.clear();
    queue.length = 0;
}

export function decodeReplay(base64Bytes) {
    // byte[] cannot cross the JSImport boundary directly, so C# sends base64.
    // Decode it here into a Uint8Array that can be transferred zero-copy to the worker.
    const binaryStr = atob(base64Bytes);
    const copy = new Uint8Array(binaryStr.length);
    for (let i = 0; i < binaryStr.length; i++) {
        copy[i] = binaryStr.charCodeAt(i);
    }
    const id = nextId++;
    return new Promise((resolve, reject) => {
        const job = { id, bytes: copy, resolve, reject };
        const free = workers.find(w => w.ready && !w.busy);
        if (free) {
            dispatch(free, job);
        } else {
            queue.push(job);
        }
    });
}
