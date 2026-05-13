let dotNetRef = null;
let registration = null;
let updateAvailable = false;
let applyingUpdate = false;
let trackedInstallingWorker = null;
let trackedWaitingWorker = null;
let reloadFallbackId = null;

export async function initialize(callbacks) {
  if (!("serviceWorker" in navigator)) {
    return false;
  }

  dotNetRef = callbacks;
  navigator.serviceWorker.addEventListener("controllerchange", onControllerChange);

  registration = await navigator.serviceWorker.ready;
  registration.addEventListener("updatefound", onUpdateFound);
  trackInstallingWorker(registration.installing);
  await registration.update();
  setUpdateAvailable(hasWaitingWorker());

  return updateAvailable;
}

export async function applyUpdate() {
  if (!registration) {
    registration = await navigator.serviceWorker.ready;
  }

  if (!registration.waiting) {
    await registration.update();
  }

  if (registration.waiting) {
    applyingUpdate = true;
    trackWaitingWorker(registration.waiting);
    console.info("PWA update: asking waiting service worker to activate.");
    registration.waiting.postMessage({ type: "SKIP_WAITING" });
    scheduleReloadFallback();
    return;
  }

  console.info("PWA update: no waiting service worker found, reloading.");
  window.location.reload();
}

export function dispose() {
  navigator.serviceWorker?.removeEventListener("controllerchange", onControllerChange);
  registration?.removeEventListener("updatefound", onUpdateFound);
  trackedInstallingWorker?.removeEventListener("statechange", onInstallingStateChange);
  trackedWaitingWorker?.removeEventListener("statechange", onWaitingStateChange);
  clearReloadFallback();
  dotNetRef = null;
  registration = null;
  trackedInstallingWorker = null;
  trackedWaitingWorker = null;
}

function onUpdateFound() {
  trackInstallingWorker(registration?.installing ?? null);
}

function trackInstallingWorker(worker) {
  if (trackedInstallingWorker === worker) {
    return;
  }

  trackedInstallingWorker?.removeEventListener("statechange", onInstallingStateChange);
  trackedInstallingWorker = worker;
  trackedInstallingWorker?.addEventListener("statechange", onInstallingStateChange);
}

function onInstallingStateChange() {
  if (trackedInstallingWorker?.state === "installed") {
    trackWaitingWorker(registration?.waiting ?? null);
    setUpdateAvailable(hasWaitingWorker());
  }
}

function onControllerChange() {
  if (applyingUpdate) {
    console.info("PWA update: controller changed, reloading.");
    clearReloadFallback();
    window.location.reload();
  }
}

function trackWaitingWorker(worker) {
  if (trackedWaitingWorker === worker) {
    return;
  }

  trackedWaitingWorker?.removeEventListener("statechange", onWaitingStateChange);
  trackedWaitingWorker = worker;
  trackedWaitingWorker?.addEventListener("statechange", onWaitingStateChange);
}

function onWaitingStateChange() {
  if (!applyingUpdate) {
    return;
  }

  console.info(`PWA update: waiting service worker state is ${trackedWaitingWorker?.state}.`);
  if (trackedWaitingWorker?.state === "activated") {
    clearReloadFallback();
    window.location.reload();
  }
}

function scheduleReloadFallback() {
  clearReloadFallback();
  reloadFallbackId = window.setTimeout(() => {
    console.info("PWA update: activation event was not observed, reloading as fallback.");
    window.location.reload();
  }, 2000);
}

function clearReloadFallback() {
  if (reloadFallbackId !== null) {
    window.clearTimeout(reloadFallbackId);
    reloadFallbackId = null;
  }
}

function hasWaitingWorker() {
  return Boolean(registration?.waiting && navigator.serviceWorker.controller);
}

function setUpdateAvailable(value) {
  if (updateAvailable === value) {
    return;
  }

  updateAvailable = value;
  dotNetRef?.invokeMethodAsync("SetUpdateAvailable", value).catch(() => {
    // Ignore transient teardown races.
  });
}
