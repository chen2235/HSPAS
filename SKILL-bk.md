
```markdown
# 鴻仁股票損益系統 HSPAS(Hung-Jen Stock Profit Analysis System) skill.md

## 1. 系統基本資訊

- 系統名稱：鴻仁股票損益系統  
- 系統簡要說明：  
  - 「鴻仁股票損益系統」用來紀錄鴻仁本人的股票與 ETF 交易紀錄（含一次性買賣與定期定額），並整合台股每日行情資料，計算每檔標的與整體投資組合的損益與報酬率。  
  - 系統包含：每日行情查詢、日曆選日查行情、歷史資料回補、ETF 專區、技術指標計算（MA、RSI）、個人交易紀錄、定期定額紀錄、儀表板（持股與買入比例）與平均成本／未實現損益查詢等功能。  

## 2. 系統目標與範圍

- 目標：  
  - 紀錄鴻仁所有買賣股票、ETF 的交易明細（現股／定期定額）。  
  - 根據每日收盤價自動更新持有部位市值與未實現損益。  
  - 提供單筆交易、單檔標的與整體組合的損益分析與報表。  
- 範圍：  
  - 台股股票與 ETF 為主要標的（暫不涵蓋期權、海外市場；若未來需要，將在本 skill.md 另行擴充）。  

- 技術棧：  
  - 後端：ASP.NET Core 9 Web API（.NET 9 LTS）  
  - 前端：HTML5 + Bootstrap + JavaScript（可用輕量 JS library）  
  - 資料庫：SQL Server（MSSQL）  

## 資料庫
**連線資訊 — **

```
Server=localhost
Database=HSPAS
User ID=hspasmgr
Password=tvhspasmgr
TrustServerCertificate=True
---

## 3. 個人買賣股票紀錄功能（一般交易）

### 3.1 目標

- 紀錄鴻仁所有「一次性買進／賣出」台股股票與 ETF 的交易明細（含手續費、交易稅）。  
- 搭配每日行情，計算：  
  - 單筆交易已實現損益  
  - 個股整體持有成本、未實現損益  
  - 整體投資組合損益與報酬率  

### 3.2 資料庫設計：TradeRecord（一般交易紀錄）

資料表名稱：`TradeRecord`  

欄位設計：

- `Id` (bigint, PK, identity)：交易紀錄流水號。  
- `TradeDate` (date)：交易日期（成交日）。  
- `StockId` (varchar(10))：股票或 ETF 代號（如 2330、0050）。  
- `StockName` (nvarchar(50))：當時顯示名稱（方便檢視）。  
- `Action` (varchar(10))：交易動作，枚舉：`BUY`、`SELL`、`DIVIDEND`（股利入帳，可視需要）。  
- `Quantity` (int)：股數（買賣都用正數，實際多空由 Action 判斷）。  
- `Price` (decimal(10,2))：成交單價（每股）。  
- `Fee` (decimal(10,2))：手續費。  
- `Tax` (decimal(10,2))：交易稅（賣出才會有）。  
- `OtherCost` (decimal(10,2), nullable)：其他成本或折讓。  
- `NetAmount` (decimal(18,2))：交易淨金額，用於現金流：  
  - 買進：`NetAmount = -(Price * Quantity + Fee + Tax + OtherCost)`。  
  - 賣出：`NetAmount = +(Price * Quantity - Fee - Tax - OtherCost)`。  
- `Note` (nvarchar(200), nullable)：備註。  
- `CreateTime` (datetime2)：建立時間。  

### 3.3 個股持有與損益計算（邏輯）

- 目前持股股數：  
  - `CurrentQty = Σ(BUY/ DCA 買入股數) − Σ(SELL 賣出股數)`。  
- 總投入成本（簡化版本）：  
  - `TotalBuyAmount = Σ(所有買入的 (Price * Quantity + Fee + Tax + OtherCost))`。  
- 平均成本：  
  - `AvgCost = TotalBuyAmount ÷ CurrentQty`（CurrentQty > 0）。  
- 已實現損益：  
  - 所有 SELL 的 `NetAmount` 加總 + `DIVIDEND` 股利。  
- 未實現損益與報酬率見第 14 章。  

### 3.4 交易紀錄 API

- `POST /api/trades`  
  - 建立交易紀錄。  

- `GET /api/trades/{stockId}?from=YYYY-MM-DD&to=YYYY-MM-DD`  
  - 查詢指定標的在期間內的所有交易紀錄。  

- `GET /api/portfolio/summary`（可選）  
  - 彙總所有持股與市值、損益（可結合第 14 章邏輯）。  

---

## 4. 定期定額買入紀錄功能（存股／定期定額）

### 4.1 目標

- 紀錄「定期定額買入」的約定與實際執行結果：  
  - 約定層（DcaPlan）：標的、週期、金額、起訖日。  
  - 執行層（DcaExecution）：每次扣款與成交紀錄。  
- 分析定期定額計畫的累積投入、持有成本與績效。  

### 4.2 資料庫：定期定額約定表（DcaPlan）

資料表名稱：`DcaPlan`  

欄位：

- `Id` (bigint, PK, identity)  
- `PlanName` (nvarchar(100))：計畫名稱。  
- `StockId` (varchar(10))  
- `StockName` (nvarchar(50))  
- `StartDate` (date)  
- `EndDate` (date, nullable)  
- `CycleType` (varchar(20))：例如 `MONTHLY`、`WEEKLY`。  
- `CycleDay` (int)：  
  - 若 MONTHLY：每月幾號。  
  - 若 WEEKLY：星期幾。  
- `Amount` (decimal(18,2))：每次扣款目標金額。  
- `IsActive` (bit)  
- `Note` (nvarchar(200), nullable)  
- `CreateTime` (datetime2)  

### 4.3 資料庫：定期定額執行紀錄表（DcaExecution）

資料表名稱：`DcaExecution`  

欄位：

- `Id` (bigint, PK, identity)  
- `PlanId` (bigint, FK → DcaPlan.Id)  
- `TradeDate` (date)  
- `StockId` (varchar(10))  
- `Quantity` (int)  
- `Price` (decimal(10,2))  
- `Fee` (decimal(10,2))  
- `Tax` (decimal(10,2))  
- `OtherCost` (decimal(10,2), nullable)  
- `NetAmount` (decimal(18,2))：實際支出淨金額（通常負值）。  
- `Status` (varchar(20))：`SUCCESS`、`FAILED`、`PARTIAL` 等。  
- `Note` (nvarchar(200), nullable)  
- `CreateTime` (datetime2)  

> 建議：每次 DCA 成功執行時，同步在 `TradeRecord` 加一筆對應的 BUY 紀錄，方便後續用統一交易表做損益分析。  

### 4.4 定期定額 API

- `POST /api/dca/plans`：新增定期定額約定。  
- `PUT /api/dca/plans/{id}`：修改/停用約定。  
- `GET /api/dca/plans`：查詢所有約定。  
- `GET /api/dca/plans/{id}`：單一約定詳細資訊與績效摘要。  
- `GET /api/dca/plans/{id}/executions`：該計畫的所有執行紀錄。  

### 4.5 定期定額績效計算

- 累積投入：`Σ |NetAmount|`。  
- 累積股數：`Σ Quantity`。  
- 平均成本：`AvgCost = 累積投入 ÷ 累積股數`。  
- 目前市值：`CurrentQty × 最新收盤價`。  
- 未實現損益與年化報酬率可依第 14 章與指數投資常用算法實作。  

---

## 5. 資料來源：TWSE 盤後資料

### 5.1 資料集與授權

- 資料集：盤後資訊 > 個股日成交資訊（上市個股日成交資訊）。[web:83][web:87][web:99][web:100]  
- 平台：政府資料開放平臺 data.gov.tw。  
- 更新頻率：每個交易日盤後更新一次。[web:83]  
- 授權：政府資料開放授權條款第 1 版，可免費使用。[web:83][web:99][web:100]  

### 5.2 主要 API 端點

1. 全市場當日盤後資料（CSV）：  
   - URL：`https://www.twse.com.tw/exchangeReport/STOCK_DAY_ALL?response=open_data`。[web:70][web:86][web:88]  
   - 回傳欄位（實際以 CSV header 為準）：  
     - 證券代號、證券名稱、成交股數、成交金額、開盤價、最高價、最低價、收盤價、漲跌價差、成交筆數。[web:70][web:86][web:88]  

> 歷史資料可透過政府資料開放平臺提供的歷史檔或 TWSE 歷史查詢頁面取得，實作時請選擇一個穩定的歷史來源並對應到 Backfill 邏輯。[web:83][web:99][web:100][web:101][web:102]  

---

## 6. 資料庫：每日行情資料（MSSQL）

### 6.1 DailyStockPrice

```sql
CREATE TABLE dbo.DailyStockPrice (
    TradeDate      date        NOT NULL, -- 交易日期
    StockId        varchar(10) NOT NULL, -- 證券代號
    StockName      nvarchar(50) NOT NULL, -- 證券名稱
    TradeVolume    bigint      NULL,     -- 成交股數
    TradeValue     bigint      NULL,     -- 成交金額
    OpenPrice      decimal(10,2) NULL,
    HighPrice      decimal(10,2) NULL,
    LowPrice       decimal(10,2) NULL,
    ClosePrice     decimal(10,2) NULL,
    PriceChange    decimal(10,2) NULL,
    Transaction    int         NULL,     -- 成交筆數
    CreateTime     datetime2   NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_DailyStockPrice PRIMARY KEY (TradeDate, StockId)
);
```

- 價格與成交量欄位需處理 CSV 的千分位與空值。
- 若未來需要市場別／產業別，可新增維度表。

---

## 7. 系統架構與技術棧概觀

- 後端：ASP.NET Core 9 Web API
    - 負責：
        - TWSE API 抓取與解析
        - MSSQL 存取
        - Trade/DCA/Portfolio 計算服務
        - REST JSON API
- 前端：HTML5 + Bootstrap
    - 頁面：
        - `/calendar`：月曆 + 當日行情
        - `/stock/{stockId}`：個股歷史與技術指標、損益資訊
        - `/etf/{etfId}`：ETF 歷史與技術指標
        - `/admin/history-backfill`：歷史回補工具
        - `/dashboard`：儀表板（持股比例與單檔買入比例）
- 資料庫：SQL Server

---

## 8. Web API：每日行情相關

### 8.1 可用日期清單（供日曆使用）

`GET /api/calendar/available-dates`

- 從 `DailyStockPrice` 查詢所有有資料的 `TradeDate`，排序後回傳陣列（字串 `YYYY-MM-DD`）。


### 8.2 取得指定日期全市場行情

`GET /api/daily-prices/by-date?date=YYYY-MM-DD`

流程：

1. 解析 `date`。
2. 查 DB：是否有 `TradeDate = date` 的紀錄。
3. 若有：
    - 直接從 DB 讀出該日全部 `DailyStockPrice`。
4. 若沒有：
    - 若 `date` 為今天：
        - 呼叫 `STOCK_DAY_ALL?response=open_data` 取得 CSV。[web:70][web:86][web:88]
        - 解析並寫入 DB。
        - 再從 DB 查出並回傳。
    - 若 `date` 為過去日期：
        - 不在此 API 即時抓歷史資料，直接回傳「無資料」。

回傳：

```json
{
  "date": "2026-03-06",
  "totalCount": 2000,
  "items": [
    {
      "stockId": "2330",
      "stockName": "台積電",
      "tradeVolume": 123456789,
      "tradeValue": 123456789000,
      "openPrice": 700.0,
      "highPrice": 710.0,
      "lowPrice": 695.0,
      "closePrice": 705.0,
      "priceChange": 5.0,
      "transaction": 12345
    }
  ]
}
```


### 8.3 取得個股歷史價量

`GET /api/daily-prices/{stockId}/history?from=YYYY-MM-DD&to=YYYY-MM-DD`

- 從 `DailyStockPrice` 查詢該 `stockId` 在 `[from, to]` 的資料，按日期排序回傳。

---

## 9. 前端：日曆查詢畫面（HTML5 + Bootstrap）

### 9.1 `/calendar` 頁面需求

- 載入時：
    - 呼叫 `GET /api/calendar/available-dates`，在月曆上標記有資料的日期。
- 使用者點選某一天：
    - 呼叫 `GET /api/daily-prices/by-date?date=YYYY-MM-DD`。
    - 若成功，顯示當日行情列表（Bootstrap Table）。
    - 若無資料，顯示提示。
- 行情列表可支援：
    - 搜尋（代號/名稱）
    - 排序（漲跌、成交量等）
- 可在表格中點選股票代號，導向 `/stock/{stockId}`。

---

## 10. 管理者用「歷史回補工具」（History Backfill Tool）

### 10.1 目的

- 讓管理者指定日期區間（例如 2020-01-01 ~ 2020-12-31），系統會：

1. 檢查該區間內哪些日期的 `DailyStockPrice` 尚未有資料。
2. 對缺資料日期執行補抓。
- 透過 Web 頁面按鈕觸發，不使用排程。


### 10.2 API：發起歷史回補

`POST /api/history/backfill`

Request Body：

```json
{
  "from": "YYYY-MM-DD",
  "to": "YYYY-MM-DD",
  "dryRun": false
}
```

行為：

1. 驗證 `from <= to`。
2. 產生區間內日期列表。
3. 找出 DB 中尚無資料的日期。
4. 若 `dryRun=true`：只回傳缺資料日期，不抓。
5. 若 `dryRun=false`：
    - 逐日執行「單日補抓流程」（請實作為共用服務），從歷史資料來源抓該日個股日成交資訊，寫入 DB。
6. 回傳每個日期的執行結果。

回應範例：

```json
{
  "from": "2020-01-01",
  "to": "2020-01-10",
  "dryRun": false,
  "results": [
    { "date": "2020-01-01", "status": "SKIPPED_ALREADY_EXISTS", "message": "DB already has data." },
    { "date": "2020-01-02", "status": "SUCCESS", "message": "Imported 2100 rows." },
    { "date": "2020-01-03", "status": "FAILED", "message": "No data available or network error." }
  ]
}
```

> 歷史資料來源請選用 data.gov.tw 或 TWSE 歷史下載頁，並在程式中實作實際抓取 URL 與格式轉換。[web:83][web:99][web:100][web:101][web:102]

### 10.3 前端：`/admin/history-backfill`

- 表單欄位：`from`、`to` 日期、`dryRun` 勾選框。
- 按鈕：
    - 「Dry Run」：dryRun = true。
    - 「開始回補」：dryRun = false。
- 下方表格顯示每個日期的結果。
- 僅管理者可訪問（權限機制可簡化）。

---

## 11. ETF 擴充（台股 ETF 行情查詢）

### 11.1 目標

- 在 DailyStockPrice 的基礎上，加入 ETF 維度表與 ETF 專用 API。
- ETF 與股票共享 DailyStockPrice，但透過 `EtfInfo` 來標示哪些代號為 ETF。


### 11.2 資料庫：EtfInfo

資料表名稱：`EtfInfo`

欄位：

- `EtfId` (varchar(10), PK)：ETF 代號（如 0050）。
- `EtfName` (nvarchar(100))：ETF 名稱。
- `Category` (nvarchar(50))：如「台股大型市值」、「高股息」等。
- `Issuer` (nvarchar(100), nullable)：發行投信。
- `IsActive` (bit)
- `CreateTime` (datetime2)


### 11.3 ETF API

- `GET /api/etf/list`
    - 回傳所有 `IsActive = 1` 的 ETF 基本資訊。
- `GET /api/etf/daily?date=YYYY-MM-DD`
    - 基於 `DailyStockPrice` + `EtfInfo`，回傳該日所有 ETF 行情。
- `GET /api/etf/{etfId}/history?from=YYYY-MM-DD&to=YYYY-MM-DD`
    - 回傳指定 ETF 的歷史行情。


### 11.4 前端 ETF 視圖

- `/etf`：ETF 清單 + 當日行情簡表。
- `/etf/{etfId}`：ETF 歷史走勢、技術指標與損益資訊（重用個股頁邏輯）。

---

## 12. 技術指標計算（Technical Indicators）

### 12.1 目標

- 為股票與 ETF 提供常用技術指標：
    - 移動平均線（MA）：例如 5、20、60 日。
    - RSI（Relative Strength Index）：例如 14 日。
- 指標於 API 查詢時動態計算，不必寫回 DB。


### 12.2 指標服務介面

設計 `ITechnicalIndicatorService`，提供：

- `CalculateMovingAverage(IEnumerable<DailyPricePoint> prices, int window)`
- `CalculateRsi(IEnumerable<DailyPricePoint> prices, int period = 14)`

`DailyPricePoint`：

- `Date` (date)
- `ClosePrice` (decimal)


### 12.3 技術指標 API

`GET /api/indicators/{stockId}?from=YYYY-MM-DD&to=YYYY-MM-DD&ma=5,20,60&rsiperiod=14`

行為：

1. 從 `DailyStockPrice` 抓取 `[from, to]` 的收盤價。
2. 計算指定的 MA 與 RSI。
3. 回傳每個日期的收盤價與指標值。

回傳範例（簡化）：

```json
{
  "stockId": "2330",
  "from": "2025-01-01",
  "to": "2025-01-31",
  "maPeriods":,[^1][^2][^3]
  "rsiPeriod": 14,
  "items": [
    {
      "date": "2025-01-09",
      "closePrice": 710.0,
      "ma": { "5": 705.2, "20": null, "60": null },
      "rsi": 62.3
    }
  ]
}
```


---

## 13. 儀表板功能（Dashboard）

### 13.1 目標

- 提供儀表板頁 `/dashboard`，包含兩個主要視圖：

1. 所有持股市值與組合比例。
2. 單一持股的買入金額組成與比例。
- 資料來源：
    - `TradeRecord`、`DcaExecution`、`DailyStockPrice`。


### 13.2 儀表板一：所有持股數量與百分比

#### 13.2.1 計算邏輯

1. 針對每檔 `StockId` 計算目前持股股數：
    - 所有 BUY + DCA 買入 − SELL。
2. 取得最新收盤價。
3. 市值：`MarketValue = CurrentQty × ClosePrice`。
4. 總市值：`TotalValue = Σ MarketValue`。
5. 比例：`WeightRatio = MarketValue ÷ TotalValue`。

#### 13.2.2 API

`GET /api/dashboard/holdings`

回傳範例：

```json
{
  "totalValue": 2000000.0,
  "items": [
    {
      "stockId": "2330",
      "stockName": "台積電",
      "quantity": 500,
      "lastClosePrice": 700.0,
      "marketValue": 350000.0,
      "weightRatio": 0.175
    }
  ]
}
```


#### 13.2.3 前端呈現

- `/dashboard`：
    - 上方：總市值、總成本、整體未實現損益。
    - 中間：持股比例餅圖或 donut chart。
    - 下方：表格列出各檔標的的股數、市值、比例。


### 13.3 儀表板二：單一持股的買入金額與百分比

#### 13.3.1 計算邏輯

以某 `stockId`：

1. 找出所有該標的的買入紀錄：
    - `TradeRecord.Action = BUY`。
    - `DcaExecution.Status = SUCCESS`。
2. 計算每一筆買入金額：
    - `BuyAmount = Price * Quantity + Fee + Tax + OtherCost` 或使用 `|NetAmount|`。
3. 總投入：`TotalInvested = Σ BuyAmount`。
4. 每筆比例：`WeightRatio = BuyAmount ÷ TotalInvested`。

#### 13.3.2 API

`GET /api/dashboard/holding/{stockId}/buy-distribution`

回傳範例：

```json
{
  "stockId": "2330",
  "stockName": "台積電",
  "totalInvested": 300000.0,
  "items": [
    {
      "source": "TRADE",
      "refId": 123,
      "tradeDate": "2025-01-05",
      "quantity": 100,
      "price": 600.0,
      "amount": 60000.0,
      "weightRatio": 0.2
    }
  ]
}
```


#### 13.3.3 前端呈現

- 在 `/stock/{stockId}` 頁面中增加一塊：
    - 買入組成餅圖。
    - 下方表格列出所有買入紀錄與金額比例。

---

## 14. 查詢股票平均成本與未實現損益

### 14.1 目標

- 提供 API 與 UI 顯示：
    - 單檔股票/ETF 的平均成本。
    - 目前未實現損益與報酬率。
- 以及整體組合的平均成本與未實現損益摘要。


### 14.2 計算公式（單檔）

以 `stockId`：

1. `CurrentQty`：目前持股股數。
2. `TotalCost`：
    - 簡化版：`Σ(所有買入的 (Price * Quantity + Fee + Tax + OtherCost))`。
3. 平均成本：
    - `AvgCost = TotalCost ÷ CurrentQty`。
4. 市值：
    - `MarketValue = CurrentQty × 最新收盤價`。
5. 未實現損益：
    - `UnrealizedPnL = MarketValue − TotalCost`。
6. 未實現報酬率：
    - `UnrealizedReturn = UnrealizedPnL ÷ TotalCost`。

### 14.3 API：單檔未實現損益

`GET /api/portfolio/stock/{stockId}/unrealized`

回傳範例：

```json
{
  "stockId": "2330",
  "stockName": "台積電",
  "currentQty": 500,
  "avgCost": 600.0,
  "lastClosePrice": 700.0,
  "marketValue": 350000.0,
  "totalCost": 300000.0,
  "unrealizedPnL": 50000.0,
  "unrealizedReturn": 0.1667
}
```


### 14.4 API：整體組合未實現損益摘要（可選）

`GET /api/portfolio/unrealized-summary`

- 回傳：
    - `TotalCost`、`TotalMarketValue`、`TotalUnrealizedPnL`、`TotalUnrealizedReturn`。


### 14.5 前端呈現

- 在 `/stock/{stockId}` / `/etf/{etfId}`：
    - 顯示平均成本、現價、未實現損益與報酬率。
- 在 `/dashboard`：
    - 顯示整體組合總成本、總市值、總未實現損益與報酬率。

---

## 15. 實作指引（給開發 AI / 開發者）

1. 使用 ASP.NET Core 9 建立 Web API 專案，採用分層架構（Controllers / Services / Repositories / Domain / Infrastructure）。
2. 使用 EF Core 或 Dapper 連接 SQL Server，建立上述所有資料表與關聯。
3. 將 TWSE 抓取邏輯封裝成獨立 Service，處理：
    - CSV/JSON 解析
    - 數字格式清洗（千分位、缺值）
    - 例外與 retry。
4. 所有關於損益、平均成本、技術指標的計算，都應實作在 Service 層，Controller 僅負責接收參數與回傳結果。
5. 前端頁面使用 HTML + Bootstrap + JS，透過 AJAX 呼叫上述 API，逐步完成：
    - 日曆查行情頁 `/calendar`
    - 個股/ETF 頁 `/stock/{stockId}`、`/etf/{etfId}`
    - 歷史回補管理頁 `/admin/history-backfill`
    - 儀表板 `/dashboard`
    - 交易紀錄與定期定額管理頁（路由可由實作者設計）。
```
可以加，而且這兩個功能都很適合做成「決策輔助」模組。我建議的功能名稱與放進 skill.md 的寫法如下，你可以複製貼到現有 skill.md 後面（章號自己調整即可）。

***

## 15. 智慧選股建議（長短期投資分類）

### 15.1 目標

- 根據目前市場資料與技術指標，協助鴻仁從「全市場」與「自選股」中，挑出：  
  - 適合 **長期投資** 的標的（偏基本面與中長期趨勢）。  
  - 適合 **短期／波段操作** 的標的（偏技術面與價量型態）。  
- 本功能提供「輔助建議」，不保證報酬，實際決策由使用者自行判斷。  

### 15.2 資料與指標基礎

- 長期投資（示意邏輯，可由 Claude 實作細節）：  
  - 優先篩選 ETF、大型權值股或基本面較穩定的公司（未來如接公司財報 API 可再強化）。 
  - 價格面：股價位於長期均線（如季線/年線）附近或上方，中長期趨勢偏多頭。 

- 短期／波段操作（示意邏輯）：  
  - 以技術面為主：  
    - 價格相對短期均線（5 日、10 日）的位置與突破。  
    - 價量變化（成交量放大）。  
    - RSI 等動能指標。 

> 本 Skill 著重在「系統如何分類與輸出建議格式」，具體選股策略可由實作時與開發 AI 進一步調整。  

### 15.3 API：長短期投資建議清單

`GET /api/recommendations/stocks?scope=all|holding|watchlist`

參數：  

- `scope`：  
  - `all`：從全市場股票＋ETF 中給建議。  
  - `holding`：只針對目前持股給建議。  
  - `watchlist`：未來如有自選股清單，可用此選項。  

行為（示意）：  

1. 從 `DailyStockPrice` 與技術指標服務取得必要的 MA、RSI 等資料（可重用第 12 章）。  
2. 依規則將標的分為：  
   - `LONG_TERM_CANDIDATE`（長期投資候選）  
   - `SHORT_TERM_CANDIDATE`（短期/波段候選）  
3. 排序：  
   - 長期：可依成交量、市值、趨勢穩定度排序。  
   - 短期：可依短期動能強度排序（例如漲幅、成交量放大倍數）。  

回傳範例：  

```json
{
  "generatedAt": "2026-03-06T15:30:00+08:00",
  "scope": "holding",
  "longTermCandidates": [
    {
      "stockId": "0050",
      "stockName": "元大台灣50",
      "reason": "ETF、大型市值、股價在季線上方且趨勢偏多",
      "tags": ["ETF", "trend_up", "quarterly_MA_support"]
    }
  ],
  "shortTermCandidates": [
    {
      "stockId": "2610",
      "stockName": "華航",
      "reason": "短期均線突破、成交量放大、技術面偏強",
      "tags": ["short_term", "volume_surge", "ma_breakout"]
    }
  ]
}
```

### 15.4 前端頁：投資建議總覽

- 路由建議：`/ideas` 或 `/recommendations`。  
- 分兩個區塊：  
  - 「長期投資候選」列表。  
  - 「短期／波段候選」列表。  
- 每檔標的顯示：  
  - 代號、名稱、現價、近一段報酬、簡短理由。  
- 點選可跳轉至 `/stock/{stockId}` 或 `/etf/{etfId}` 詳細頁。  

***

## 16. 季線風險警示（跌破季線監控）

### 16.1 目標

- 即時（在你打開系統或手動刷新時）檢查「目前持股」中：  
  - 哪些股票／ETF 已跌破季線（60 日均線），可能代表中期趨勢轉弱。  
- 讓鴻仁可以在儀表板上快速看到這些「風險標的」，決定是否調整持股。  

> 季線通常代表約 60 個交易日（約一季）的移動平均線，是常見判斷中長期趨勢的指標。 [rich01](https://rich01.com/what-is-moving-average-line/)

### 16.2 計算邏輯

對每檔「目前持有的標的」：  

1. 由 `DailyStockPrice` 取得最近至少 60 個交易日的收盤價。  
2. 計算 60 日移動平均（MA60，季線）。 [optionsprincipal](https://optionsprincipal.cc/%E8%82%A1%E7%A5%A860%E6%97%A5%E6%98%AF%E4%BB%80%E9%BA%BC%E7%B7%9A%EF%BC%9F)
3. 取得最新收盤價 `ClosePrice`。  
4. 判斷：  
   - 若 `ClosePrice < MA60`，視為「跌破季線」。  
   - 可額外記錄跌破幅度：`(ClosePrice - MA60) / MA60`。  
5. 選擇是否只顯示「最近 N 日才剛跌破」的標的，或全部跌破者。  

### 16.3 API：持股跌破季線清單

`GET /api/alerts/below-quarterly-ma?days=60`

參數（可選）：  

- `days`：用來計算季線的天數（預設 60）。  

行為：  

- 對所有當前持股（`CurrentQty > 0`）計算季線與現價，比較是否跌破。  

回傳範例：  

```json
{
  "asOf": "2026-03-06",
  "maDays": 60,
  "items": [
    {
      "stockId": "2330",
      "stockName": "台積電",
      "currentQty": 500,
      "lastClosePrice": 550.0,
      "ma": 580.0,
      "belowMa": true,
      "diff": -30.0,
      "diffPercent": -0.0517
    }
  ]
}
```

### 16.4 前端呈現

- 建議整合在 `/dashboard` 儀表板中，加入一個「風險警示」區塊：  
  - 列出所有「已跌破季線」的持股，按跌破幅度排序。  
  - 讓你一打開儀表板就可以看到這些標的。  
- 也可以在 `/stock/{stockId}` 頁面顯示：  
  - 現價 vs MA60 的狀態（例如：顯示一行文字或顏色標示「在季線下方」）。  

***




