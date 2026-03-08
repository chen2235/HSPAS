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



