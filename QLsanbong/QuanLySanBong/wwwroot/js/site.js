(() => {
    const root = document.documentElement;
    const toggle = document.getElementById('themeToggle');
    const menuButton = document.getElementById('mobileMenu');
    const nav = document.getElementById('mainNav');

    function updateThemeButton() {
        if (!toggle) return;
        const light = root.dataset.theme === 'light';
        toggle.innerHTML = light ? '<i class="bi bi-moon-stars-fill"></i><span>Tối</span>' : '<i class="bi bi-sun-fill"></i><span>Sáng</span>';
        toggle.setAttribute('aria-label', light ? 'Chuyển sang giao diện tối' : 'Chuyển sang giao diện sáng');
    }

    updateThemeButton();
    toggle?.addEventListener('click', () => {
        root.dataset.theme = root.dataset.theme === 'light' ? 'dark' : 'light';
        try { localStorage.setItem('sanphui-theme', root.dataset.theme); } catch (error) { }
        updateThemeButton();
    });

    menuButton?.addEventListener('click', () => {
        const isOpen = nav.classList.toggle('open');
        menuButton.setAttribute('aria-expanded', String(isOpen));
        menuButton.innerHTML = isOpen ? '<i class="bi bi-x-lg"></i>' : '<i class="bi bi-list"></i>';
    });

    const searchForm = document.getElementById('fieldSearch');
    const peopleSelect = document.getElementById('searchPeople');
    const searchMessage = document.getElementById('searchMessage');
    const cards = [...document.querySelectorAll('.field-card[data-people]')];
    function filterFields(people) {
        let visibleCount = 0;
        cards.forEach(card => {
            const visible = !people || card.dataset.people === people;
            card.hidden = !visible;
            if (visible) visibleCount++;
        });
        if (searchMessage) searchMessage.textContent = people ? `Có ${visibleCount} nhóm sân phù hợp.` : 'Đang hiển thị tất cả loại sân.';
        document.getElementById('fields')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
    searchForm?.addEventListener('submit', event => {
        event.preventDefault();
        filterFields(peopleSelect?.value || '');
    });
    document.querySelectorAll('[data-select-type]').forEach(link => link.addEventListener('click', () => {
        if (peopleSelect) peopleSelect.value = link.dataset.selectType;
        filterFields(link.dataset.selectType);
    }));
    document.getElementById('showAllFields')?.addEventListener('click', () => {
        if (peopleSelect) peopleSelect.value = '';
        filterFields('');
    });


    const supportForm = document.getElementById('supportForm');
    const supportInput = document.getElementById('supportInput');
    const supportMessages = document.getElementById('supportMessages');
    const supportHistory = [];

    function addSupportMessage(text, role) {
        const message = document.createElement('div');
        message.className = `support-message ${role}`;
        message.textContent = text;
        supportMessages?.append(message);
        if (supportMessages) supportMessages.scrollTop = supportMessages.scrollHeight;
        return message;
    }

    async function askSupport(question) {
        if (!question?.trim() || !supportMessages) return;
        addSupportMessage(question.trim(), 'user');
        supportHistory.push({ role: 'user', content: question.trim() });
        const typing = addSupportMessage('Đang tìm câu trả lời…', 'assistant');
        const sendButton = supportForm?.querySelector('button[type="submit"]');
        if (sendButton) sendButton.disabled = true;
        try {
            const response = await fetch('/Support/Ask', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ message: question.trim(), history: supportHistory.slice(0, -1).slice(-8) })
            });
            const data = await response.json();
            typing.remove();
            let reply = response.status === 429 ? 'Bạn gửi hơi nhanh rồi. Chờ một chút rồi thử lại nhé.' : (data.reply || 'Mình chưa trả lời được câu này. Bạn gọi 0909 123 456 để gặp nhân viên nhé.');
            if (data.mode === 'faq-ollama-unavailable') reply += '\n\n⚠️ Ollama chưa chạy. Hãy mở ứng dụng Ollama trên máy rồi gửi lại câu hỏi.';
            else if (data.mode === 'faq-model-missing') reply += '\n\n⚠️ Chưa tải model AI. Mở Terminal và chạy: & "$env:LOCALAPPDATA\\Programs\\Ollama\\ollama.exe" pull gemma3:4b';
            else if (data.mode === 'faq-ollama-timeout') reply += '\n\n⚠️ Ollama phản hồi quá lâu. Chờ model tải xong rồi thử lại nhé.';
            else if (data.mode === 'faq-ollama-error') reply += '\n\n⚠️ Ollama gặp lỗi. Kiểm tra ứng dụng Ollama và thử lại.';
            addSupportMessage(reply, 'assistant');
            supportHistory.push({ role: 'assistant', content: reply });
        } catch {
            typing.remove();
            addSupportMessage('Kết nối đang gián đoạn. Bạn thử lại hoặc gọi 0909 123 456 nhé.', 'assistant');
        } finally {
            if (sendButton) sendButton.disabled = false;
            supportInput?.focus();
        }
    }

    supportForm?.addEventListener('submit', event => {
        event.preventDefault();
        const question = supportInput?.value || '';
        if (supportInput) supportInput.value = '';
        void askSupport(question);
    });
    document.querySelectorAll('.support-suggestions button').forEach(button => button.addEventListener('click', () => void askSupport(button.textContent || '')));
})();
