```markdown
# 鴻仁股票損益系統 HSPAS(Hung-Jen Stock Profit Analysis System) skill.md

## 1. 系統基本資訊

- 系統名稱：鴻仁股票損益系統  
- 英文名稱建議：Hung-Jen Stock Profit Analysis System（簡稱：HSPAS 或 SPAS）  
- 系統簡要說明：  
  - 「鴻仁股票損益系統」用來紀錄鴻仁本人的股票與 ETF 交易紀錄（含一次性買賣與定期定額），並整合台股每日行情資料，計算每檔標的與整體投資組合的損益與報酬率。  
  - 系統包含：每日行情查詢（日曆選日）、歷史資料回補、ETF 專區、技術指標計算（MA、RSI）、個人交易紀錄、定期定額紀錄、儀表板（持股與買入比例）、平均成本／未實現損益查詢、智慧選股建議與季線風險警示等功能。  

- 技術棧：  
  - 後端：ASP.NET Core 9 Web API（.NET 9 LTS）  
  - 前端：HTML5 + Bootstrap + JavaScript  
  - 資料庫：SQL Server（MSSQL）  

## 資料庫

**連線資訊 — **

```

Server=localhost
Database=HSPAS
User ID=hspasmgr
Password=tvhspasmgr
TrustServerCertificate=True

```

## Web 服務

**連線資訊 — **

```

URL=http://localhost:5117
Swagger=http://localhost:5117/openapi/v1.json
啟動腳本=start.ps1
停止腳本=stop.ps1

```

---

## 2. 系統目標與範圍

- 目標：  
  - 紀錄鴻仁所有買賣股票、ETF 的交易明細（現股／定期定額）。  
  - 根據每日收盤價自動更新持有部位市值與未實現損益。  
  - 提供單筆交易、單檔標的與整體組合的損益分析與報表。  
  - 結合簡單技術指標，提供長期／短期投資的輔助建議與風險警示。  

- 範圍：  
  - 台股股票與 ETF 為主要標的（暫不涵蓋期權、海外市場；未來如需再擴充）。  

---

## 3. 前端主框架與功能導覽

### 3.1 主頁面布局結構

系統所有主要頁面（例如 `/dashboard`、`/calendar`、`/stock/{stockId}` 等）統一使用三區塊布局：

1. 最上方橫幅（Header / Top Bar）  
   - 位置：畫面最上方，水平橫幅。  
   - 內容：  
     - 左側顯示系統名稱文字：**「鴻仁股票損益系統 HSPAS」**。  
     - 右側預留空間，可放使用者名稱、設定／登出按鈕（之後實作）。  

2. 左側功能列（Sidebar / Navigation）  
   - 位置：畫面左邊垂直側邊欄。  
   - 內容：功能選單（見 3.2 功能列表 Table）。  
   - 行為：  
     - 點選功能列項目時，右側主內容區載入對應頁面。  
     - 選中項目需有高亮顯示。  

3. 右側主內容區（Main Content）  
   - 位置：左側功能列右方，占據大部分寬度。  
   - 內容：顯示目前選取功能的畫面（儀表板、日曆、交易紀錄、報表等）。  

實作建議：  
- 使用 Bootstrap Grid 或 CSS Flexbox 建立：上方固定 Header，底下左右分割 Sidebar + Main Content。  

### 3.2 左側功能列 – 功能列表

左側功能列為整個系統主導覽，功能項目如下：

| 功能代碼 | 顯示名稱             | 分類       | 路由 / 頁面                      | 用途說明 |
|----------|----------------------|------------|----------------------------------|----------|
| DASH     | 儀表板 Dashboard     | 概覽       | `/dashboard`                     | 顯示總體組合市值、損益摘要、持股比例與季線風險警示 |
| CAL      | 日曆行情查詢         | 行情查詢   | `/calendar`                      | Google Calendar 風格月曆，點日期查當日台股全市場行情 |
| STOCK    | 個股/ETF 查詢入口    | 行情查詢   | `/stock`（搜尋入口）            | 搜尋並跳轉到 `/stock/{stockId}` 或 `/etf/{etfId}` 詳細頁 |
| ETF      | ETF 專區             | 行情查詢   | `/etf`                           | 顯示 ETF 清單、基本資訊與簡要行情，可進入單一 ETF 頁 |
| TRD      | 交易紀錄管理         | 紀錄維護   | `/trades`                        | 管理一般買賣交易紀錄（TradeRecord） |
| DCA      | 定期定額管理         | 紀錄維護   | `/dca`                           | 管理定期定額約定與執行紀錄（DcaPlan + DcaExecution） |
| PNL      | 損益與成本查詢       | 分析報表   | `/pnl`                           | 查詢單檔與整體投資組合的平均成本與未實現損益 |
| IDEAS    | 投資建議（長/短期）  | 決策輔助   | `/recommendations` 或 `/ideas`   | 顯示長期投資候選與短期／波段候選清單 |
| ALERT    | 風險警示（季線跌破） | 決策輔助   | `/alerts` 或整合在 `/dashboard` | 列出目前持股中跌破季線（MA60）的標的 |
| HIST     | 歷史資料回補         | 管理工具   | `/admin/history-backfill`        | 管理者用，指定日期區間回補每日行情資料 |
| CONF     | 系統設定（預留）     | 管理工具   | `/settings`                      | 預留未來使用，控制系統參數與顯示偏好 |

前端行為：  
- Sidebar 點選功能後，使用前端路由或 hyperlink 導向指定路由，右側 Main Content 依路由顯示對應頁。  
- Header + Sidebar 為共用 Layout，不隨頁面切換而重繪。  

---

## 4. 個人買賣股票紀錄功能（一般交易）

### 4.1 目標

- 紀錄所有「一次性買進／賣出」台股股票與 ETF 的交易明細（含手續費、交易稅）。  
- 搭配每日行情，計算：  
  - 單筆交易已實現損益  
  - 個股整體持有成本、未實現損益  
  - 整體投資組合損益與報酬率  

### 4.2 資料庫：TradeRecord（一般交易紀錄）

- Table: `TradeRecord`  

欄位（Column）：  

- `Id` bigint, PK, identity  
- `TradeDate` date, NOT NULL  
- `StockId` varchar(10), NOT NULL  
- `StockName` nvarchar(50), NOT NULL  
- `Action` varchar(10), NOT NULL  // `BUY` / `SELL` / `DIVIDEND`  
- `Quantity` int, NOT NULL  
- `Price` decimal(19,4), NOT NULL  
- `Fee` decimal(19,4), NOT NULL  
- `Tax` decimal(19,4), NOT NULL  
- `OtherCost` decimal(19,4), NULL  
- `NetAmount` decimal(19,4), NOT NULL  // 買進負值、賣出正值  
- `Note` nvarchar(200), NULL  
- `CreateTime` datetime2, NOT NULL  

### 4.3 個股持有與損益計算（邏輯）

- 目前持股股數 `CurrentQty`：  
  - 所有 BUY + DCA 買入 − SELL。  
- 總投入成本（簡化）：  
  - `TotalBuyAmount = Σ(所有買入的 (Price * Quantity + Fee + Tax + OtherCost))`。  
- 平均成本：  
  - `AvgCost = TotalBuyAmount ÷ CurrentQty`（CurrentQty > 0）。  
- 已實現損益：  
  - 所有 SELL 的 `NetAmount` 加總 + `DIVIDEND` 股利。  
- 未實現損益與報酬率：見第 14 章。  

### 4.4 交易紀錄 API

- `POST /api/trades`  
- `GET /api/trades/{stockId}?from=YYYY-MM-DD&to=YYYY-MM-DD`  
- `GET /api/portfolio/summary`（可選）  

---

## 5. 定期定額買入紀錄功能（存股／定期定額）

### 5.1 目標

- 紀錄「定期定額買入」的約定與實際執行結果：  
  - 約定（DcaPlan）：標的、週期、金額、起訖日。  
  - 執行（DcaExecution）：每次扣款與成交紀錄。  
- 分析定期定額計畫的累積投入、持有成本與績效。  

### 5.2 資料庫：DcaPlan

- Table: `DcaPlan`  

欄位：  

- `Id` bigint, PK, identity  
- `PlanName` nvarchar(100), NOT NULL  
- `StockId` varchar(10), NOT NULL  
- `StockName` nvarchar(50), NOT NULL  
- `StartDate` date, NOT NULL  
- `EndDate` date, NULL  
- `CycleType` varchar(20), NOT NULL  // `MONTHLY` / `WEEKLY`  
- `CycleDay` int, NOT NULL  
- `Amount` decimal(19,4), NOT NULL  
- `IsActive` bit, NOT NULL  
- `Note` nvarchar(200), NULL  
- `CreateTime` datetime2, NOT NULL  

### 5.3 資料庫：DcaExecution

- Table: `DcaExecution`  

欄位：  

- `Id` bigint, PK, identity  
- `PlanId` bigint, FK → `DcaPlan.Id`, NOT NULL  
- `TradeDate` date, NOT NULL  
- `StockId` varchar(10), NOT NULL  
- `Quantity` int, NOT NULL  
- `Price` decimal(19,4), NOT NULL  
- `Fee` decimal(19,4), NOT NULL  
- `Tax` decimal(19,4), NOT NULL  
- `OtherCost` decimal(19,4), NULL  
- `NetAmount` decimal(19,4), NOT NULL  
- `Status` varchar(20), NOT NULL  // `SUCCESS` / `FAILED` / `PARTIAL`  
- `Note` nvarchar(200), NULL  
- `CreateTime` datetime2, NOT NULL  

> 建議：每次 DCA 成功執行時，同步在 `TradeRecord` 加一筆 BUY 紀錄，方便統一用 `TradeRecord` 計算整體損益。  

### 5.4 API

- `POST /api/dca/plans`  
- `PUT /api/dca/plans/{id}`  
- `GET /api/dca/plans`  
- `GET /api/dca/plans/{id}`  
- `GET /api/dca/plans/{id}/executions`  

### 5.5 定期定額績效計算

- 累積投入：`Σ |NetAmount|`。  
- 累積股數：`Σ Quantity`。  
- 平均成本：`AvgCost = 累積投入 ÷ 累積股數`。  
- 目前市值與未實現損益：同第 14 章邏輯。  

---

## 6. 資料來源：TWSE 盤後資料

### 6.1 資料集與授權

- 資料集：盤後資訊 > 個股日成交資訊（上市個股日成交資訊）。  
- 平台：政府資料開放平臺 data.gov.tw。  
- 更新頻率：每個交易日盤後更新一次。  
- 授權：政府資料開放授權條款第 1 版。  

### 6.2 主要 API 端點

- 全市場當日盤後資料（CSV）：  
  - `https://www.twse.com.tw/exchangeReport/STOCK_DAY_ALL?response=open_data`  
  - 欄位：證券代號、證券名稱、成交股數、成交金額、開盤價、最高價、最低價、收盤價、漲跌價差、成交筆數。  

> 歷史資料：實作時從 data.gov.tw 或 TWSE 歷史下載頁取得，並餵給「歷史回補工具」。  

---

## 7. 資料庫：每日行情資料

### 7.1 DailyStockPrice

- Table: `DailyStockPrice`  

欄位：  

- `TradeDate` date, PK(1)  
- `StockId` varchar(10), PK(2)  
- `StockName` nvarchar(50), NOT NULL  
- `TradeVolume` bigint, NULL  
- `TradeValue` decimal(19,4), NULL  
- `OpenPrice` decimal(19,4), NULL  
- `HighPrice` decimal(19,4), NULL  
- `LowPrice` decimal(19,4), NULL  
- `ClosePrice` decimal(19,4), NULL  
- `PriceChange` decimal(19,4), NULL  
- `Transaction` int, NULL  
- `CreateTime` datetime2, NOT NULL  

---

## 8. 系統架構概觀

- 後端：ASP.NET Core 9 Web API（分層：Controllers / Services / Repositories / Domain / Infrastructure）。  
- 前端：HTML5 + Bootstrap + JS（共用 Layout：Header + Sidebar + Main Content）。  
- 資料庫：SQL Server。  

---

## 9. Web API：每日行情相關

### 9.1 可用日期清單

`GET /api/calendar/available-dates`  

### 9.2 取得指定日期全市場行情

`GET /api/daily-prices/by-date?date=YYYY-MM-DD`  

### 9.3 取得個股歷史價量

`GET /api/daily-prices/{stockId}/history?from=YYYY-MM-DD&to=YYYY-MM-DD`  

---

## 10. 前端：日曆查詢畫面

### 10.1 `/calendar`

- 載入 `/api/calendar/available-dates`。  
- 點日期時呼叫 `/api/daily-prices/by-date`。  
- 顯示行情表格，支援搜尋、排序。  
- 點代號跳 `/stock/{stockId}`。  

---

## 11. 管理者「歷史回補工具」

### 11.1 API

`POST /api/history/backfill`  

Body:  

```json
{
  "from": "YYYY-MM-DD",
  "to": "YYYY-MM-DD",
  "dryRun": false
}
```


### 11.2 前端 `/admin/history-backfill`

- 輸入日期區間 + dryRun 勾選。
- 顯示每日期結果。

---

## 12. ETF 擴充

### 12.1 EtfInfo

- Table: `EtfInfo`

欄位：

- `EtfId` varchar(10), PK
- `EtfName` nvarchar(100), NOT NULL
- `Category` nvarchar(50), NOT NULL
- `Issuer` nvarchar(100), NULL
- `IsActive` bit, NOT NULL
- `CreateTime` datetime2, NOT NULL


### 12.2 ETF API

- `GET /api/etf/list`
- `GET /api/etf/daily?date=YYYY-MM-DD`
- `GET /api/etf/{etfId}/history?from=...&to=...`


### 12.3 ETF 前端

- `/etf`
- `/etf/{etfId}`

---

## 13. 技術指標計算（MA、RSI）

### 13.1 服務介面

`ITechnicalIndicatorService`：

- `CalculateMovingAverage(...)`
- `CalculateRsi(...)`


### 13.2 API

`GET /api/indicators/{stockId}?from=YYYY-MM-DD&to=YYYY-MM-DD&ma=5,20,60&rsiperiod=14`

---

## 14. 儀表板功能（Dashboard）

### 14.1 儀表板一：持股市值與比例

API：`GET /api/dashboard/holdings`

### 14.2 儀表板二：單一持股買入金額與比例

API：`GET /api/dashboard/holding/{stockId}/buy-distribution`

前端 `/dashboard`：

- 顯示總體摘要、持股比例圖、買入組成等。

---

## 15. 平均成本與未實現損益查詢

### 15.1 單檔計算與 API

`GET /api/portfolio/stock/{stockId}/unrealized`

### 15.2 整體組合摘要

`GET /api/portfolio/unrealized-summary`

---

## 16. 智慧選股建議（長短期投資分類）

### 16.1 API

`GET /api/recommendations/stocks?scope=all|holding|watchlist`

前端 `/recommendations` 或 `/ideas`。

---

## 17. 季線風險警示（跌破季線監控）

### 17.1 API

`GET /api/alerts/below-quarterly-ma?days=60`

前端：整合在 `/dashboard` 的「風險警示」區塊或 `/alerts` 頁。

---

## 18. 實作指引（給開發者／開發 AI）

1. 使用 ASP.NET Core 9 建立 Web API，採分層架構。
2. 使用 EF Core 建立上述實體與 DbContext，金額欄位使用 `decimal(19,4)`。
3. 實作 TWSE 資料抓取 Service、技術指標 Service。
4. 前端實作共用 Layout（Header + Sidebar + Main Content），並依路由建立各頁面骨架。
5. 逐步實作 API 與前端頁面，確保與本 skill.md 規格一致。