// 歷史資料回補工具頁
HSPAS.registerPage('backfill', function () {
    const fromDate = document.getElementById('fromDate');
    const toDate = document.getElementById('toDate');
    const btnBackfill = document.getElementById('btnBackfill');
    const loadingMsg = document.getElementById('loadingMsg');
    const resultSection = document.getElementById('resultSection');
    const resultBody = document.getElementById('resultBody');
    const msgEl = document.getElementById('backfillMsg');

    // 預設日期：起始 = 當週第一天（週一），結束 = 今天
    const today = new Date();
    const day = today.getDay(); // 0=日, 1=一, ..., 6=六
    const diffToMon = day === 0 ? 6 : day - 1;
    const monday = new Date(today);
    monday.setDate(today.getDate() - diffToMon);
    const fmt = d => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
    fromDate.value = fmt(monday);
    toDate.value = fmt(today);

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

    const statusMap = {
        'SUCCESS': { label: '成功', badge: 'success' },
        'PARTIAL': { label: '部分成功', badge: 'warning' },
        'NO_DATA': { label: '無資料', badge: 'secondary' },
        'FAILED':  { label: '失敗', badge: 'danger' }
    };

    btnBackfill.addEventListener('click', () => execute());

    async function execute() {
        if (!fromDate.value || !toDate.value) {
            showMsg('warning', '請選擇起始與結束日期。');
            return;
        }
        resultSection.style.display = 'none';
        msgEl.innerHTML = '';
        loadingMsg.style.display = 'block';
        btnBackfill.disabled = true;

        try {
            const resp = await fetch('/api/history/backfill', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ from: fromDate.value, to: toDate.value })
            });
            const data = await resp.json();
            loadingMsg.style.display = 'none';

            if (data.error) {
                showMsg('danger', data.error);
                return;
            }

            // 摘要
            document.getElementById('resultTitle').textContent =
                `回補結果（${data.from} ~ ${data.to}）`;
            document.getElementById('summaryDays').textContent = data.totalDays;
            document.getElementById('summarySuccess').textContent = data.successDays;
            document.getElementById('summaryNoData').textContent = data.noDataDays;
            document.getElementById('summaryFailed').textContent = data.failedDays;
            document.getElementById('summaryTse').textContent = data.totalTseCount.toLocaleString();
            document.getElementById('summaryOtc').textContent = data.totalOtcCount.toLocaleString();

            // 逐日明細
            resultBody.innerHTML = data.results.map(r => {
                const s = statusMap[r.status] || { label: r.status, badge: 'secondary' };
                return `<tr>
                    <td>${r.date}</td>
                    <td><span class="badge bg-${s.badge}">${s.label}</span></td>
                    <td>${r.tseCount.toLocaleString()}</td>
                    <td>${r.otcCount.toLocaleString()}</td>
                    <td>${r.message}</td>
                </tr>`;
            }).join('');

            resultSection.style.display = 'block';

            if (data.failedDays > 0) {
                showMsg('warning', `回補完成，${data.successDays} 日成功，${data.failedDays} 日失敗。`);
            } else {
                showMsg('success', `回補完成！${data.successDays} 日成功，共 TSE ${data.totalTseCount.toLocaleString()} 筆 + OTC ${data.totalOtcCount.toLocaleString()} 筆。`);
            }
        } catch (e) {
            loadingMsg.style.display = 'none';
            showMsg('danger', `回補請求失敗：${e.message}`);
        } finally {
            btnBackfill.disabled = false;
        }
    }
});
