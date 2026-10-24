(() => {
    const root = document.querySelector('[data-pf-library-search]');
    if (!root) return;
    const input = root.querySelector('[data-pf-search-input]');
    const results = root.querySelector('[data-pf-search-results]');
    const endpoint = root.dataset.endpoint;
    if (!input || !results || !endpoint) return;

    let timer;
    let controller;
    let sequence = 0;
    let activeIndex = -1;
    let links = [];

    const hide = () => {
        results.hidden = true;
        results.replaceChildren();
        input.setAttribute('aria-expanded', 'false');
        activeIndex = -1;
        links = [];
    };
    const showText = (message) => {
        results.replaceChildren();
        const node = document.createElement('div');
        node.className = 'pf-search-message';
        node.textContent = message;
        results.append(node);
        results.hidden = false;
        input.setAttribute('aria-expanded', 'true');
    };
    const markActive = (index) => {
        activeIndex = index;
        links.forEach((link, i) => {
            link.classList.toggle('is-active', i === index);
            link.setAttribute('aria-selected', String(i === index));
        });
        if (index >= 0) links[index].scrollIntoView({ block: 'nearest' });
    };
    const render = (papers) => {
        if (!papers.length) { showText('No matching papers in your library.'); return; }
        results.replaceChildren();
        papers.forEach((paper, i) => {
            const link = document.createElement('a');
            link.href = paper.url;
            link.className = 'pf-search-suggestion';
            link.setAttribute('role', 'option');
            link.id = `pf-suggestion-${i}`;
            const title = document.createElement('span');
            title.className = 'pf-search-suggestion-title';
            title.textContent = paper.title;
            const meta = document.createElement('small');
            meta.textContent = [paper.publicationDate, paper.status].filter(Boolean).join(' · ');
            link.append(title, meta);
            results.append(link);
        });
        links = Array.from(results.querySelectorAll('a'));
        results.hidden = false;
        input.setAttribute('aria-expanded', 'true');
    };
    input.addEventListener('input', () => {
        clearTimeout(timer);
        controller?.abort();
        const thisSequence = ++sequence;
        const query = input.value.trim();
        if (query.length < 2) { hide(); return; }
        timer = setTimeout(async () => {
            controller = new AbortController();
            showText('Searching your library…');
            try {
                const response = await fetch(`${endpoint}?q=${encodeURIComponent(query)}`, {
                    signal: controller.signal, headers: { Accept: 'application/json' }, credentials: 'same-origin'
                });
                if (!response.ok) throw new Error('Search unavailable');
                const papers = await response.json();
                if (thisSequence === sequence) render(papers);
            } catch (error) {
                if (error.name !== 'AbortError' && thisSequence === sequence)
                    showText('Suggestions unavailable. Press Enter to search all saved papers.');
            }
        }, 350);
    });
    input.addEventListener('keydown', (event) => {
        if (results.hidden || !links.length) { if (event.key === 'Escape') hide(); return; }
        if (event.key === 'ArrowDown') { event.preventDefault(); markActive((activeIndex + 1) % links.length); }
        if (event.key === 'ArrowUp') { event.preventDefault(); markActive((activeIndex - 1 + links.length) % links.length); }
        if (event.key === 'Escape') { hide(); }
        if (event.key === 'Enter' && activeIndex >= 0) { event.preventDefault(); window.location.assign(links[activeIndex].href); }
    });
    document.addEventListener('pointerdown', event => {
        if (!root.contains(event.target)) hide();
    });
    root.addEventListener('submit', () => { clearTimeout(timer); controller?.abort(); });
})();

