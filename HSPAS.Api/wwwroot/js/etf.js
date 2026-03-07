HSPAS.registerPage('etf', async function () {
    const body = document.getElementById('etfListBody');
    try {
        const resp = await fetch('/api/etf/list');
        const items = await resp.json();
        if (items.length === 0) { body.innerHTML = '<tr><td colspan="5" class="text-center text-muted">尚無 ETF 資料。請先在 EtfInfo 資料表中新增。</td></tr>'; return; }
        body.innerHTML = items.map(i => `<tr>
            <td><a href="#/stock?id=${i.etfId}">${i.etfId}</a></td>
            <td>${i.etfName}</td><td>${i.category || '-'}</td><td>${i.issuer || '-'}</td>
            <td>${i.isActive ? '<span class="badge bg-success">啟用</span>' : '<span class="badge bg-secondary">停用</span>'}</td>
        </tr>`).join('');
    } catch { body.innerHTML = '<tr><td colspan="5" class="text-center text-muted">載入失敗</td></tr>'; }
});
