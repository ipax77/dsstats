const sessions = new WeakMap();

export function initialize(modal, receiver) {
    if (!modal?.isConnected || sessions.has(modal)) return;
    const state = { opener: null, scroll: 0 };
    state.shown = () => {
        const body = modal.querySelector('.ds-detail-scroll');
        if (body) body.scrollTop = state.scroll;
    };
    state.hiding = () => {
        state.scroll = modal.querySelector('.ds-detail-scroll')?.scrollTop ?? 0;
    };
    state.hidden = () => {
        // Player profiles temporarily hide their Bootstrap parent.
        if (modal.dataset.profileSuspended === 'true') {
            delete modal.dataset.profileSuspended;
            return;
        }
        state.scroll = 0;
        if (state.opener?.isConnected) state.opener.focus({ preventScroll: true });
        state.opener = null;
        receiver.invokeMethodAsync('Dismissed');
    };
    modal.addEventListener('shown.bs.modal', state.shown);
    modal.addEventListener('hide.bs.modal', state.hiding);
    modal.addEventListener('hidden.bs.modal', state.hidden);
    sessions.set(modal, state);
    state.observer = new MutationObserver(() => {
        if (!modal.isConnected) dispose(modal);
    });
    state.observer.observe(document.body, { childList: true, subtree: true });
}

export function open(modal) {
    const state = sessions.get(modal);
    if (!state) return;
    if (!modal.classList.contains('show')) state.opener ??= document.activeElement;
    state.scroll = 0;
    const body = modal.querySelector('.ds-detail-scroll');
    if (body) body.scrollTop = 0;
    bootstrap.Modal.getOrCreateInstance(modal).show();
}

export function close(modal) {
    bootstrap.Modal.getInstance(modal)?.hide();
}

export function dispose(modal) {
    const state = sessions.get(modal);
    if (!state) return;
    state.observer.disconnect();
    modal.removeEventListener('shown.bs.modal', state.shown);
    modal.removeEventListener('hide.bs.modal', state.hiding);
    modal.removeEventListener('hidden.bs.modal', state.hidden);
    const instance = bootstrap.Modal.getInstance(modal);
    instance?.hide();
    instance?.dispose();
    sessions.delete(modal);
}
