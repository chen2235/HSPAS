
```markdown
# 鴻仁生活紀錄系統 Hung-Jen Stock Profit Analysis System skill.md

## 1. 系統基本資訊

- 系統名稱：鴻仁生活紀錄系統  
- 英文名稱建議：Hung-Jen Stock Profit Analysis System（簡稱：HSPAS 或 SPAS）    
- 系統簡要說明：  
  - 「鴻仁生活紀錄系統」用來整合鴻仁本人的投資紀錄、健康紀錄與生活記帳等資訊，提供長期追蹤與分析。  
  - 第一階段沿用原「鴻仁股票損益系統」的全部功能，集中到「股票損益紀錄」主功能底下。  
  - 未來將陸續新增「健康管理紀錄」、「生活計帳」等生活相關模組。  

- 技術棧：  
  - 後端：ASP.NET Core 9 Web API（.NET 9 LTS）  
  - 前端：HTML5 + Bootstrap + JavaScript  
  - 資料庫：SQL Server（MSSQL）  

## 資料庫

**連線資訊 —**

```text
Server=localhost
Database=HSPAS
User ID=hspasmgr
Password=tvhspasmgr
TrustServerCertificate=True
```

> 註：資料庫名稱目前仍沿用 HSPAS，如未來改名可再調整。

## Web 服務

**連線資訊 —**

```text
URL=http://localhost:5117
Swagger=http://localhost:5117/openapi/v1.json
啟動腳本=start.ps1
停止腳本=stop.ps1
```


---

## 2. 系統目標與範圍

- 目標：
    - 紀錄鴻仁所有買賣股票、ETF 的交易明細（現股／定期定額），並提供損益分析與風險警示。
    - 紀錄個人健康檢查報告（含每季與公司年度健檢），提供趨勢儀表板。
    - 紀錄生活計帳（例如「妹妹紀錄」）並提供年度分析。
- 範圍：
    - 投資模組：台股股票與 ETF 為主要標的（暫不涵蓋期權、海外市場；未來如需再擴充）。
    - 健康模組：健檢報告紀錄與儀表板。
    - 生活模組：生活計帳與分析（首波為「妹妹紀錄」）。

---

## 3. 前端主框架與功能導覽

### 3.1 主頁面布局結構

系統所有主要頁面（例如 `/dashboard`、`/calendar`、`/stock/{stockId}`、`/health/...`、`/life/...` 等）統一使用三區塊布局：

1. 最上方橫幅（Header / Top Bar）
    - 位置：畫面最上方，水平橫幅。
    - 內容：
        - 左側顯示系統名稱文字：**「鴻仁生活紀錄系統」**。
        - 右側預留空間，可放使用者名稱、設定／登出按鈕（之後實作）。
2. 左側功能列（Sidebar / Navigation）
    - 位置：畫面左邊垂直側邊欄。
    - 內容：由後端 API 傳回的三層式功能選單（見 3.2）。
    - 行為：
        - 點選功能列的 Level 3 項目時，右側主內容區載入對應頁面。
        - 選中項目需有高亮顯示。
3. 右側主內容區（Main Content）
    - 位置：左側功能列右方，占據大部分寬度。
    - 內容：顯示目前選取功能的畫面（儀表板、日曆、交易紀錄、報表、健檢儀表板、生活計帳分析等）。

實作建議：

- 使用 Bootstrap Grid 或 CSS Flexbox 建立：上方固定 Header，底下左右分割 Sidebar + Main Content。

---

### 3.2 左側功能列 – 三層式選單（DB 驅動）

本系統的 Sidebar 使用「三層式選單 + 資料庫維護」模式：

- Level 1：主功能（例如：股票損益紀錄、健康管理紀錄、生活計帳）。
- Level 2：次功能（主功能底下的模組群組，如「健檢報告紀錄」）。
- Level 3：次子功能（實際有對應頁面的具體功能，例如「每三個月報告紀錄上傳」）。

前端 Sidebar 不再硬寫選單列表，而是：

- 由 `/api/menu/tree` 取得完整樹狀選單資料。
- 在畫面上以可展開/收合的樹狀結構呈現。
- 一般僅 Level 3 具實際路由（可依需求放寬）。


#### 3.2.1 功能選單資料表 – `MenuFunction`

為了支援三層式選單與拖拉調整階層與排序，新增資料表 `MenuFunction`：

```sql
CREATE TABLE [dbo].[MenuFunction] (
    [Id]          BIGINT        IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [ParentId]    BIGINT        NULL,
    [Level]       INT           NOT NULL,           -- 1 / 2 / 3
    [FuncCode]    VARCHAR(50)   NOT NULL,           -- 功能代碼，系統內唯一
    [DisplayName] NVARCHAR(100) NOT NULL,           -- 顯示名稱
    [RouteUrl]    NVARCHAR(200) NULL,               -- 一般僅 Level 3 會有值
    [SortOrder]   INT           NOT NULL,           -- 同一 ParentId 下的排序
    [IsActive]    BIT           NOT NULL DEFAULT(1),
    [Remark]      NVARCHAR(200) NULL,
    [CreateTime]  DATETIME2     NOT NULL DEFAULT(SYSDATETIME())
);
GO

-- ParentId 外鍵（可選，不強制）
-- ALTER TABLE [dbo].[MenuFunction]
-- ADD CONSTRAINT FK_MenuFunction_Parent
-- FOREIGN KEY (ParentId) REFERENCES [dbo].[MenuFunction](Id);
```

設計重點：使用 `Level` + `ParentId` + `SortOrder` 來管理階層與顯示順序，方便後續拖拉調整。

#### 3.2.2 `MenuFunction` 初始資料 – 股票損益紀錄（既有功能）

說明：

- 既有所有股票相關功能統一歸到 Level 1 「股票損益紀錄」底下。
- 先用一個 Level 2「股票損益分析」包住所有 Level 3 功能（之後如要更細分，可再新增 Level 2 群組）。

```sql
-- Level 1：主功能
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES (NULL, 1, 'STOCK_ROOT', N'股票損益紀錄', NULL, 1, 1, N'原 HSPAS 全部功能集中於此');

DECLARE @STOCK_ROOT_ID BIGINT = SCOPE_IDENTITY();

-- Level 2：股票損益分析（群組）
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES (@STOCK_ROOT_ID, 2, 'STOCK_ANALYSIS', N'股票損益分析', NULL, 1, 1, N'底下放原有股票功能');

DECLARE @STOCK_ANALYSIS_ID BIGINT = SCOPE_IDENTITY();

-- Level 3：各功能頁（對應原 Sidebar）
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES
(@STOCK_ANALYSIS_ID, 3, 'STOCK_DASH',   N'股票儀表板 Dashboard',   N'/dashboard',                     1, 1, N'原 DASH'),
(@STOCK_ANALYSIS_ID, 3, 'STOCK_CAL',    N'日曆行情查詢',            N'/calendar',                      2, 1, N'原 CAL'),
(@STOCK_ANALYSIS_ID, 3, 'STOCK_STOCK',  N'個股/ETF 查詢入口',       N'/stock',                         3, 1, N'原 STOCK'),
(@STOCK_ANALYSIS_ID, 3, 'STOCK_ETF',    N'ETF 專區',                N'/etf',                           4, 1, N'原 ETF'),
(@STOCK_ANALYSIS_ID, 3, 'STOCK_TRD',    N'交易紀錄管理',            N'/trades',                        5, 1, N'原 TRD'),
(@STOCK_ANALYSIS_ID, 3, 'STOCK_DCA',    N'定期定額管理',            N'/dca',                           6, 1, N'原 DCA'),
(@STOCK_ANALYSIS_ID, 3, 'STOCK_PNL',    N'損益與成本查詢',          N'/pnl',                           7, 1, N'原 PNL'),
(@STOCK_ANALYSIS_ID, 3, 'STOCK_IDEAS',  N'投資建議（長/短期）',     N'/recommendations',              8, 1, N'原 IDEAS'),
(@STOCK_ANALYSIS_ID, 3, 'STOCK_ALERT',  N'風險警示（季線跌破）',    N'/alerts',                        9, 1, N'原 ALERT，可視需要整合在 /dashboard'),
(@STOCK_ANALYSIS_ID, 3, 'STOCK_HIST',   N'歷史資料回補',            N'/admin/history-backfill',       10, 1, N'原 HIST'),
(@STOCK_ANALYSIS_ID, 3, 'STOCK_CONF',   N'系統設定（預留）',        N'/settings',                     11, 1, N'原 CONF');
GO
```


#### 3.2.3 `MenuFunction` 初始資料 – 健康管理紀錄（未來功能）

```sql
-- Level 1：健康管理紀錄
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES (NULL, 1, 'HEALTH_ROOT', N'健康管理紀錄', NULL, 2, 1, N'健康相關主功能');

DECLARE @HEALTH_ROOT_ID BIGINT = SCOPE_IDENTITY();

-- Level 2：健檢報告紀錄
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES (@HEALTH_ROOT_ID, 2, 'HEALTH_CHECKUP', N'健檢報告紀錄', NULL, 1, 1, N'個人與公司健檢報告');

DECLARE @HEALTH_CHECKUP_ID BIGINT = SCOPE_IDENTITY();

-- Level 3：健檢報告各功能
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES
(@HEALTH_CHECKUP_ID, 3, 'HEALTH_CHECKUP_QTR_UP',   N'每三個月報告紀錄上傳',    N'/health/checkup/qtr/upload',        1, 1, N'個人每季健檢報告上傳'),
(@HEALTH_CHECKUP_ID, 3, 'HEALTH_CHECKUP_QTR_DASH', N'每三個月報告儀表板',      N'/health/checkup/qtr/dashboard',     2, 1, N'個人每季健檢儀表板'),
(@HEALTH_CHECKUP_ID, 3, 'HEALTH_CHECKUP_CO_UP',    N'公司每年報告紀錄上傳',    N'/health/checkup/company/upload',    3, 1, N'公司年度健檢報告上傳'),
(@HEALTH_CHECKUP_ID, 3, 'HEALTH_CHECKUP_CO_DASH',  N'公司每年報告儀表板',      N'/health/checkup/company/dashboard', 4, 1, N'公司年度健檢儀表板');
GO
```


#### 3.2.4 `MenuFunction` 初始資料 – 生活計帳（未來功能）

```sql
-- Level 1：生活計帳
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES (NULL, 1, 'LIFE_ROOT', N'生活計帳', NULL, 3, 1, N'生活收支與紀錄');

DECLARE @LIFE_ROOT_ID BIGINT = SCOPE_IDENTITY();

-- Level 2：妹妹紀錄
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES (@LIFE_ROOT_ID, 2, 'LIFE_SIS', N'妹妹紀錄', NULL, 1, 1, N'妹妹相關收支與事件紀錄');

DECLARE @LIFE_SIS_ID BIGINT = SCOPE_IDENTITY();

-- Level 3：妹妹紀錄各功能
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES
(@LIFE_SIS_ID, 3, 'LIFE_SIS_RECORD',          N'妹妹紀錄維護',       N'/life/sister/records',          1, 1, N'基本紀錄維護畫面'),
(@LIFE_SIS_ID, 3, 'LIFE_SIS_YEARLY_ANALYSIS', N'妹妹紀錄年度分析',   N'/life/sister/yearly-analysis',  2, 1, N'年度統計與圖表');
GO
```


---

### 3.3 系統設定 – 功能選單排序與階層調整

此功能提供「系統功能選單排序與階層調整」頁面，讓使用者可以透過拖拉（drag \& drop）方式調整：

- 同層級功能的顯示順序（更新 `SortOrder`）。
- 把次功能移到另一個主功能底下（更新 `ParentId`、`Level`）。
- 把次子功能移到另一個主功能或其它次功能底下（同樣更新 `ParentId`、`Level`）。


#### 3.3.1 前端頁面（概念）

- 路由建議：
    - `/settings/menu-sorting`（可作為 `/settings` 底下的一個子頁）。
- 畫面需求：
    - 顯示整棵功能樹（Level 1 ~ 3），可展開/收合。
    - 支援拖拉節點重新排序與改變父階層。
    - 提供「儲存」按鈕，一次把所有變更送到後端。


#### 3.3.2 `/api/menu/tree` 輸出 JSON 結構

- 取得目前功能選單樹：

```http
GET /api/menu/tree
```

- Response（範例結構）：

```json
[
  {
    "id": 1,
    "parentId": null,
    "level": 1,
    "funcCode": "STOCK_ROOT",
    "displayName": "股票損益紀錄",
    "routeUrl": null,
    "sortOrder": 1,
    "isActive": true,
    "children": [
      {
        "id": 2,
        "parentId": 1,
        "level": 2,
        "funcCode": "STOCK_ANALYSIS",
        "displayName": "股票損益分析",
        "routeUrl": null,
        "sortOrder": 1,
        "isActive": true,
        "children": [
          {
            "id": 3,
            "parentId": 2,
            "level": 3,
            "funcCode": "STOCK_DASH",
            "displayName": "股票儀表板 Dashboard",
            "routeUrl": "/dashboard",
            "sortOrder": 1,
            "isActive": true,
            "children": []
          }
          // ... 其他 Level 3 功能
        ]
      }
    ]
  },
  {
    "id": 10,
    "parentId": null,
    "level": 1,
    "funcCode": "HEALTH_ROOT",
    "displayName": "健康管理紀錄",
    "routeUrl": null,
    "sortOrder": 2,
    "isActive": true,
    "children": [
      {
        "id": 11,
        "parentId": 10,
        "level": 2,
        "funcCode": "HEALTH_CHECKUP",
        "displayName": "健檢報告紀錄",
        "routeUrl": null,
        "sortOrder": 1,
        "isActive": true,
        "children": [
          {
            "id": 12,
            "parentId": 11,
            "level": 3,
            "funcCode": "HEALTH_CHECKUP_QTR_UP",
            "displayName": "每三個月報告紀錄上傳",
            "routeUrl": "/health/checkup/qtr/upload",
            "sortOrder": 1,
            "isActive": true,
            "children": []
          }
          // ... 其他健檢 Level 3 功能
        ]
      }
    ]
  }
  // ... LIFE_ROOT 相關
]
```

> `children` 為遞迴樹狀結構，Level 3 的 children 一律為空陣列。

#### 3.3.3 `/api/menu/reorder` 儲存排序與階層調整

- 用途：接收前端拖拉後的結果，一次更新所有受影響節點的 `ParentId`、`Level`、`SortOrder`。

```http
POST /api/menu/reorder
Content-Type: application/json
```

- Request Body 範例：

```json
[
  { "id": 1,  "parentId": null, "level": 1, "sortOrder": 1 },
  { "id": 2,  "parentId": 1,    "level": 2, "sortOrder": 1 },
  { "id": 3,  "parentId": 2,    "level": 3, "sortOrder": 1 },
  { "id": 12, "parentId": 1,    "level": 2, "sortOrder": 2 }
]
```

後端需做基本驗證，避免不合法階層（例如 Level 1 不應有 ParentId）。

---

## 4. 個人買賣股票紀錄功能（一般交易）

> 本章節為股票損益紀錄主功能（STOCK_ROOT）下的一部分。

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
- 未實現損益與報酬率：見第 15 章。


### 4.4 交易紀錄 API

- `POST /api/trades`
- `GET /api/trades/{stockId}?from=YYYY-MM-DD&to=YYYY-MM-DD`
- `GET /api/portfolio/summary`（可選）

---

### 4.5 交易紀錄管理：上傳國泰證日對帳單並自動帶入交易明細

（內容同你原 skill.md，已經整理過，這裡不重複展開；可以沿用原來版本。）

---

## 5. 定期定額紀錄功能（DCA）

（沿用原 skill.md：`DcaPlan`、`DcaExecution`、相關 API。）

---

## 6. 每日行情資料抓取（TWSE 上市 + TPEx 上櫃）

### 6.1 上市（TSE）盤後資料

- 資料來源：證交所 TWSE `STOCK_DAY_ALL` CSV
- 服務介面：`ITwseDataService`
- 實作：CSV 千分位清洗、空值處理、retry 機制

### 6.2 上櫃（OTC）盤後資料

- 資料來源：櫃買中心 TPEx `DAILY_CLOSE_quotes` CSV
- 實作：`ParseTpexCsv` 解析（民國日期 YYYMMDD → DateTime、18 欄位對應）
- 民國日期轉換：`ParseRocDate`

---

## 7. 每日行情資料表：DailyStockPrice

- Table: `DailyStockPrice`（PK: TradeDate + StockId）
- 主要欄位：TradeDate, StockId, StockName, TradeVolume, TradeValue(`decimal(19,4)`), OpenPrice, HighPrice, LowPrice, ClosePrice, PriceChange, Transactions
- `MarketType` nvarchar(5), NOT NULL, DEFAULT 'TSE' — 區分上市（TSE）／上櫃（OTC）

---

## 8. 系統架構與實作建議

- 後端：ASP.NET Core 9 Web API，採用分層架構（Controllers / Services / Repositories / Domain / Infrastructure）。
- 前端：HTML5 + Bootstrap + JavaScript，實作共用 Layout（Header + Sidebar + Main Content）。
- 資料庫：SQL Server，金額欄位一律使用 `decimal(19,4)`。

---

## 9. Web API – 行情與交易

（沿用原 skill.md 中的 calendar、daily-prices、history-backfill 等 API 定義。）

---

## 10. 日曆行情查詢（/calendar）

- 月曆元件標記有資料日期，點選日期顯示當日行情列表
- 行情列表含搜尋（代號/名稱）、排序（漲跌、成交量等）
- 新增「市場別」欄位（TSE 藍色 badge / OTC 黃色 badge），支援搜尋篩選
- 進入頁面自動載入當日行情，預設成交股數由大到小排序

---

## 11. 歷史資料回補（/admin/history-backfill）

### 11.1 單日回補模式

- 服務方法：`IDailyPriceService.BackfillOneDayAsync(date)`
  - 抓取 TSE + OTC 兩來源資料
  - 先刪該日舊資料（`ExecuteDeleteAsync`）→ 重新寫入（`AddRange + SaveChangesAsync`）
- 額外方法：`FetchTseDailyAsync(date)`、`FetchOtcDailyAsync(date)`

### 11.2 API

- `POST /api/history/backfill`
  - Request：`{ "date": "YYYY-MM-DD" }`（單一日期）
  - Response：`BackfillOneDayResult`（date, status, tseCount, otcCount, message）

### 11.3 前端 `/backfill`

- 單一日期選擇器 + 回補按鈕
- 結果卡片顯示 TSE / OTC / 合計數量

---

## 12. ETF 擴充

- Table: `EtfInfo`
- ETF 相關 API：`/api/etf/list`、`/api/etf/daily`、`/api/etf/{etfId}/history`
- 前端：`/etf`、`/etf/{etfId}`

---

## 13. 技術指標計算（MA、RSI）

- 服務介面：`ITechnicalIndicatorService`
    - `CalculateMovingAverage(...)`
    - `CalculateRsi(...)`
- API：`GET /api/indicators/{stockId}?from=YYYY-MM-DD&to=YYYY-MM-DD&ma=5,20,60&rsiperiod=14`

---

## 14. 儀表板功能（Dashboard）

- API：`GET /api/dashboard/holdings`（持股市值與比例）
- API：`GET /api/dashboard/holding/{stockId}/buy-distribution`（單一持股買入金額與比例）
- 前端 `/dashboard` 顯示總體摘要、持股比例圖、買入組成與風險警示。

---

## 15. 平均成本與未實現損益查詢

- 單檔 API：`GET /api/portfolio/stock/{stockId}/unrealized`
- 組合摘要 API：`GET /api/portfolio/unrealized-summary`

---

## 16. 智慧選股建議（長短期投資分類）

- API：`GET /api/recommendations/stocks?scope=all|holding|watchlist`
- 前端：`/recommendations` 或 `/ideas`。

---

## 17. 季線風險警示（跌破季線監控）

- API：`GET /api/alerts/below-quarterly-ma?days=60`
- 前端：整合在 `/dashboard` 的「風險警示」區塊或 `/alerts` 頁。

---

## 18. 實作指引（給開發者／開發 AI）

1. 使用 ASP.NET Core 9 建立 Web API，採分層架構。
2. 使用 EF Core 建立上述實體與 DbContext，金額欄位使用 `decimal(19,4)`。
3. 實作 TWSE + TPEx 資料抓取 Service（上市與上櫃雙來源）、技術指標 Service。
4. 前端實作共用 Layout（Header + Sidebar + Main Content），並依路由建立各頁面骨架。
5. 實作 `MenuFunction` 相關 API（/api/menu/tree、/api/menu/reorder）與設定頁 `/settings/menu-sorting`，實現三層選單與拖拉排序。
6. 歷史回補採「單日模式」：`BackfillOneDayAsync(date)` 同時抓取 TSE+OTC，先刪後插。
7. 逐步實作各模組（股票損益紀錄、健康管理紀錄、生活計帳），確保與本 skill.md 規格一致。
```