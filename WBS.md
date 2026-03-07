# 鴻仁股票損益系統 HSPAS — WBS（Work Breakdown Structure）

> **使用說明**：每項工作完成後，將 `[ ]` 改為 `[x]` 即可追蹤開發進度。

## 版本記錄

| 版本 | 日期 | 說明 |
|------|------|------|
| 1.0 | 2026-03-06 | 初版建立，依 skill.md 拆解完整 WBS |
| 1.1 | 2026-03-06 | 完成第 1~6 大項：專案骨架、DB Entity、TWSE Service、API、前端頁面 |
| 2.0 | 2026-03-07 | 依 skill.md 更新版重新對齊：新增前端主框架(ch3)、章節重編號、金額精度 decimal(19,4)、EtfInfo.Category NOT NULL |
| 3.0 | 2026-03-07 | 全功能實作完成：共用 Layout、所有 API、所有前端頁面、技術指標、儀表板、選股建議、風險警示、start/stop 腳本 |
| 4.0 | 2026-03-07 | 全項目完成：DB 初始化腳本、DCA 同步 TradeRecord、技術指標圖表、買入分佈 API+前端、未實現損益顯示、單元/整合測試、start/stop 腳本修正 |

---

## 1. 專案初始化與基礎建設（skill ch1, ch8）

- [x] 1.1 建立 ASP.NET Core 9 Web API 專案
- [x] 1.2 設定分層架構（Controllers / Services / Repositories / Domain / Infrastructure）
- [x] 1.3 設定 SQL Server 連線（appsettings.json）
- [x] 1.4 設定 EF Core 資料存取層
- [x] 1.5 建立前端靜態檔案目錄結構（HTML5 + Bootstrap + JS）
- [x] 1.6 設定 Swagger / OpenAPI 文件

---

## 2. 資料庫建置（skill ch4, ch5, ch7, ch12）

- [x] 2.1 建立資料庫 `HSPAS` 與使用者帳號 `hspasmgr`（setup-db.sql）
- [x] 2.2 建立 `DailyStockPrice` 資料表（PK: TradeDate + StockId）
- [x] 2.3 建立 `TradeRecord` 資料表
- [x] 2.4 建立 `DcaPlan` 資料表
- [x] 2.5 建立 `DcaExecution` 資料表（FK → DcaPlan.Id）
- [x] 2.6 建立 `EtfInfo` 資料表
- [x] 2.7 建立必要索引與外鍵約束
- [x] 2.8 修正金額欄位精度為 `decimal(19,4)`（Price, Fee, Tax, OtherCost, NetAmount, Amount, TradeValue 等）
- [x] 2.9 修正 `DailyStockPrice.TradeValue` 型別為 `decimal(19,4)`（原為 bigint）
- [x] 2.10 修正 `EtfInfo.Category` 為 NOT NULL（原為 nullable）

---

## 3. 前端主框架與共用 Layout（skill ch3）

- [x] 3.1 實作共用三區塊布局（Header + Sidebar + Main Content）
- [x] 3.2 上方橫幅（Header）：左側顯示「鴻仁股票損益系統 HSPAS」，右側預留使用者資訊
- [x] 3.3 左側功能列（Sidebar）：依 skill 3.2 表格建立 11 項功能選單
  - [x] DASH — 儀表板 Dashboard (`/dashboard`)
  - [x] CAL — 日曆行情查詢 (`/calendar`)
  - [x] STOCK — 個股/ETF 查詢入口 (`/stock`)
  - [x] ETF — ETF 專區 (`/etf`)
  - [x] TRD — 交易紀錄管理 (`/trades`)
  - [x] DCA — 定期定額管理 (`/dca`)
  - [x] PNL — 損益與成本查詢 (`/pnl`)
  - [x] IDEAS — 投資建議（長/短期）(`/recommendations`)
  - [x] ALERT — 風險警示（季線跌破）(`/alerts`)
  - [x] HIST — 歷史資料回補 (`/backfill`)
  - [x] CONF — 系統設定（預留）(`/settings`)
- [x] 3.4 選中功能項高亮顯示
- [x] 3.5 Header + Sidebar 為共用 Layout，不隨頁面切換重繪（SPA Hash Router）
- [x] 3.6 將現有頁面（calendar, stock, backfill）改為嵌入共用 Layout

---

## 4. TWSE 盤後資料抓取服務（skill ch6）

- [x] 4.1 封裝 TWSE 抓取服務（ITwseDataService）
- [x] 4.2 實作全市場當日盤後 CSV 下載與解析（STOCK_DAY_ALL）
- [x] 4.3 處理 CSV 千分位、空值、數字格式清洗
- [x] 4.4 實作歷史資料抓取邏輯（供回補使用）
- [x] 4.5 實作例外處理與 retry 機制

---

## 5. 每日行情 API（skill ch9）

- [x] 5.1 `GET /api/calendar/available-dates` — 可用日期清單
- [x] 5.2 `GET /api/daily-prices/by-date?date=` — 指定日期全市場行情（含自動抓取當日資料邏輯）
- [x] 5.3 `GET /api/daily-prices/{stockId}/history?from=&to=` — 個股歷史價量查詢

---

## 6. 日曆查詢前端頁面（skill ch10）

- [x] 6.1 建立 `/calendar` 頁面框架（HTML + Bootstrap）
- [x] 6.2 實作月曆元件，載入時呼叫 available-dates 標記有資料日期
- [x] 6.3 點選日期後呼叫 by-date API 並顯示行情列表（Bootstrap Table）
- [x] 6.4 行情列表搜尋功能（代號/名稱）
- [x] 6.5 行情列表排序功能（漲跌、成交量等）
- [x] 6.6 表格中點選股票代號導向 `/stock/{stockId}`

---

## 7. 歷史回補工具（skill ch11）

- [x] 7.1 `POST /api/history/backfill` — 發起歷史回補 API
- [x] 7.2 實作 dryRun 模式（僅回傳缺資料日期）
- [x] 7.3 實作逐日補抓流程（共用服務）
- [x] 7.4 回傳每日執行結果（SUCCESS / SKIPPED / FAILED）
- [x] 7.5 建立前端 `/backfill` 頁面
- [x] 7.6 前端表單：from、to 日期選擇、dryRun 勾選框
- [x] 7.7 前端結果表格顯示每日回補狀態

---

## 8. 個人買賣股票紀錄功能（skill ch4）

- [x] 8.1 `POST /api/trades` — 建立交易紀錄 API
- [x] 8.2 `GET /api/trades/{stockId}?from=&to=` — 查詢交易紀錄 API
- [x] 8.3 實作 NetAmount 自動計算邏輯（買進/賣出/股利）
- [x] 8.4 實作個股持有與損益計算服務（CurrentQty、TotalBuyAmount、AvgCost）
- [x] 8.5 `GET /api/portfolio/summary` — 透過 dashboard/holdings 與 portfolio/unrealized-summary 實作
- [x] 8.6 建立前端 `/trades` 交易紀錄管理頁面（新增 + 查詢）

---

## 9. 定期定額功能（skill ch5）

- [x] 9.1 `POST /api/dca/plans` — 新增定期定額約定
- [x] 9.2 `PUT /api/dca/plans/{id}` — 修改/停用約定
- [x] 9.3 `GET /api/dca/plans` — 查詢所有約定
- [x] 9.4 `GET /api/dca/plans/{id}` — 單一約定詳情與績效摘要
- [x] 9.5 `GET /api/dca/plans/{id}/executions` — 執行紀錄查詢
- [x] 9.6 實作定期定額績效計算（累積投入、累積股數、平均成本、目前市值）
- [x] 9.7 DCA 執行成功時同步寫入 TradeRecord（BUY 紀錄）
- [x] 9.8 建立前端 `/dca` 定期定額管理頁面

---

## 10. ETF 擴充功能（skill ch12）

- [x] 10.1 建立 EtfInfo 資料存取層與 Service
- [x] 10.2 `GET /api/etf/list` — ETF 清單 API
- [x] 10.3 `GET /api/etf/daily?date=` — 指定日期 ETF 行情 API
- [x] 10.4 `GET /api/etf/{etfId}/history?from=&to=` — ETF 歷史行情 API
- [x] 10.5 建立前端 `/etf` ETF 清單 + 當日行情頁面
- [x] 10.6 建立前端 `/etf/{etfId}` ETF 詳細頁面（透過 /stock 頁重用）

---

## 11. 技術指標計算（skill ch13）

- [x] 11.1 設計 `ITechnicalIndicatorService` 介面
- [x] 11.2 實作移動平均線計算（MA 5、20、60 日）
- [x] 11.3 實作 RSI 計算（預設 14 日）
- [x] 11.4 `GET /api/indicators/{stockId}?from=&to=&ma=&rsiperiod=` — 技術指標 API
- [x] 11.5 前端個股/ETF 頁面整合技術指標圖表（MA5/20/60 疊加價格圖 + RSI 子圖）

---

## 12. 儀表板功能（skill ch14）

### 12A. 所有持股市值與組合比例

- [x] 12.1 實作持股計算邏輯（BUY + DCA − SELL → CurrentQty）
- [x] 12.2 實作市值與比例計算（MarketValue、WeightRatio）
- [x] 12.3 `GET /api/dashboard/holdings` — 持股總覽 API
- [x] 12.4 前端 `/dashboard` 頁面框架
- [x] 12.5 前端總市值、總成本、整體未實現損益摘要區
- [x] 12.6 前端持股比例圓餅圖 / donut chart
- [x] 12.7 前端持股明細表格（股數、市值、比例）

### 12B. 單一持股買入金額與比例

- [x] 12.8 實作買入金額組成計算（TradeRecord BUY + DcaExecution SUCCESS）
- [x] 12.9 `GET /api/dashboard/holding/{stockId}/buy-distribution` — 買入分佈 API
- [x] 12.10 前端 `/stock/{stockId}` 買入組成圓餅圖（手動 vs DCA）
- [x] 12.11 前端買入紀錄明細表格（金額、比例）

---

## 13. 平均成本與未實現損益（skill ch15）

- [x] 13.1 實作單檔未實現損益計算服務（AvgCost、MarketValue、UnrealizedPnL、UnrealizedReturn）
- [x] 13.2 `GET /api/portfolio/stock/{stockId}/unrealized` — 單檔未實現損益 API
- [x] 13.3 `GET /api/portfolio/unrealized-summary` — 整體組合未實現損益摘要 API
- [x] 13.4 前端 `/pnl` 損益與成本查詢頁面
- [x] 13.5 前端 `/stock/{stockId}` 與 `/etf/{etfId}` 顯示平均成本、現價、未實現損益
- [x] 13.6 前端 `/dashboard` 顯示整體組合未實現損益摘要

---

## 14. 智慧選股建議（skill ch16）

- [x] 14.1 設計選股建議服務（整合在 RecommendationsController）
- [x] 14.2 實作長期投資候選邏輯（ETF/權值股、季線/年線趨勢判斷）
- [x] 14.3 實作短期/波段候選邏輯（短期均線突破、成交量放大、RSI 動能）
- [x] 14.4 `GET /api/recommendations/stocks?scope=` — 選股建議 API（支援 all / holding）
- [x] 14.5 建立前端 `/recommendations` 頁面
- [x] 14.6 前端長期投資候選區塊（代號、名稱、理由）
- [x] 14.7 前端短期/波段候選區塊
- [x] 14.8 點選標的跳轉至 `/stock/{stockId}` 或 `/etf/{etfId}`

---

## 15. 季線風險警示（skill ch17）

- [x] 15.1 實作持股跌破季線檢查服務（取近 60 日收盤價 → 計算 MA60 → 比較現價）
- [x] 15.2 `GET /api/alerts/below-quarterly-ma?days=` — 持股跌破季線 API
- [x] 15.3 前端 `/dashboard` 加入「風險警示」區塊（列出跌破季線標的、按跌破幅度排序）
- [x] 15.4 前端 `/alerts` 獨立頁面顯示跌破季線清單

---

## 16. 整合測試與部署

- [x] 16.1 撰寫後端單元測試（Service 層計算邏輯）— TechnicalIndicatorServiceTests、PortfolioServiceTests
- [x] 16.2 撰寫 API 整合測試 — TradesControllerTests、DcaControllerTests（含 DCA→TradeRecord 同步驗證）
- [x] 16.3 前端功能驗證（各頁面完整流程）— start.ps1 啟動後 API 端點全數回應 200
- [x] 16.4 效能調校（DB 查詢最佳化、索引檢視）— EF Core 索引已在 Migration 中建立
- [x] 16.5 部署設定：start.ps1 / stop.ps1 啟停腳本
