HSPAS.registerPage('life/utility/water/period-records', async function () {
    const msgEl = document.getElementById('waterMsg');
    const confirmCard = document.getElementById('confirmCard');

    const FIXED_ADDRESS = '新北市汐止區福山街60巷12號四樓';
    const FIXED_WATER_NO = 'K-22-020975-0';
    const FIXED_METER_NO = 'C108015226';

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

    // ===== Upload PDF =====
    document.getElementById('btnUploadWater').addEventListener('click', async () => {
        const fileInput = document.getElementById('waterPdfFile');
        if (!fileInput.files.length) { showMsg('warning', '請選擇 PDF 檔案'); return; }

        const spinner = document.getElementById('uploadSpinner');
        spinner.classList.remove('d-none');

        const formData = new FormData();
        formData.append('file', fileInput.files[0]);

        try {
            const resp = await fetch('/api/life/utility/water/upload', { method: 'POST', body: formData });
            if (resp.ok) {
                const d = await resp.json();

                const isDuplicate = await checkDuplicate(d.waterNo, d.billingEndDate);
                if (isDuplicate) {
                    showMsg('warning', '此水費已紀錄，無需重覆紀錄');
                    fileInput.value = '';
                    confirmCard.classList.add('d-none');
                    return;
                }

                showMsg('success', `解析成功！用水度數: ${d.totalUsage ?? d.currentUsage} 度，應繳總金額: NT$${Number(d.totalAmount).toLocaleString()}。請確認下方資料後儲存。`);
                fileInput.value = '';
                fillConfirmForm(d);
                confirmCard.classList.remove('d-none');
                confirmCard.scrollIntoView({ behavior: 'smooth', block: 'start' });
            } else {
                const err = await resp.json();
                showMsg('danger', err.error || '解析失敗');
            }
        } catch (e) {
            showMsg('danger', `上傳失敗：${e.message}`);
        } finally {
            spinner.classList.add('d-none');
        }
    });

    async function checkDuplicate(waterNo, billingEndDate) {
        try {
            const resp = await fetch('/api/life/utility/water/period-records');
            if (!resp.ok) return false;
            const list = await resp.json();
            return list.some(r => r.billingEndDate === billingEndDate);
        } catch {
            return false;
        }
    }

    function fillConfirmForm(d) {
        document.getElementById('cfmStartDate').value = d.billingStartDate || '';
        document.getElementById('cfmEndDate').value = d.billingEndDate || '';
        document.getElementById('cfmDays').value = d.billingDays || '';
        document.getElementById('cfmTotalUsage').value = d.totalUsage || '';
        document.getElementById('cfmCurrentUsage').value = d.currentUsage || '';
        document.getElementById('cfmCurrentReading').value = d.currentMeterReading || '';
        document.getElementById('cfmPreviousReading').value = d.previousMeterReading || '';
        document.getElementById('cfmTotalAmount').value = d.totalAmount || '';
        document.getElementById('cfmRawDetailJson').value = d.rawDetailJson || '';
        document.getElementById('cfmBillingPeriodText').value = d.billingPeriodText || '';
        document.getElementById('cfmRemark').value = '';
        document.getElementById('cfmRemarkCount').textContent = '0';

        const detailEl = document.getElementById('cfmDetailItems');
        if (d.rawDetailJson) {
            try {
                const raw = JSON.parse(d.rawDetailJson);
                if (raw.Items && raw.Items.length > 0) {
                    detailEl.innerHTML = `<table class="table table-sm mb-0">
                        <tbody>${raw.Items.map(i => `<tr>
                            <td>${i.Name}</td>
                            <td class="text-end ${i.Amount < 0 ? 'text-success' : ''}">NT$${Number(i.Amount).toLocaleString(undefined, {minimumFractionDigits: 1})}</td>
                        </tr>`).join('')}</tbody>
                    </table>`;
                    return;
                }
            } catch {}
        }
        detailEl.innerHTML = '<span class="text-muted">無明細</span>';
    }

    // ===== Confirm save =====
    document.getElementById('btnConfirmSave').addEventListener('click', async () => {
        const body = {
            waterAddress: FIXED_ADDRESS,
            waterNo: FIXED_WATER_NO,
            meterNo: FIXED_METER_NO,
            billingStartDate: document.getElementById('cfmStartDate').value,
            billingEndDate: document.getElementById('cfmEndDate').value,
            billingDays: parseInt(document.getElementById('cfmDays').value) || null,
            billingPeriodText: document.getElementById('cfmBillingPeriodText').value || null,
            totalUsage: parseInt(document.getElementById('cfmTotalUsage').value) || null,
            currentUsage: parseInt(document.getElementById('cfmCurrentUsage').value) || 0,
            currentMeterReading: parseInt(document.getElementById('cfmCurrentReading').value) || 0,
            previousMeterReading: parseInt(document.getElementById('cfmPreviousReading').value) || 0,
            totalAmount: parseFloat(document.getElementById('cfmTotalAmount').value) || 0,
            rawDetailJson: document.getElementById('cfmRawDetailJson').value || null,
            remark: document.getElementById('cfmRemark').value || null,
        };

        if (!body.billingStartDate || !body.billingEndDate) {
            showMsg('warning', '計費起始日、計費結束日為必填');
            return;
        }

        try {
            const resp = await fetch('/api/life/utility/water/save', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
            });
            if (resp.ok) {
                const saved = await resp.json();
                showMsg('success', `儲存成功！用水度數: ${saved.totalUsage ?? saved.currentUsage} 度，應繳總金額: NT$${Number(saved.totalAmount).toLocaleString()}`);
                confirmCard.classList.add('d-none');
                await loadRecords();
            } else {
                const err = await resp.json();
                showMsg('danger', err.error || '儲存失敗');
            }
        } catch (e) {
            showMsg('danger', `儲存失敗：${e.message}`);
        }
    });

    document.getElementById('btnCancelConfirm').addEventListener('click', () => {
        confirmCard.classList.add('d-none');
    });

    document.getElementById('cfmRemark').addEventListener('input', (e) => {
        document.getElementById('cfmRemarkCount').textContent = e.target.value.length;
    });
    document.getElementById('editRemark').addEventListener('input', (e) => {
        document.getElementById('editRemarkCount').textContent = e.target.value.length;
    });

    // Load all records
    async function loadRecords() {
        try {
            const resp = await fetch('/api/life/utility/water/period-records');
            if (!resp.ok) throw new Error('載入失敗');
            const list = await resp.json();
            document.getElementById('waterCount').textContent = list.length;

            if (list.length === 0) {
                document.getElementById('waterBody').innerHTML = '<tr><td colspan="8" class="text-center text-muted">尚無資料</td></tr>';
                return;
            }

            function truncate(str, max) {
                if (!str) return '';
                return str.length > max ? str.substring(0, max) + '…' : str;
            }

            document.getElementById('waterBody').innerHTML = list.map(r => `<tr>
                <td>${r.billingPeriodText || ''}</td>
                <td class="text-end">${r.totalUsage != null ? r.totalUsage : '-'}</td>
                <td class="text-end">${r.currentUsage}</td>
                <td class="text-end">${r.currentMeterReading}</td>
                <td class="text-end">${r.previousMeterReading}</td>
                <td class="text-end">NT$${Number(r.totalAmount).toLocaleString()}</td>
                <td class="small" title="${(r.remark || '').replace(/"/g, '&quot;')}">${truncate(r.remark, 20) || '-'}</td>
                <td>
                    <button class="btn btn-sm btn-outline-info btn-detail" data-id="${r.id}">明細</button>
                    <button class="btn btn-sm btn-outline-warning btn-edit" data-id="${r.id}">修改</button>
                    <button class="btn btn-sm btn-outline-danger btn-delete" data-id="${r.id}">刪除</button>
                </td>
            </tr>`).join('');

            document.querySelectorAll('.btn-detail').forEach(btn => {
                btn.addEventListener('click', () => showDetail(btn.dataset.id));
            });
            document.querySelectorAll('.btn-edit').forEach(btn => {
                btn.addEventListener('click', () => showEdit(btn.dataset.id));
            });
            document.querySelectorAll('.btn-delete').forEach(btn => {
                btn.addEventListener('click', () => doDelete(btn.dataset.id));
            });
        } catch (e) {
            showMsg('danger', `載入失敗：${e.message}`);
        }
    }

    // Show detail modal
    async function showDetail(id) {
        try {
            const resp = await fetch(`/api/life/utility/water/period-records/${id}`);
            if (!resp.ok) throw new Error('載入失敗');
            const d = await resp.json();

            let detailItems = '';
            if (d.rawDetailJson) {
                try {
                    const raw = JSON.parse(d.rawDetailJson);
                    if (raw.Items && raw.Items.length > 0) {
                        detailItems = `<h6 class="mt-3">水費明細</h6>
                        <table class="table table-sm">
                            <thead><tr><th>項目</th><th class="text-end">金額</th></tr></thead>
                            <tbody>${raw.Items.map(i => `<tr>
                                <td>${i.Name}</td>
                                <td class="text-end ${i.Amount < 0 ? 'text-success' : ''}">NT$${Number(i.Amount).toLocaleString(undefined, {minimumFractionDigits: 1})}</td>
                            </tr>`).join('')}</tbody>
                        </table>`;
                    }
                } catch {}
            }

            document.getElementById('detailContent').innerHTML = `
                <div class="row">
                    <div class="col-md-6">
                        <h6>基本資訊</h6>
                        <table class="table table-sm">
                            <tr><td class="text-muted">用水地址</td><td>${d.waterAddress}</td></tr>
                            <tr><td class="text-muted">水號</td><td>${d.waterNo}</td></tr>
                            <tr><td class="text-muted">水表號碼</td><td>${d.meterNo}</td></tr>
                        </table>
                    </div>
                    <div class="col-md-6">
                        <h6>計費資訊</h6>
                        <table class="table table-sm">
                            <tr><td class="text-muted">計費期間</td><td>${d.billingPeriodText || (d.billingStartDate + ' ~ ' + d.billingEndDate)}</td></tr>
                            <tr><td class="text-muted">計費天數</td><td>${d.billingDays != null ? d.billingDays + ' 天' : '-'}</td></tr>
                            <tr><td class="text-muted">總用水度數</td><td>${d.totalUsage != null ? d.totalUsage + ' 度' : '-'}</td></tr>
                            <tr><td class="text-muted">本期用水度數</td><td>${d.currentUsage} 度</td></tr>
                            <tr><td class="text-muted">本期指針</td><td>${d.currentMeterReading}</td></tr>
                            <tr><td class="text-muted">上期指針</td><td>${d.previousMeterReading}</td></tr>
                        </table>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <h6>金額</h6>
                        <table class="table table-sm">
                            <tr><td class="text-muted">應繳總金額</td><td class="fw-bold">NT$${Number(d.totalAmount).toLocaleString()}</td></tr>
                        </table>
                    </div>
                    <div class="col-md-6">${detailItems}</div>
                </div>
                <div class="mt-3">
                    <h6>備註 <small class="text-muted">(最多500字)</small></h6>
                    <textarea id="detailRemark" class="form-control" rows="4" maxlength="500" placeholder="輸入備註…">${d.remark || ''}</textarea>
                    <div class="d-flex justify-content-between align-items-center mt-2">
                        <small class="text-muted"><span id="detailRemarkCount">${(d.remark || '').length}</span> / 500</small>
                        <button id="btnSaveDetailRemark" class="btn btn-sm btn-outline-primary" data-id="${d.id}">儲存備註</button>
                    </div>
                </div>
            `;

            const remarkEl = document.getElementById('detailRemark');
            const countEl = document.getElementById('detailRemarkCount');
            remarkEl.addEventListener('input', () => { countEl.textContent = remarkEl.value.length; });

            document.getElementById('btnSaveDetailRemark').addEventListener('click', async (e) => {
                const rid = e.target.dataset.id;
                try {
                    const resp = await fetch(`/api/life/utility/water/period-records/${rid}`, {
                        method: 'PUT',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ remark: remarkEl.value || null }),
                    });
                    if (resp.ok) {
                        showMsg('success', '備註已儲存');
                        await loadRecords();
                    } else {
                        showMsg('danger', '備註儲存失敗');
                    }
                } catch (err) {
                    showMsg('danger', `備註儲存失敗：${err.message}`);
                }
            });

            new bootstrap.Modal(document.getElementById('detailModal')).show();
        } catch (e) {
            showMsg('danger', `載入明細失敗：${e.message}`);
        }
    }

    // Show edit modal
    async function showEdit(id) {
        try {
            const resp = await fetch(`/api/life/utility/water/period-records/${id}`);
            if (!resp.ok) throw new Error('載入失敗');
            const d = await resp.json();

            document.getElementById('editId').value = d.id;
            document.getElementById('editStartDate').value = d.billingStartDate;
            document.getElementById('editEndDate').value = d.billingEndDate;
            document.getElementById('editDays').value = d.billingDays || '';
            document.getElementById('editTotalUsage').value = d.totalUsage || '';
            document.getElementById('editCurrentUsage').value = d.currentUsage;
            document.getElementById('editCurrentReading').value = d.currentMeterReading;
            document.getElementById('editPreviousReading').value = d.previousMeterReading;
            document.getElementById('editAmount').value = d.totalAmount;
            document.getElementById('editRemark').value = d.remark || '';
            document.getElementById('editRemarkCount').textContent = (d.remark || '').length;

            new bootstrap.Modal(document.getElementById('editModal')).show();
        } catch (e) {
            showMsg('danger', `載入失敗：${e.message}`);
        }
    }

    // Save edit
    document.getElementById('btnSaveEdit').addEventListener('click', async () => {
        const id = document.getElementById('editId').value;
        const body = {
            billingStartDate: document.getElementById('editStartDate').value || null,
            billingEndDate: document.getElementById('editEndDate').value || null,
            billingDays: parseInt(document.getElementById('editDays').value) || null,
            totalUsage: parseInt(document.getElementById('editTotalUsage').value) || null,
            currentUsage: parseInt(document.getElementById('editCurrentUsage').value) || null,
            currentMeterReading: parseInt(document.getElementById('editCurrentReading').value) || null,
            previousMeterReading: parseInt(document.getElementById('editPreviousReading').value) || null,
            totalAmount: parseFloat(document.getElementById('editAmount').value) || null,
            remark: document.getElementById('editRemark').value || null,
        };

        try {
            const resp = await fetch(`/api/life/utility/water/period-records/${id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
            });
            if (resp.ok) {
                bootstrap.Modal.getInstance(document.getElementById('editModal')).hide();
                showMsg('success', '修改成功');
                await loadRecords();
            } else {
                const err = await resp.json();
                showMsg('danger', err.error || '修改失敗');
            }
        } catch (e) {
            showMsg('danger', `修改失敗：${e.message}`);
        }
    });

    // Delete
    async function doDelete(id) {
        if (!confirm('確定要刪除此筆水費紀錄？')) return;
        try {
            const resp = await fetch(`/api/life/utility/water/period-records/${id}`, { method: 'DELETE' });
            if (resp.ok) {
                showMsg('success', '已刪除');
                await loadRecords();
            } else {
                showMsg('danger', '刪除失敗');
            }
        } catch (e) {
            showMsg('danger', `刪除失敗：${e.message}`);
        }
    }

    await loadRecords();
});
