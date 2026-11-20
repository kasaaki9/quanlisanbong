(() => {
    const root = document.documentElement;
    const themeButton = document.getElementById('adminThemeToggle');
    const sidebar = document.getElementById('adminSidebar');
    const sidebarButton = document.getElementById('sidebarToggle');

    function renderThemeIcon() {
        if (!themeButton) return;
        themeButton.innerHTML = root.dataset.adminTheme === 'light' ? '<i class="bi bi-moon-stars-fill"></i>' : '<i class="bi bi-sun-fill"></i>';
    }
    renderThemeIcon();
    themeButton?.addEventListener('click', () => {
        root.dataset.adminTheme = root.dataset.adminTheme === 'light' ? 'dark' : 'light';
        try { localStorage.setItem('sanphui-admin-theme', root.dataset.adminTheme); } catch (error) { }
        renderThemeIcon();
    });
    sidebarButton?.addEventListener('click', () => sidebar?.classList.toggle('open'));
    document.querySelectorAll('.admin-nav-link').forEach(link => link.addEventListener('click', () => {
        if (window.innerWidth <= 760) sidebar?.classList.remove('open');
    }));

    const moduleSearch = document.getElementById('moduleSearch');
    const moduleRows = [...document.querySelectorAll('.module-data-row')];
    const noResults = document.getElementById('moduleNoResults');
    moduleSearch?.addEventListener('input', () => {
        const query = moduleSearch.value.trim().toLocaleLowerCase('vi');
        let visibleRows = 0;
        moduleRows.forEach(row => {
            const match = row.textContent.toLocaleLowerCase('vi').includes(query);
            row.hidden = !match;
            if (match) visibleRows++;
        });
        if (noResults) noResults.hidden = query.length === 0 || visibleRows > 0;
    });
})();


