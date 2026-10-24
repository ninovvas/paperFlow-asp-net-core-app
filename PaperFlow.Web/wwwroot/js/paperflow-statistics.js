(() => {
    'use strict';
    if (!window.Chart || !window.paperflowStatistics) {
        console.warn('Chart.js is unavailable; statistical summaries remain visible.');
        return;
    }
    const data = window.paperflowStatistics;
    const charts = [];
    const palette = ['#687bf2', '#23a78b', '#42a5ee', '#f5ad4b', '#cf87ee', '#e77b8f', '#58b9bd', '#8695b2', '#8ac861', '#c2a1ff'];
    const ctx = id => document.getElementById(id);
    const isDark = () => document.documentElement.getAttribute('data-pf-theme') === 'dark';
    const colors = () => ({ text: isDark() ? '#d8e3f6' : '#455777', grid: isDark() ? 'rgba(196,214,240,.12)' : 'rgba(36,55,80,.10)' });
    const axis = horizontal => ({ beginAtZero: true, ticks: { color: colors().text, precision: 0 }, grid: { color: colors().grid }, ...(horizontal ? {} : {}) });
    const common = () => ({ responsive: true, maintainAspectRatio: false, plugins: { legend: { labels: { color: colors().text } }, tooltip: { enabled: true } } });
    const monthly = data.monthly || [];
    charts.push(new Chart(ctx('monthlyChart'), { type: 'bar', data: { labels: monthly.map(x => x.month), datasets: [{ label: 'Added', data: monthly.map(x => x.added), backgroundColor: palette[0], borderRadius: 5 }, { label: 'Completed', data: monthly.map(x => x.completed), backgroundColor: palette[1], borderRadius: 5 }] }, options: { ...common(), scales: { x: { ticks: { color: colors().text }, grid: { display: false } }, y: axis() } } }));
    const pie = (id, rows) => { charts.push(new Chart(ctx(id), { type: 'doughnut', data: { labels: rows.map(x => x.label), datasets: [{ data: rows.map(x => x.value), backgroundColor: rows.map((_, i) => palette[i % palette.length]), borderWidth: 0 }] }, options: { ...common(), cutout: '65%' } })); };
    pie('statusChart', data.statuses || []);
    pie('sourceChart', data.sources || []);
    const categories = (data.categories || []).slice().reverse();
    charts.push(new Chart(ctx('categoryChart'), { type: 'bar', data: { labels: categories.map(x => x.label), datasets: [{ label: 'Papers', data: categories.map(x => x.value), backgroundColor: palette[2], borderRadius: 5 }] }, options: { ...common(), indexAxis: 'y', scales: { x: axis(), y: { ticks: { color: colors().text }, grid: { display: false } } } } }));
    new MutationObserver(() => { const color = colors(); charts.forEach(chart => { chart.options.plugins.legend.labels.color = color.text; for (const scale of Object.values(chart.options.scales || {})) { if (scale.ticks) scale.ticks.color = color.text; if (scale.grid && scale.grid.display !== false) scale.grid.color = color.grid; } chart.update('none'); }); }).observe(document.documentElement, { attributes: true, attributeFilter: ['data-pf-theme'] });
})();

