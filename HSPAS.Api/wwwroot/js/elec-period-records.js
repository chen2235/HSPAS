HSPAS.registerPage('life/utility/electricity/period-records', async function () {
    const msgEl = document.getElementById('elecMsg');
    const confirmCard = document.getElementById('confirmCard');

    // 固定欄位
    const FIXED_ADDRESS = '新北市汐止區福山街60巷12號四樓';
    const FIXED_POWER_NO = '16-36-6055-40-7';
    const FIXED_BLACKOUT_GROUP = 'C';

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

    // ===== Step 1: Upload PDF → parse only =====
    document.getElementById('btnUploadElec').addEventListener('click', async () => {
        const fileInput = document.getElementById('elecPdfFile');
        if (!fileInput.files.length) { showMsg('warning', '請選擇 PDF 檔案'); return; }

        const spinner = document.getElementById('uploadSpinner');
        spinner.classList.remove('d-none');

        const formData = new FormData();
        formData.append('file', fileInput.files[0]);

        try {
            const resp = await fetch('/api/life/utility/electricity/upload', { method: 'POST', body: formData });
            if (resp.ok) {
                const d = await resp.json();

                // 檢查是否重覆：比對 powerNo + billingEndDate
                const isDuplicate = await checkDuplicate(d.powerNo, d.billingEndDate);
                if (isDuplicate) {
                    showMsg('warning', '此電費已紀錄，無需重覆紀錄');
                    fileInput.value = '';
                    confirmCard.classList.add('d-none');
                    return;
                }

                showMsg('success', `解析成功！計費度數: ${d.kwh} 度，繳費總金額: NT$${Number(d.totalAmount).toLocaleString()}。請確認下方資料後儲存。`);
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

    // 檢查是否已存在相同 billingEndDate 的紀錄
    async function checkDuplicate(powerNo, billingEndDate) {
        try {
            const resp = await fetch('/api/life/utility/electricity/period-records');
            if (!resp.ok) return false;
            const list = await resp.json();
            return list.some(r => r.billingEndDate === billingEndDate);
        } catch {
            return false;
        }
    }

    // Fill confirmation form with parsed data
    function fillConfirmForm(d) {
        document.getElementById('cfmTariffType').value = d.tariffType || '';
        document.getElementById('cfmSharedCount').value = d.sharedMeterHouseholdCount || '';
        document.getElementById('cfmStartDate').value = d.billingStartDate || '';
        document.getElementById('cfmEndDate').value = d.billingEndDate || '';
        document.getElementById('cfmDays').value = d.billingDays || '';
        document.getElementById('cfmReadDate').value = d.readOrDebitDate || '';
        document.getElementById('cfmKwh').value = d.kwh || '';
        document.getElementById('cfmKwhPerDay').value = d.kwhPerDay || '';
        document.getElementById('cfmAvgPrice').value = d.avgPricePerKwh || '';
        document.getElementById('cfmTotalAmount').value = d.totalAmount || '';
        document.getElementById('cfmInvPeriod').value = d.invoicePeriod || '';
        document.getElementById('cfmInvNo').value = d.invoiceNo || '';
        document.getElementById('cfmRawDetailJson').value = d.rawDetailJson || '';
        document.getElementById('cfmBillingPeriodText').value = d.billingPeriodText || '';
        document.getElementById('cfmInvoiceAmount').value = d.invoiceAmount || '';
        document.getElementById('cfmRemark').value = '';
        document.getElementById('cfmRemarkCount').textContent = '0';

        // Render detail items preview
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

    // ===== Step 2: Confirm save =====
    document.getElementById('btnConfirmSave').addEventListener('click', async () => {
        const body = {
            address: FIXED_ADDRESS,
            powerNo: FIXED_POWER_NO,
            blackoutGroup: FIXED_BLACKOUT_GROUP,
            billingStartDate: document.getElementById('cfmStartDate').value,
            billingEndDate: document.getElementById('cfmEndDate').value,
            billingDays: parseInt(document.getElementById('cfmDays').value) || 0,
            billingPeriodText: document.getElementById('cfmBillingPeriodText').value || null,
            readOrDebitDate: document.getElementById('cfmReadDate').value,
            kwh: parseInt(document.getElementById('cfmKwh').value) || 0,
            kwhPerDay: parseFloat(document.getElementById('cfmKwhPerDay').value) || null,
            avgPricePerKwh: parseFloat(document.getElementById('cfmAvgPrice').value) || null,
            totalAmount: parseFloat(document.getElementById('cfmTotalAmount').value) || 0,
            invoiceAmount: parseFloat(document.getElementById('cfmInvoiceAmount').value) || null,
            tariffType: document.getElementById('cfmTariffType').value || null,
            sharedMeterHouseholdCount: parseInt(document.getElementById('cfmSharedCount').value) || null,
            invoicePeriod: document.getElementById('cfmInvPeriod').value || null,
            invoiceNo: document.getElementById('cfmInvNo').value || null,
            rawDetailJson: document.getElementById('cfmRawDetailJson').value || null,
            remark: document.getElementById('cfmRemark').value || null,
        };

        if (!body.billingStartDate || !body.billingEndDate || !body.readOrDebitDate) {
            showMsg('warning', '計費起始日、計費結束日、抄表/扣款日為必填');
            return;
        }

        try {
            const resp = await fetch('/api/life/utility/electricity/save', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
            });
            if (resp.ok) {
                const saved = await resp.json();
                showMsg('success', `儲存成功！計費度數: ${saved.kwh} 度，繳費總金額: NT$${Number(saved.totalAmount).toLocaleString()}`);
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

    // Cancel confirm
    document.getElementById('btnCancelConfirm').addEventListener('click', () => {
        confirmCard.classList.add('d-none');
    });

    // 備註字數計數器
    document.getElementById('cfmRemark').addEventListener('input', (e) => {
        document.getElementById('cfmRemarkCount').textContent = e.target.value.length;
    });
    document.getElementById('editRemark').addEventListener('input', (e) => {
        document.getElementById('editRemarkCount').textContent = e.target.value.length;
    });

    // Load all records (no filter)
    async function loadRecords() {
        try {
            const resp = await fetch('/api/life/utility/electricity/period-records');
            if (!resp.ok) throw new Error('載入失敗');
            const list = await resp.json();
            document.getElementById('elecCount').textContent = list.length;

            if (list.length === 0) {
                document.getElementById('elecBody').innerHTML = '<tr><td colspan="10" class="text-center text-muted">尚無資料</td></tr>';
                return;
            }

            function truncate(str, max) {
                if (!str) return '';
                return str.length > max ? str.substring(0, max) + '…' : str;
            }

            document.getElementById('elecBody').innerHTML = list.map(r => `<tr>
                <td>${r.billingPeriodText || ''}</td>
                <td>${r.readOrDebitDate}</td>
                <td class="text-end">${r.kwh.toLocaleString()}</td>
                <td class="text-end">${r.kwhPerDay != null ? r.kwhPerDay.toFixed(2) : '-'}</td>
                <td class="text-end">${r.avgPricePerKwh != null ? r.avgPricePerKwh.toFixed(2) : '-'}</td>
                <td class="text-end">NT$${Number(r.totalAmount).toLocaleString()}</td>
                <td>${r.invoicePeriod || '-'}</td>
                <td>${r.invoiceNo || '-'}</td>
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
            const resp = await fetch(`/api/life/utility/electricity/period-records/${id}`);
            if (!resp.ok) throw new Error('載入失敗');
            const d = await resp.json();

            let detailItems = '';
            if (d.rawDetailJson) {
                try {
                    const raw = JSON.parse(d.rawDetailJson);
                    if (raw.Items && raw.Items.length > 0) {
                        detailItems = `<h6 class="mt-3">電費明細</h6>
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
                            <tr><td class="text-muted">用電地址</td><td>${d.address}</td></tr>
                            <tr><td class="text-muted">電號</td><td>${d.powerNo}</td></tr>
                            <tr><td class="text-muted">輪流停電組別</td><td>${d.blackoutGroup || '-'}</td></tr>
                        </table>
                    </div>
                    <div class="col-md-6">
                        <h6>計費資訊</h6>
                        <table class="table table-sm">
                            <tr><td class="text-muted">計費期間</td><td>${d.billingPeriodText || (d.billingStartDate + ' ~ ' + d.billingEndDate)}</td></tr>
                            <tr><td class="text-muted">計費天數</td><td>${d.billingDays} 天</td></tr>
                            <tr><td class="text-muted">抄表/扣款日</td><td>${d.readOrDebitDate}</td></tr>
                            <tr><td class="text-muted">計費度數</td><td>${d.kwh.toLocaleString()} 度</td></tr>
                            <tr><td class="text-muted">日平均度數</td><td>${d.kwhPerDay != null ? d.kwhPerDay.toFixed(2) : '-'}</td></tr>
                            <tr><td class="text-muted">每度平均電價</td><td>${d.avgPricePerKwh != null ? d.avgPricePerKwh.toFixed(2) + ' 元' : '-'}</td></tr>
                            <tr><td class="text-muted">電價種類</td><td>${d.tariffType || '-'}</td></tr>
                            <tr><td class="text-muted">分攤戶數</td><td>${d.sharedMeterHouseholdCount || '-'}</td></tr>
                        </table>
                    </div>
                </div>
                <div class="row">
                    <div class="col-md-6">
                        <h6>金額與發票</h6>
                        <table class="table table-sm">
                            <tr><td class="text-muted">繳費總金額</td><td class="fw-bold">NT$${Number(d.totalAmount).toLocaleString()}</td></tr>
                            <tr><td class="text-muted">發票期別</td><td>${d.invoicePeriod || '-'}</td></tr>
                            <tr><td class="text-muted">發票號碼</td><td>${d.invoiceNo || '-'}</td></tr>
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

            // 字數計數
            const remarkEl = document.getElementById('detailRemark');
            const countEl = document.getElementById('detailRemarkCount');
            remarkEl.addEventListener('input', () => { countEl.textContent = remarkEl.value.length; });

            // 儲存備註
            document.getElementById('btnSaveDetailRemark').addEventListener('click', async (e) => {
                const rid = e.target.dataset.id;
                try {
                    const resp = await fetch(`/api/life/utility/electricity/period-records/${rid}`, {
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
            const resp = await fetch(`/api/life/utility/electricity/period-records/${id}`);
            if (!resp.ok) throw new Error('載入失敗');
            const d = await resp.json();

            document.getElementById('editId').value = d.id;
            document.getElementById('editStartDate').value = d.billingStartDate;
            document.getElementById('editEndDate').value = d.billingEndDate;
            document.getElementById('editDays').value = d.billingDays;
            document.getElementById('editReadDate').value = d.readOrDebitDate;
            document.getElementById('editKwh').value = d.kwh;
            document.getElementById('editKwhPerDay').value = d.kwhPerDay || '';
            document.getElementById('editAvgPrice').value = d.avgPricePerKwh || '';
            document.getElementById('editAmount').value = d.totalAmount;
            document.getElementById('editInvPeriod').value = d.invoicePeriod || '';
            document.getElementById('editInvNo').value = d.invoiceNo || '';
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
            readOrDebitDate: document.getElementById('editReadDate').value || null,
            kwh: parseInt(document.getElementById('editKwh').value) || null,
            kwhPerDay: parseFloat(document.getElementById('editKwhPerDay').value) || null,
            avgPricePerKwh: parseFloat(document.getElementById('editAvgPrice').value) || null,
            totalAmount: parseFloat(document.getElementById('editAmount').value) || null,
            invoicePeriod: document.getElementById('editInvPeriod').value || null,
            invoiceNo: document.getElementById('editInvNo').value || null,
            remark: document.getElementById('editRemark').value || null,
        };

        try {
            const resp = await fetch(`/api/life/utility/electricity/period-records/${id}`, {
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
        if (!confirm('確定要刪除此筆電費紀錄？')) return;
        try {
            const resp = await fetch(`/api/life/utility/electricity/period-records/${id}`, { method: 'DELETE' });
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

    // Auto-load on page enter
    await loadRecords();
});
