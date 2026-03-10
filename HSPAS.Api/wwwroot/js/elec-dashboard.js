HSPAS.registerPage('life/utility/electricity/dashboard', async function () {
    const msgEl = document.getElementById('elecDashMsg');
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
        const resp = await fetch(`/api/life/utility/electricity/dashboard?year=${year}`);
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
            const totalKwh = data.reduce((s, m) => s + m.kwhTotal, 0);
            const totalAmount = data.reduce((s, m) => s + m.amountTotal, 0);
            const totalBills = data.reduce((s, m) => s + m.billCount, 0);
            document.getElementById('yearKwhTotal').textContent = totalKwh.toLocaleString();
            document.getElementById('yearAmountTotal').textContent = 'NT$' + totalAmount.toLocaleString();
            document.getElementById('yearBillCount').textContent = totalBills;

            // Chart
            renderChart(data, year);

            // Comparison
            renderComparison(data, prevData, year, prevYear);

            // Table
            if (data.length === 0) {
                document.getElementById('dashBody').innerHTML = '<tr><td colspan="5" class="text-center text-muted">無資料</td></tr>';
                return;
            }

            document.getElementById('dashBody').innerHTML = data.map(m => `<tr>
                <td>${m.month} 月</td>
                <td class="text-end">${m.kwhTotal.toLocaleString()}</td>
                <td class="text-end">NT$${m.amountTotal.toLocaleString()}</td>
                <td class="text-end">${m.billCount}</td>
                <td class="small">${formatRemarks(m.remarks)}</td>
            </tr>`).join('');
        } catch (e) {
            showMsg('danger', `載入失敗：${e.message}`);
        }
    }

    function renderChart(data, year) {
        const labels = Array.from({ length: 12 }, (_, i) => (i + 1) + '月');
        const kwhData = new Array(12).fill(0);
        const amtData = new Array(12).fill(0);
        data.forEach(m => {
            kwhData[m.month - 1] = m.kwhTotal;
            amtData[m.month - 1] = m.amountTotal;
        });

        if (chartInstance) chartInstance.destroy();

        chartInstance = new Chart(document.getElementById('elecChart'), {
            data: {
                labels: labels,
                datasets: [
                    {
                        type: 'bar',
                        label: '計費度數 (kWh)',
                        data: kwhData,
                        backgroundColor: 'rgba(54, 162, 235, 0.5)',
                        borderColor: 'rgba(54, 162, 235, 1)',
                        borderWidth: 1,
                        yAxisID: 'y',
                    },
                    {
                        type: 'line',
                        label: '繳費總金額 (NT$)',
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
                    title: { display: true, text: year + ' 年度用電與電費趨勢' },
                },
                scales: {
                    y: {
                        type: 'linear',
                        display: true,
                        position: 'left',
                        title: { display: true, text: '度數 (kWh)' },
                    },
                    y1: {
                        type: 'linear',
                        display: true,
                        position: 'right',
                        title: { display: true, text: '金額 (NT$)' },
                        grid: { drawOnChartArea: false },
                    },
                },
            },
        });
    }

    function toMonthArray(data) {
        const arr = new Array(12).fill(0);
        data.forEach(m => { arr[m.month - 1] = m; });
        return arr;
    }

    function formatRemarks(remarks) {
        if (!remarks || remarks.length === 0) return '';
        const joined = remarks.join('；');
        if (joined.length > 30) {
            return `<span title="${joined.replace(/"/g, '&quot;')}">${joined.substring(0, 30)}…</span>`;
        }
        return joined;
    }

    function pctChange(curr, prev) {
        if (prev === 0) return curr === 0 ? '—' : '+∞';
        const pct = ((curr - prev) / prev * 100).toFixed(1);
        return (pct > 0 ? '+' : '') + pct + '%';
    }

    function yoyColor(curr, prev) {
        if (prev === 0 && curr === 0) return '';
        // 電費 / 用電：增加為紅，減少為綠
        return curr > prev ? 'text-danger' : curr < prev ? 'text-success' : '';
    }

    function renderComparison(currData, prevData, year, prevYear) {
        // Update header labels
        document.getElementById('compareTitle').textContent = `${year} vs ${prevYear}`;
        document.getElementById('thCurrKwh').textContent = `${year} 度數`;
        document.getElementById('thPrevKwh').textContent = `${prevYear} 度數`;
        document.getElementById('thCurrAmt').textContent = `${year} 金額`;
        document.getElementById('thPrevAmt').textContent = `${prevYear} 金額`;

        const currMonths = toMonthArray(currData);
        const prevMonths = toMonthArray(prevData);

        // YoY summary cards
        const currTotalKwh = currData.reduce((s, m) => s + m.kwhTotal, 0);
        const prevTotalKwh = prevData.reduce((s, m) => s + m.kwhTotal, 0);
        const currTotalAmt = currData.reduce((s, m) => s + m.amountTotal, 0);
        const prevTotalAmt = prevData.reduce((s, m) => s + m.amountTotal, 0);

        const yoyKwhEl = document.getElementById('yoyKwh');
        yoyKwhEl.textContent = pctChange(currTotalKwh, prevTotalKwh);
        yoyKwhEl.className = 'mb-0 ' + yoyColor(currTotalKwh, prevTotalKwh);

        const yoyAmtEl = document.getElementById('yoyAmount');
        yoyAmtEl.textContent = pctChange(currTotalAmt, prevTotalAmt);
        yoyAmtEl.className = 'mb-0 ' + yoyColor(currTotalAmt, prevTotalAmt);

        const diffKwhVal = currTotalKwh - prevTotalKwh;
        const diffKwhEl = document.getElementById('diffKwh');
        diffKwhEl.textContent = (diffKwhVal > 0 ? '+' : '') + diffKwhVal.toLocaleString() + ' 度';
        diffKwhEl.className = 'mb-0 ' + yoyColor(currTotalKwh, prevTotalKwh);

        const diffAmtVal = currTotalAmt - prevTotalAmt;
        const diffAmtEl = document.getElementById('diffAmount');
        diffAmtEl.textContent = (diffAmtVal > 0 ? '+' : '') + 'NT$' + Math.round(diffAmtVal).toLocaleString();
        diffAmtEl.className = 'mb-0 ' + yoyColor(currTotalAmt, prevTotalAmt);

        // Comparison chart
        renderCompareChart(currMonths, prevMonths, year, prevYear);

        // Comparison table
        renderCompareTable(currMonths, prevMonths);
    }

    function renderCompareChart(currMonths, prevMonths, year, prevYear) {
        const labels = Array.from({ length: 12 }, (_, i) => (i + 1) + '月');
        const currKwh = currMonths.map(m => m ? (m.kwhTotal || 0) : 0);
        const prevKwh = prevMonths.map(m => m ? (m.kwhTotal || 0) : 0);
        const currAmt = currMonths.map(m => m ? (m.amountTotal || 0) : 0);
        const prevAmt = prevMonths.map(m => m ? (m.amountTotal || 0) : 0);

        if (compareChartInstance) compareChartInstance.destroy();

        compareChartInstance = new Chart(document.getElementById('compareChart'), {
            data: {
                labels: labels,
                datasets: [
                    {
                        type: 'bar',
                        label: `${year} 度數`,
                        data: currKwh,
                        backgroundColor: 'rgba(54, 162, 235, 0.6)',
                        borderColor: 'rgba(54, 162, 235, 1)',
                        borderWidth: 1,
                        yAxisID: 'y',
                    },
                    {
                        type: 'bar',
                        label: `${prevYear} 度數`,
                        data: prevKwh,
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
                                const ck = currKwh[idx], pk = prevKwh[idx];
                                const ca = currAmt[idx], pa = prevAmt[idx];
                                const lines = [];
                                if (pk > 0) lines.push(`度數 YoY: ${pctChange(ck, pk)}`);
                                if (pa > 0) lines.push(`金額 YoY: ${pctChange(ca, pa)}`);
                                return lines;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        type: 'linear', display: true, position: 'left',
                        title: { display: true, text: '度數 (kWh)' },
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

    function renderCompareTable(currMonths, prevMonths) {
        const rows = [];
        for (let i = 0; i < 12; i++) {
            const ck = currMonths[i] ? (currMonths[i].kwhTotal || 0) : 0;
            const pk = prevMonths[i] ? (prevMonths[i].kwhTotal || 0) : 0;
            const ca = currMonths[i] ? (currMonths[i].amountTotal || 0) : 0;
            const pa = prevMonths[i] ? (prevMonths[i].amountTotal || 0) : 0;

            if (ck === 0 && pk === 0) continue;

            const kwhDiff = ck - pk;
            const amtDiff = ca - pa;
            const kwhYoy = pctChange(ck, pk);
            const amtYoy = pctChange(ca, pa);
            const kc = yoyColor(ck, pk);
            const ac = yoyColor(ca, pa);

            const currRemarks = currMonths[i] ? (currMonths[i].remarks || []) : [];
            const prevRemarks = prevMonths[i] ? (prevMonths[i].remarks || []) : [];
            const allRemarks = [...currRemarks, ...prevRemarks];

            rows.push(`<tr>
                <td>${i + 1} 月</td>
                <td class="text-end">${ck.toLocaleString()}</td>
                <td class="text-end">${pk.toLocaleString()}</td>
                <td class="text-end ${kc}">${(kwhDiff > 0 ? '+' : '') + kwhDiff.toLocaleString()}</td>
                <td class="text-end ${kc}">${kwhYoy}</td>
                <td class="text-end">NT$${Math.round(ca).toLocaleString()}</td>
                <td class="text-end">NT$${Math.round(pa).toLocaleString()}</td>
                <td class="text-end ${ac}">${(amtDiff > 0 ? '+' : '') + 'NT$' + Math.round(amtDiff).toLocaleString()}</td>
                <td class="text-end ${ac}">${amtYoy}</td>
                <td class="small">${formatRemarks(allRemarks)}</td>
            </tr>`);
        }

        // 合計列
        if (rows.length > 0) {
            let sumCk = 0, sumPk = 0, sumCa = 0, sumPa = 0;
            for (let i = 0; i < 12; i++) {
                sumCk += currMonths[i] ? (currMonths[i].kwhTotal || 0) : 0;
                sumPk += prevMonths[i] ? (prevMonths[i].kwhTotal || 0) : 0;
                sumCa += currMonths[i] ? (currMonths[i].amountTotal || 0) : 0;
                sumPa += prevMonths[i] ? (prevMonths[i].amountTotal || 0) : 0;
            }
            const sumKwhDiff = sumCk - sumPk;
            const sumAmtDiff = sumCa - sumPa;
            const skc = yoyColor(sumCk, sumPk);
            const sac = yoyColor(sumCa, sumPa);
            rows.push(`<tr class="table-secondary fw-bold">
                <td>合計</td>
                <td class="text-end">${sumCk.toLocaleString()}</td>
                <td class="text-end">${sumPk.toLocaleString()}</td>
                <td class="text-end ${skc}">${(sumKwhDiff > 0 ? '+' : '') + sumKwhDiff.toLocaleString()}</td>
                <td class="text-end ${skc}">${pctChange(sumCk, sumPk)}</td>
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
