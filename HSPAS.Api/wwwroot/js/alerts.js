HSPAS.registerPage('alerts', function () {
    const msgEl = document.getElementById('alertMsg');

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

    document.getElementById('btnAlertRefresh').addEventListener('click', load);

    // 進入時自動載入
    load();

    async function load() {
        document.getElementById('alertLoading').style.display = 'block';
        msgEl.innerHTML = '';
        try {
            const resp = await fetch('/api/alerts/below-quarterly-ma?days=60');
            const data = await resp.json();
            document.getElementById('alertLoading').style.display = 'none';
            const body = document.getElementById('alertListBody');
            if (!data.items || data.items.length === 0) {
                body.innerHTML = '<tr><td colspan="7" class="text-center text-success">所有持股皆在季線上方</td></tr>';
                showMsg('success', '檢查完成：所有持股皆在季線上方，無需警示。');
                return;
            }
            body.innerHTML = data.items.map(i => `<tr class="table-danger">
                <td><a href="#/stock?id=${i.stockId}">${i.stockId}</a></td>
                <td>${i.stockName}</td><td>${i.currentQty}</td>
                <td>${Number(i.lastClosePrice).toFixed(2)}</td><td>${Number(i.ma).toFixed(2)}</td>
                <td class="price-down">${Number(i.diff).toFixed(2)}</td>
                <td class="price-down">${(i.diffPercent * 100).toFixed(2)}%</td>
            </tr>`).join('');
            showMsg('warning', `檢查完成：共 ${data.items.length} 檔持股跌破季線，請留意風險。`);
        } catch (e) {
            document.getElementById('alertLoading').style.display = 'none';
            showMsg('danger', `檢查失敗：${e.message}`);
        }
    }
});
