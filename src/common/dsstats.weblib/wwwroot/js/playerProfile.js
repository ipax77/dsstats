const sessions = new WeakMap();

export function initialize(dialog, receiver) {
    // Navigation can remove the element while the module import is in flight.
    if (!dialog?.isConnected) return false;
    if (sessions.has(dialog)) return true;
    const state = { receiver, opener: null, parent: null };
    state.cancel = event => { event.preventDefault(); receiver.invokeMethodAsync("Close"); };
    state.keys = event => {
        if (!event.target.matches('[role="tab"]')) return;
        const tabs = [...event.target.closest('[role="tablist"]').querySelectorAll('[role="tab"]')];
        let next = tabs.indexOf(event.target);
        if (event.key === "ArrowRight") next = (next + 1) % tabs.length;
        else if (event.key === "ArrowLeft") next = (next + tabs.length - 1) % tabs.length;
        else if (event.key === "Home") next = 0;
        else if (event.key === "End") next = tabs.length - 1;
        else return;
        event.preventDefault();
        tabs[next].focus();
        tabs[next].click();
    };
    dialog.addEventListener("cancel", state.cancel);
    dialog.addEventListener("keydown", state.keys);
    sessions.set(dialog, state);
    // Observe ancestors too: navigation may remove an entire containing subtree.
    state.observer = new MutationObserver(() => {
        if (!dialog.isConnected) dispose(dialog);
    });
    state.observer.observe(document.body, { childList: true, subtree: true });
    return true;
}

async function hideModal(modal) {
    if (!modal) return;
    const instance = bootstrap.Modal.getInstance(modal);
    if (!instance) return;
    await new Promise(resolve => {
        modal.addEventListener("hidden.bs.modal", resolve, { once: true });
        instance.hide();
    });
}

export async function open(dialog) {
    const state = sessions.get(dialog);
    if (!state || dialog.open) return;
    state.opener ??= document.activeElement;
    const active = document.querySelector(".modal.show");
    state.parent ??= active;
    await hideModal(active);
    if (!dialog.isConnected || !sessions.has(dialog)) return;
    dialog.showModal();
}

export function scrollTop(dialog) { return dialog?.querySelector(".profile-scroll")?.scrollTop ?? 0; }
export function restoreScroll(dialog, top) { const body = dialog?.querySelector(".profile-scroll"); if (body) body.scrollTop = top; }
export function suspend(dialog) { dialog?.close(); }

export function close(dialog) {
    dialog?.close();
    const state = sessions.get(dialog);
    if (state?.parent?.isConnected) bootstrap.Modal.getOrCreateInstance(state.parent).show();
    if (state?.opener?.isConnected) state.opener.focus();
    if (state) { state.opener = null; state.parent = null; }
}

export function dispose(dialog) {
    const state = sessions.get(dialog);
    if (!state) return;
    state.observer.disconnect();
    dialog.close();
    dialog.removeEventListener("cancel", state.cancel);
    dialog.removeEventListener("keydown", state.keys);
    sessions.delete(dialog);
}
