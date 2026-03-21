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

## 6. TWSE 每日行情資料抓取

（沿用原 skill.md。）

---

## 7. 每日行情資料表：DailyStockPrice

（沿用原 skill.md。）

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

（沿用原 skill.md。）

---

## 11. 歷史資料回補（/admin/history-backfill）

（沿用原 skill.md。）

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
3. 實作 TWSE 資料抓取 Service、技術指標 Service。
4. 前端實作共用 Layout（Header + Sidebar + Main Content），並依路由建立各頁面骨架。
5. 實作 `MenuFunction` 相關 API（/api/menu/tree、/api/menu/reorder）與設定頁 `/settings/menu-sorting`，實現三層選單與拖拉排序。
6. 逐步實作各模組（股票損益紀錄、健康管理紀錄、生活計帳），確保與本 skill.md 規格一致。
```

***

## 2. menu_skill.md（專門給 Claude 開發 menu API）

請另存成 `menu_skill.md`，當作「只看選單與排序」的小規格書。

```markdown
# 鴻仁生活紀錄系統 – 功能選單與排序管理 skill（menu_skill.md）

## 1. 目的

本文件專注描述「功能選單」相關規格，包含：

- `MenuFunction` 資料表結構與初始化資料。  
- 生成前端三層 Sidebar 的 API：`GET /api/menu/tree`。  
- 儲存拖拉排序與階層變更的 API：`POST /api/menu/reorder`。  
- 系統設定頁面 `/settings/menu-sorting` 的基本需求。  

目標：讓開發 AI（Claude）可以根據本文件，完整實作 menu 相關的後端與前端行為。

---

## 2. 系統概念與階層規則

### 2.1 三層式選單結構

- Level 1：主功能  
  - 例如：股票損益紀錄（STOCK_ROOT）、健康管理紀錄（HEALTH_ROOT）、生活計帳（LIFE_ROOT）。  
- Level 2：次功能  
  - 例如：股票損益分析（STOCK_ANALYSIS）、健檢報告紀錄（HEALTH_CHECKUP）、妹妹紀錄（LIFE_SIS）。  
- Level 3：次子功能（實際頁面）  
  - 例如：股票儀表板 Dashboard、每三個月報告紀錄上傳、妹妹紀錄年度分析等。  

規則：

- Level 1：`ParentId = NULL`。  
- Level 2：`ParentId` 指向某個 Level 1。  
- Level 3：`ParentId` 指向某個 Level 2。  
- 同一個 `ParentId` 底下的項目由 `SortOrder` 決定顯示順序。  

### 2.2 前端行為

- Sidebar 初始化時呼叫 `GET /api/menu/tree`，取得完整樹狀結構後渲染。  
- 一般只有 Level 3 會有 `RouteUrl`，點擊後導向該路由。  
- 可展開/收合 Level 1、Level 2 節點。  

---

## 3. 資料表設計：MenuFunction

### 3.1 Table 結構

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

-- ParentId 外鍵（可選）
-- ALTER TABLE [dbo].[MenuFunction]
-- ADD CONSTRAINT FK_MenuFunction_Parent
-- FOREIGN KEY (ParentId) REFERENCES [dbo].[MenuFunction](Id);
```

實作重點：

- `FuncCode` 作為系統內唯一代碼，程式可用它做條件判斷。
- 透過 `ParentId` + `Level` 表達樹狀階層。
- 使用 `SortOrder` 控制同層順序。


### 3.2 初始資料 – 股票損益紀錄（STOCK_*）

（建表後可直接執行）

```sql
-- Level 1：股票損益紀錄
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES (NULL, 1, 'STOCK_ROOT', N'股票損益紀錄', NULL, 1, 1, N'原 HSPAS 全部功能集中於此');

DECLARE @STOCK_ROOT_ID BIGINT = SCOPE_IDENTITY();

-- Level 2：股票損益分析
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES (@STOCK_ROOT_ID, 2, 'STOCK_ANALYSIS', N'股票損益分析', NULL, 1, 1, N'底下放原有股票功能');

DECLARE @STOCK_ANALYSIS_ID BIGINT = SCOPE_IDENTITY();

-- Level 3：各功能頁
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


### 3.3 初始資料 – 健康管理紀錄（HEALTH_*）

```sql
-- Level 1：健康管理紀錄
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES (NULL, 1, 'HEALTH_ROOT', N'健康管理紀錄', NULL, 2, 1, N'健康相關主功能');

DECLARE @HEALTH_ROOT_ID BIGINT = SCOPE_IDENTITY();

-- Level 2：健檢報告紀錄
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES (@HEALTH_ROOT_ID, 2, 'HEALTH_CHECKUP', N'健檢報告紀錄', NULL, 1, 1, N'個人與公司健檢報告');

DECLARE @HEALTH_CHECKUP_ID BIGINT = SCOPE_IDENTITY();

-- Level 3：健檢報告功能
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES
(@HEALTH_CHECKUP_ID, 3, 'HEALTH_CHECKUP_QTR_UP',   N'每三個月報告紀錄上傳',    N'/health/checkup/qtr/upload',        1, 1, N'個人每季健檢報告上傳'),
(@HEALTH_CHECKUP_ID, 3, 'HEALTH_CHECKUP_QTR_DASH', N'每三個月報告儀表板',      N'/health/checkup/qtr/dashboard',     2, 1, N'個人每季健檢儀表板'),
(@HEALTH_CHECKUP_ID, 3, 'HEALTH_CHECKUP_CO_UP',    N'公司每年報告紀錄上傳',    N'/health/checkup/company/upload',    3, 1, N'公司年度健檢報告上傳'),
(@HEALTH_CHECKUP_ID, 3, 'HEALTH_CHECKUP_CO_DASH',  N'公司每年報告儀表板',      N'/health/checkup/company/dashboard', 4, 1, N'公司年度健檢儀表板');
GO
```


### 3.4 初始資料 – 生活計帳（LIFE_*）

```sql
-- Level 1：生活計帳
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES (NULL, 1, 'LIFE_ROOT', N'生活計帳', NULL, 3, 1, N'生活收支與紀錄');

DECLARE @LIFE_ROOT_ID BIGINT = SCOPE_IDENTITY();

-- Level 2：妹妹紀錄
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES (@LIFE_ROOT_ID, 2, 'LIFE_SIS', N'妹妹紀錄', NULL, 1, 1, N'妹妹相關收支與事件紀錄');

DECLARE @LIFE_SIS_ID BIGINT = SCOPE_IDENTITY();

-- Level 3：妹妹紀錄功能
INSERT INTO [dbo].[MenuFunction] ([ParentId], [Level], [FuncCode], [DisplayName], [RouteUrl], [SortOrder], [IsActive], [Remark])
VALUES
(@LIFE_SIS_ID, 3, 'LIFE_SIS_RECORD',          N'妹妹紀錄維護',       N'/life/sister/records',          1, 1, N'基本紀錄維護畫面'),
(@LIFE_SIS_ID, 3, 'LIFE_SIS_YEARLY_ANALYSIS', N'妹妹紀錄年度分析',   N'/life/sister/yearly-analysis',  2, 1, N'年度統計與圖表');
GO
```


---

## 4. API：GET /api/menu/tree

### 4.1 目的

- 提供前端一支 API，取得完整的三層選單樹。
- Sidebar 初始化時只要呼叫一次即可。


### 4.2 規格

- Method：`GET`
- URL：`/api/menu/tree`
- Request：無參數。
- Response：JSON 陣列，每個元素代表一個 Level 1 節點，包含遞迴的 `children`。


### 4.3 Response JSON 範例

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
          // ... 其他 Level 3
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
        ]
      }
    ]
  }
]
```

實作建議：

- 後端從 `MenuFunction` 讀出所有 `IsActive = 1` 的資料，依 `Level` + `ParentId` + `SortOrder` 組樹狀結構。
- 建議在 Service 層先組好 List<TreeNode> 再回傳給 Controller。

---

## 5. API：POST /api/menu/reorder

### 5.1 目的

- 接收前端拖拉後的結果，一次更新所有節點的 `ParentId`、`Level`、`SortOrder`。


### 5.2 規格

- Method：`POST`
- URL：`/api/menu/reorder`
- Request Body：JSON 陣列，每一個元素代表一筆需要更新的節點。


### 5.3 Request Body 範例

```json
[
  { "id": 1,  "parentId": null, "level": 1, "sortOrder": 1 },
  { "id": 2,  "parentId": 1,    "level": 2, "sortOrder": 1 },
  { "id": 3,  "parentId": 2,    "level": 3, "sortOrder": 1 },
  { "id": 12, "parentId": 1,    "level": 2, "sortOrder": 2 }
]
```

行為說明：

- 後端依照每個 item 的 `id` 更新 `MenuFunction` 對應列的 `ParentId`、`Level`、`SortOrder`。
- 必要驗證：
    - Level 1 的 `ParentId` 必須為 NULL。
    - Level 2 的 `ParentId` 必須指向 Level 1。
    - Level 3 的 `ParentId` 必須指向 Level 2。
    - `SortOrder` 應為大於 0 的整數。
- Response：
    - 成功：200 OK，內文可簡單回傳 `{ "success": true }`。
    - 失敗：400 Bad Request，回傳錯誤原因。

---

## 6. 前端頁面：/settings/menu-sorting

### 6.1 功能目的

- 提供一個設定頁，讓使用者透過拖拉方式：
    - 調整同層級功能顯示順序。
    - 把子功能移到另一個主功能底下。
    - 把次子功能直接移到另一個主功能或其他次功能底下。


### 6.2 操作流程

1. 進入 `/settings/menu-sorting`。
2. 前端呼叫 `GET /api/menu/tree`，渲染功能樹。
3. 使用者透過 drag \& drop 調整節點位置（包含排序與階層）。
4. 使用者按下「儲存」按鈕，前端將目前樹狀結構轉成 `POST /api/menu/reorder` 所需格式，送到後端。
5. 後端更新 DB 成功後回傳成功訊息，前端可重新呼叫 `GET /api/menu/tree` 重新渲染。

### 6.3 UI 建議

- 左側：樹狀清單（顯示 `DisplayName`、可能加上 `FuncCode` 小字）。
- 支援拖拉調整階層與順序的 UI 元件（例如 Sortable Tree）。
- 右側：節點資訊區（選到某節點時，顯示 FuncCode、RouteUrl、Remark 等，視需求可允許編輯）。

---

## 7. 實作指引（給 Claude）

1. 建立 `MenuFunction` 實體與 DbSet。
2. 撰寫 Repository / Service：
    - 取得所有 MenuFunction，組成 Tree DTO。
    - 更新多筆 MenuFunction 的 ParentId/Level/SortOrder。
3. 實作兩支 API：
    - `GET /api/menu/tree`
    - `POST /api/menu/reorder`
4. 前端實作：
    - Sidebar：使用 `GET /api/menu/tree` 結果渲染三層選單。
    - `/settings/menu-sorting`：支持拖拉並呼叫 `POST /api/menu/reorder`。
```
