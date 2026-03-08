// 每三個月報告紀錄上傳
HSPAS.registerPage('health/checkup/qtr/upload', function () {
    const msgEl = document.getElementById('qtrUploadMsg');
    // 暫存上傳後的檔案資訊（OCR 解析後帶入儲存）
    let uploadedFileInfo = { sourceFileName: null, sourceFilePath: null, ocrJsonRaw: null };

    function showMsg(type, text) {
        const icon = type === 'success' ? '✔' : type === 'danger' ? '✘' : '⚠';
        msgEl.innerHTML = `<div class="alert alert-${type} alert-dismissible fade show" role="alert">
            <strong>${icon}</strong> ${text}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>`;
        msgEl.scrollIntoView({ behavior: 'smooth' });
        if (type === 'success') setTimeout(() => { const a = msgEl.querySelector('.alert'); if (a) a.remove(); }, 5000);
    }

    function val(id) { const v = document.getElementById(id)?.value; return v ? parseFloat(v) : null; }

    function setVal(id, v) {
        const el = document.getElementById(id);
        if (el && v != null) el.value = v;
    }

    // 預設報告日期為今天
    document.getElementById('qtrReportDate').value = new Date().toISOString().split('T')[0];

    // ========== 上傳檔案 + OCR 解析 ==========
    document.getElementById('btnUploadFile').addEventListener('click', async () => {
        const fileInput = document.getElementById('qtrFile');
        if (!fileInput.files.length) { showMsg('warning', '請先選擇檔案。'); return; }

        const btn = document.getElementById('btnUploadFile');
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm"></span> 解析中...';

        const formData = new FormData();
        formData.append('file', fileInput.files[0]);
        formData.append('reportDate', document.getElementById('qtrReportDate').value);
        formData.append('hospitalName', document.getElementById('qtrHospital').value);

        try {
            const resp = await fetch('/api/health/checkup/qtr/upload', { method: 'POST', body: formData });
            const data = await resp.json();

            if (!resp.ok) {
                showMsg('danger', data.error || '上傳失敗');
                return;
            }

            // 儲存檔案資訊
            uploadedFileInfo.sourceFileName = data.sourceFileName;
            uploadedFileInfo.sourceFilePath = data.sourceFilePath;
            uploadedFileInfo.ocrJsonRaw = data.rawText || null;

            // 將 OCR 辨識結果填入表單
            if (data.reportDate) setVal('qtrReportDate', data.reportDate);
            if (data.hospitalName) setVal('qtrHospital', data.hospitalName);

            if (data.values) {
                const v = data.values;
                setVal('valTCholesterol', v.tCholesterol);
                setVal('valTriglyceride', v.triglyceride);
                setVal('valHDL', v.hdl);
                setVal('valSGPT', v.sgpT_ALT);
                setVal('valCreatinine', v.creatinine);
                setVal('valUricAcid', v.uricAcid);
                setVal('valMDRD', v.mdrD_EGFR);
                setVal('valCKDEPI', v.ckdepI_EGFR);
                setVal('valAcSugar', v.acSugar);
                setVal('valHba1c', v.hba1c);
            }

            // 標記異常欄位的背景色
            if (data.flags) {
                highlightField('valTriglyceride', data.flags.triglycerideHigh);
                highlightField('valHDL', data.flags.hdlLow);
                highlightField('valAcSugar', data.flags.acSugarHigh);
                highlightField('valHba1c', data.flags.hba1cHigh);
            }

            // 顯示解析結果統計
            const filledCount = countFilled(data.values);
            showMsg(data.success ? 'success' : 'warning',
                `${data.message}（已辨識 ${filledCount}/10 項數值，請確認後按「儲存報告」）`);

        } catch (e) {
            showMsg('danger', '上傳失敗：' + e.message);
        } finally {
            btn.disabled = false;
            btn.innerHTML = '<i class="bi bi-cloud-upload"></i> 上傳並解析';
        }
    });

    function highlightField(id, isAbnormal) {
        const el = document.getElementById(id);
        if (!el) return;
        el.classList.remove('border-danger', 'border-success', 'border-2');
        if (isAbnormal) {
            el.classList.add('border-danger', 'border-2');
        } else if (el.value) {
            el.classList.add('border-success', 'border-2');
        }
    }

    function countFilled(values) {
        if (!values) return 0;
        return ['tCholesterol','triglyceride','hdl','sgpT_ALT','creatinine',
                'uricAcid','mdrD_EGFR','ckdepI_EGFR','acSugar','hba1c']
            .filter(k => values[k] != null).length;
    }

    // ========== 儲存報告 ==========
    document.getElementById('btnSaveReport').addEventListener('click', async () => {
        const reportDate = document.getElementById('qtrReportDate').value;
        if (!reportDate) { showMsg('warning', '請輸入報告日期。'); return; }

        const body = {
            reportDate: reportDate,
            hospitalName: document.getElementById('qtrHospital').value || '廖內科',
            values: {
                tCholesterol: val('valTCholesterol'),
                triglyceride: val('valTriglyceride'),
                hdl: val('valHDL'),
                sgpT_ALT: val('valSGPT'),
                creatinine: val('valCreatinine'),
                uricAcid: val('valUricAcid'),
                mdrD_EGFR: val('valMDRD'),
                ckdepI_EGFR: val('valCKDEPI'),
                acSugar: val('valAcSugar'),
                hba1c: val('valHba1c')
            },
            flags: {},
            sourceFileName: uploadedFileInfo.sourceFileName,
            sourceFilePath: uploadedFileInfo.sourceFilePath,
            ocrJsonRaw: uploadedFileInfo.ocrJsonRaw
        };

        try {
            const resp = await fetch('/api/health/checkup/qtr/manual', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body)
            });
            const data = await resp.json();
            if (resp.ok) {
                showMsg('success', `報告已儲存！編號 ${data.reportId}，日期 ${data.reportDate}`);
                resetForm();
                loadHistory();
            } else {
                showMsg('danger', data.error || '儲存失敗');
            }
        } catch (e) {
            showMsg('danger', '儲存失敗：' + e.message);
        }
    });

    // ========== 重置表單 ==========
    document.getElementById('btnResetForm').addEventListener('click', resetForm);

    function resetForm() {
        document.getElementById('qtrReportDate').value = new Date().toISOString().split('T')[0];
        document.getElementById('qtrHospital').value = '廖內科';
        document.getElementById('qtrFile').value = '';
        uploadedFileInfo = { sourceFileName: null, sourceFilePath: null, ocrJsonRaw: null };
        ['valTCholesterol', 'valTriglyceride', 'valHDL', 'valSGPT', 'valCreatinine',
         'valUricAcid', 'valMDRD', 'valCKDEPI', 'valAcSugar', 'valHba1c'].forEach(id => {
            const el = document.getElementById(id);
            if (el) {
                el.value = '';
                el.classList.remove('border-danger', 'border-success', 'border-2');
            }
        });
    }

    // ========== 載入歷史報告 ==========
    async function loadHistory() {
        try {
            const resp = await fetch('/api/health/checkup/qtr/list');
            const list = await resp.json();
            const tbody = document.getElementById('qtrHistoryBody');
            const noData = document.getElementById('qtrNoData');

            if (!list.length) {
                tbody.innerHTML = '';
                noData.style.display = 'block';
                return;
            }
            noData.style.display = 'none';

            tbody.innerHTML = list.map(r => {
                const v = r.values || {};
                const f = r.flags || {};
                return `<tr>
                    <td>${r.reportDate}</td>
                    <td>${r.hospitalName || '-'}</td>
                    <td>${fmt(v.tCholesterol)}</td>
                    <td>${fmtFlag(v.triglyceride, f.triglycerideHigh)}</td>
                    <td>${fmtFlag(v.hdl, f.hdlLow, true)}</td>
                    <td>${fmt(v.sgpT_ALT)}</td>
                    <td>${fmt(v.creatinine)}</td>
                    <td>${fmt(v.uricAcid)}</td>
                    <td>${fmtFlag(v.acSugar, f.acSugarHigh)}</td>
                    <td>${fmtFlag(v.hba1c, f.hba1cHigh)}</td>
                    <td>
                        <button class="btn btn-sm btn-outline-danger btn-del-report" data-id="${r.reportId}">
                            <i class="bi bi-trash"></i>
                        </button>
                    </td>
                </tr>`;
            }).join('');

            tbody.querySelectorAll('.btn-del-report').forEach(btn => {
                btn.addEventListener('click', async () => {
                    if (!confirm('確定要刪除此報告？')) return;
                    try {
                        const resp = await fetch(`/api/health/checkup/qtr/${btn.dataset.id}`, { method: 'DELETE' });
                        if (resp.ok) { showMsg('success', '已刪除'); loadHistory(); }
                        else { showMsg('danger', '刪除失敗'); }
                    } catch (e) { showMsg('danger', '刪除失敗：' + e.message); }
                });
            });
        } catch (e) {
            showMsg('danger', '載入歷史報告失敗：' + e.message);
        }
    }

    function fmt(v) { return v != null ? Number(v).toFixed(2) : '-'; }

    function fmtFlag(v, flag, isLow) {
        if (v == null) return '-';
        const val = Number(v).toFixed(2);
        if (flag) {
            const arrow = isLow ? '↓' : '↑';
            return `<span class="text-danger fw-bold">${val} ${arrow}</span>`;
        }
        return val;
    }

    // 初始化載入歷史
    loadHistory();
});
