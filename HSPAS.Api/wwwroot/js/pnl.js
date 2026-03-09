HSPAS.registerPage('pnl', async function () {
    const msgEl = document.getElementById('pnlMsg');
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
        const resp = await fetch('/api/portfolio/unrealized-summary');
        if (resp.ok) {
            const d = await resp.json();
            document.getElementById('pnlTotalCost').textContent = fmtM(d.totalCost);
            document.getElementById('pnlTotalMV').textContent = fmtM(d.totalMarketValue);
            const pnlVal = d.totalUnrealizedPnL;
            const pnlCls = pnlVal >= 1 ? 'text-primary' : 'text-danger';
            const pnlSign = pnlVal >= 1 ? '+' : '';
            const retVal = d.totalUnrealizedReturn * 100;
            const retCls2 = retVal >= 1 ? 'text-primary' : 'text-danger';
            const retSign = retVal >= 1 ? '+' : '';
            document.getElementById('pnlTotalPnL').innerHTML = `<span class="${pnlCls}">${pnlSign}${fmtM(pnlVal)}</span>`;
            document.getElementById('pnlTotalReturn').innerHTML = `<span class="${retCls2}">${retSign}${retVal.toFixed(2)}%</span>`;
        }
    } catch {}

    // 標題列排序
    document.querySelectorAll('.sortable-pnl').forEach(th => {
        th.addEventListener('click', () => {
            const col = th.dataset.col;
            if (sortCol === col) sortAsc = !sortAsc;
            else { sortCol = col; sortAsc = true; }
            renderTable();
        });
    });

    // 進入時自動載入所有持股損益
    await loadAllHoldings();

    async function loadAllHoldings() {
        document.getElementById('pnlLoading').style.display = 'block';
        try {
            const resp = await fetch('/api/dashboard/holdings');
            if (!resp.ok) throw new Error('載入失敗');
            const data = await resp.json();
            const items = data.items || [];
            document.getElementById('holdingsCount').textContent = items.length;

            if (items.length === 0) {
                document.getElementById('holdingsBody').innerHTML = '<tr><td colspan="10" class="text-center text-muted">無持股資料</td></tr>';
                document.getElementById('pnlLoading').style.display = 'none';
                return;
            }

            // 逐檔查詢未實現損益
            allDetails = [];
            for (const item of items) {
                try {
                    const r = await fetch(`/api/portfolio/stock/${item.stockId}/unrealized`);
                    if (r.ok) {
                        allDetails.push(await r.json());
                    }
                } catch {}
            }

            document.getElementById('pnlLoading').style.display = 'none';

            if (allDetails.length === 0) {
                document.getElementById('holdingsBody').innerHTML = '<tr><td colspan="10" class="text-center text-muted">無損益資料</td></tr>';
                return;
            }

            renderTable();
        } catch (e) {
            document.getElementById('pnlLoading').style.display = 'none';
            showMsg('danger', `載入持股損益失敗：${e.message}`);
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

        document.getElementById('holdingsBody').innerHTML = sorted.map(d => {
            const retPct = d.unrealizedReturn * 100;
            const retPctStr = retPct.toFixed(2);
            // 整列顏色: >=100% 藍色, >=1%且<100% 黑色, <=0% 紅色
            let retCls = '';
            if (retPct >= 100) retCls = 'text-primary';
            else if (retPct <= 0) retCls = 'text-danger';
            return `<tr class="${retCls}">
                <td>${d.stockId}</td>
                <td>${d.stockName}</td>
                <td>${fmtInt(d.currentQty)}</td>
                <td>${fmt(d.avgCost)}</td>
                <td>${fmt(d.lastClosePrice)}</td>
                <td>${fmtM(d.totalCost)}</td>
                <td>${fmtM(d.marketValue)}</td>
                <td>${fmtM(d.unrealizedPnL)}</td>
                <td class="fw-bold">${retPctStr}%</td>
                <td><a href="#/stock?id=${d.stockId}" target="_blank" class="btn btn-sm btn-outline-primary">詳細</a></td>
            </tr>`;
        }).join('');
    }

    function fmtM(v) { return 'NT$' + Number(v).toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 0 }); }
    function fmt(v) { return v != null ? Number(v).toFixed(2) : '-'; }
    function fmtInt(v) { return v != null ? Number(v).toLocaleString() : '-'; }
});
