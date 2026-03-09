// 儀表板頁
HSPAS.registerPage('dashboard', async function () {
    const holdingsBody = document.getElementById('holdingsBody');
    const alertsBody = document.getElementById('alertsBody');
    let allHoldings = [];
    let sortCol = 'marketValue';
    let sortAsc = false;

    // 標題列排序
    document.querySelectorAll('.sortable-dash').forEach(th => {
        th.addEventListener('click', () => {
            const col = th.dataset.col;
            if (sortCol === col) sortAsc = !sortAsc;
            else { sortCol = col; sortAsc = true; }
            renderHoldings();
        });
    });

    // 載入持股
    try {
        const resp = await fetch('/api/dashboard/holdings');
        if (resp.ok) {
            const data = await resp.json();
            document.getElementById('totalMarketValue').textContent = fmtMoney(data.totalMarketValue);
            document.getElementById('totalCost').textContent = fmtMoney(data.totalCost);
            const pnl = data.totalUnrealizedPnL;
            const pnlEl = document.getElementById('totalPnL');
            pnlEl.textContent = (pnl >= 0 ? '+' : '') + fmtMoney(pnl);
            pnlEl.className = pnl >= 1 ? 'text-primary' : 'text-danger';
            document.getElementById('pnlCard').classList.toggle('positive', pnl > 0);
            document.getElementById('pnlCard').classList.toggle('negative', pnl < 0);
            const ret = data.totalUnrealizedReturn;
            const retEl = document.getElementById('totalReturn');
            retEl.textContent = (ret >= 0 ? '+' : '') + (ret * 100).toFixed(2) + '%';
            retEl.className = (ret * 100) >= 1 ? 'text-primary' : 'text-danger';
            document.getElementById('returnCard').classList.toggle('positive', ret > 0);
            document.getElementById('returnCard').classList.toggle('negative', ret < 0);

            if (data.items && data.items.length > 0) {
                allHoldings = data.items;
                renderHoldings();

                // 圓餅圖
                new Chart(document.getElementById('holdingsChart'), {
                    type: 'doughnut',
                    data: {
                        labels: data.items.map(i => `${i.stockId} ${i.stockName}`),
                        datasets: [{ data: data.items.map(i => i.marketValue) }]
                    },
                    options: { plugins: { legend: { position: 'right' } } }
                });
            } else {
                holdingsBody.innerHTML = '<tr><td colspan="6" class="text-center text-muted">尚無持股資料</td></tr>';
            }
        } else {
            holdingsBody.innerHTML = '<tr><td colspan="6" class="text-center text-muted">尚無持股資料</td></tr>';
        }
    } catch (e) {
        holdingsBody.innerHTML = '<tr><td colspan="6" class="text-center text-muted">載入失敗</td></tr>';
    }

    function renderHoldings() {
        const sorted = [...allHoldings].sort((a, b) => {
            let va = a[sortCol], vb = b[sortCol];
            if (va == null) va = '';
            if (vb == null) vb = '';
            if (typeof va === 'string') return sortAsc ? va.localeCompare(vb) : vb.localeCompare(va);
            return sortAsc ? va - vb : vb - va;
        });
        holdingsBody.innerHTML = sorted.map(i => `<tr>
            <td><a href="#/stock?id=${i.stockId}">${i.stockId}</a></td>
            <td>${i.stockName}</td><td>${fmtInt(i.quantity)}</td>
            <td>${fmtPrice(i.lastClosePrice)}</td><td>${fmtMoney(i.marketValue)}</td>
            <td>${(i.weightRatio * 100).toFixed(1)}%</td>
        </tr>`).join('');
    }

    // 風險警示排序
    let allAlerts = [];
    let alertSortCol = 'diff';
    let alertSortAsc = false;

    document.querySelectorAll('.sortable-alert').forEach(th => {
        th.addEventListener('click', () => {
            const col = th.dataset.col;
            if (alertSortCol === col) alertSortAsc = !alertSortAsc;
            else { alertSortCol = col; alertSortAsc = true; }
            renderAlerts();
        });
    });

    function renderAlerts() {
        if (allAlerts.length === 0) {
            alertsBody.innerHTML = '<tr><td colspan="7" class="text-center text-success">所有持股皆在季線上方</td></tr>';
            return;
        }
        const sorted = [...allAlerts].sort((a, b) => {
            let va = a[alertSortCol], vb = b[alertSortCol];
            if (va == null) va = '';
            if (vb == null) vb = '';
            if (typeof va === 'string') return alertSortAsc ? va.localeCompare(vb) : vb.localeCompare(va);
            return alertSortAsc ? va - vb : vb - va;
        });
        alertsBody.innerHTML = sorted.map(i => `<tr class="table-danger">
            <td><a href="#/stock?id=${i.stockId}">${i.stockId}</a></td>
            <td>${i.stockName}</td><td>${fmtInt(i.currentQty)}</td>
            <td>${fmtPrice(i.lastClosePrice)}</td><td>${fmtPrice(i.ma)}</td>
            <td class="price-down">${fmtPrice(i.diff)}</td>
            <td class="price-down">${(i.diffPercent * 100).toFixed(2)}%</td>
        </tr>`).join('');
    }

    // 載入風險警示
    try {
        const resp = await fetch('/api/alerts/below-quarterly-ma?days=60');
        if (resp.ok) {
            const data = await resp.json();
            allAlerts = (data.items && data.items.length > 0) ? data.items : [];
            renderAlerts();
        } else {
            alertsBody.innerHTML = '<tr><td colspan="7" class="text-center text-muted">尚無資料</td></tr>';
        }
    } catch {
        alertsBody.innerHTML = '<tr><td colspan="7" class="text-center text-muted">載入失敗</td></tr>';
    }

    function fmtMoney(v) { return v != null ? 'NT$' + Number(v).toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 0 }) : '--'; }
    function fmtPrice(v) { return v != null ? Number(v).toFixed(2) : '-'; }
    function fmtInt(v) { return v != null ? Number(v).toLocaleString() : '-'; }
});
