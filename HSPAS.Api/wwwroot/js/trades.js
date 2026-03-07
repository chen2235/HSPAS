// 交易紀錄管理頁
HSPAS.registerPage('trades', function () {
    const msgEl = document.getElementById('tradeMsg');
    const actionMap = { 'BUY': '買進', 'SELL': '賣出', 'DIVIDEND': '股利' };
    let editModal = null;
    let allTradeItems = [];
    let sortCol = 'tradeDate';
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

    // 交易日期預設為今天
    document.getElementById('tradeDate').value = new Date().toISOString().split('T')[0];

    // ===== 股票代號自動帶出名稱 =====
    let nameTimer = null;
    document.getElementById('tradeStockId').addEventListener('input', function () {
        clearTimeout(nameTimer);
        const sid = this.value.trim();
        const nameInput = document.getElementById('tradeStockName');
        const hint = document.getElementById('stockNameHint');
        if (!sid || sid.length < 4) {
            nameInput.value = '';
            hint.textContent = '';
            return;
        }
        hint.textContent = '查詢中...';
        nameTimer = setTimeout(async () => {
            try {
                const resp = await fetch(`/api/trades/stock-name/${sid}`);
                if (resp.ok) {
                    const data = await resp.json();
                    if (data.stockName) {
                        nameInput.value = data.stockName;
                        hint.textContent = '';
                    } else {
                        nameInput.value = '';
                        hint.textContent = '查無此代號，請手動輸入名稱';
                        nameInput.removeAttribute('readonly');
                    }
                }
            } catch {
                hint.textContent = '查詢失敗';
            }
        }, 400);
    });

    // ===== 新增交易 =====
    document.getElementById('btnCreateTrade').addEventListener('click', async () => {
        const nameInput = document.getElementById('tradeStockName');
        if (!nameInput.value && nameInput.hasAttribute('readonly')) {
            nameInput.removeAttribute('readonly');
            nameInput.focus();
            showMsg('warning', '請輸入股票名稱。');
            return;
        }

        const body = {
            tradeDate: document.getElementById('tradeDate').value,
            stockId: document.getElementById('tradeStockId').value.trim(),
            stockName: nameInput.value.trim(),
            action: document.getElementById('tradeAction').value,
            quantity: parseInt(document.getElementById('tradeQty').value) || 0,
            price: parseFloat(document.getElementById('tradePrice').value) || 0,
            fee: parseFloat(document.getElementById('tradeFee').value) || 0,
            tax: parseFloat(document.getElementById('tradeTax').value) || 0,
            otherCost: parseFloat(document.getElementById('tradeOther').value) || 0,
            note: document.getElementById('tradeNote').value
        };

        if (!body.tradeDate || !body.stockId) {
            showMsg('warning', '請填寫交易日期與股票代號。');
            return;
        }
        if (!body.stockName) {
            showMsg('warning', '請填寫股票名稱。');
            return;
        }

        try {
            const resp = await fetch('/api/trades', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body)
            });
            const data = await resp.json();
            if (resp.ok) {
                showMsg('success', `新增交易紀錄成功！編號 ${data.id}，${actionMap[body.action]} ${body.stockId} ${body.stockName} ${body.quantity}股，淨額 ${Number(data.netAmount).toLocaleString()}`);
                // 重置表單
                document.getElementById('tradeDate').value = new Date().toISOString().split('T')[0];
                document.getElementById('tradeStockId').value = '';
                nameInput.value = '';
                nameInput.setAttribute('readonly', '');
                document.getElementById('stockNameHint').textContent = '';
                document.getElementById('tradeAction').value = 'BUY';
                document.getElementById('tradeQty').value = '1';
                document.getElementById('tradePrice').value = '';
                document.getElementById('tradeFee').value = '0';
                document.getElementById('tradeTax').value = '0';
                document.getElementById('tradeOther').value = '0';
                document.getElementById('tradeNote').value = '';
                // 自動更新查詢結果
                if (document.getElementById('tradesResult').style.display !== 'none') {
                    doQuery();
                }
            } else {
                showMsg('danger', `新增失敗：${data.error || JSON.stringify(data)}`);
            }
        } catch (e) {
            showMsg('danger', `新增請求失敗：${e.message}`);
        }
    });

    // ===== 查詢交易紀錄 =====
    document.getElementById('btnQueryTrades').addEventListener('click', doQuery);

    async function doQuery() {
        const sid = document.getElementById('queryStockId').value.trim();
        const from = document.getElementById('queryFrom').value;
        const to = document.getElementById('queryTo').value;

        const params = [];
        if (sid) params.push(`stockId=${sid}`);
        if (from) params.push(`from=${from}`);
        if (to) params.push(`to=${to}`);
        const url = '/api/trades' + (params.length ? '?' + params.join('&') : '');

        try {
            const resp = await fetch(url);
            allTradeItems = await resp.json();
            document.getElementById('tradesCount').textContent = allTradeItems.length;
            renderTable();
            document.getElementById('tradesResult').style.display = 'block';
        } catch (e) {
            showMsg('danger', `查詢失敗：${e.message}`);
        }
    }

    // 標題列排序
    document.querySelectorAll('.sortable-trade').forEach(th => {
        th.style.cursor = 'pointer';
        th.addEventListener('click', () => {
            const col = th.dataset.col;
            if (sortCol === col) sortAsc = !sortAsc;
            else { sortCol = col; sortAsc = true; }
            renderTable();
        });
    });

    function renderTable() {
        if (allTradeItems.length === 0) {
            document.getElementById('tradesBody').innerHTML = '<tr><td colspan="13" class="text-center text-muted">查無交易紀錄</td></tr>';
            return;
        }
        const sorted = [...allTradeItems].sort((a, b) => {
            let va = a[sortCol], vb = b[sortCol];
            if (va == null) va = '';
            if (vb == null) vb = '';
            if (typeof va === 'string') return sortAsc ? va.localeCompare(vb) : vb.localeCompare(va);
            return sortAsc ? va - vb : vb - va;
        });
        document.getElementById('tradesBody').innerHTML = sorted.map(i => {
            const cls = i.action === 'BUY' ? 'price-up' : i.action === 'SELL' ? 'price-down' : '';
            return `<tr>
                <td>${i.id}</td>
                <td>${i.tradeDate?.substring(0,10)}</td>
                <td>${i.stockId}</td>
                <td>${i.stockName}</td>
                <td class="${cls}">${actionMap[i.action] || i.action}</td>
                <td>${fmtInt(i.quantity)}</td>
                <td>${fmt(i.price)}</td>
                <td>${fmt(i.fee)}</td>
                <td>${fmt(i.tax)}</td>
                <td>${fmt(i.otherCost)}</td>
                <td class="fw-bold">${fmt(i.netAmount)}</td>
                <td>${i.note || ''}</td>
                <td>
                    <button class="btn btn-sm btn-outline-primary btn-edit" data-id="${i.id}"
                        data-date="${i.tradeDate?.substring(0,10)}"
                        data-stockid="${i.stockId}" data-stockname="${i.stockName}"
                        data-action="${i.action}" data-qty="${i.quantity}"
                        data-price="${i.price}" data-fee="${i.fee}"
                        data-tax="${i.tax}" data-other="${i.otherCost || 0}"
                        data-note="${(i.note || '').replace(/"/g, '&quot;')}">
                        修改
                    </button>
                </td>
            </tr>`;
        }).join('');

        document.querySelectorAll('.btn-edit').forEach(btn => {
            btn.addEventListener('click', () => openEditModal(btn));
        });
    }

    // ===== 修改 Modal =====
    function openEditModal(btn) {
        document.getElementById('editId').value = btn.dataset.id;
        document.getElementById('editDate').value = btn.dataset.date;
        document.getElementById('editStockId').value = btn.dataset.stockid;
        document.getElementById('editStockName').value = btn.dataset.stockname;
        document.getElementById('editAction').value = btn.dataset.action;
        document.getElementById('editQty').value = btn.dataset.qty;
        document.getElementById('editPrice').value = btn.dataset.price;
        document.getElementById('editFee').value = btn.dataset.fee;
        document.getElementById('editTax').value = btn.dataset.tax;
        document.getElementById('editOther').value = btn.dataset.other;
        document.getElementById('editNote').value = btn.dataset.note;

        if (!editModal) {
            editModal = new bootstrap.Modal(document.getElementById('editTradeModal'));
        }
        editModal.show();
    }

    // 儲存修改
    document.getElementById('btnSaveTrade').addEventListener('click', async () => {
        const id = document.getElementById('editId').value;
        const stockId = document.getElementById('editStockId').value;
        const body = {
            tradeDate: document.getElementById('editDate').value,
            stockName: document.getElementById('editStockName').value.trim(),
            action: document.getElementById('editAction').value,
            quantity: parseInt(document.getElementById('editQty').value) || 0,
            price: parseFloat(document.getElementById('editPrice').value) || 0,
            fee: parseFloat(document.getElementById('editFee').value) || 0,
            tax: parseFloat(document.getElementById('editTax').value) || 0,
            otherCost: parseFloat(document.getElementById('editOther').value) || 0,
            note: document.getElementById('editNote').value
        };

        try {
            const resp = await fetch(`/api/trades/${id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body)
            });
            if (resp.ok) {
                editModal.hide();
                showMsg('success', `修改成功！編號 ${id}（${stockId} ${body.stockName}）已更新。`);
                doQuery();
            } else {
                const data = await resp.json();
                editModal.hide();
                showMsg('danger', `修改失敗：${data.error || JSON.stringify(data)}`);
            }
        } catch (e) {
            editModal.hide();
            showMsg('danger', `修改請求失敗：${e.message}`);
        }
    });

    // 刪除
    document.getElementById('btnDeleteTrade').addEventListener('click', async () => {
        const id = document.getElementById('editId').value;
        const stockId = document.getElementById('editStockId').value;
        const stockName = document.getElementById('editStockName').value;
        if (!confirm(`確定要刪除編號 ${id}（${stockId} ${stockName}）的交易紀錄嗎？此操作無法復原。`)) return;

        try {
            const resp = await fetch(`/api/trades/${id}`, { method: 'DELETE' });
            if (resp.ok) {
                editModal.hide();
                showMsg('success', `已刪除交易紀錄，編號 ${id}（${stockId} ${stockName}）。`);
                doQuery();
            } else {
                editModal.hide();
                showMsg('danger', `刪除失敗，編號 ${id}。`);
            }
        } catch (e) {
            editModal.hide();
            showMsg('danger', `刪除請求失敗：${e.message}`);
        }
    });

    function fmt(v) { return v != null ? Number(v).toFixed(2) : '-'; }
    function fmtInt(v) { return v != null ? Number(v).toLocaleString() : '-'; }
});
