HSPAS.registerPage('us/pnl', async function () {
    const msgEl = document.getElementById('usPnlMsg');
    let allDetails = [];
    let sortCol = 'unrealizedReturn';
    let sortAsc = false;

    function showMsg(type, text) {
        const icon = type === 'success' ? '✔' : type === 'danger' ? '✘' : '⚠';
        const now = new Date().toLocaleTimeString();
        msgEl.innerHTML = `<div class="alert alert-${type} alert-dismissible fade show" role="alert">
            <strong>${icon}</strong> ${text} <small class="text-muted ms-2">${now}</small>
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>`;
        msgEl.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        if (type === 'success') setTimeout(() => { msgEl.querySelector('.alert')?.classList.remove('show'); }, 5000);
    }

    // 載入整體摘要
    try {
        const resp = await fetch('/api/us/portfolio/unrealized-summary');
        if (resp.ok) {
            const d = await resp.json();
            document.getElementById('usPnlTotalCost').textContent = fmtUsd(d.totalCost);
            document.getElementById('usPnlTotalMV').textContent = fmtUsd(d.totalMarketValue);
            const pnlVal = d.totalUnrealizedPnL;
            const pnlCls = pnlVal >= 1 ? 'text-primary' : 'text-danger';
            const pnlSign = pnlVal >= 1 ? '+' : '';
            const retVal = d.totalUnrealizedReturn * 100;
            const retCls = retVal >= 1 ? 'text-primary' : 'text-danger';
            const retSign = retVal >= 1 ? '+' : '';
            document.getElementById('usPnlTotalPnL').innerHTML = `<span class="${pnlCls}">${pnlSign}${fmtUsd(pnlVal)}</span>`;
            document.getElementById('usPnlTotalReturn').innerHTML = `<span class="${retCls}">${retSign}${retVal.toFixed(2)}%</span>`;
        }
    } catch {}

    // 排序
    document.querySelectorAll('.sortable-uspnl').forEach(th => {
        th.addEventListener('click', () => {
            const col = th.dataset.col;
            if (sortCol === col) sortAsc = !sortAsc;
            else { sortCol = col; sortAsc = true; }
            renderTable();
        });
    });

    // 載入所有持股損益
    await loadAllHoldings();

    async function loadAllHoldings() {
        document.getElementById('usPnlLoading').style.display = 'block';
        try {
            const resp = await fetch('/api/us/portfolio/holdings');
            if (!resp.ok) throw new Error('載入失敗');
            const data = await resp.json();
            const items = data.items || [];
            document.getElementById('usHoldingsCount').textContent = items.length;

            if (items.length === 0) {
                document.getElementById('usHoldingsBody').innerHTML = '<tr><td colspan="9" class="text-center text-muted">無美股持股資料</td></tr>';
                document.getElementById('usPnlLoading').style.display = 'none';
                return;
            }

            // 逐檔查詢未實現損益
            allDetails = [];
            for (const item of items) {
                try {
                    const r = await fetch(`/api/us/portfolio/stock/${item.stockSymbol}/unrealized`);
                    if (r.ok) allDetails.push(await r.json());
                } catch {}
            }

            document.getElementById('usPnlLoading').style.display = 'none';

            if (allDetails.length === 0) {
                document.getElementById('usHoldingsBody').innerHTML = '<tr><td colspan="9" class="text-center text-muted">無損益資料</td></tr>';
                return;
            }

            renderTable();
        } catch (e) {
            document.getElementById('usPnlLoading').style.display = 'none';
            showMsg('danger', `載入美股損益失敗：${e.message}`);
        }
    }

    function renderTable() {
        const sorted = [...allDetails].sort((a, b) => {
            let va = a[sortCol], vb = b[sortCol];
            if (va == null) va = '';
            if (vb == null) vb = '';
            if (typeof va === 'string') return sortAsc ? va.localeCompare(vb) : vb.localeCompare(va);
            return sortAsc ? va - vb : vb - va;
        });

        document.getElementById('usHoldingsBody').innerHTML = sorted.map(d => {
            const retPct = d.unrealizedReturn * 100;
            const retPctStr = retPct.toFixed(2);
            let retCls = '';
            if (retPct >= 100) retCls = 'text-primary';
            else if (retPct <= 0) retCls = 'text-danger';
            return `<tr class="${retCls}">
                <td><strong>${d.stockSymbol}</strong></td>
                <td>${d.stockName}</td>
                <td>${fmtQty(d.currentQty)}</td>
                <td>${fmtPrice(d.avgCost)}</td>
                <td>${d.lastPrice ? fmtPrice(d.lastPrice) : '--'}</td>
                <td>${fmtUsd(d.totalCost)}</td>
                <td>${fmtUsd(d.marketValue)}</td>
                <td>${fmtUsd(d.unrealizedPnL)}</td>
                <td class="fw-bold">${retPctStr}%</td>
            </tr>`;
        }).join('');
    }

    function fmtUsd(v) { return '$' + Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
    function fmtPrice(v) { return v != null ? '$' + Number(v).toFixed(2) : '--'; }
    function fmtQty(v) { return v != null ? Number(v).toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 6 }) : '-'; }
});
