// 美股儀表板頁
HSPAS.registerPage('us/dashboard', async function () {
    const holdingsBody = document.getElementById('usDashHoldingsBody');
    const recentBody = document.getElementById('usRecentBody');
    let allHoldings = [];
    let sortCol = 'marketValue';
    let sortAsc = false;

    // 排序
    document.querySelectorAll('.sortable-usdash').forEach(th => {
        th.addEventListener('click', () => {
            const col = th.dataset.col;
            if (sortCol === col) sortAsc = !sortAsc;
            else { sortCol = col; sortAsc = true; }
            renderHoldings();
        });
    });

    // 載入持股
    try {
        const resp = await fetch('/api/us/portfolio/holdings');
        if (resp.ok) {
            const data = await resp.json();
            document.getElementById('usTotalMV').textContent = fmtUsd(data.totalMarketValue);
            document.getElementById('usTotalCost').textContent = fmtUsd(data.totalCost);

            const pnl = data.totalUnrealizedPnL;
            const pnlEl = document.getElementById('usTotalPnL');
            pnlEl.textContent = (pnl >= 0 ? '+' : '') + fmtUsd(pnl);
            pnlEl.className = pnl >= 1 ? 'text-primary' : 'text-danger';
            document.getElementById('usPnlCard').classList.toggle('positive', pnl > 0);
            document.getElementById('usPnlCard').classList.toggle('negative', pnl < 0);

            const ret = data.totalUnrealizedReturn;
            const retEl = document.getElementById('usTotalReturn');
            retEl.textContent = (ret >= 0 ? '+' : '') + (ret * 100).toFixed(2) + '%';
            retEl.className = (ret * 100) >= 1 ? 'text-primary' : 'text-danger';
            document.getElementById('usReturnCard').classList.toggle('positive', ret > 0);
            document.getElementById('usReturnCard').classList.toggle('negative', ret < 0);

            if (data.items && data.items.length > 0) {
                allHoldings = data.items;
                renderHoldings();

                // 圓餅圖
                new Chart(document.getElementById('usHoldingsChart'), {
                    type: 'doughnut',
                    data: {
                        labels: data.items.map(i => `${i.stockSymbol} ${i.stockName}`),
                        datasets: [{ data: data.items.map(i => i.marketValue) }]
                    },
                    options: { plugins: { legend: { position: 'right' } } }
                });
            } else {
                holdingsBody.innerHTML = '<tr><td colspan="6" class="text-center text-muted">尚無美股持股資料</td></tr>';
            }
        } else {
            holdingsBody.innerHTML = '<tr><td colspan="6" class="text-center text-muted">尚無美股持股資料</td></tr>';
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
            <td><strong>${i.stockSymbol}</strong></td>
            <td>${i.stockName}</td>
            <td>${fmtQty(i.quantity)}</td>
            <td>${i.lastPrice ? fmtPrice(i.lastPrice) : '--'}</td>
            <td>${fmtUsd(i.marketValue)}</td>
            <td>${(i.weightRatio * 100).toFixed(1)}%</td>
        </tr>`).join('');
    }

    // 載入最近交易
    try {
        const resp = await fetch('/api/us/trades/recent?count=10');
        if (resp.ok) {
            const items = await resp.json();
            if (items.length > 0) {
                recentBody.innerHTML = items.map(i => {
                    const actionText = i.action === 'BUY' ? '買進' : i.action === 'SELL' ? '賣出' : '股利';
                    const cls = i.action === 'BUY' ? 'price-up' : i.action === 'SELL' ? 'price-down' : '';
                    return `<tr>
                        <td>${i.tradeDate?.substring(0,10)}</td>
                        <td><strong>${i.stockSymbol}</strong></td>
                        <td>${i.stockName}</td>
                        <td class="${cls}">${actionText}</td>
                        <td>${fmtQty(i.quantity)}</td>
                        <td>${fmtPrice(i.price)}</td>
                        <td class="fw-bold">${fmtUsd(i.netAmount)}</td>
                    </tr>`;
                }).join('');
            } else {
                recentBody.innerHTML = '<tr><td colspan="7" class="text-center text-muted">尚無交易紀錄</td></tr>';
            }
        }
    } catch {
        recentBody.innerHTML = '<tr><td colspan="7" class="text-center text-muted">載入失敗</td></tr>';
    }

    function fmtUsd(v) { return '$' + Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
    function fmtPrice(v) { return v != null ? '$' + Number(v).toFixed(2) : '--'; }
    function fmtQty(v) { return v != null ? Number(v).toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 6 }) : '-'; }
});
