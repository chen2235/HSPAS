HSPAS.registerPage('life/utility/water/dashboard', async function () {
    const msgEl = document.getElementById('waterDashMsg');
    const currentYear = new Date().getFullYear();
    let chartInstance = null;
    let compareChartInstance = null;

    function showMsg(type, text) {
        const icon = type === 'success' ? '✔' : type === 'danger' ? '✘' : '⚠';
        const now = new Date().toLocaleTimeString();
        msgEl.innerHTML = `<div class="alert alert-${type} alert-dismissible fade show" role="alert">
            <strong>${icon}</strong> ${text} <small class="text-muted ms-2">${now}</small>
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>`;
    }

    // Year dropdown
    const yearSel = document.getElementById('dashYear');
    for (let y = currentYear + 1; y >= 2020; y--) {
        const opt = document.createElement('option');
        opt.value = y; opt.textContent = y + '年';
        if (y === currentYear) opt.selected = true;
        yearSel.appendChild(opt);
    }

    document.getElementById('btnDashSearch').addEventListener('click', loadDashboard);

    async function fetchDashboard(year) {
        const resp = await fetch(`/api/life/utility/water/dashboard?year=${year}`);
        if (!resp.ok) throw new Error('載入失敗');
        return resp.json();
    }

    async function loadDashboard() {
        const year = parseInt(yearSel.value);
        const prevYear = year - 1;
        try {
            const [data, prevData] = await Promise.all([
                fetchDashboard(year),
                fetchDashboard(prevYear),
            ]);

            // Year summary
            const totalUsage = data.reduce((s, p) => s + p.usageTotal, 0);
            const totalAmount = data.reduce((s, p) => s + p.amountTotal, 0);
            document.getElementById('yearUsageTotal').textContent = totalUsage.toLocaleString();
            document.getElementById('yearAmountTotal').textContent = 'NT$' + totalAmount.toLocaleString();
            document.getElementById('yearBillCount').textContent = data.length;

            // Chart
            renderChart(data, year);

            // Comparison
            renderComparison(data, prevData, year, prevYear);

            // Table
            if (data.length === 0) {
                document.getElementById('dashBody').innerHTML = '<tr><td colspan="5" class="text-center text-muted">無資料</td></tr>';
                return;
            }

            document.getElementById('dashBody').innerHTML = data.map(p => `<tr>
                <td>第 ${p.periodIndex} 期</td>
                <td>${p.periodLabel}</td>
                <td class="text-end">${p.usageTotal.toLocaleString()}</td>
                <td class="text-end">NT$${p.amountTotal.toLocaleString()}</td>
                <td class="small">${formatRemark(p.remark)}</td>
            </tr>`).join('');
        } catch (e) {
            showMsg('danger', `載入失敗：${e.message}`);
        }
    }

    function renderChart(data, year) {
        const labels = data.map(p => p.periodLabel);
        const usageData = data.map(p => p.usageTotal);
        const amtData = data.map(p => p.amountTotal);

        if (chartInstance) chartInstance.destroy();

        chartInstance = new Chart(document.getElementById('waterChart'), {
            data: {
                labels: labels,
                datasets: [
                    {
                        type: 'bar',
                        label: '用水度數',
                        data: usageData,
                        backgroundColor: 'rgba(54, 162, 235, 0.5)',
                        borderColor: 'rgba(54, 162, 235, 1)',
                        borderWidth: 1,
                        yAxisID: 'y',
                    },
                    {
                        type: 'line',
                        label: '應繳總金額 (NT$)',
                        data: amtData,
                        borderColor: 'rgba(255, 99, 132, 1)',
                        backgroundColor: 'rgba(255, 99, 132, 0.1)',
                        borderWidth: 2,
                        pointRadius: 4,
                        yAxisID: 'y1',
                        fill: true,
                    },
                ],
            },
            options: {
                responsive: true,
                plugins: {
                    title: { display: true, text: year + ' 年度用水與水費趨勢' },
                },
                scales: {
                    y: {
                        type: 'linear', display: true, position: 'left',
                        title: { display: true, text: '度數' },
                    },
                    y1: {
                        type: 'linear', display: true, position: 'right',
                        title: { display: true, text: '金額 (NT$)' },
                        grid: { drawOnChartArea: false },
                    },
                },
            },
        });
    }

    function formatRemark(remark) {
        if (!remark) return '';
        if (remark.length > 30) {
            return `<span title="${remark.replace(/"/g, '&quot;')}">${remark.substring(0, 30)}…</span>`;
        }
        return remark;
    }

    function pctChange(curr, prev) {
        if (prev === 0) return curr === 0 ? '—' : '+∞';
        const pct = ((curr - prev) / prev * 100).toFixed(1);
        return (pct > 0 ? '+' : '') + pct + '%';
    }

    function yoyColor(curr, prev) {
        if (prev === 0 && curr === 0) return '';
        return curr > prev ? 'text-danger' : curr < prev ? 'text-success' : '';
    }

    function renderComparison(currData, prevData, year, prevYear) {
        document.getElementById('compareTitle').textContent = `${year} vs ${prevYear}`;
        document.getElementById('thCurrUsage').textContent = `${year} 度數`;
        document.getElementById('thPrevUsage').textContent = `${prevYear} 度數`;
        document.getElementById('thCurrAmt').textContent = `${year} 金額`;
        document.getElementById('thPrevAmt').textContent = `${prevYear} 金額`;

        const currTotalUsage = currData.reduce((s, p) => s + p.usageTotal, 0);
        const prevTotalUsage = prevData.reduce((s, p) => s + p.usageTotal, 0);
        const currTotalAmt = currData.reduce((s, p) => s + p.amountTotal, 0);
        const prevTotalAmt = prevData.reduce((s, p) => s + p.amountTotal, 0);

        const yoyUsageEl = document.getElementById('yoyUsage');
        yoyUsageEl.textContent = pctChange(currTotalUsage, prevTotalUsage);
        yoyUsageEl.className = 'mb-0 ' + yoyColor(currTotalUsage, prevTotalUsage);

        const yoyAmtEl = document.getElementById('yoyAmount');
        yoyAmtEl.textContent = pctChange(currTotalAmt, prevTotalAmt);
        yoyAmtEl.className = 'mb-0 ' + yoyColor(currTotalAmt, prevTotalAmt);

        const diffUsageVal = currTotalUsage - prevTotalUsage;
        const diffUsageEl = document.getElementById('diffUsage');
        diffUsageEl.textContent = (diffUsageVal > 0 ? '+' : '') + diffUsageVal.toLocaleString() + ' 度';
        diffUsageEl.className = 'mb-0 ' + yoyColor(currTotalUsage, prevTotalUsage);

        const diffAmtVal = currTotalAmt - prevTotalAmt;
        const diffAmtEl = document.getElementById('diffAmount');
        diffAmtEl.textContent = (diffAmtVal > 0 ? '+' : '') + 'NT$' + Math.round(diffAmtVal).toLocaleString();
        diffAmtEl.className = 'mb-0 ' + yoyColor(currTotalAmt, prevTotalAmt);

        renderCompareChart(currData, prevData, year, prevYear);
        renderCompareTable(currData, prevData);
    }

    function renderCompareChart(currData, prevData, year, prevYear) {
        // Water bills are by period, not monthly. Use max period count as labels.
        const maxPeriods = Math.max(currData.length, prevData.length, 1);
        const labels = Array.from({ length: maxPeriods }, (_, i) => `第 ${i + 1} 期`);
        const currUsage = Array.from({ length: maxPeriods }, (_, i) => currData[i] ? currData[i].usageTotal : 0);
        const prevUsage = Array.from({ length: maxPeriods }, (_, i) => prevData[i] ? prevData[i].usageTotal : 0);
        const currAmt = Array.from({ length: maxPeriods }, (_, i) => currData[i] ? currData[i].amountTotal : 0);
        const prevAmt = Array.from({ length: maxPeriods }, (_, i) => prevData[i] ? prevData[i].amountTotal : 0);

        if (compareChartInstance) compareChartInstance.destroy();

        compareChartInstance = new Chart(document.getElementById('compareChart'), {
            data: {
                labels: labels,
                datasets: [
                    {
                        type: 'bar',
                        label: `${year} 度數`,
                        data: currUsage,
                        backgroundColor: 'rgba(54, 162, 235, 0.6)',
                        borderColor: 'rgba(54, 162, 235, 1)',
                        borderWidth: 1,
                        yAxisID: 'y',
                    },
                    {
                        type: 'bar',
                        label: `${prevYear} 度數`,
                        data: prevUsage,
                        backgroundColor: 'rgba(54, 162, 235, 0.2)',
                        borderColor: 'rgba(54, 162, 235, 0.5)',
                        borderWidth: 1,
                        borderDash: [5, 5],
                        yAxisID: 'y',
                    },
                    {
                        type: 'line',
                        label: `${year} 金額`,
                        data: currAmt,
                        borderColor: 'rgba(255, 99, 132, 1)',
                        backgroundColor: 'rgba(255, 99, 132, 0.1)',
                        borderWidth: 2,
                        pointRadius: 4,
                        yAxisID: 'y1',
                        fill: false,
                    },
                    {
                        type: 'line',
                        label: `${prevYear} 金額`,
                        data: prevAmt,
                        borderColor: 'rgba(255, 99, 132, 0.4)',
                        borderDash: [6, 3],
                        borderWidth: 2,
                        pointRadius: 3,
                        pointStyle: 'triangle',
                        yAxisID: 'y1',
                        fill: false,
                    },
                ],
            },
            options: {
                responsive: true,
                plugins: {
                    title: { display: true, text: `同期比較：${year} vs ${prevYear}` },
                    tooltip: {
                        callbacks: {
                            afterBody: function (items) {
                                const idx = items[0].dataIndex;
                                const cu = currUsage[idx], pu = prevUsage[idx];
                                const ca = currAmt[idx], pa = prevAmt[idx];
                                const lines = [];
                                if (pu > 0) lines.push(`度數 YoY: ${pctChange(cu, pu)}`);
                                if (pa > 0) lines.push(`金額 YoY: ${pctChange(ca, pa)}`);
                                return lines;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        type: 'linear', display: true, position: 'left',
                        title: { display: true, text: '度數' },
                    },
                    y1: {
                        type: 'linear', display: true, position: 'right',
                        title: { display: true, text: '金額 (NT$)' },
                        grid: { drawOnChartArea: false },
                    },
                },
            },
        });
    }

    function renderCompareTable(currData, prevData) {
        const maxPeriods = Math.max(currData.length, prevData.length);
        const rows = [];

        let sumCu = 0, sumPu = 0, sumCa = 0, sumPa = 0;

        for (let i = 0; i < maxPeriods; i++) {
            const cu = currData[i] ? currData[i].usageTotal : 0;
            const pu = prevData[i] ? prevData[i].usageTotal : 0;
            const ca = currData[i] ? currData[i].amountTotal : 0;
            const pa = prevData[i] ? prevData[i].amountTotal : 0;

            sumCu += cu; sumPu += pu; sumCa += ca; sumPa += pa;

            if (cu === 0 && pu === 0) continue;

            const usageDiff = cu - pu;
            const amtDiff = ca - pa;
            const uc = yoyColor(cu, pu);
            const ac = yoyColor(ca, pa);

            const currRemark = currData[i] ? (currData[i].remark || '') : '';
            const prevRemark = prevData[i] ? (prevData[i].remark || '') : '';
            const allRemarks = [currRemark, prevRemark].filter(r => r);

            rows.push(`<tr>
                <td>第 ${i + 1} 期</td>
                <td class="text-end">${cu.toLocaleString()}</td>
                <td class="text-end">${pu.toLocaleString()}</td>
                <td class="text-end ${uc}">${(usageDiff > 0 ? '+' : '') + usageDiff.toLocaleString()}</td>
                <td class="text-end ${uc}">${pctChange(cu, pu)}</td>
                <td class="text-end">NT$${Math.round(ca).toLocaleString()}</td>
                <td class="text-end">NT$${Math.round(pa).toLocaleString()}</td>
                <td class="text-end ${ac}">${(amtDiff > 0 ? '+' : '') + 'NT$' + Math.round(amtDiff).toLocaleString()}</td>
                <td class="text-end ${ac}">${pctChange(ca, pa)}</td>
                <td class="small">${formatRemark(allRemarks.join('；'))}</td>
            </tr>`);
        }

        // Summary row
        if (rows.length > 0) {
            const sumUsageDiff = sumCu - sumPu;
            const sumAmtDiff = sumCa - sumPa;
            const suc = yoyColor(sumCu, sumPu);
            const sac = yoyColor(sumCa, sumPa);
            rows.push(`<tr class="table-secondary fw-bold">
                <td>合計</td>
                <td class="text-end">${sumCu.toLocaleString()}</td>
                <td class="text-end">${sumPu.toLocaleString()}</td>
                <td class="text-end ${suc}">${(sumUsageDiff > 0 ? '+' : '') + sumUsageDiff.toLocaleString()}</td>
                <td class="text-end ${suc}">${pctChange(sumCu, sumPu)}</td>
                <td class="text-end">NT$${Math.round(sumCa).toLocaleString()}</td>
                <td class="text-end">NT$${Math.round(sumPa).toLocaleString()}</td>
                <td class="text-end ${sac}">${(sumAmtDiff > 0 ? '+' : '') + 'NT$' + Math.round(sumAmtDiff).toLocaleString()}</td>
                <td class="text-end ${sac}">${pctChange(sumCa, sumPa)}</td>
                <td></td>
            </tr>`);
        }

        document.getElementById('compareBody').innerHTML = rows.length > 0
            ? rows.join('')
            : '<tr><td colspan="10" class="text-center text-muted">無比較資料</td></tr>';
    }

    // Auto-load
    await loadDashboard();
});
