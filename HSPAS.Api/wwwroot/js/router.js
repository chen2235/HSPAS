// SPA Hash Router — 負責依 hash 載入對應頁面 JS
(function () {
    const mainContent = document.getElementById('main-content');
    const sidebarLinks = document.querySelectorAll('#sidebar a.nav-link');

    // 頁面模組註冊表（每個頁面需提供 init 函式）
    const pages = {};

    // 註冊頁面模組
    window.HSPAS = {
        registerPage(name, initFn) {
            pages[name] = initFn;
        }
    };

    // 路由表：hash → { html 路徑, js 路徑陣列 }
    const routes = {
        'dashboard':       { html: '/pages/dashboard.html',  js: ['/js/dashboard.js'] },
        'calendar':        { html: '/pages/calendar.html',   js: ['/js/calendar.js'] },
        'stock':           { html: '/pages/stock.html',      js: ['/js/stock.js'] },
        'etf':             { html: '/pages/etf.html',        js: ['/js/etf.js'] },
        'trades':          { html: '/pages/trades.html',     js: ['/js/trades.js'] },
        'dca':             { html: '/pages/dca.html',        js: ['/js/dca.js'] },
        'pnl':             { html: '/pages/pnl.html',        js: ['/js/pnl.js'] },
        'recommendations': { html: '/pages/recommendations.html', js: ['/js/recommendations.js'] },
        'alerts':          { html: '/pages/alerts.html',     js: ['/js/alerts.js'] },
        'backfill':        { html: '/pages/backfill.html',   js: ['/js/backfill.js'] },
        'settings':        { html: '/pages/settings.html',   js: ['/js/settings.js'] },
    };

    // 已載入的 script URL set（避免重複載入）
    const loadedScripts = new Set();

    async function navigate() {
        let hash = location.hash.replace('#/', '') || 'dashboard';
        // 支援 #/stock?id=2330 的 query string
        const qIdx = hash.indexOf('?');
        let query = '';
        if (qIdx !== -1) {
            query = hash.substring(qIdx);
            hash = hash.substring(0, qIdx);
        }

        const route = routes[hash];
        if (!route) {
            mainContent.innerHTML = '<div class="alert alert-warning mt-3">此頁面不存在。</div>';
            return;
        }

        // 高亮 sidebar
        sidebarLinks.forEach(a => {
            a.classList.toggle('active', a.dataset.page === hash);
        });

        // 載入 HTML fragment
        try {
            const resp = await fetch(route.html);
            if (!resp.ok) {
                mainContent.innerHTML = `<div class="alert alert-secondary mt-3">此功能尚未實作（${hash}）。</div>`;
                return;
            }
            mainContent.innerHTML = await resp.text();
        } catch {
            mainContent.innerHTML = `<div class="alert alert-danger mt-3">載入頁面失敗。</div>`;
            return;
        }

        // 載入 JS（只載入一次）
        for (const src of route.js) {
            if (!loadedScripts.has(src)) {
                await loadScript(src);
                loadedScripts.add(src);
            }
        }

        // 呼叫頁面 init（每次切頁都要重新 init）
        if (pages[hash]) {
            pages[hash](query);
        }
    }

    function loadScript(src) {
        return new Promise((resolve, reject) => {
            const s = document.createElement('script');
            s.src = src;
            s.onload = resolve;
            s.onerror = () => {
                // 檔案不存在時不阻塞
                console.warn('Script not found:', src);
                resolve();
            };
            document.body.appendChild(s);
        });
    }

    window.addEventListener('hashchange', navigate);
    navigate();
})();
