// Clients/DecodeClient.razor.js
// Worker pool manager - runs on the main thread.
// Manages N decoder workers, queues jobs, and routes results back to callers.

const workers = [];      // array of { worker, ready, busy }
const pending = new Map(); // jobId -> { resolve, reject }
const queue   = [];      // queued jobs waiting for a free worker
let   nextId  = 1;
let   readyResolve = null;
let   lifecycleListenersInstalled = false;
let   browserPauseStartedAt = null;
let   browserPauseMilliseconds = 0;

const documentLifecycleEvents = ['freeze', 'resume'];
const windowLifecycleEvents = ['pagehide', 'pageshow'];

function beginBrowserPause() {
    browserPauseStartedAt ??= Date.now();
}

function endBrowserPause() {
    if (browserPauseStartedAt === null) {
        return;
    }

    browserPauseMilliseconds += Math.max(0, Date.now() - browserPauseStartedAt);
    browserPauseStartedAt = null;
}

function handleDocumentLifecycleEvent(event) {
    if (event.type === 'freeze') {
        beginBrowserPause();
    } else if (event.type === 'resume') {
        endBrowserPause();
    }
}

function handleWindowLifecycleEvent(event) {
    if (!event.persisted) {
        return;
    }

    if (event.type === 'pagehide') {
        beginBrowserPause();
    } else if (event.type === 'pageshow') {
        endBrowserPause();
    }
}

function installLifecycleListeners() {
    if (lifecycleListenersInstalled) {
        return;
    }

    documentLifecycleEvents.forEach(eventName =>
        document.addEventListener(eventName, handleDocumentLifecycleEvent));
    windowLifecycleEvents.forEach(eventName =>
        window.addEventListener(eventName, handleWindowLifecycleEvent));

    lifecycleListenersInstalled = true;
}

function removeLifecycleListeners() {
    if (!lifecycleListenersInstalled) {
        return;
    }

    documentLifecycleEvents.forEach(eventName =>
        document.removeEventListener(eventName, handleDocumentLifecycleEvent));
    windowLifecycleEvents.forEach(eventName =>
        window.removeEventListener(eventName, handleWindowLifecycleEvent));

    lifecycleListenersInstalled = false;
    browserPauseStartedAt = null;
    browserPauseMilliseconds = 0;
}

export function consumeBrowserPauseMilliseconds() {
    if (browserPauseStartedAt !== null) {
        const now = Date.now();
        browserPauseMilliseconds += Math.max(0, now - browserPauseStartedAt);
        browserPauseStartedAt = now;
    }

    const pauseMilliseconds = browserPauseMilliseconds;
    browserPauseMilliseconds = 0;
    return Math.round(pauseMilliseconds);
}

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
    installLifecycleListeners();
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
    removeLifecycleListeners();
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
