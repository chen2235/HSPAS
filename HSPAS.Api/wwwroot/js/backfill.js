// 歷史回補工具頁
HSPAS.registerPage('backfill', function () {
    const fromDate = document.getElementById('fromDate');
    const toDate = document.getElementById('toDate');
    const btnDryRun = document.getElementById('btnDryRun');
    const btnBackfill = document.getElementById('btnBackfill');
    const loadingMsg = document.getElementById('loadingMsg');
    const resultSection = document.getElementById('resultSection');
    const resultBody = document.getElementById('resultBody');
    const resultSummary = document.getElementById('resultSummary');
    const msgEl = document.getElementById('backfillMsg');

    const statusMap = {
        'SUCCESS': '成功',
        'SKIPPED_ALREADY_EXISTS': '已有資料（略過）',
        'MISSING': '缺少資料',
        'FAILED': '失敗',
        'NO_DATA': '該日無資料'
    };

    function showMsg(type, text) {
        const icon = type === 'success' ? '✔' : type === 'danger' ? '✘' : '⚠';
        const now = new Date().toLocaleTimeString();
        msgEl.innerHTML = `<div class="alert alert-${type} alert-dismissible fade show" role="alert">
            <strong>${icon}</strong> ${text} <small class="text-muted ms-2">${now}</small>
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>`;
        msgEl.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        if (type === 'success') setTimeout(() => { msgEl.querySelector('.alert')?.classList.remove('show'); }, 8000);
    }

    btnDryRun.addEventListener('click', () => execute(true));
    btnBackfill.addEventListener('click', () => {
        if (!confirm('確定要開始回補嗎？')) return;
        execute(false);
    });

    async function execute(dryRun) {
        if (!fromDate.value || !toDate.value) {
            showMsg('warning', '請選擇起始與結束日期。');
            return;
        }
        resultSection.style.display = 'none';
        msgEl.innerHTML = '';
        loadingMsg.style.display = 'block';
        btnDryRun.disabled = btnBackfill.disabled = true;

        try {
            const resp = await fetch('/api/history/backfill', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ from: fromDate.value, to: toDate.value, dryRun })
            });
            const data = await resp.json();
            loadingMsg.style.display = 'none';

            if (!data.results) {
                showMsg('danger', '回補失敗：伺服器回應格式異常。');
                return;
            }

            const total = data.results.length;
            const success = data.results.filter(r => r.status === 'SUCCESS').length;
            const skipped = data.results.filter(r => r.status === 'SKIPPED_ALREADY_EXISTS').length;
            const missing = data.results.filter(r => r.status === 'MISSING').length;
            const failed = data.results.filter(r => r.status === 'FAILED').length;
            const noData = data.results.filter(r => r.status === 'NO_DATA').length;

            let summary = `共 ${total} 日`;
            if (dryRun) {
                summary += `，缺少資料 ${missing} 日，已有資料 ${skipped} 日`;
                showMsg('info', `試算檢查完成：${fromDate.value} 至 ${toDate.value}，共 ${total} 日，其中 ${missing} 日缺少資料。`);
            } else {
                summary += `，成功 ${success} 日，略過 ${skipped} 日，無資料 ${noData} 日，失敗 ${failed} 日`;
                if (failed > 0) {
                    showMsg('warning', `回補完成，但有 ${failed} 日失敗。成功 ${success} 日，共新增 ${success} 日行情資料。`);
                } else {
                    showMsg('success', `回補完成！成功新增 ${success} 日行情資料（${fromDate.value} 至 ${toDate.value}）。`);
                }
            }
            resultSummary.textContent = summary;

            resultBody.innerHTML = data.results.map(r => {
                let badge = 'secondary';
                if (r.status === 'SUCCESS') badge = 'success';
                else if (r.status === 'SKIPPED_ALREADY_EXISTS') badge = 'info';
                else if (r.status === 'MISSING') badge = 'warning';
                else if (r.status === 'FAILED') badge = 'danger';
                const label = statusMap[r.status] || r.status;
                return `<tr><td>${r.date}</td><td><span class="badge bg-${badge}">${label}</span></td><td>${r.message}</td></tr>`;
            }).join('');
            resultSection.style.display = 'block';
        } catch (e) {
            loadingMsg.style.display = 'none';
            showMsg('danger', `回補請求失敗：${e.message}`);
        } finally {
            btnDryRun.disabled = btnBackfill.disabled = false;
        }
    }
});
