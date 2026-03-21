# 美股股票儀表板（US Stock Dashboard）

> **模組代碼**：US_DASHBOARD
> **路由**：`/us/dashboard`
> **選單位置**：股票損益紀錄 → 美股投資 → 美股儀表板

---

## 1. 功能概述

美股投資組合總覽儀表板：
- 4 張摘要卡片：總市值(USD)、總成本(USD)、未實現損益(USD)、報酬率(%)
- 持股比例圓餅圖（Doughnut Chart）
- 持股明細表格（含排序）
- 最近交易紀錄

---

## 2. 後端 API

複用 `IUsPortfolioService` 的 `GetHoldingsAsync`：
- `GET /api/us/portfolio/holdings` — 持股總覽（含 totalMarketValue, totalCost, totalUnrealizedPnL, totalUnrealizedReturn, items[]）

新增 API：
- `GET /api/us/trades/recent?count=10` — 最近 N 筆美股交易紀錄

---

## 3. 前端頁面 `/us/dashboard`

### 3.1 摘要卡片列

4 張 Bootstrap Card：
1. **總市值** — `$XX,XXX`（USD 格式）
2. **總成本** — `$XX,XXX`
3. **未實現損益** — 藍色正/紅色負，邊框同步顏色
4. **報酬率** — `+XX.XX%` / `-XX.XX%`

### 3.2 持股比例圓餅圖

- Chart.js Doughnut Chart
- Labels: `NVDA NVIDIA Corp`, `AAPL Apple Inc` ...
- Data: 各檔市值 (USD)
- Legend: 右側

### 3.3 持股明細表格

| 欄位 | 排序 | 說明 |
|------|------|------|
| 代號 | Y | StockSymbol |
| 名稱 | Y | StockName |
| 股數 | Y | Quantity（小數） |
| 現價 | Y | LastPrice (USD) |
| 市值 | Y | MarketValue (USD) |
| 比例 | Y | WeightRatio % |

- 預設按市值降序排列

### 3.4 最近交易紀錄

- 最近 10 筆交易
- 欄位：日期、代號、名稱、動作(買/賣)、股數、價格、應收付額(USD)
- BUY 紅字、SELL 綠字

---

## 4. 前端 HTML/JS 檔案

- **HTML**: `pages/us-dashboard.html`
- **JS**: `js/us-dashboard.js`
- **路由 hash**: `us/dashboard`
- **HSPAS.registerPage**: `'us/dashboard'`

---

## 5. 圓餅圖顏色方案

使用 Chart.js 預設色盤，若持股數量 > 10 則自動循環。

---

## 6. 響應式設計

- 摘要卡片：`col-md-3`（≥768px 4 欄，<768px 堆疊）
- 圖表+明細：`col-md-6`（左右並排，<768px 堆疊）
- 最近交易：`col-12`（全寬）
