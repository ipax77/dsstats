import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';

const source = await readFile(new URL('../../common/dsstats.weblib/wwwroot/js/playerProfile.js', import.meta.url), 'utf8');
const profile = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

const observers = [];
globalThis.MutationObserver = class {
    constructor(callback) { this.callback = callback; observers.push(this); }
    observe(target, options) { this.target = target; this.options = options; }
    disconnect() { this.disconnected = true; }
};
globalThis.document = { body: {}, activeElement: null, querySelector: () => null };

class Dialog extends EventTarget {
    isConnected = true;
    open = false;
    scroll = { scrollTop: 12 };
    showModal() { this.open = true; }
    close() { this.open = false; }
    querySelector() { return this.scroll; }
}

test('missing or removed dialogs are safe during initialization and subsequent calls', async () => {
    const removed = new Dialog();
    removed.isConnected = false;
    for (const dialog of [null, removed]) {
        assert.equal(profile.initialize(dialog, {}), false);
        await profile.open(dialog);
        profile.dispose(dialog);
    }
    assert.equal(profile.scrollTop(null), 0);
    profile.restoreScroll(null, 100);
    profile.suspend(null);
    profile.close(null);
});

test('normal opening, scrolling, cancellation and closing still work', async () => {
    const dialog = new Dialog();
    const calls = [];
    const receiver = { invokeMethodAsync: method => { calls.push(method); return Promise.resolve(); } };
    assert.equal(profile.initialize(dialog, receiver), true);
    assert.equal(profile.initialize(dialog, receiver), true);
    await profile.open(dialog);
    assert.equal(dialog.open, true);
    assert.equal(profile.scrollTop(dialog), 12);
    profile.restoreScroll(dialog, 42);
    assert.equal(profile.scrollTop(dialog), 42);
    const cancel = new Event('cancel', { cancelable: true });
    dialog.dispatchEvent(cancel);
    assert.equal(cancel.defaultPrevented, true);
    assert.deepEqual(calls, ['Close']);
    profile.suspend(dialog);
    assert.equal(dialog.open, false);
    await profile.open(dialog);
    profile.close(dialog);
    assert.equal(dialog.open, false);
    profile.dispose(dialog);
});

test('removing a containing subtree cleans up listeners without server interop', async () => {
    const dialog = new Dialog();
    let calls = 0;
    profile.initialize(dialog, { invokeMethodAsync: () => { calls++; } });
    const observer = observers.at(-1);
    assert.equal(observer.target, document.body);
    assert.deepEqual(observer.options, { childList: true, subtree: true });
    await profile.open(dialog);
    dialog.isConnected = false;
    observer.callback();
    assert.equal(observer.disconnected, true);
    assert.equal(dialog.open, false);
    dialog.dispatchEvent(new Event('cancel'));
    assert.equal(calls, 0);
    await profile.open(dialog);
    assert.equal(dialog.open, false);
    profile.dispose(dialog);
});
