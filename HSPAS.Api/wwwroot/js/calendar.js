// 日曆行情頁
HSPAS.registerPage('calendar', function () {
    let currentYear, currentMonth;
    let availableDatesSet = new Set();
    let allItems = [];
    let sortCol = 'tradeVolume';
    let sortAsc = false;

    const grid = document.getElementById('calendarGrid');
    const label = document.getElementById('currentMonthLabel');
    const priceSection = document.getElementById('priceSection');
    const noDataMsg = document.getElementById('noDataMsg');
    const loadingMsg = document.getElementById('loadingMsg');
    const priceBody = document.getElementById('priceBody');
    const searchInput = document.getElementById('searchInput');
    const resultCount = document.getElementById('resultCount');
    const selectedDateLabel = document.getElementById('selectedDateLabel');

    const now = new Date();
    currentYear = now.getFullYear();
    currentMonth = now.getMonth();

    document.getElementById('btnPrevMonth').addEventListener('click', () => { currentMonth--; if (currentMonth < 0) { currentMonth = 11; currentYear--; } renderCalendar(); });
    document.getElementById('btnNextMonth').addEventListener('click', () => { currentMonth++; if (currentMonth > 11) { currentMonth = 0; currentYear++; } renderCalendar(); });
    searchInput.addEventListener('input', renderTable);

    document.querySelectorAll('.sortable').forEach(th => {
        th.style.cursor = 'pointer';
        th.addEventListener('click', () => {
            const col = th.dataset.col;
            if (sortCol === col) sortAsc = !sortAsc;
            else { sortCol = col; sortAsc = true; }
            renderTable();
        });
    });

    init();

    async function init() {
        try {
            const resp = await fetch('/api/calendar/available-dates');
            const dates = await resp.json();
            availableDatesSet = new Set(dates);
        } catch (e) {
            console.error('Failed to load available dates', e);
        }
        renderCalendar();
        // 進入時自動載入當日行情
        autoLoadToday();
    }

    async function autoLoadToday() {
        const todayStr = `${currentYear}-${String(currentMonth + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
        // 選中今天的日曆格
        const cells = grid.querySelectorAll('.calendar-day:not(.calendar-header):not(.other-month)');
        cells.forEach(c => {
            if (c.textContent == String(now.getDate())) {
                c.classList.add('selected');
            }
        });
        // 直接載入今日行情
        priceSection.style.display = 'none';
        noDataMsg.style.display = 'none';
        loadingMsg.style.display = 'block';
        searchInput.value = '';
        try {
            const resp = await fetch(`/api/daily-prices/by-date?date=${todayStr}`);
            const data = await resp.json();
            loadingMsg.style.display = 'none';
            if (data.totalCount === 0) { noDataMsg.style.display = 'block'; return; }
            allItems = data.items;
            selectedDateLabel.textContent = `${data.date} 行情（共 ${data.totalCount} 筆）`;
            priceSection.style.display = 'block';
            renderTable();
        } catch (e) {
            loadingMsg.style.display = 'none';
            noDataMsg.style.display = 'block';
        }
    }

    function renderCalendar() {
        label.textContent = `${currentYear} 年 ${currentMonth + 1} 月`;
        const headers = grid.querySelectorAll('.calendar-header');
        grid.innerHTML = '';
        headers.forEach(h => grid.appendChild(h));

        const firstDay = new Date(currentYear, currentMonth, 1).getDay();
        const daysInMonth = new Date(currentYear, currentMonth + 1, 0).getDate();

        for (let i = 0; i < firstDay; i++) {
            const cell = document.createElement('div');
            cell.className = 'calendar-day other-month';
            grid.appendChild(cell);
        }

        for (let d = 1; d <= daysInMonth; d++) {
            const cell = document.createElement('div');
            cell.className = 'calendar-day';
            cell.textContent = d;
            const dateStr = `${currentYear}-${String(currentMonth + 1).padStart(2, '0')}-${String(d).padStart(2, '0')}`;
            if (availableDatesSet.has(dateStr)) cell.classList.add('has-data');
            cell.addEventListener('click', () => onDayClick(dateStr, cell));
            grid.appendChild(cell);
        }
    }

    async function onDayClick(dateStr, cell) {
        grid.querySelectorAll('.calendar-day').forEach(c => c.classList.remove('selected'));
        cell.classList.add('selected');
        priceSection.style.display = 'none';
        noDataMsg.style.display = 'none';
        loadingMsg.style.display = 'block';
        searchInput.value = '';

        try {
            const resp = await fetch(`/api/daily-prices/by-date?date=${dateStr}`);
            const data = await resp.json();
            loadingMsg.style.display = 'none';
            if (data.totalCount === 0) { noDataMsg.style.display = 'block'; return; }
            allItems = data.items;
            selectedDateLabel.textContent = `${data.date} 行情（共 ${data.totalCount} 筆）`;
            priceSection.style.display = 'block';
            renderTable();
        } catch (e) {
            loadingMsg.style.display = 'none';
            noDataMsg.style.display = 'block';
            console.error(e);
        }
    }

    function renderTable() {
        const keyword = searchInput.value.trim().toLowerCase();
        let filtered = allItems;
        if (keyword) {
            filtered = allItems.filter(i =>
                i.stockId.toLowerCase().includes(keyword) || i.stockName.toLowerCase().includes(keyword));
        }
        filtered.sort((a, b) => {
            let va = a[sortCol], vb = b[sortCol];
            if (va == null) va = sortAsc ? Infinity : -Infinity;
            if (vb == null) vb = sortAsc ? Infinity : -Infinity;
            if (typeof va === 'string') return sortAsc ? va.localeCompare(vb) : vb.localeCompare(va);
            return sortAsc ? va - vb : vb - va;
        });
        resultCount.textContent = `顯示 ${filtered.length} / ${allItems.length} 筆`;
        priceBody.innerHTML = filtered.map(i => {
            const changeClass = (i.priceChange > 0) ? 'price-up' : (i.priceChange < 0) ? 'price-down' : '';
            const changeSign = (i.priceChange > 0) ? '+' : '';
            return `<tr>
                <td><a href="#/stock?id=${i.stockId}">${i.stockId}</a></td>
                <td>${i.stockName}</td>
                <td>${fmt(i.closePrice)}</td>
                <td class="${changeClass}">${changeSign}${fmt(i.priceChange)}</td>
                <td>${fmt(i.openPrice)}</td>
                <td>${fmt(i.highPrice)}</td>
                <td>${fmt(i.lowPrice)}</td>
                <td>${fmtInt(i.tradeVolume)}</td>
                <td>${fmtInt(i.transaction)}</td>
            </tr>`;
        }).join('');
    }

    function fmt(v) { return v != null ? Number(v).toFixed(2) : '-'; }
    function fmtInt(v) { return v != null ? Number(v).toLocaleString() : '-'; }
});
