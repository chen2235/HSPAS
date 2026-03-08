// SPA Hash Router — 負責依 hash 載入對應頁面 JS，Sidebar 由 API 動態載入三層選單
(function () {
    const mainContent = document.getElementById('main-content');
    const sidebar = document.getElementById('sidebar');

    // 頁面模組註冊表（每個頁面需提供 init 函式）
    const pages = {};

    // 註冊頁面模組
    window.HSPAS = {
        registerPage(name, initFn) {
            pages[name] = initFn;
        },
        reloadSidebar: null // 供外部呼叫重新載入選單
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
        'settings/menu-sorting': { html: '/pages/menu-sorting.html', js: ['/js/menu-sorting.js'] },
    };

    // RouteUrl → 路由 hash 的對應（DB 中的 RouteUrl 轉成前端 hash）
    const routeUrlToHash = {
        '/dashboard': 'dashboard',
        '/calendar': 'calendar',
        '/stock': 'stock',
        '/etf': 'etf',
        '/trades': 'trades',
        '/dca': 'dca',
        '/pnl': 'pnl',
        '/recommendations': 'recommendations',
        '/alerts': 'alerts',
        '/admin/history-backfill': 'backfill',
        '/settings': 'settings',
        '/settings/menu-sorting': 'settings/menu-sorting',
    };

    // FuncCode → Bootstrap Icon 對應
    const funcCodeIcons = {
        'STOCK_ROOT': 'bi-bar-chart-line',
        'STOCK_ANALYSIS': 'bi-graph-up-arrow',
        'STOCK_DASH': 'bi-speedometer2',
        'STOCK_CAL': 'bi-calendar3',
        'STOCK_STOCK': 'bi-graph-up',
        'STOCK_ETF': 'bi-collection',
        'STOCK_TRD': 'bi-receipt',
        'STOCK_DCA': 'bi-piggy-bank',
        'STOCK_PNL': 'bi-calculator',
        'STOCK_IDEAS': 'bi-lightbulb',
        'STOCK_ALERT': 'bi-exclamation-triangle',
        'STOCK_HIST': 'bi-cloud-download',
        'STOCK_CONF': 'bi-gear',
        'HEALTH_ROOT': 'bi-heart-pulse',
        'HEALTH_CHECKUP': 'bi-clipboard2-pulse',
        'HEALTH_CHECKUP_QTR_UP': 'bi-upload',
        'HEALTH_CHECKUP_QTR_DASH': 'bi-speedometer',
        'HEALTH_CHECKUP_CO_UP': 'bi-building-up',
        'HEALTH_CHECKUP_CO_DASH': 'bi-building',
        'LIFE_ROOT': 'bi-wallet2',
        'LIFE_SIS': 'bi-people',
        'LIFE_SIS_RECORD': 'bi-journal-text',
        'LIFE_SIS_YEARLY_ANALYSIS': 'bi-bar-chart-steps',
        'ADMIN_ROOT': 'bi-gear-wide-connected',
        'ADMIN_FUNC': 'bi-menu-button-wide',
        'ADMIN_MENU_SORT': 'bi-list-nested',
    };

    // 已載入的 script URL set（避免重複載入）
    const loadedScripts = new Set();

    // ===== Sidebar 動態載入 =====
    async function loadSidebar() {
        try {
            const resp = await fetch('/api/menu/tree');
            if (!resp.ok) {
                sidebar.innerHTML = '<div class="text-warning p-2"><small>選單載入失敗</small></div>';
                renderFallbackSidebar();
                return;
            }
            const tree = await resp.json();
            renderSidebar(tree);
        } catch {
            renderFallbackSidebar();
        }
    }

    function renderSidebar(tree) {
        let html = '';
        for (const l1 of tree) {
            // Level 1 作為主功能標題（可展開/收合，預設收合）
            const icon1 = funcCodeIcons[l1.funcCode] || 'bi-folder';
            html += `<div class="nav-section sidebar-l1" data-func="${l1.funcCode}">
                <span class="icon"><i class="bi ${icon1}"></i></span>${l1.displayName}
                <i class="bi bi-chevron-right sidebar-toggle ms-auto"></i>
            </div>`;

            // 預設收合（加 collapsed class）
            html += `<div class="sidebar-l1-children collapsed" data-parent="${l1.funcCode}">`;
            for (const l2 of (l1.children || [])) {
                const icon2 = funcCodeIcons[l2.funcCode] || 'bi-folder2';
                html += `<div class="nav-subsection sidebar-l2" data-func="${l2.funcCode}">
                    <span class="icon"><i class="bi ${icon2}"></i></span>${l2.displayName}
                    <i class="bi bi-chevron-right sidebar-toggle ms-auto"></i>
                </div>`;

                // L2 子層也預設收合
                html += `<div class="sidebar-l2-children collapsed" data-parent="${l2.funcCode}">`;
                for (const l3 of (l2.children || [])) {
                    const icon3 = funcCodeIcons[l3.funcCode] || 'bi-file-earmark';
                    const hashKey = l3.routeUrl ? (routeUrlToHash[l3.routeUrl] || '') : '';
                    if (hashKey && routes[hashKey]) {
                        html += `<a class="nav-link" href="#/${hashKey}" data-page="${hashKey}">
                            <span class="icon"><i class="bi ${icon3}"></i></span>${l3.displayName}
                        </a>`;
                    } else {
                        html += `<a class="nav-link disabled" href="#" data-page="">
                            <span class="icon"><i class="bi ${icon3}"></i></span>${l3.displayName}
                            <span class="badge bg-secondary ms-auto" style="font-size:.6rem">預留</span>
                        </a>`;
                    }
                }
                html += '</div>';
            }
            html += '</div>';
        }

        sidebar.innerHTML = html;

        // 綁定展開/收合事件
        sidebar.querySelectorAll('.sidebar-l1').forEach(el => {
            el.addEventListener('click', () => {
                const children = sidebar.querySelector(`.sidebar-l1-children[data-parent="${el.dataset.func}"]`);
                if (children) {
                    children.classList.toggle('collapsed');
                    el.querySelector('.sidebar-toggle').classList.toggle('bi-chevron-down');
                    el.querySelector('.sidebar-toggle').classList.toggle('bi-chevron-right');
                }
            });
        });

        sidebar.querySelectorAll('.sidebar-l2').forEach(el => {
            el.addEventListener('click', () => {
                const children = sidebar.querySelector(`.sidebar-l2-children[data-parent="${el.dataset.func}"]`);
                if (children) {
                    children.classList.toggle('collapsed');
                    el.querySelector('.sidebar-toggle').classList.toggle('bi-chevron-down');
                    el.querySelector('.sidebar-toggle').classList.toggle('bi-chevron-right');
                }
            });
        });

        // 自動展開當前頁面所在的主功能
        expandActiveMenu();
    }

    // 根據當前 hash 自動展開對應的 L1/L2
    function expandActiveMenu() {
        let hash = location.hash.replace('#/', '') || 'dashboard';
        const qIdx = hash.indexOf('?');
        if (qIdx !== -1) hash = hash.substring(0, qIdx);

        // 找到 active 的 nav-link 並展開其父層
        const activeLink = sidebar.querySelector(`a.nav-link[data-page="${hash}"]`);
        if (activeLink) {
            // 展開 L2
            const l2Children = activeLink.closest('.sidebar-l2-children');
            if (l2Children) {
                l2Children.classList.remove('collapsed');
                const l2Func = l2Children.dataset.parent;
                const l2Toggle = sidebar.querySelector(`.sidebar-l2[data-func="${l2Func}"] .sidebar-toggle`);
                if (l2Toggle) { l2Toggle.classList.remove('bi-chevron-right'); l2Toggle.classList.add('bi-chevron-down'); }
            }
            // 展開 L1
            const l1Children = activeLink.closest('.sidebar-l1-children');
            if (l1Children) {
                l1Children.classList.remove('collapsed');
                const l1Func = l1Children.dataset.parent;
                const l1Toggle = sidebar.querySelector(`.sidebar-l1[data-func="${l1Func}"] .sidebar-toggle`);
                if (l1Toggle) { l1Toggle.classList.remove('bi-chevron-right'); l1Toggle.classList.add('bi-chevron-down'); }
            }
        }

        highlightSidebar();
    }

    function renderFallbackSidebar() {
        // API 不可用時的靜態 fallback
        sidebar.innerHTML = `
            <div class="nav-section">概覽</div>
            <a class="nav-link" href="#/dashboard" data-page="dashboard"><span class="icon"><i class="bi bi-speedometer2"></i></span>儀表板</a>
            <div class="nav-section">行情查詢</div>
            <a class="nav-link" href="#/calendar" data-page="calendar"><span class="icon"><i class="bi bi-calendar3"></i></span>日曆行情查詢</a>
            <a class="nav-link" href="#/stock" data-page="stock"><span class="icon"><i class="bi bi-graph-up"></i></span>個股/ETF 查詢</a>
            <a class="nav-link" href="#/etf" data-page="etf"><span class="icon"><i class="bi bi-collection"></i></span>ETF 專區</a>
            <div class="nav-section">紀錄維護</div>
            <a class="nav-link" href="#/trades" data-page="trades"><span class="icon"><i class="bi bi-receipt"></i></span>交易紀錄管理</a>
            <a class="nav-link" href="#/dca" data-page="dca"><span class="icon"><i class="bi bi-piggy-bank"></i></span>定期定額管理</a>
            <div class="nav-section">分析報表</div>
            <a class="nav-link" href="#/pnl" data-page="pnl"><span class="icon"><i class="bi bi-calculator"></i></span>損益與成本查詢</a>
            <div class="nav-section">決策輔助</div>
            <a class="nav-link" href="#/recommendations" data-page="recommendations"><span class="icon"><i class="bi bi-lightbulb"></i></span>投資建議</a>
            <a class="nav-link" href="#/alerts" data-page="alerts"><span class="icon"><i class="bi bi-exclamation-triangle"></i></span>風險警示</a>
            <div class="nav-section">管理工具</div>
            <a class="nav-link" href="#/backfill" data-page="backfill"><span class="icon"><i class="bi bi-cloud-download"></i></span>歷史資料回補</a>
            <a class="nav-link" href="#/settings" data-page="settings"><span class="icon"><i class="bi bi-gear"></i></span>系統設定</a>
        `;
    }

    function highlightSidebar() {
        let hash = location.hash.replace('#/', '') || 'dashboard';
        const qIdx = hash.indexOf('?');
        if (qIdx !== -1) hash = hash.substring(0, qIdx);

        sidebar.querySelectorAll('a.nav-link').forEach(a => {
            a.classList.toggle('active', a.dataset.page === hash);
        });
    }

    // 提供外部重新載入 sidebar
    window.HSPAS.reloadSidebar = loadSidebar;

    // ===== 路由導航 =====
    async function navigate() {
        let hash = location.hash.replace('#/', '') || 'dashboard';
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
        highlightSidebar();

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
                console.warn('Script not found:', src);
                resolve();
            };
            document.body.appendChild(s);
        });
    }

    // 初始化：先載入 sidebar，再導航到當前頁面
    loadSidebar().then(() => {
        navigate();
    });
    window.addEventListener('hashchange', navigate);
})();
