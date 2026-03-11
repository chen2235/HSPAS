# 鴻仁生活紀錄系統 HSPAS — WBS（Work Breakdown Structure）

> **使用說明**：每項工作完成後，將 `[ ]` 改為 `[x]` 即可追蹤開發進度。
>
> **系統全名**：鴻仁生活紀錄系統（Hung-Jen Stock Profit Analysis System, HSPAS）
> **技術棧**：ASP.NET Core 9 Web API + HTML5/Bootstrap/JS + SQL Server

## 版本記錄

| 版本 | 日期 | 說明 |
|------|------|------|
| 1.0 | 2026-03-06 | 初版建立，依 skill.md 拆解完整 WBS |
| 1.1 | 2026-03-06 | 完成第 1~6 大項：專案骨架、DB Entity、TWSE Service、API、前端頁面 |
| 2.0 | 2026-03-07 | 依 skill.md 更新版重新對齊：新增前端主框架(ch3)、章節重編號、金額精度 decimal(19,4)、EtfInfo.Category NOT NULL |
| 3.0 | 2026-03-07 | 全功能實作完成：共用 Layout、所有 API、所有前端頁面、技術指標、儀表板、選股建議、風險警示、start/stop 腳本 |
| 4.0 | 2026-03-07 | 全項目完成：DB 初始化腳本、DCA 同步 TradeRecord、技術指標圖表、買入分佈 API+前端、未實現損益顯示、單元/整合測試、start/stop 腳本修正 |
| 5.0 | 2026-03-07 | UX 優化：所有頁面 alert() 改為 inline 訊息、中文化、排序功能、自動載入、表單預設值、DCA/交易紀錄編輯功能、損益顏色標示、靜態檔案 no-cache、run.bat/stop.bat |
| 6.0 | 2026-03-07 | 新增「上傳國泰證日對帳單」功能：PDF 解析 API、批次新增 API、前端匯入介面與待確認明細表格 |
| 7.0 | 2026-03-08 | 新增「功能選單管理」：MenuFunction 資料表與初始資料（§2.11-2.14）、三層式動態 Sidebar（§3B）、GET /api/menu/tree、POST /api/menu/reorder、選單排序設定頁面 /settings/menu-sorting |
| 8.0 | 2026-03-08 | 新增「上櫃（OTC）行情資料」：TPEx 上櫃盤後 CSV 抓取（§4.6-4.8）、MarketType 欄位（§2.15）、雙來源回補 TSE+OTC（§7.8）、前端市場別顯示（§6.8） |
| 9.0 | 2026-03-08 | 重構回補為「單日模式」：移除 BackfillService 區間批次，新增 IDailyPriceService.BackfillOneDayAsync（先刪後插）、API 改為單一日期、前端簡化為單日選擇器（§7.8-7.11） |
| 10.0 | 2026-03-08 | 新增「每三個月健檢報告」模組：Tesseract OCR 影像辨識（eng+chi_tra 雙語、四方向旋轉偵測、逐行解析+數值範圍驗證）、QuarterHealthReport/Detail 資料表、上傳+手動輸入+儀表板前端、趨勢圖+比較卡片（§16） |
| 11.0 | 2026-03-09 | Bug fix：總市值計算改為每檔各自取最新收盤價（與明細一致）；損益顏色規則調整（藍=賺/紅=賠，三段式明細顏色）；WBS 新增生活計帳-每期水電費瓦斯紀錄（§17B, skill §18） |
| 12.0 | 2026-03-09 | 完成「每期電費紀錄」模組：台電 PDF 帳單解析（PdfPig + 全形正規化 + 民國日期轉換）、CRUD API、每期紀錄前端、電費儀表板前端、重複上傳偵測（同電號+計費結束日更新）、LIFE_UTILITY 選單種子資料（§17B.1-17B.2） |
| 13.0 | 2026-03-10 | 電費儀表板同期比較分析（YoY 雙年度疊加圖+比較明細表+合計列）、Sidebar 隱藏/顯示切換（含 localStorage 記憶）、備註欄位擴充至 500 字（textarea+字數計數+明細 Modal 可編輯備註+儀表板/比較表顯示備註）（§17B.2, §3B, §17B.1） |
| 14.0 | 2026-03-11 | 完成「每期水費紀錄」模組：台水 PDF 帳單解析（PdfPig + NFKC 正規化解決 CJK 相容漢字 U+F963 度 + Letters API 逐行抽取 + 條碼水號解析）、CRUD API、每期紀錄前端（上傳→解析→確認→儲存）、水費儀表板前端（同期比較分析 YoY）、重複上傳偵測（同水號+計費結束日更新）（§17B.3） |

---

## 1. 專案初始化與基礎建設（skill §1, §8）

- [x] 1.1 建立 ASP.NET Core 9 Web API 專案
- [x] 1.2 設定分層架構（Controllers / Services / Repositories / Domain / Infrastructure）
- [x] 1.3 設定 SQL Server 連線（appsettings.json）— Server=localhost, DB=HSPAS, User=hspasmgr
- [x] 1.4 設定 EF Core 資料存取層
- [x] 1.5 建立前端靜態檔案目錄結構（HTML5 + Bootstrap + JS）
- [x] 1.6 設定 Swagger / OpenAPI 文件（http://localhost:5117/openapi/v1.json）

---

## 2. 資料庫建置（skill §4, §5, §7, §12, §3.2）

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
- [x] 2.11 建立 `MenuFunction` 資料表（Id, ParentId, Level, FuncCode, DisplayName, RouteUrl, SortOrder, IsActive, Remark, CreateTime）
- [x] 2.12 `MenuFunction` 初始資料 — 股票損益紀錄（STOCK_ROOT → STOCK_ANALYSIS → 11 個 Level 3 功能）
- [x] 2.13 `MenuFunction` 初始資料 — 健康管理紀錄（HEALTH_ROOT → HEALTH_CHECKUP → 4 個 Level 3 功能）
- [x] 2.14 `MenuFunction` 初始資料 — 生活計帳（LIFE_ROOT → LIFE_SIS → 2 個 Level 3 功能；LIFE_UTILITY → 6 個 Level 3 功能）
- [x] 2.15 `DailyStockPrice` 新增 `MarketType` 欄位（nvarchar(5), 預設 "TSE"，區分上市/上櫃）

---

## 3. 前端主框架與共用 Layout（skill §3）

- [x] 3.1 實作共用三區塊布局（Header + Sidebar + Main Content）
- [x] 3.2 上方橫幅（Header）：左側顯示「鴻仁生活紀錄系統」，右側預留使用者資訊
- [x] 3.3 左側功能列（Sidebar）：依 skill §3.2 建立功能選單
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

### 3B. 三層式功能選單系統（skill §3.2, §3.3, menu_skill.md）

- [x] 3.7 建立 `MenuFunction` EF Core 實體（`Entities/MenuFunction.cs`）與 DbSet + FuncCode 唯一索引
- [x] 3.8 建立 `IMenuService` / `MenuService`（組樹狀 DTO `GetMenuTreeAsync`、批次更新 `ReorderAsync` 含階層驗證）
- [x] 3.9 `GET /api/menu/tree` — 回傳完整三層選單樹 JSON（遞迴 children，僅 IsActive=1）
- [x] 3.10 `POST /api/menu/reorder` — 接收拖拉排序結果，驗證階層合法性（L1 ParentId=NULL, L2→L1, L3→L2, SortOrder>0），批次更新 DB
- [x] 3.11 前端 Sidebar 改為 API 驅動：`router.js` 新增 `loadSidebar()` 呼叫 `/api/menu/tree` 動態渲染三層可展開/收合選單（含 fallback 靜態選單）
- [x] 3.12 前端 `/settings/menu-sorting` 頁面：拖拉排序 UI（HTML5 Drag & Drop）+ 節點資訊面板 + 儲存按鈕呼叫 `/api/menu/reorder`
- [x] 3.13 Sidebar 隱藏/顯示切換：Header 左側新增 toggle icon（`bi-list` ↔ `bi-layout-sidebar-inset`），CSS transition 動畫（width 0 + opacity fade），localStorage 記憶偏好（`hspas_sidebar_hidden`）

---

## 4. TWSE 盤後資料抓取服務（skill ch6）

- [x] 4.1 封裝 TWSE 抓取服務（ITwseDataService）
- [x] 4.2 實作全市場當日盤後 CSV 下載與解析（STOCK_DAY_ALL）
- [x] 4.3 處理 CSV 千分位、空值、數字格式清洗
- [x] 4.4 實作歷史資料抓取邏輯（供回補使用）
- [x] 4.5 實作例外處理與 retry 機制
- [x] 4.6 新增上櫃（OTC）盤後資料抓取（TPEx DAILY_CLOSE_quotes CSV）
- [x] 4.7 實作 TPEx CSV 解析（ParseTpexCsv：民國日期、18 欄位對應）
- [x] 4.8 實作民國日期轉換（ParseRocDate：YYYMMDD → DateTime）

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
- [x] 6.7 進入頁面自動載入當日行情，預設成交股數由大到小排序
- [x] 6.8 行情列表新增「市場別」欄位（TSE 藍色 badge / OTC 黃色 badge），支援搜尋篩選

---

## 7. 歷史回補工具（skill ch11）

- [x] 7.1 `POST /api/history/backfill` — 發起歷史回補 API
- [x] 7.2 實作 dryRun 模式（僅回傳缺資料日期）
- [x] 7.3 實作逐日補抓流程（共用服務）
- [x] 7.4 回傳每日執行結果（SUCCESS / SKIPPED / FAILED）
- [x] 7.5 建立前端 `/backfill` 頁面
- [x] 7.6 前端表單：from、to 日期選擇、dryRun 勾選框
- [x] 7.7 前端結果表格顯示每日回補狀態
- [x] 7.8 重構為單日回補模式：移除 `IBackfillService` / `BackfillService` 區間批次邏輯
- [x] 7.9 `IDailyPriceService` 新增 `BackfillOneDayAsync(date)`、`FetchTseDailyAsync(date)`、`FetchOtcDailyAsync(date)`
- [x] 7.10 `BackfillOneDayAsync` 實作：抓取 TSE+OTC → 先刪該日舊資料 → 重新寫入（ExecuteDeleteAsync + AddRange + SaveChangesAsync）
- [x] 7.11 `POST /api/history/backfill` 改為接收單一 `date` 參數，回傳 `BackfillOneDayResult`（date, status, tseCount, otcCount, message）
- [x] 7.12 前端 `/backfill` 改為單一日期選擇器 + 回補按鈕 + 結果卡片（TSE/OTC/合計數量）

---

## 8. 個人買賣股票紀錄功能（skill ch4）

- [x] 8.1 `POST /api/trades` — 建立交易紀錄 API
- [x] 8.2 `GET /api/trades` — 查詢交易紀錄 API（支援 stockId、from、to 篩選，留空查全部）
- [x] 8.3 `PUT /api/trades/{id}` — 修改交易紀錄 API（自動重算 NetAmount）
- [x] 8.4 `DELETE /api/trades/{id}` — 刪除交易紀錄 API
- [x] 8.5 `GET /api/trades/stock-name/{stockId}` — 股票名稱查詢 API（TradeRecords → DailyStockPrices → EtfInfos 三層查找）
- [x] 8.6 實作 NetAmount 自動計算邏輯（買進/賣出/股利）
- [x] 8.7 實作個股持有與損益計算服務（CurrentQty、TotalBuyAmount、AvgCost）
- [x] 8.8 建立前端 `/trades` 交易紀錄管理頁面
  - [x] 8.8.1 新增表單：交易日期預設系統日、股票代號自動帶出名稱（debounce 400ms）、股數預設 1、欄位名稱「單股成交價」
  - [x] 8.8.2 新增完成後重置表單（交易日期恢復系統日）並自動更新查詢結果
  - [x] 8.8.3 進入頁面自動查詢所有交易紀錄
  - [x] 8.8.4 查詢結果標題列可排序（編號、日期、代號、名稱、動作、股數、價格、淨額），預設日期降序
  - [x] 8.8.5 查詢結果每列「修改」按鈕，開啟 Bootstrap Modal 編輯/刪除
  - [x] 8.8.6 所有操作（新增/修改/刪除）顯示 inline 訊息（showMsg）

### 8B. 上傳國泰證日對帳單並自動帶入交易明細（skill ch4.5）

- [x] 8.9 後端 PDF 解析服務（使用 UglyToad.PdfPig 套件，基於 Word 座標定位解析）
  - [x] 8.9.1 安裝 PDF 解析套件（UglyToad.PdfPig 1.7.0-custom-5）
  - [x] 8.9.2 實作國泰證日對帳單 PDF 解密與文字解析邏輯（CathayStatementParserService）
  - [x] 8.9.3 解析交易明細欄位對應：商品名稱→StockName、成交股數→Quantity、單價→Price、手續費→Fee、交易稅→Tax、客戶應收付額→NetAmount/Action
  - [x] 8.9.4 交易方向判斷：客戶應收付額「+」→SELL（NetAmount 正值）、「-」→BUY（NetAmount 負值）
  - [x] 8.9.5 基礎驗證（TradeDate 合法日期、StockId 非空、Quantity/Price > 0、NetAmount 方向一致）
- [x] 8.10 `POST /api/trades/cathay-daily-statement/parse` — 上傳與解析國泰證日對帳單 API
  - [x] 8.10.1 接收 multipart/form-data（file: PDF, password: string）
  - [x] 8.10.2 回傳解析後交易明細陣列（tradeDate, stockId, stockName, action, quantity, price, fee, tax, otherCost, netAmount, customerReceivablePayableRaw）
  - [x] 8.10.3 密碼錯誤或格式不符回傳適當錯誤訊息
- [x] 8.11 `POST /api/trades/batch` — 批次新增多筆交易紀錄 API
  - [x] 8.11.1 接收 items 陣列，逐筆建立 TradeRecord
  - [x] 8.11.2 回傳成功筆數與失敗明細
- [x] 8.12 前端 `/trades` 匯入國泰證日對帳單區塊
  - [x] 8.12.1 檔案選擇按鈕（僅接受 .pdf）、密碼輸入框（預設值 A120683373）、「解析對帳單」按鈕
  - [x] 8.12.2 呼叫後端解析 API，顯示「待確認交易明細表格」（交易日期、股票代號、股票名稱、交易別、股數、單價、手續費、交易稅、客戶應收付額）
  - [x] 8.12.3 每列可單筆移除（排除不匯入的明細）、可編輯股票代號與備註欄位
  - [x] 8.12.4 備註文字說明：「客戶應收付額：+ 代表賣出（SELL），- 代表買入（BUY）」
  - [x] 8.12.5 「確認新增」按鈕呼叫批次 API 寫入，成功後顯示訊息、清空暫存表格、自動更新查詢結果

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
  - [x] 9.8.1 股票代號自動帶出名稱，股票名稱自動帶入計畫名稱（「存XX」）
  - [x] 9.8.2 開始日期預設系統日、金額預設 2000
  - [x] 9.8.3 約定清單每列可修改（Bootstrap Modal：計畫名稱、金額、狀態啟用/停用、結束日期、備註）
  - [x] 9.8.4 週期金額分析列表：依 cycleType + cycleDay 分組統計（如「每月/第6日」），顯示啟用約定數與每期總金額

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
- [x] 12.5 前端總市值、總成本、整體未實現損益摘要區（未實現損益/報酬率：>=1 藍色+號、<1 紅色；卡片邊框同步）
- [x] 12.6 前端持股比例圓餅圖 / donut chart
- [x] 12.7 前端持股明細表格（股數、市值、比例），標題列可排序，預設市值降序

### 12B. 單一持股買入金額與比例

- [x] 12.8 實作買入金額組成計算（TradeRecord BUY + DcaExecution SUCCESS）
- [x] 12.9 `GET /api/dashboard/holding/{stockId}/buy-distribution` — 買入分佈 API
- [x] 12.10 前端 `/stock/{stockId}` 買入組成圓餅圖（手動 vs DCA）
- [x] 12.11 前端買入紀錄明細表格（金額、比例）

---

## 13. 平均成本與未實現損益（skill ch15）

- [x] 13.1 實作單檔未實現損益計算服務（AvgCost、MarketValue、UnrealizedPnL、UnrealizedReturn）— 總市值已修正為每檔各自取最新收盤價
- [x] 13.2 `GET /api/portfolio/stock/{stockId}/unrealized` — 單檔未實現損益 API
- [x] 13.3 `GET /api/portfolio/unrealized-summary` — 整體組合未實現損益摘要 API
- [x] 13.4 前端 `/pnl` 損益與成本查詢頁面
  - [x] 13.4.1 進入時自動載入所有持股損益明細
  - [x] 13.4.2 標題列可排序（代號、名稱、持股、平均成本、現價、總成本、市值、未實現損益、報酬率），預設報酬率降序
  - [x] 13.4.3 報酬率整列顏色：≥100% 藍色、≥1%且<100% 黑色、≤0% 紅色
  - [x] 13.4.5 整體摘要未實現損益/報酬率：>=1 加「+」符號、藍色字；<1 紅色字
  - [x] 13.4.4 每檔持股「詳細」按鈕以 target="_blank" 另開新視窗至個股走勢頁
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
- [x] 14.9 進入頁面自動載入建議結果

---

## 15. 季線風險警示（skill ch17）

- [x] 15.1 實作持股跌破季線檢查服務（取近 60 日收盤價 → 計算 MA60 → 比較現價）
- [x] 15.2 `GET /api/alerts/below-quarterly-ma?days=` — 持股跌破季線 API
- [x] 15.3 前端 `/dashboard` 季線風險警示區塊
  - [x] 15.3.1 標題列可排序（代號、名稱、股數、現價、季線（MA60）、偏離、偏離%），預設偏離降序
  - [x] 15.3.2 欄位名稱「季線（MA60）」
- [x] 15.4 前端 `/alerts` 獨立頁面，進入時自動載入檢查結果

---

## 16. 健康管理紀錄模組（skill §2）

> Level 1: HEALTH_ROOT → Level 2: HEALTH_CHECKUP → Level 3 功能頁

### 16A. 每三個月健檢報告（廖內科）

#### 16A.1 資料庫設計

- [x] 16.1 建立 `QuarterHealthReport` 主表（Id, ReportDate, HospitalName, SourceFileName, SourceFilePath, OcrJsonRaw, CreatedAt, UpdatedAt）
- [x] 16.2 建立 `QuarterHealthReportDetail` 明細表（10 項檢驗值 + 4 項異常旗標）
  - [x] 脂質：TCholesterol, Triglyceride, HDL（decimal(6,2) nullable）
  - [x] 肝功能：SGPT_ALT（decimal(6,2) nullable）
  - [x] 腎功能：Creatinine, UricAcid, MDRD_EGFR, CKDEPI_EGFR（decimal(6,2) nullable）
  - [x] 血糖：AcSugar（decimal(6,2) nullable）, Hba1c（decimal(4,2) nullable）
  - [x] 異常旗標：TriglycerideHigh, HDLLow, AcSugarHigh, Hba1cHigh（bit nullable）
- [x] 16.3 EF Core Entity + DbContext 配置（一對一 FK、CreatedAt 預設值）
- [x] 16.4 EF Core Migration `AddQuarterHealthReport`

#### 16A.2 OCR 影像辨識服務（Tesseract）

- [x] 16.5 安裝 Tesseract NuGet 套件（`Tesseract` 5.2.0）
- [x] 16.6 下載 tessdata 訓練資料（`eng.traineddata` 23MB + `chi_tra.traineddata` 57MB）
- [x] 16.7 四方向旋轉偵測（0°/90°/180°/270°），以英文字母數字計數評分，20% 門檻避免誤判
- [x] 16.8 雙語言 × 雙 PSM 模式辨識（eng + chi_tra × Auto + SingleBlock），合併取聯集
- [x] 16.9 逐行關鍵字比對解析（`ParseByLine`）：每行比對項目關鍵字（含 OCR 常見誤判字如「種化血色素」「薩化血色素」）
- [x] 16.10 數值範圍驗證（`PickBestNumber`）：每項檢驗定義合理 min/max，排除 OCR 亂碼數字，優先選有小數點的值
- [x] 16.11 報告日期偵測（民國年 → 西元，支援「報告日期」「就醫日期」格式）
- [x] 16.12 醫療院所偵測（中文 OCR 比對「廖內科」「醫院」「診所」等關鍵字）

#### 16A.3 後端 API（`HealthCheckupQuarterController`）

- [x] 16.13 `POST /api/health/checkup/qtr/upload` — 上傳 JPG/PNG + OCR 解析，回傳辨識數值供前端確認
- [x] 16.14 `POST /api/health/checkup/qtr/manual` — 手動儲存報告（接收確認後的數值，自動計算異常旗標）
- [x] 16.15 `GET /api/health/checkup/qtr/{reportId}` — 單筆報告明細
- [x] 16.16 `GET /api/health/checkup/qtr/list` — 歷史報告列表（依 ReportDate 降序）
- [x] 16.17 `DELETE /api/health/checkup/qtr/{reportId}` — 刪除報告

#### 16A.4 前端 — 每三個月報告紀錄上傳 (`/health/checkup/qtr/upload`)

- [x] 16.18 上傳區塊：檔案選擇（JPG/PNG）、報告日期、醫療院所、「上傳並解析」按鈕（含 spinner）
- [x] 16.19 手動輸入區塊：10 項檢驗欄位分四群組（脂質代謝 / 肝功能 / 腎功能 / 血糖）
- [x] 16.20 OCR 辨識後自動填入欄位，異常值紅框 + 正常值綠框標示，顯示「已辨識 X/10 項」
- [x] 16.21 儲存報告按鈕（含上傳檔案路徑 + OCR 原始 JSON 一併寫入）
- [x] 16.22 歷史報告列表（報告日期、醫療院所、各項數值、刪除操作）

#### 16A.5 前端 — 每三個月報告儀表板 (`/health/checkup/qtr/dashboard`)

- [x] 16.23 報告選擇下拉 + 摘要 Badge（紅色=異常 / 綠色=正常，含 ↑↓ 箭頭）
- [x] 16.24 與上一次比較卡片（8 項指標：本次值 vs 前次值 + 差異 ↑↓，顏色標示好壞方向）
- [x] 16.25 趨勢圖（Chart.js 折線圖 × 4）：三酸甘油脂(參考線 150)、HDL(參考線 40)、飯前血糖(參考線 100)、HbA1c(參考線 5.6)
- [x] 16.26 完整歷史紀錄表（10 項檢驗值，異常值紅字+箭頭、正常值綠字）

### 16B. 公司每年健檢報告（未來功能）

- [ ] 16.27 公司每年報告紀錄上傳 API 與前端 `/health/checkup/company/upload`
- [ ] 16.28 公司每年報告儀表板前端 `/health/checkup/company/dashboard`

---

## 17. 生活計帳模組（skill §2, §18）

> Level 1: LIFE_ROOT → Level 2: LIFE_SIS / LIFE_UTILITY → Level 3 功能頁

### 17A. 妹妹紀錄（未來功能）

- [ ] 17.1 妹妹紀錄資料表設計（收支與事件紀錄）
- [ ] 17.2 妹妹紀錄維護 API 與前端 `/life/sister/records`
- [ ] 17.3 妹妹紀錄年度分析 API 與前端 `/life/sister/yearly-analysis`

### 17B. 每期水電費瓦斯紀錄（skill §18）

#### 17B.1 每期電費紀錄

- [x] 17.4 建立電費紀錄資料表 `Life_ElectricityBillPeriod`（21 欄位：Address, PowerNo, BlackoutGroup, BillingStartDate/EndDate, BillingDays, ReadOrDebitDate, Kwh, KwhPerDay, AvgPricePerKwh, TotalAmount, InvoiceAmount, TariffType, SharedMeterHouseholdCount, InvoicePeriod, InvoiceNo, RawDetailJson, Remark, CreateTime, UpdateTime）
- [x] 17.5 EF Core Entity + DbContext 配置（複合索引 PowerNo+BillingEndDate、PowerNo+ReadOrDebitDate）+ Migration `AddLifeElectricityBillPeriod` + `SeedLifeUtilityMenu`
- [x] 17.6 後端 PDF 解析服務 `TaipowerBillParserService`：UglyToad.PdfPig 解密 + 全形正規化 + 民國日期轉換 + 18 欄位 Regex 抽取（地址含樓層、雙日期抄表日解析、電價種類+時間種類組合、逐項費用明細 JSON）
- [x] 17.7 `POST /api/life/utility/electricity/upload` — 上傳台電 PDF 帳單解析+儲存 API（含重複偵測：同電號+計費結束日自動更新）
- [x] 17.8 `GET /api/life/utility/electricity/period-records` — 電費紀錄列表 API（支援 year/month 篩選）
- [x] 17.9 `GET /api/life/utility/electricity/period-records/{id}` — 單筆電費紀錄明細 API
- [x] 17.10 `PUT /api/life/utility/electricity/period-records/{id}` — 修改電費紀錄 API
- [x] 17.11 `DELETE /api/life/utility/electricity/period-records/{id}` — 刪除電費紀錄 API
- [x] 17.12 前端 `/life/utility/electricity/period-records` 頁面：PDF 上傳（含 spinner）、年/月篩選、紀錄列表（明細 Modal / 修改 Modal / 刪除確認）、電費明細項目表格
- [x] 17.12a 備註欄位擴充：Entity `Remark` MaxLength 200→500 + Migration `ExpandRemarkTo500`
- [x] 17.12b 紀錄列表新增「備註」欄位（超過 20 字截斷+hover tooltip 顯示完整內容）
- [x] 17.12c 明細 Modal 新增可編輯備註區塊：textarea（4行, maxlength=500）+ 即時字數計數（n/500）+「儲存備註」按鈕（呼叫 PUT API 單獨存備註）
- [x] 17.12d 修改 Modal / 上傳確認區備註改為 textarea（4行, maxlength=500）+ 即時字數計數

#### 17B.2 每期電費儀表板

- [x] 17.13 `GET /api/life/utility/electricity/dashboard?year=` — 電費儀表板資料 API（依 BillingEndDate 月份分組，回傳 kwhTotal / amountTotal / billCount / remarks[]）
- [x] 17.14 前端 `/life/utility/electricity/dashboard` 頁面：年份篩選、年度摘要卡片（總度數/總金額/帳單數）、Chart.js 雙軸圖（柱狀=度數+折線=金額）、月份明細表格（含備註欄）
- [x] 17.15a 同期比較分析：前端同時呼叫選定年+前一年 dashboard API（Promise.all），YoY 摘要卡片 ×4（用電量 YoY%、電費 YoY%、用電量差異、電費差異，紅=增加/綠=減少）
- [x] 17.15b 同期比較圖表：Chart.js 雙年度疊加（實心柱狀=今年度數 vs 透明柱狀=去年度數，實線=今年金額 vs 虛線=去年金額），tooltip 顯示逐月 YoY%
- [x] 17.15c 同期比較明細表：逐月對比（今年/去年 度數+金額 + 差異 + YoY%，紅/綠色標示）+ 合計列（`table-secondary fw-bold`，各年度總度數/總金額/總差異/總 YoY%）+ 備註欄（合併兩年備註，超過 30 字截斷+tooltip）

#### 17B.3 每期水費紀錄 / 儀表板

##### 17B.3a 每期水費紀錄

- [x] 17.16 建立水費紀錄資料表 `Life_WaterBillPeriod`（14 欄位：WaterAddress, WaterNo, MeterNo, BillingStartDate/EndDate, BillingDays, BillingPeriodText, TotalUsage, CurrentUsage, CurrentMeterReading, PreviousMeterReading, TotalAmount, RawDetailJson, Remark, CreateTime, UpdateTime）
- [x] 17.17 EF Core Entity `LifeWaterBillPeriod` + DbContext 配置（索引 WaterNo+BillingEndDate、CreateTime 預設 SYSUTCDATETIME()）+ Migration `AddLifeWaterBillPeriod`
- [x] 17.18 後端 PDF 解析服務 `TaiwaterBillParserService`：UglyToad.PdfPig 解密（密碼 2gaijdrl）+ NFKC 正規化（解決 CJK 相容漢字 U+F963 度）+ 全形→半形轉換 + Letters API 逐行文字抽取 + Regex 抽取（水號從條碼字串 171101K220209750 解析為 K-22-020975-0、模糊 CJK 匹配 ExtractFieldIntFuzzy、費用明細含基本費/用水費/維護費/C退還負值）
- [x] 17.19 `IWaterBillService` / `WaterBillService`：Save（重複偵測同水號+計費結束日自動更新）、GetList、GetById、Update、Delete、GetDashboard（依 BillingEndDate 分期，UsageTotal 用 TotalUsage??CurrentUsage fallback）
- [x] 17.20 `POST /api/life/utility/water/upload` — 上傳台水 PDF 帳單解析 API
- [x] 17.21 `POST /api/life/utility/water/save` — 確認儲存水費紀錄 API
- [x] 17.22 `GET /api/life/utility/water/period-records` — 水費紀錄列表 API
- [x] 17.23 `GET /api/life/utility/water/period-records/{id}` — 單筆水費紀錄明細 API
- [x] 17.24 `PUT /api/life/utility/water/period-records/{id}` — 修改水費紀錄 API
- [x] 17.25 `DELETE /api/life/utility/water/period-records/{id}` — 刪除水費紀錄 API
- [x] 17.26 前端 `/life/utility/water/period-records` 頁面：PDF 上傳→解析→確認區塊（可修改所有欄位）、固定資訊列（用水地址/水號/水表號碼）、紀錄列表（明細 Modal / 修改 Modal / 刪除確認）、費用明細 JSON 展示、備註欄位（textarea 500 字+字數計數）

##### 17B.3b 每期水費儀表板

- [x] 17.27 `GET /api/life/utility/water/dashboard?year=` — 水費儀表板資料 API（依 BillingEndDate 年份篩選，回傳 periodIndex / periodLabel / usageTotal / amountTotal / remark）
- [x] 17.28 前端 `/life/utility/water/dashboard` 頁面：年份篩選、年度摘要卡片（總用水/總金額/帳單筆數）、Chart.js 雙軸圖（柱狀=用水度數+折線=應繳金額）、期別明細表格（含備註欄）
- [x] 17.29 同期比較分析：前端同時呼叫選定年+前一年 dashboard API（Promise.all），YoY 摘要卡片 ×4（用水量 YoY%、水費 YoY%、用水量差異、水費差異，紅=增加/綠=減少）
- [x] 17.30 同期比較圖表：Chart.js 雙年度疊加（實心柱狀=今年度數 vs 透明柱狀=去年度數，實線=今年金額 vs 虛線=去年金額），tooltip 顯示逐期 YoY%
- [x] 17.31 同期比較明細表：逐期對比（今年/去年 度數+金額 + 差異 + YoY%，紅/綠色標示）+ 合計列 + 備註欄

#### 17B.4 每期瓦斯紀錄 / 儀表板（預留）

- [ ] 17.18 每期瓦斯紀錄 API 與前端 `/life/utility/gas/period-records`（待補規格）
- [ ] 17.19 每期瓦斯儀表板前端 `/life/utility/gas/dashboard`（待補規格）

---

## 18. 整合測試與部署

- [x] 18.1 撰寫後端單元測試（Service 層計算邏輯）— TechnicalIndicatorServiceTests、PortfolioServiceTests
- [x] 18.2 撰寫 API 整合測試 — TradesControllerTests、DcaControllerTests（含 DCA→TradeRecord 同步驗證）
- [x] 18.3 前端功能驗證（各頁面完整流程）— 啟動後 API 端點全數回應 200
- [x] 18.4 效能調校（DB 查詢最佳化、索引檢視）— EF Core 索引已在 Migration 中建立
- [x] 18.5 部署設定：run.bat（前景啟動）/ stop.bat 啟停腳本

---

## 19. UX 優化與前端強化

- [x] 19.1 所有頁面 `alert()` 替換為 inline `showMsg()` 訊息（Bootstrap dismissible alerts，含圖示、時間戳、自動捲動、成功訊息自動消失）
- [x] 19.2 全站中文化（所有標籤、按鈕、狀態 badge、圖表標籤、貨幣格式 NT$）
- [x] 19.3 開發環境靜態檔案 no-cache（Program.cs StaticFileOptions）避免瀏覽器快取舊 JS/CSS
- [x] 19.4 GitHub 版控初始化（.gitignore、first commit、push to origin/main）

---

## 20. IIS 部署指引

### 20.1 發佈指令

```
cd HSPAS.Api
dotnet publish -c Release
```
D:\0.TradeVan\2235Stock-trading\HSPAS.Api\bin\Release\net9.0\publish
發佈輸出目錄：`HSPAS.Api\bin\Release\net9.0\publish\`

### 20.2 需複製至 IIS 網站目錄的檔案

將以下 `publish\` 目錄下的**所有檔案與資料夾**完整複製至 IIS 網站根目錄（例如 `C:\inetpub\wwwroot\HSPAS\`）：

```
publish\
├── appsettings.json                          ← 連線字串等設定（部署前請修改）
├── appsettings.Development.json              ← 開發環境設定（正式環境可移除）
├── web.config                                ← IIS 必要設定檔（ASP.NET Core Module）
├── HSPAS.Api.exe                             ← 應用程式主執行檔
├── HSPAS.Api.dll                             ← 應用程式主程式庫
├── HSPAS.Api.deps.json                       ← 相依套件描述
├── HSPAS.Api.runtimeconfig.json              ← 執行階段設定
├── HSPAS.Api.staticwebassets.endpoints.json   ← 靜態資源端點描述
├── HSPAS.Api.pdb                             ← 偵錯符號檔（正式環境可選擇不複製）
│
├── *.dll（根目錄下所有 DLL）                   ← 相依套件（共 47 個 DLL）
│   ├── Azure.Core.dll
│   ├── Azure.Identity.dll
│   ├── Microsoft.AspNetCore.OpenApi.dll
│   ├── Microsoft.Data.SqlClient.dll
│   ├── Microsoft.EntityFrameworkCore.dll
│   ├── Microsoft.EntityFrameworkCore.Relational.dll
│   ├── Microsoft.EntityFrameworkCore.SqlServer.dll
│   ├── Microsoft.EntityFrameworkCore.Abstractions.dll
│   ├── Microsoft.Extensions.*.dll（共 8 個）
│   ├── Microsoft.Identity.Client.dll
│   ├── Microsoft.Identity.Client.Extensions.Msal.dll
│   ├── Microsoft.IdentityModel.*.dll（共 6 個）
│   ├── Microsoft.OpenApi.dll
│   ├── Microsoft.SqlServer.Server.dll
│   ├── Microsoft.Win32.SystemEvents.dll
│   ├── Microsoft.Bcl.AsyncInterfaces.dll
│   ├── System.ClientModel.dll
│   ├── System.Configuration.ConfigurationManager.dll
│   ├── System.Drawing.Common.dll
│   ├── System.IdentityModel.Tokens.Jwt.dll
│   ├── System.Memory.Data.dll
│   ├── System.Runtime.Caching.dll
│   ├── System.Security.Cryptography.ProtectedData.dll
│   ├── System.Security.Permissions.dll
│   ├── System.Windows.Extensions.dll
│   ├── UglyToad.PdfPig.dll
│   ├── UglyToad.PdfPig.Core.dll
│   ├── UglyToad.PdfPig.Fonts.dll
│   ├── UglyToad.PdfPig.Tokenization.dll
│   ├── UglyToad.PdfPig.Tokens.dll
│   ├── Tesseract.dll                         ← OCR 引擎（健檢報告辨識）
│   └── InteropDotNet.dll                     ← Tesseract 相依
│
├── runtimes\                                  ← 平台相依原生程式庫（整個資料夾複製）
│   ├── win\lib\net6.0\                       ← Windows 平台 DLL
│   ├── win-x64\native\                       ← SQL Server SNI 原生元件
│   ├── win-x86\native\
│   ├── win-arm\native\
│   ├── win-arm64\native\
│   └── unix\lib\net6.0\                      ← Linux 平台 DLL（Windows IIS 可不複製）
│
├── tessdata\                                     ← Tesseract OCR 訓練資料（必須複製）
│   ├── eng.traineddata                          ← 英文模型（23MB）
│   └── chi_tra.traineddata                      ← 繁體中文模型（57MB）
│
└── wwwroot\                                   ← 前端靜態檔案（整個資料夾複製）
    ├── index.html                            ← SPA 主頁
    ├── css\site.css                          ← 自訂樣式
    ├── js\                                   ← JavaScript 模組
    │   ├── router.js
    │   ├── dashboard.js
    │   ├── calendar.js
    │   ├── stock.js
    │   ├── etf.js
    │   ├── trades.js
    │   ├── dca.js
    │   ├── pnl.js
    │   ├── recommendations.js
    │   ├── alerts.js
    │   ├── backfill.js
    │   ├── settings.js
    │   ├── menu-sorting.js
    │   ├── health-qtr-upload.js              ← 健檢報告上傳（OCR 辨識 + 手動輸入）
    │   ├── health-qtr-dashboard.js           ← 健檢報告儀表板（趨勢圖 + 比較）
    │   ├── elec-period-records.js            ← 每期電費紀錄（PDF 上傳 + CRUD）
    │   ├── elec-dashboard.js                 ← 電費儀表板（雙軸圖 + 月份表格）
    │   ├── water-period-records.js           ← 每期水費紀錄（PDF 上傳 + CRUD）
    │   └── water-dashboard.js                ← 水費儀表板（雙軸圖 + 同期比較）
    └── pages\                                ← HTML 頁面片段
        ├── dashboard.html
        ├── calendar.html
        ├── stock.html
        ├── etf.html
        ├── trades.html
        ├── dca.html
        ├── pnl.html
        ├── recommendations.html
        ├── alerts.html
        ├── backfill.html
        ├── settings.html
        ├── menu-sorting.html
        ├── health-qtr-upload.html            ← 健檢報告上傳頁面
        ├── health-qtr-dashboard.html         ← 健檢報告儀表板頁面
        ├── elec-period-records.html          ← 每期電費紀錄頁面
        ├── elec-dashboard.html               ← 電費儀表板頁面
        ├── water-period-records.html         ← 每期水費紀錄頁面
        └── water-dashboard.html              ← 水費儀表板頁面
```

> **簡易做法**：直接將 `publish\` 資料夾內的所有內容完整複製到 IIS 網站目錄即可。

### 20.3 IIS 設定注意事項

1. **安裝 ASP.NET Core Hosting Bundle**（.NET 9.0 Runtime）
2. **應用程式集區**：設定為「No Managed Code」（無受控程式碼）
3. **appsettings.json**：部署前修改 `ConnectionStrings:DefaultConnection` 為正式環境 DB 連線
4. **web.config**：publish 時已自動產生，包含 ASP.NET Core Module 設定，無需手動修改

