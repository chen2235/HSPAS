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
| 5.0 | 2026-03-07 | UX 優化：所有頁面 alert() 改為 inline 訊息、中文化、排序功能、自動載入、表單預設值、DCA/交易紀錄編輯功能、損益顏色標示、靜態檔案 no-cache、run.bat/stop.bat |
| 6.0 | 2026-03-07 | 新增「上傳國泰證日對帳單」功能：PDF 解析 API、批次新增 API、前端匯入介面與待確認明細表格 |

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
- [x] 6.7 進入頁面自動載入當日行情，預設成交股數由大到小排序

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
- [x] 12.5 前端總市值、總成本、整體未實現損益摘要區
- [x] 12.6 前端持股比例圓餅圖 / donut chart
- [x] 12.7 前端持股明細表格（股數、市值、比例），標題列可排序，預設市值降序

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
  - [x] 13.4.1 進入時自動載入所有持股損益明細
  - [x] 13.4.2 標題列可排序（代號、名稱、持股、平均成本、現價、總成本、市值、未實現損益、報酬率），預設報酬率降序
  - [x] 13.4.3 報酬率整列顏色：≤0% 紅色、≥70% 藍色
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

## 16. 整合測試與部署

- [x] 16.1 撰寫後端單元測試（Service 層計算邏輯）— TechnicalIndicatorServiceTests、PortfolioServiceTests
- [x] 16.2 撰寫 API 整合測試 — TradesControllerTests、DcaControllerTests（含 DCA→TradeRecord 同步驗證）
- [x] 16.3 前端功能驗證（各頁面完整流程）— 啟動後 API 端點全數回應 200
- [x] 16.4 效能調校（DB 查詢最佳化、索引檢視）— EF Core 索引已在 Migration 中建立
- [x] 16.5 部署設定：run.bat（前景啟動）/ stop.bat 啟停腳本

---

## 17. UX 優化與前端強化

- [x] 17.1 所有頁面 `alert()` 替換為 inline `showMsg()` 訊息（Bootstrap dismissible alerts，含圖示、時間戳、自動捲動、成功訊息自動消失）
- [x] 17.2 全站中文化（所有標籤、按鈕、狀態 badge、圖表標籤、貨幣格式 NT$）
- [x] 17.3 開發環境靜態檔案 no-cache（Program.cs StaticFileOptions）避免瀏覽器快取舊 JS/CSS
- [x] 17.4 GitHub 版控初始化（.gitignore、first commit、push to origin/main）

---

## 18. IIS 部署指引

### 18.1 發佈指令

```
cd HSPAS.Api
dotnet publish -c Release
```
D:\0.TradeVan\2235Stock-trading\HSPAS.Api\bin\Release\net9.0\publish
發佈輸出目錄：`HSPAS.Api\bin\Release\net9.0\publish\`

### 18.2 需複製至 IIS 網站目錄的檔案

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
│   └── UglyToad.PdfPig.Tokens.dll
│
├── runtimes\                                  ← 平台相依原生程式庫（整個資料夾複製）
│   ├── win\lib\net6.0\                       ← Windows 平台 DLL
│   ├── win-x64\native\                       ← SQL Server SNI 原生元件
│   ├── win-x86\native\
│   ├── win-arm\native\
│   ├── win-arm64\native\
│   └── unix\lib\net6.0\                      ← Linux 平台 DLL（Windows IIS 可不複製）
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
    │   └── settings.js
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
        └── settings.html
```

> **簡易做法**：直接將 `publish\` 資料夾內的所有內容完整複製到 IIS 網站目錄即可。

### 18.3 IIS 設定注意事項

1. **安裝 ASP.NET Core Hosting Bundle**（.NET 9.0 Runtime）
2. **應用程式集區**：設定為「No Managed Code」（無受控程式碼）
3. **appsettings.json**：部署前修改 `ConnectionStrings:DefaultConnection` 為正式環境 DB 連線
4. **web.config**：publish 時已自動產生，包含 ASP.NET Core Module 設定，無需手動修改
