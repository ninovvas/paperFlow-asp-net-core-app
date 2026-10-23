/* Unobtrusive, optional theme switch. No external dependencies. */
(() => {
    const key = 'paperflow.theme';
    const html = document.documentElement;
    const controls = () => document.querySelectorAll('[data-pf-theme-toggle]');

    function readPreference() {
        try { return window.localStorage.getItem(key); }
        catch { return null; }
    }
    function persist(value) {
        try { window.localStorage.setItem(key, value); }
        catch { /* Theme still works for this page. */ }
    }
    function current() {
        return html.getAttribute('data-pf-theme') === 'dark' ? 'dark' : 'light';
    }
    function apply(mode) {
        const next = mode === 'dark' ? 'dark' : 'light';
        html.setAttribute('data-pf-theme', next);
        controls().forEach(button => {
            button.setAttribute('aria-pressed', String(next === 'dark'));
            button.setAttribute('aria-label', next === 'dark' ? 'Switch to light theme' : 'Switch to dark theme');
            button.title = next === 'dark' ? 'Switch to light theme' : 'Switch to dark theme';
            const label = button.querySelector('[data-pf-theme-label]');
            if (label) label.textContent = next === 'dark' ? 'Light mode' : 'Dark mode';
            const icon = button.querySelector('[data-pf-theme-icon]');
            if (icon) icon.className = next === 'dark' ? 'ti ti-sun' : 'ti ti-moon';
        });
    }

    // Set the preference at startup. The tiny inline head snippet in README avoids flash.
    apply(readPreference() === 'dark' ? 'dark' : 'light');

    document.addEventListener('click', (event) => {
        const button = event.target.closest('[data-pf-theme-toggle]');
        if (!button) return;
        const next = current() === 'dark' ? 'light' : 'dark';
        apply(next);
        persist(next);
    });

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => apply(current()), { once: true });
    }
})();
