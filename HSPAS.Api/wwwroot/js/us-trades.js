// 美股交易紀錄管理頁
HSPAS.registerPage('us/trades', function () {
    const msgEl = document.getElementById('usTradeMsg');
    const actionMap = { 'BUY': '買進', 'SELL': '賣出', 'DIVIDEND': '股利' };
    let editModal = null;
    let allItems = [];
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

    // 預設日期
    document.getElementById('usTradeDate').value = new Date().toISOString().split('T')[0];

    // ===== 新增交易 =====
    document.getElementById('btnCreateUsTrade').addEventListener('click', async () => {
        const body = {
            tradeDate: document.getElementById('usTradeDate').value,
            stockSymbol: document.getElementById('usStockSymbol').value.trim().toUpperCase(),
            stockName: document.getElementById('usStockName').value.trim(),
            action: document.getElementById('usAction').value,
            quantity: parseFloat(document.getElementById('usQty').value) || 0,
            price: parseFloat(document.getElementById('usPrice').value) || 0,
            fee: parseFloat(document.getElementById('usFee').value) || 0,
            tax: parseFloat(document.getElementById('usTax').value) || 0,
            currency: 'USD',
            settlementDate: document.getElementById('usSettlementDate').value || null,
            exchangeRate: parseFloat(document.getElementById('usExchangeRate').value) || null,
            note: document.getElementById('usNote').value
        };

        if (!body.tradeDate || !body.stockSymbol) {
            showMsg('warning', '請填寫交易日期與股票代號。');
            return;
        }
        if (!body.stockName) {
            showMsg('warning', '請填寫股票名稱。');
            return;
        }

        try {
            const resp = await fetch('/api/us/trades', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body)
            });
            const data = await resp.json();
            if (resp.ok) {
                showMsg('success', `新增成功！編號 ${data.id}，${actionMap[body.action]} ${body.stockSymbol} ${body.stockName} ${body.quantity}股，淨額 $${Number(data.netAmount).toFixed(2)}`);
                // Reset form
                document.getElementById('usTradeDate').value = new Date().toISOString().split('T')[0];
                document.getElementById('usStockSymbol').value = '';
                document.getElementById('usStockName').value = '';
                document.getElementById('usAction').value = 'BUY';
                document.getElementById('usQty').value = '1';
                document.getElementById('usPrice').value = '';
                document.getElementById('usFee').value = '0';
                document.getElementById('usTax').value = '0';
                document.getElementById('usSettlementDate').value = '';
                document.getElementById('usExchangeRate').value = '';
                document.getElementById('usNote').value = '';
                if (document.getElementById('usTradesResult').style.display !== 'none') doQuery();
            } else {
                showMsg('danger', `新增失敗：${data.error || JSON.stringify(data)}`);
            }
        } catch (e) {
            showMsg('danger', `新增請求失敗：${e.message}`);
        }
    });

    // ===== 查詢 =====
    document.getElementById('btnQueryUsTrades').addEventListener('click', doQuery);
    doQuery();

    async function doQuery() {
        const sym = document.getElementById('usQuerySymbol').value.trim().toUpperCase();
        const from = document.getElementById('usQueryFrom').value;
        const to = document.getElementById('usQueryTo').value;
        const params = [];
        if (sym) params.push(`symbol=${sym}`);
        if (from) params.push(`from=${from}`);
        if (to) params.push(`to=${to}`);
        const url = '/api/us/trades' + (params.length ? '?' + params.join('&') : '');

        try {
            const resp = await fetch(url);
            allItems = await resp.json();
            document.getElementById('usTradesCount').textContent = allItems.length;
            renderTable();
            document.getElementById('usTradesResult').style.display = 'block';
        } catch (e) {
            showMsg('danger', `查詢失敗：${e.message}`);
        }
    }

    // 排序
    document.querySelectorAll('.sortable-ustrade').forEach(th => {
        th.style.cursor = 'pointer';
        th.addEventListener('click', () => {
            const col = th.dataset.col;
            if (sortCol === col) sortAsc = !sortAsc;
            else { sortCol = col; sortAsc = true; }
            renderTable();
        });
    });

    function renderTable() {
        if (allItems.length === 0) {
            document.getElementById('usTradesBody').innerHTML = '<tr><td colspan="13" class="text-center text-muted">查無交易紀錄</td></tr>';
            return;
        }
        const sorted = [...allItems].sort((a, b) => {
            let va = a[sortCol], vb = b[sortCol];
            if (va == null) va = '';
            if (vb == null) vb = '';
            if (typeof va === 'string') return sortAsc ? va.localeCompare(vb) : vb.localeCompare(va);
            return sortAsc ? va - vb : vb - va;
        });
        document.getElementById('usTradesBody').innerHTML = sorted.map(i => {
            const cls = i.action === 'BUY' ? 'price-up' : i.action === 'SELL' ? 'price-down' : '';
            return `<tr>
                <td>${i.id}</td>
                <td>${i.tradeDate?.substring(0,10)}</td>
                <td><strong>${i.stockSymbol}</strong></td>
                <td>${i.stockName}</td>
                <td class="${cls}">${actionMap[i.action] || i.action}</td>
                <td>${fmtQty(i.quantity)}</td>
                <td>$${fmt(i.price)}</td>
                <td>$${fmt(i.fee)}</td>
                <td>$${fmt(i.tax)}</td>
                <td class="fw-bold">$${fmt(i.netAmount)}</td>
                <td>${i.settlementDate?.substring(0,10) || '-'}</td>
                <td>${i.note || ''}</td>
                <td>
                    <button class="btn btn-sm btn-outline-primary btn-edit-us" data-id="${i.id}"
                        data-date="${i.tradeDate?.substring(0,10)}"
                        data-symbol="${i.stockSymbol}" data-name="${i.stockName}"
                        data-action="${i.action}" data-qty="${i.quantity}"
                        data-price="${i.price}" data-fee="${i.fee}"
                        data-tax="${i.tax}" data-settlement="${i.settlementDate?.substring(0,10) || ''}"
                        data-note="${(i.note || '').replace(/"/g, '&quot;')}">
                        修改
                    </button>
                </td>
            </tr>`;
        }).join('');

        document.querySelectorAll('.btn-edit-us').forEach(btn => {
            btn.addEventListener('click', () => openEditModal(btn));
        });
    }

    // ===== 修改 Modal =====
    function openEditModal(btn) {
        document.getElementById('editUsId').value = btn.dataset.id;
        document.getElementById('editUsDate').value = btn.dataset.date;
        document.getElementById('editUsSymbol').value = btn.dataset.symbol;
        document.getElementById('editUsName').value = btn.dataset.name;
        document.getElementById('editUsAction').value = btn.dataset.action;
        document.getElementById('editUsQty').value = btn.dataset.qty;
        document.getElementById('editUsPrice').value = btn.dataset.price;
        document.getElementById('editUsFee').value = btn.dataset.fee;
        document.getElementById('editUsTax').value = btn.dataset.tax;
        document.getElementById('editUsSettlement').value = btn.dataset.settlement;
        document.getElementById('editUsNote').value = btn.dataset.note;
        if (!editModal) editModal = new bootstrap.Modal(document.getElementById('editUsTradeModal'));
        editModal.show();
    }

    document.getElementById('btnSaveUsTrade').addEventListener('click', async () => {
        const id = document.getElementById('editUsId').value;
        const symbol = document.getElementById('editUsSymbol').value;
        const body = {
            tradeDate: document.getElementById('editUsDate').value,
            stockName: document.getElementById('editUsName').value.trim(),
            action: document.getElementById('editUsAction').value,
            quantity: parseFloat(document.getElementById('editUsQty').value) || 0,
            price: parseFloat(document.getElementById('editUsPrice').value) || 0,
            fee: parseFloat(document.getElementById('editUsFee').value) || 0,
            tax: parseFloat(document.getElementById('editUsTax').value) || 0,
            settlementDate: document.getElementById('editUsSettlement').value || null,
            note: document.getElementById('editUsNote').value
        };

        try {
            const resp = await fetch(`/api/us/trades/${id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body)
            });
            if (resp.ok) {
                editModal.hide();
                showMsg('success', `修改成功！編號 ${id}（${symbol}）已更新。`);
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

    document.getElementById('btnDeleteUsTrade').addEventListener('click', async () => {
        const id = document.getElementById('editUsId').value;
        const symbol = document.getElementById('editUsSymbol').value;
        const name = document.getElementById('editUsName').value;
        if (!confirm(`確定要刪除編號 ${id}（${symbol} ${name}）的交易紀錄嗎？此操作無法復原。`)) return;

        try {
            const resp = await fetch(`/api/us/trades/${id}`, { method: 'DELETE' });
            if (resp.ok) {
                editModal.hide();
                showMsg('success', `已刪除交易紀錄，編號 ${id}（${symbol} ${name}）。`);
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

    // ===== 匯入對帳單 =====
    let pendingTrades = [];
    const cathayMsg = document.getElementById('usCathayMsg');

    function showCathayMsg(type, text) {
        const icon = type === 'success' ? '✔' : type === 'danger' ? '✘' : '⚠';
        const now = new Date().toLocaleTimeString();
        cathayMsg.innerHTML = `<div class="alert alert-${type} alert-dismissible fade show" role="alert">
            <strong>${icon}</strong> ${text} <small class="text-muted ms-2">${now}</small>
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>`;
        if (type === 'success') setTimeout(() => { cathayMsg.querySelector('.alert')?.classList.remove('show'); }, 5000);
    }

    document.getElementById('btnParseUsPdf').addEventListener('click', async () => {
        const fileInput = document.getElementById('usPdfFile');
        const password = document.getElementById('usPdfPassword').value;
        if (!fileInput.files || fileInput.files.length === 0) {
            showCathayMsg('warning', '請選擇 PDF 檔案。');
            return;
        }

        const formData = new FormData();
        formData.append('file', fileInput.files[0]);
        formData.append('password', password || 'A120683373');

        showCathayMsg('info', '解析中，請稍候...');

        try {
            const resp = await fetch('/api/us/trades/cathay-statement/parse', {
                method: 'POST',
                body: formData
            });
            const data = await resp.json();
            if (resp.ok) {
                pendingTrades = data.map((item, idx) => ({ ...item, _idx: idx }));
                renderPending();
                showCathayMsg('success', `成功解析 ${data.length} 筆美股交易明細，請確認後按「確認新增」。`);
            } else {
                showCathayMsg('danger', `解析失敗：${data.error || JSON.stringify(data)}`);
            }
        } catch (e) {
            showCathayMsg('danger', `解析請求失敗：${e.message}`);
        }
    });

    function renderPending() {
        const section = document.getElementById('usPendingSection');
        const body = document.getElementById('usPendingBody');
        document.getElementById('usPendingCount').textContent = pendingTrades.length;

        if (pendingTrades.length === 0) {
            section.style.display = 'none';
            return;
        }
        section.style.display = 'block';

        body.innerHTML = pendingTrades.map((i, rowIdx) => {
            const cls = i.action === 'BUY' ? 'price-up' : i.action === 'SELL' ? 'price-down' : '';
            const actionText = i.action === 'BUY' ? '買進' : i.action === 'SELL' ? '賣出' : i.action;
            return `<tr>
                <td>${i.tradeDate}</td>
                <td><strong>${i.stockSymbol}</strong></td>
                <td>${i.stockName}</td>
                <td>${i.market || '美國'}</td>
                <td class="${cls}">${actionText}</td>
                <td>${i.currency || 'USD'}</td>
                <td>${fmtQty(i.quantity)}</td>
                <td>$${fmt(i.price)}</td>
                <td>$${fmt(i.amount)}</td>
                <td>$${fmt(i.fee)}</td>
                <td>$${fmt(i.tax)}</td>
                <td class="fw-bold">$${fmt(i.netAmount)}</td>
                <td>${i.settlementDate || '-'}</td>
                <td><input type="text" class="form-control form-control-sm us-pending-note" data-idx="${rowIdx}" value="國泰美股對帳單匯入" style="width:120px"></td>
                <td><button class="btn btn-sm btn-outline-danger btn-remove-us-pending" data-idx="${rowIdx}">移除</button></td>
            </tr>`;
        }).join('');

        document.querySelectorAll('.us-pending-note').forEach(input => {
            input.addEventListener('change', () => {
                pendingTrades[parseInt(input.dataset.idx)]._note = input.value.trim();
            });
        });
        document.querySelectorAll('.btn-remove-us-pending').forEach(btn => {
            btn.addEventListener('click', () => {
                pendingTrades.splice(parseInt(btn.dataset.idx), 1);
                renderPending();
            });
        });
    }

    document.getElementById('btnClearUsPending').addEventListener('click', () => {
        pendingTrades = [];
        renderPending();
    });

    document.getElementById('btnBatchCreateUs').addEventListener('click', async () => {
        if (pendingTrades.length === 0) {
            showCathayMsg('warning', '沒有待新增的交易明細。');
            return;
        }

        const items = pendingTrades.map(i => ({
            tradeDate: i.tradeDate,
            stockSymbol: i.stockSymbol,
            stockName: i.stockName,
            action: i.action,
            market: i.market || '美國',
            currency: i.currency || 'USD',
            quantity: i.quantity,
            price: i.price,
            amount: i.amount,
            fee: i.fee,
            tax: i.tax,
            settlementDate: i.settlementDate,
            settlementCurrency: i.settlementCurrency,
            exchangeRate: i.exchangeRate,
            netAmountTwd: i.netAmountTwd,
            tradeRef: i.tradeRef,
            note: i._note || '國泰美股對帳單匯入'
        }));

        try {
            const resp = await fetch('/api/us/trades/batch', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ items })
            });
            const data = await resp.json();
            if (resp.ok) {
                const msg = data.errorCount > 0
                    ? `成功新增 ${data.successCount} 筆，失敗 ${data.errorCount} 筆。`
                    : `已成功新增 ${data.successCount} 筆美股交易紀錄。`;
                showCathayMsg('success', msg);
                pendingTrades = [];
                renderPending();
                document.getElementById('usPdfFile').value = '';
                doQuery();
            } else {
                showCathayMsg('danger', `新增失敗：${data.error || JSON.stringify(data)}`);
            }
        } catch (e) {
            showCathayMsg('danger', `新增請求失敗：${e.message}`);
        }
    });

    function fmt(v) { return v != null ? Number(v).toFixed(2) : '-'; }
    function fmtQty(v) { return v != null ? Number(v).toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 6 }) : '-'; }
});
