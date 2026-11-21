(() => {
    const panel = document.getElementById('supportPanel');
    const openButton = document.getElementById('supportOpen');
    const closeButton = document.getElementById('supportClose');
    const input = document.getElementById('supportInput');

    if (!panel || !openButton) return;

    openButton.addEventListener('click', () => {
        panel.hidden = !panel.hidden;
        openButton.setAttribute('aria-expanded', String(!panel.hidden));
        if (!panel.hidden) input?.focus();
    });

    closeButton?.addEventListener('click', () => {
        panel.hidden = true;
        openButton.setAttribute('aria-expanded', 'false');
    });
})();