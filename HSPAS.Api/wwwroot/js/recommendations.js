HSPAS.registerPage('recommendations', function () {
    const msgEl = document.getElementById('recMsg');

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

    document.getElementById('btnRecRefresh').addEventListener('click', load);

    // 進入時自動載入
    load();

    async function load() {
        const scope = document.getElementById('recScope').value;
        document.getElementById('recLoading').style.display = 'block';
        msgEl.innerHTML = '';
        try {
            const resp = await fetch(`/api/recommendations/stocks?scope=${scope}`);
            const data = await resp.json();
            document.getElementById('recLoading').style.display = 'none';

            const lt = data.longTermCandidates || [];
            document.getElementById('longTermBody').innerHTML = lt.length === 0
                ? '<tr><td colspan="3" class="text-muted text-center">無候選</td></tr>'
                : lt.map(i => `<tr><td><a href="#/stock?id=${i.stockId}">${i.stockId}</a></td><td>${i.stockName}</td><td>${i.reason}</td></tr>`).join('');

            const st = data.shortTermCandidates || [];
            document.getElementById('shortTermBody').innerHTML = st.length === 0
                ? '<tr><td colspan="3" class="text-muted text-center">無候選</td></tr>'
                : st.map(i => `<tr><td><a href="#/stock?id=${i.stockId}">${i.stockId}</a></td><td>${i.stockName}</td><td>${i.reason}</td></tr>`).join('');

            showMsg('success', `建議產生完成：長期候選 ${lt.length} 檔，短期候選 ${st.length} 檔。`);
        } catch (e) {
            document.getElementById('recLoading').style.display = 'none';
            showMsg('danger', `載入失敗：${e.message}`);
        }
    }
});
