HSPAS.registerPage('dca', async function () {
    const msgEl = document.getElementById('dcaMsg');
    const body = document.getElementById('dcaPlansBody');
    let editModal = null;

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

    // 開始日期預設為今天
    document.getElementById('dcaStartDate').value = new Date().toISOString().split('T')[0];

    // 股票代號自動帶出名稱
    let nameTimer = null;
    document.getElementById('dcaStockId').addEventListener('input', function () {
        clearTimeout(nameTimer);
        const sid = this.value.trim();
        const nameInput = document.getElementById('dcaStockName');
        const hint = document.getElementById('dcaStockNameHint');
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
                        // 自動帶入計畫名稱
                        const planNameInput = document.getElementById('dcaPlanName');
                        if (!planNameInput.value) {
                            planNameInput.value = `存${data.stockName}`;
                        }
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

    await loadPlans();

    document.getElementById('btnCreateDca').addEventListener('click', async () => {
        const nameInput = document.getElementById('dcaStockName');
        if (!nameInput.value && nameInput.hasAttribute('readonly')) {
            nameInput.removeAttribute('readonly');
            nameInput.focus();
            showMsg('warning', '請輸入股票名稱。');
            return;
        }
        const req = {
            planName: document.getElementById('dcaPlanName').value,
            stockId: document.getElementById('dcaStockId').value.trim(),
            stockName: nameInput.value.trim(),
            startDate: document.getElementById('dcaStartDate').value,
            cycleType: document.getElementById('dcaCycleType').value,
            cycleDay: parseInt(document.getElementById('dcaCycleDay').value) || 1,
            amount: parseFloat(document.getElementById('dcaAmount').value) || 0
        };
        if (!req.stockId || !req.startDate) {
            showMsg('warning', '請填寫股票代號與開始日期。');
            return;
        }
        if (!req.planName) {
            showMsg('warning', '請填寫計畫名稱。');
            return;
        }
        if (!req.stockName) {
            showMsg('warning', '請填寫股票名稱。');
            return;
        }
        try {
            const resp = await fetch('/api/dca/plans', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(req) });
            const data = await resp.json();
            if (resp.ok) {
                showMsg('success', `新增定期定額約定成功！編號 ${data.id}，「${req.planName}」${req.stockId} ${req.stockName}，${req.cycleType === 'MONTHLY' ? '每月' : '每週'}第${req.cycleDay}日，金額 ${Number(req.amount).toLocaleString()}`);
                await loadPlans();
            } else {
                showMsg('danger', `新增失敗：${data.error || JSON.stringify(data)}`);
            }
        } catch (e) {
            showMsg('danger', `新增請求失敗：${e.message}`);
        }
    });

    // 編輯 Modal
    function openEditModal(btn) {
        document.getElementById('editDcaId').value = btn.dataset.id;
        document.getElementById('editDcaPlanName').value = btn.dataset.planname;
        document.getElementById('editDcaStock').value = `${btn.dataset.stockid} ${btn.dataset.stockname}`;
        document.getElementById('editDcaAmount').value = btn.dataset.amount;
        document.getElementById('editDcaIsActive').value = btn.dataset.active;
        document.getElementById('editDcaEndDate').value = btn.dataset.enddate || '';
        document.getElementById('editDcaNote').value = btn.dataset.note || '';

        if (!editModal) {
            editModal = new bootstrap.Modal(document.getElementById('editDcaModal'));
        }
        editModal.show();
    }

    document.getElementById('btnSaveDca').addEventListener('click', async () => {
        const id = document.getElementById('editDcaId').value;
        const reqBody = {
            planName: document.getElementById('editDcaPlanName').value.trim(),
            isActive: document.getElementById('editDcaIsActive').value === 'true',
            amount: parseFloat(document.getElementById('editDcaAmount').value) || 0,
            endDate: document.getElementById('editDcaEndDate').value || null,
            note: document.getElementById('editDcaNote').value
        };
        try {
            const resp = await fetch(`/api/dca/plans/${id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(reqBody)
            });
            if (resp.ok) {
                editModal.hide();
                showMsg('success', `修改成功！約定編號 ${id}「${reqBody.planName}」已更新。${reqBody.isActive ? '' : '（已停用）'}`);
                await loadPlans();
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

    async function loadPlans() {
        try {
            const resp = await fetch('/api/dca/plans');
            const plans = await resp.json();
            if (plans.length === 0) {
                body.innerHTML = '<tr><td colspan="8" class="text-center text-muted">無約定</td></tr>';
                renderCycleSummary([]);
                return;
            }
            body.innerHTML = plans.map(p => `<tr>
                <td>${p.id}</td><td>${p.planName}</td><td>${p.stockId} ${p.stockName}</td>
                <td>${p.cycleType === 'MONTHLY' ? '每月' : p.cycleType === 'WEEKLY' ? '每週' : p.cycleType} / 第${p.cycleDay}${p.cycleType === 'WEEKLY' ? '天' : '日'}</td><td>${Number(p.amount).toLocaleString()}</td>
                <td>${p.isActive ? '<span class="badge bg-success">啟用</span>' : '<span class="badge bg-secondary">停用</span>'}</td>
                <td>${p.startDate?.substring(0,10)}</td>
                <td>
                    <button class="btn btn-sm btn-outline-primary btn-edit-dca"
                        data-id="${p.id}" data-planname="${(p.planName || '').replace(/"/g, '&quot;')}"
                        data-stockid="${p.stockId}" data-stockname="${p.stockName}"
                        data-amount="${p.amount}" data-active="${p.isActive}"
                        data-enddate="${p.endDate?.substring(0,10) || ''}"
                        data-note="${(p.note || '').replace(/"/g, '&quot;')}">
                        修改
                    </button>
                </td>
            </tr>`).join('');

            document.querySelectorAll('.btn-edit-dca').forEach(btn => {
                btn.addEventListener('click', () => openEditModal(btn));
            });

            renderCycleSummary(plans);
        } catch {
            body.innerHTML = '<tr><td colspan="8" class="text-center text-muted">載入失敗</td></tr>';
            renderCycleSummary([]);
        }
    }

    function renderCycleSummary(plans) {
        const summaryBody = document.getElementById('dcaCycleSummaryBody');
        const activePlans = plans.filter(p => p.isActive);
        if (activePlans.length === 0) {
            summaryBody.innerHTML = '<tr><td colspan="3" class="text-center text-muted">無啟用中的約定</td></tr>';
            return;
        }
        const cycleLabel = { 'MONTHLY': '每月', 'WEEKLY': '每週' };
        // group by cycleType + cycleDay
        const grouped = {};
        activePlans.forEach(p => {
            const key = `${p.cycleType}|${p.cycleDay}`;
            if (!grouped[key]) grouped[key] = { cycleType: p.cycleType, cycleDay: p.cycleDay, count: 0, total: 0 };
            grouped[key].count++;
            grouped[key].total += Number(p.amount);
        });
        const sortedGroups = Object.values(grouped).sort((a, b) => {
            if (a.cycleType !== b.cycleType) return a.cycleType.localeCompare(b.cycleType);
            return a.cycleDay - b.cycleDay;
        });
        const rows = sortedGroups.map(g => {
            const label = `${cycleLabel[g.cycleType] || g.cycleType} / 第${g.cycleDay}${g.cycleType === 'WEEKLY' ? '天' : '日'}`;
            return `<tr>
                <td>${label}</td>
                <td>${g.count} 檔</td>
                <td class="fw-bold">NT$${g.total.toLocaleString()}</td>
            </tr>`;
        }).join('');
        const grandTotal = activePlans.reduce((s, p) => s + Number(p.amount), 0);
        summaryBody.innerHTML = rows + `<tr class="table-info fw-bold">
            <td>合計（啟用中）</td>
            <td>${activePlans.length} 檔</td>
            <td>NT$${grandTotal.toLocaleString()}</td>
        </tr>`;
    }
});
