(() => {
    const widget = document.getElementById('supportWidget');
    const panel = document.getElementById('supportPanel');
    const openButton = document.getElementById('supportOpen');
    const closeButton = document.getElementById('supportClose');
    const input = document.getElementById('supportInput');

    if (!widget || !panel || !openButton) return;

    let closeFallback;

    const finishClose = () => {
        if (!panel.classList.contains('is-closing')) return;
        window.clearTimeout(closeFallback);
        panel.hidden = true;
        panel.classList.remove('is-opening', 'is-closing');
        widget.classList.remove('is-open');
        openButton.setAttribute('aria-expanded', 'false');
    };

    const openPanel = () => {
        window.clearTimeout(closeFallback);
        panel.hidden = false;
        panel.classList.remove('is-closing', 'is-opening');
        void panel.offsetWidth;
        widget.classList.add('is-open');
        panel.classList.add('is-opening');
        openButton.setAttribute('aria-expanded', 'true');
        input?.focus();
    };

    const closePanel = () => {
        if (panel.hidden || panel.classList.contains('is-closing')) return;
        panel.classList.remove('is-opening');
        panel.classList.add('is-closing');
        closeFallback = window.setTimeout(finishClose, 450);
    };

    openButton.addEventListener('click', () => {
        if (panel.hidden) openPanel();
        else closePanel();
    });

    closeButton?.addEventListener('click', closePanel);

    panel.addEventListener('animationend', event => {
        if (event.target !== panel) return;
        if (event.animationName === 'support-chat-open')
            panel.classList.remove('is-opening');
        else if (event.animationName === 'support-chat-close')
            finishClose();
    });
})();