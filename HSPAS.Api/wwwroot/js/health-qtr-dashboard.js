// 每三個月報告儀表板
HSPAS.registerPage('health/checkup/qtr/dashboard', function () {
    const msgEl = document.getElementById('qtrDashMsg');
    let allReports = [];
    let chartInstances = {};

    function showMsg(type, text) {
        msgEl.innerHTML = `<div class="alert alert-${type} alert-dismissible fade show" role="alert">
            ${text}<button type="button" class="btn-close" data-bs-dismiss="alert"></button></div>`;
    }

    // 載入所有報告
    async function loadReports() {
        try {
            const resp = await fetch('/api/health/checkup/qtr/list');
            allReports = await resp.json();

            if (!allReports.length) {
                showMsg('info', '尚無健檢報告，請先至「每三個月報告紀錄上傳」新增資料。');
                return;
            }

            // 填充下拉選單
            const sel = document.getElementById('dashReportSelect');
            sel.innerHTML = allReports.map(r =>
                `<option value="${r.reportId}">${r.reportDate}（${r.hospitalName || '-'}）</option>`
            ).join('');

            // 預設選最新一筆
            sel.value = allReports[0].reportId;
            showReportSummary(allReports[0], allReports[1] || null);

            // 渲染趨勢圖
            renderCharts();

            // 渲染歷史表
            renderHistoryTable();

            // 渲染比較卡片
            renderCompareCards(allReports[0], allReports[1] || null);

            sel.addEventListener('change', () => {
                const idx = allReports.findIndex(r => r.reportId == sel.value);
                if (idx >= 0) {
                    showReportSummary(allReports[idx], allReports[idx + 1] || null);
                    renderCompareCards(allReports[idx], allReports[idx + 1] || null);
                }
            });
        } catch (e) {
            showMsg('danger', '載入報告失敗：' + e.message);
        }
    }

    // 顯示選中報告摘要
    function showReportSummary(report, prev) {
        const v = report.values || {};
        const f = report.flags || {};

        const items = [
            { label: '總膽固醇', value: v.tCholesterol, unit: 'mg/dL', flag: false },
            { label: '三酸甘油脂', value: v.triglyceride, unit: 'mg/dL', flag: f.triglycerideHigh, high: true },
            { label: 'HDL', value: v.hdl, unit: 'mg/dL', flag: f.hdlLow, low: true },
            { label: 'SGPT', value: v.sgpT_ALT, unit: 'U/L', flag: false },
            { label: '肌酐', value: v.creatinine, unit: 'mg/dL', flag: false },
            { label: '尿酸', value: v.uricAcid, unit: 'mg/dL', flag: false },
            { label: '飯前血糖', value: v.acSugar, unit: 'mg/dL', flag: f.acSugarHigh, high: true },
            { label: 'HbA1c', value: v.hba1c, unit: '%', flag: f.hba1cHigh, high: true },
        ];

        document.getElementById('dashSummaryArea').innerHTML = `
            <div class="d-flex flex-wrap gap-2">
                ${items.map(i => {
                    if (i.value == null) return '';
                    const cls = i.flag ? 'bg-danger text-white' : 'bg-success text-white';
                    const arrow = i.flag ? (i.low ? ' ↓' : ' ↑') : '';
                    return `<span class="badge ${cls} fs-6 p-2">${i.label}: ${Number(i.value).toFixed(2)}${arrow} ${i.unit}</span>`;
                }).join('')}
            </div>`;
    }

    // 比較卡片
    function renderCompareCards(current, previous) {
        const container = document.getElementById('dashCompareCards');
        if (!previous) {
            container.style.display = 'none';
            return;
        }
        container.style.display = 'flex';

        const cv = current.values || {};
        const pv = previous.values || {};

        const metrics = [
            { key: 'tCholesterol', label: '總膽固醇', unit: 'mg/dL', refLow: null, refHigh: 200 },
            { key: 'triglyceride', label: '三酸甘油脂', unit: 'mg/dL', refLow: null, refHigh: 150 },
            { key: 'hdl', label: 'HDL', unit: 'mg/dL', refLow: 40, refHigh: null, invertColor: true },
            { key: 'sgpT_ALT', label: 'SGPT (ALT)', unit: 'U/L', refLow: null, refHigh: 41 },
            { key: 'creatinine', label: '肌酐', unit: 'mg/dL', refLow: 0.64, refHigh: 1.27 },
            { key: 'uricAcid', label: '尿酸', unit: 'mg/dL', refLow: 3.4, refHigh: 7.0 },
            { key: 'acSugar', label: '飯前血糖', unit: 'mg/dL', refLow: 70, refHigh: 100 },
            { key: 'hba1c', label: 'HbA1c', unit: '%', refLow: 4.0, refHigh: 5.6 },
        ];

        let html = '<div class="col-12"><h5>與上一次比較</h5></div>';
        for (const m of metrics) {
            const curVal = cv[m.key];
            const prevVal = pv[m.key];
            if (curVal == null && prevVal == null) continue;

            const diff = (curVal != null && prevVal != null) ? curVal - prevVal : null;
            let diffText = '-';
            let diffClass = 'text-muted';
            if (diff != null) {
                const sign = diff > 0 ? '+' : '';
                diffText = `${sign}${diff.toFixed(2)}`;
                // 判斷變化方向是好是壞
                if (m.invertColor) {
                    diffClass = diff > 0 ? 'text-success' : diff < 0 ? 'text-danger' : 'text-muted';
                } else {
                    diffClass = diff < 0 ? 'text-success' : diff > 0 ? 'text-danger' : 'text-muted';
                }
            }

            html += `<div class="col-md-3 col-sm-6 mb-2">
                <div class="card h-100">
                    <div class="card-body text-center py-2">
                        <div class="text-muted small">${m.label}</div>
                        <div class="fs-4 fw-bold">${curVal != null ? Number(curVal).toFixed(2) : '-'}</div>
                        <div class="small">前次 ${prevVal != null ? Number(prevVal).toFixed(2) : '-'}</div>
                        <div class="${diffClass} fw-bold">${diffText} ${diff != null ? (diff > 0 ? '↑' : diff < 0 ? '↓' : '→') : ''}</div>
                    </div>
                </div>
            </div>`;
        }
        container.innerHTML = html;
    }

    // 趨勢圖（使用 Chart.js）
    function renderCharts() {
        document.getElementById('dashCharts').style.display = 'flex';

        // 時間軸（由舊到新）
        const sorted = [...allReports].reverse();
        const labels = sorted.map(r => r.reportDate);

        drawChart('chartTriglyceride', labels,
            sorted.map(r => r.values?.triglyceride),
            '三酸甘油脂', 'rgba(255,99,132,1)', 150, '參考上限 150');

        drawChart('chartHDL', labels,
            sorted.map(r => r.values?.hdl),
            'HDL', 'rgba(54,162,235,1)', 40, '參考下限 40');

        drawChart('chartAcSugar', labels,
            sorted.map(r => r.values?.acSugar),
            '飯前血糖', 'rgba(255,159,64,1)', 100, '參考上限 100');

        drawChart('chartHba1c', labels,
            sorted.map(r => r.values?.hba1c),
            'HbA1c', 'rgba(153,102,255,1)', 5.6, '參考上限 5.6');
    }

    function drawChart(canvasId, labels, data, label, color, refLine, refLabel) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        // 銷毀舊圖表
        if (chartInstances[canvasId]) {
            chartInstances[canvasId].destroy();
        }

        const ctx = canvas.getContext('2d');
        chartInstances[canvasId] = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: label,
                        data: data,
                        borderColor: color,
                        backgroundColor: color.replace('1)', '0.1)'),
                        borderWidth: 2,
                        tension: 0.3,
                        fill: false,
                        pointRadius: 4,
                        pointHoverRadius: 6
                    },
                    {
                        label: refLabel,
                        data: labels.map(() => refLine),
                        borderColor: 'rgba(220,53,69,0.5)',
                        borderWidth: 1,
                        borderDash: [5, 5],
                        pointRadius: 0,
                        fill: false
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { position: 'bottom' }
                },
                scales: {
                    y: { beginAtZero: false }
                }
            }
        });
    }

    // 歷史紀錄表
    function renderHistoryTable() {
        const tbody = document.getElementById('dashHistoryBody');
        tbody.innerHTML = allReports.map(r => {
            const v = r.values || {};
            const f = r.flags || {};
            return `<tr>
                <td>${r.reportDate}</td>
                <td>${fmt(v.tCholesterol)}</td>
                <td>${fmtFlag(v.triglyceride, f.triglycerideHigh)}</td>
                <td>${fmtFlag(v.hdl, f.hdlLow, true)}</td>
                <td>${fmt(v.sgpT_ALT)}</td>
                <td>${fmt(v.creatinine)}</td>
                <td>${fmt(v.uricAcid)}</td>
                <td>${fmt(v.mdrD_EGFR)}</td>
                <td>${fmt(v.ckdepI_EGFR)}</td>
                <td>${fmtFlag(v.acSugar, f.acSugarHigh)}</td>
                <td>${fmtFlag(v.hba1c, f.hba1cHigh)}</td>
            </tr>`;
        }).join('');
    }

    function fmt(v) { return v != null ? Number(v).toFixed(2) : '-'; }

    function fmtFlag(v, flag, isLow) {
        if (v == null) return '-';
        const val = Number(v).toFixed(2);
        if (flag) {
            const arrow = isLow ? '↓' : '↑';
            return `<span class="text-danger fw-bold">${val} ${arrow}</span>`;
        }
        return `<span class="text-success">${val}</span>`;
    }

    // 初始化
    loadReports();
});
