// 個股歷史走勢頁（含技術指標、未實現損益、買入分佈）
HSPAS.registerPage('stock', function (query) {
    const stockIdInput = document.getElementById('stockIdInput');
    const fromDate = document.getElementById('fromDate');
    const toDate = document.getElementById('toDate');
    const btnQuery = document.getElementById('btnQuery');
    const chartSection = document.getElementById('chartSection');
    const historySection = document.getElementById('historySection');
    const loadingMsg = document.getElementById('loadingMsg');
    const noDataMsg = document.getElementById('noDataMsg');
    const stockTitle = document.getElementById('stockTitle');
    const historyBody = document.getElementById('historyBody');
    const historyCount = document.getElementById('historyCount');

    const msgEl = document.getElementById('stockMsg');

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

    let priceChart = null;
    let rsiChart = null;
    let buyDistChart = null;

    const params = new URLSearchParams(query);
    if (params.get('id')) stockIdInput.value = params.get('id');

    const now = new Date();
    toDate.value = now.toISOString().split('T')[0];
    const ago = new Date(now.getFullYear(), now.getMonth() - 3, now.getDate());
    fromDate.value = ago.toISOString().split('T')[0];

    btnQuery.addEventListener('click', doQuery);
    if (stockIdInput.value) doQuery();

    async function doQuery() {
        const sid = stockIdInput.value.trim();
        if (!sid) { showMsg('warning', '請輸入股票代號。'); return; }

        chartSection.style.display = 'none';
        historySection.style.display = 'none';
        noDataMsg.style.display = 'none';
        document.getElementById('unrealizedSection').style.display = 'none';
        document.getElementById('buyDistSection').style.display = 'none';
        loadingMsg.style.display = 'block';

        try {
            const [priceResp, indicatorResp] = await Promise.all([
                fetch(`/api/daily-prices/${sid}/history?from=${fromDate.value}&to=${toDate.value}`),
                fetch(`/api/indicators/${sid}?from=${fromDate.value}&to=${toDate.value}&ma=5,20,60&rsiperiod=14`)
            ]);
            const priceData = await priceResp.json();
            loadingMsg.style.display = 'none';

            if (!priceData.items || priceData.items.length === 0) {
                noDataMsg.style.display = 'block';
                loadUnrealized(sid);
                loadBuyDistribution(sid);
                return;
            }

            const items = priceData.items;
            stockTitle.textContent = `${sid} ${items[0].stockName || sid}`;

            let indicatorData = null;
            if (indicatorResp.ok) {
                indicatorData = await indicatorResp.json();
            }

            renderChart(items, indicatorData);

            historyCount.textContent = items.length;
            historyBody.innerHTML = [...items].reverse().map(i => {
                const cls = (i.priceChange > 0) ? 'price-up' : (i.priceChange < 0) ? 'price-down' : '';
                const sign = (i.priceChange > 0) ? '+' : '';
                return `<tr>
                    <td>${i.date}</td>
                    <td>${fmt(i.openPrice)}</td><td>${fmt(i.highPrice)}</td>
                    <td>${fmt(i.lowPrice)}</td><td>${fmt(i.closePrice)}</td>
                    <td class="${cls}">${sign}${fmt(i.priceChange)}</td>
                    <td>${fmtInt(i.tradeVolume)}</td><td>${fmtInt(i.transaction)}</td>
                </tr>`;
            }).join('');

            chartSection.style.display = 'block';
            historySection.style.display = 'block';

            // 載入未實現損益與買入分佈
            loadUnrealized(sid);
            loadBuyDistribution(sid);
        } catch (e) {
            loadingMsg.style.display = 'none';
            noDataMsg.style.display = 'block';
            console.error(e);
        }
    }

    function renderChart(items, indicators) {
        const labels = items.map(i => i.date);
        const closes = items.map(i => i.closePrice);
        const volumes = items.map(i => i.tradeVolume);

        // 建立技術指標 datasets
        const datasets = [
            { label: '收盤價', data: closes, borderColor: '#0d6efd', backgroundColor: 'rgba(13,110,253,0.1)', fill: true, tension: 0.3, yAxisID: 'y', borderWidth: 2 },
            { label: '成交量', data: volumes, type: 'bar', backgroundColor: 'rgba(108,117,125,0.3)', yAxisID: 'y1' }
        ];

        // 加入 MA 線 (11.5)
        if (indicators && indicators.items) {
            const maColors = { '5': '#ff6384', '20': '#36a2eb', '60': '#ff9f40' };
            const maLabels = { '5': '5日均線', '20': '20日均線', '60': '60日均線（季線）' };
            const indicatorMap = {};
            indicators.items.forEach(item => { indicatorMap[item.date] = item; });

            for (const period of (indicators.maPeriods || [])) {
                const maData = labels.map(d => {
                    const ind = indicatorMap[d];
                    return ind && ind.ma && ind.ma[period.toString()] != null ? ind.ma[period.toString()] : null;
                });
                datasets.push({
                    label: maLabels[period] || `${period}日均線`,
                    data: maData,
                    borderColor: maColors[period.toString()] || '#999',
                    backgroundColor: 'transparent',
                    borderWidth: 1.5,
                    pointRadius: 0,
                    tension: 0.3,
                    yAxisID: 'y'
                });
            }
        }

        if (priceChart) priceChart.destroy();
        priceChart = new Chart(document.getElementById('priceChart'), {
            type: 'line',
            data: { labels, datasets },
            options: {
                responsive: true,
                interaction: { mode: 'index', intersect: false },
                scales: {
                    y: { position: 'left', title: { display: true, text: '價格' } },
                    y1: { position: 'right', title: { display: true, text: '成交量' }, grid: { drawOnChartArea: false } }
                }
            }
        });

        // RSI 圖表 (11.5)
        if (indicators && indicators.items) {
            const indicatorMap = {};
            indicators.items.forEach(item => { indicatorMap[item.date] = item; });
            const rsiData = labels.map(d => {
                const ind = indicatorMap[d];
                return ind && ind.rsi != null ? ind.rsi : null;
            });

            if (rsiChart) rsiChart.destroy();
            rsiChart = new Chart(document.getElementById('rsiChart'), {
                type: 'line',
                data: {
                    labels,
                    datasets: [{
                        label: '相對強弱指標 RSI（14日）',
                        data: rsiData,
                        borderColor: '#6f42c1',
                        backgroundColor: 'rgba(111,66,193,0.1)',
                        fill: true,
                        tension: 0.3,
                        pointRadius: 0,
                        borderWidth: 1.5
                    }]
                },
                options: {
                    responsive: true,
                    interaction: { mode: 'index', intersect: false },
                    scales: {
                        y: {
                            min: 0, max: 100,
                            title: { display: true, text: 'RSI' }
                        }
                    },
                    plugins: {
                        annotation: {
                            annotations: {
                                overbought: { type: 'line', yMin: 70, yMax: 70, borderColor: 'red', borderDash: [5, 5], borderWidth: 1 },
                                oversold: { type: 'line', yMin: 30, yMax: 30, borderColor: 'green', borderDash: [5, 5], borderWidth: 1 }
                            }
                        }
                    }
                }
            });
        }
    }

    // 13.5: 載入未實現損益
    async function loadUnrealized(stockId) {
        try {
            const resp = await fetch(`/api/portfolio/stock/${stockId}/unrealized`);
            if (!resp.ok) return;
            const data = await resp.json();
            if (!data) return;
            document.getElementById('urQty').textContent = fmtInt(data.currentQty);
            document.getElementById('urAvgCost').textContent = fmt(data.avgCost);
            document.getElementById('urLastClose').textContent = fmt(data.lastClosePrice);
            document.getElementById('urMarketValue').textContent = 'NT$' + fmtInt(Math.round(data.marketValue));

            const pnl = data.unrealizedPnL;
            document.getElementById('urPnL').textContent = (pnl >= 0 ? '+' : '') + 'NT$' + fmtInt(Math.round(pnl));
            const pnlCard = document.getElementById('urPnlCard');
            pnlCard.classList.toggle('positive', pnl > 0);
            pnlCard.classList.toggle('negative', pnl < 0);

            const ret = data.unrealizedReturn;
            document.getElementById('urReturn').textContent = (ret >= 0 ? '+' : '') + (ret * 100).toFixed(2) + '%';
            const retCard = document.getElementById('urReturnCard');
            retCard.classList.toggle('positive', ret > 0);
            retCard.classList.toggle('negative', ret < 0);

            document.getElementById('unrealizedSection').style.display = 'block';
        } catch { }
    }

    // 12.10, 12.11: 載入買入分佈
    async function loadBuyDistribution(stockId) {
        try {
            const resp = await fetch(`/api/dashboard/holding/${stockId}/buy-distribution`);
            if (!resp.ok) return;
            const data = await resp.json();
            if (!data.items || data.items.length === 0) return;

            document.getElementById('buyDistCount').textContent = data.items.length;
            document.getElementById('buyDistTotal').textContent = 'NT$' + Number(data.totalAmount).toLocaleString(undefined, { maximumFractionDigits: 0 });

            // 圓餅圖：手動 vs DCA
            const manualTotal = data.manualTotal || 0;
            const dcaTotal = data.dcaTotal || 0;
            if (buyDistChart) buyDistChart.destroy();
            buyDistChart = new Chart(document.getElementById('buyDistChart'), {
                type: 'doughnut',
                data: {
                    labels: ['手動買入', 'DCA 定期定額'],
                    datasets: [{
                        data: [manualTotal, dcaTotal],
                        backgroundColor: ['#0d6efd', '#198754']
                    }]
                },
                options: { plugins: { legend: { position: 'bottom' } } }
            });

            // 明細表格
            document.getElementById('buyDistBody').innerHTML = data.items.map(i => `<tr>
                <td><span class="badge ${i.source === 'DCA' ? 'bg-success' : 'bg-primary'}">${i.source === 'DCA' ? '定期定額' : '手動買入'}</span></td>
                <td>${i.tradeDate}</td>
                <td>${fmtInt(i.quantity)}</td>
                <td>${fmt(i.price)}</td>
                <td>NT$${Number(i.amount).toLocaleString(undefined, { maximumFractionDigits: 0 })}</td>
                <td>${(i.ratio * 100).toFixed(1)}%</td>
            </tr>`).join('');

            document.getElementById('buyDistSection').style.display = 'block';
        } catch { }
    }

    function fmt(v) { return v != null ? Number(v).toFixed(2) : '-'; }
    function fmtInt(v) { return v != null ? Number(v).toLocaleString() : '-'; }
});
