(() => {
    const menu = document.getElementById('notificationMenu');
    const toggle = document.getElementById('notificationToggle');
    const dropdown = document.getElementById('notificationDropdown');
    const badge = document.getElementById('notificationBadge');
    const preview = document.getElementById('notificationPreview');
    if (!menu || !toggle || !dropdown || !preview) return;

    const close = () => {
        dropdown.hidden = true;
        toggle.setAttribute('aria-expanded', 'false');
    };

    toggle.addEventListener('click', event => {
        event.stopPropagation();
        dropdown.hidden = !dropdown.hidden;
        toggle.setAttribute('aria-expanded', String(!dropdown.hidden));
    });
    document.addEventListener('click', event => {
        if (!menu.contains(event.target)) close();
    });
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape') close();
    });

    function showEmpty(message) {
        const empty = document.createElement('p');
        empty.className = 'notification-empty-preview';
        empty.textContent = message;
        preview.replaceChildren(empty);
    }

    async function loadNotifications() {
        try {
            const response = await fetch('/ThongBao/Recent', { headers: { Accept: 'application/json' }, credentials: 'same-origin' });
            if (!response.ok) throw new Error('Notification request failed');
            const data = await response.json();
            const count = Number(data.unreadCount || 0);
            if (badge) {
                badge.textContent = count > 99 ? '99+' : String(count);
                badge.hidden = count === 0;
            }
            if (!Array.isArray(data.items) || data.items.length === 0) {
                showEmpty('Bạn chưa có thông báo mới.');
                return;
            }
            const list = document.createDocumentFragment();
            for (const item of data.items) {
                const link = document.createElement('a');
                link.className = `notification-preview-item${item.trangThai ? ' is-read' : ''}`;
                link.href = '/ThongBao';
                const dot = document.createElement('span');
                dot.className = 'notification-preview-dot';
                const copy = document.createElement('span');
                copy.className = 'notification-preview-copy';
                const message = document.createElement('strong');
                message.textContent = item.noiDung || 'Thông báo mới';
                const date = document.createElement('time');
                date.textContent = item.ngayGui || '';
                copy.append(message, date);
                link.append(dot, copy);
                list.append(link);
            }
            preview.replaceChildren(list);
        } catch {
            showEmpty('Chưa tải được thông báo. Hãy làm mới trang để thử lại.');
        }
    }

    void loadNotifications();
})();