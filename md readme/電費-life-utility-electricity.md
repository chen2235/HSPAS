```markdown
# 生活計帳 – 每期電費紀錄與儀表板規格 (life-utility-electricity.md)

本文檔為「鴻仁生活紀錄系統」中，生活計帳模組底下「每期水電費瓦斯紀錄」之子功能：「每期電費紀錄」與「每期電費儀表板」之詳細規格，並說明是否拆出電費明細表的設計考量。

---

## 1. 需求背景與目標

- 背景：
  - 台灣電力公司透過 PDF 電子帳單提供每期電費資訊。
  - 使用者希望集中管理歷史電費資料，進行年/月度用電與金額分析，同時保留完整帳單細項以便日後進階分析。
- 目標：
  - 提供「每期電費紀錄」功能，支援上傳台電 PDF 並自動解析欄位，使用者可以檢視與修改。
  - 提供「每期電費儀表板」功能，依年/月視覺化呈現用電度數與繳費總金額。
  - 預留「費用結構分析」能力，未來可針對流動電費、各種優惠與扣減做統計。

---

## 2. 系統流程概述

### 2.1 每期電費紀錄 – 上傳與解析流程

1. 使用者於前端選擇「每期電費紀錄」畫面。
2. 點選「上傳台電電費 PDF」：
   - 初始階段可先支援單檔上傳，未來可擴充為多檔。
3. 後端以固定密碼 `0928284285` 解密 PDF，進行內容解析。
4. 從帳單中抽取欄位，建立一筆「暫存電費紀錄」：
   - 固定欄位：用電地址、電號、輪流停電組別。
   - 每期欄位：計費期間、抄表/扣款日、計費度數、日平均度數、當期每度平均電價、繳費總金額、發票期別、發票號碼等。
   - 其他細項（例如流動電費、節電獎勵、電子帳單優惠等），先存入 RawDetailJson，日後可拆到明細表。
5. 前端列表顯示解析結果，提供「明細」、「修改」操作。
6. 使用者確認資料無誤後儲存，即成為正式紀錄，供儀表板使用。

若解析失敗或欄位缺漏：

- 顯示錯誤訊息與部分成功解析的欄位。
- 使用者可選擇放棄該 PDF，或以「新增/修改」方式補齊欄位後儲存。

---


## 3. 欄位設計

------------------------------------------------------------
-- 1. 每期電費主表：Life_ElectricityBillPeriod
------------------------------------------------------------
CREATE TABLE [dbo].[Life_ElectricityBillPeriod] (
    [Id]                        BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,  -- 主鍵

    -- 電號基本資料 (固定欄位；目前先假設一個使用者只有一組，未來可抽到獨立表)
    [Address]                   NVARCHAR(200)   NOT NULL,      -- 用電地址
    [PowerNo]                   VARCHAR(20)     NOT NULL,      -- 電號，例如 16-36-6055-40-7
    [BlackoutGroup]             CHAR(1)         NULL,          -- 輪流停電組別，例如 C

    -- 計費期間與日期
    [BillingStartDate]          DATE            NOT NULL,      -- 計費起始日
    [BillingEndDate]            DATE            NOT NULL,      -- 計費結束日
    [BillingDays]               INT             NOT NULL,      -- 計費天數（例如 57）
    [BillingPeriodText]         NVARCHAR(100)   NULL,          -- 原始文字，例如「114/12/10 至 115/02/04（共 57 天）」

    [ReadOrDebitDate]           DATE            NOT NULL,      -- 抄表/扣款日

    -- 用電度數與平均
    [Kwh]                       INT             NOT NULL,      -- 計費度數（本期用電度數）
    [KwhPerDay]                 DECIMAL(9,2)    NULL,          -- 日平均度數
    [AvgPricePerKwh]            DECIMAL(9,4)    NULL,          -- 當期每度平均電價

    -- 金額相關
    [TotalAmount]               DECIMAL(19,2)   NOT NULL,      -- 繳費總金額（最後應繳金額）
    [InvoiceAmount]             DECIMAL(19,2)   NULL,          -- 發票金額（通常應等於 TotalAmount）

    -- 電價類別與其他資訊
    [TariffType]                NVARCHAR(100)   NULL,          -- 電價種類，例如「表燈 非營業用 / 非時間電價」
    [SharedMeterHouseholdCount] INT             NULL,          -- 公共用電分攤戶數

    -- 發票資訊
    [InvoicePeriod]             NVARCHAR(50)    NULL,          -- 例如「115年03-04月」
    [InvoiceNo]                 NVARCHAR(20)    NULL,          -- 發票號碼，例如「ZD-31664747」

    -- 原始明細 JSON（第一階段使用，第二階段拆明細表時仍可保留）
    [RawDetailJson]             NVARCHAR(MAX)   NULL,          -- 儲存解析出來的電費明細 JSON

    -- 系統管理欄位
    [Remark]                    NVARCHAR(200)   NULL,
    [CreateTime]                DATETIME2(0)    NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdateTime]                DATETIME2(0)    NULL,
    [CreateUser]                NVARCHAR(50)    NULL,
    [UpdateUser]                NVARCHAR(50)    NULL
);
GO

-- 索引：常用查詢為「依年/月」統計，因此對 BillingEndDate 建複合索引
CREATE INDEX IX_Life_ElecBillPeriod_PowerNo_EndDate
ON [dbo].[Life_ElectricityBillPeriod] ([PowerNo], [BillingEndDate]);
GO

-- 若習慣以抄表/扣款日做月份判斷，也可另外建立索引
CREATE INDEX IX_Life_ElecBillPeriod_PowerNo_ReadDate
ON [dbo].[Life_ElectricityBillPeriod] ([PowerNo], [ReadOrDebitDate]);
GO


------------------------------------------------------------
-- 2. 每期電費明細表：Life_ElectricityBillDetail
--    （第二階段使用：費用結構分析）
------------------------------------------------------------
CREATE TABLE [dbo].[Life_ElectricityBillDetail] (
    [Id]            BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,

    [PeriodId]      BIGINT          NOT NULL,          -- 對應 Life_ElectricityBillPeriod.Id

    [ItemName]      NVARCHAR(50)    NOT NULL,          -- 項目名稱，例如「流動電費」「節電獎勵」
    [ItemType]      VARCHAR(20)     NULL,              -- 項目類型，例如：CHARGE / DISCOUNT / OTHER
    [Amount]        DECIMAL(19,2)   NOT NULL,          -- 金額，可為正數或負數

    [SortOrder]     INT             NOT NULL DEFAULT(1),   -- 同一期內的顯示順序（可選）

    [Remark]        NVARCHAR(200)   NULL,
    [CreateTime]    DATETIME2(0)    NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdateTime]    DATETIME2(0)    NULL,
    [CreateUser]    NVARCHAR(50)    NULL,
    [UpdateUser]    NVARCHAR(50)    NULL
);
GO

ALTER TABLE [dbo].[Life_ElectricityBillDetail]
ADD CONSTRAINT FK_Life_ElecBillDetail_Period
FOREIGN KEY ([PeriodId]) REFERENCES [dbo].[Life_ElectricityBillPeriod] ([Id]);
GO

-- 索引：方便依 PeriodId 聚合與查詢
CREATE INDEX IX_Life_ElecBillDetail_Period
ON [dbo].[Life_ElectricityBillDetail] ([PeriodId]);

-- 若常做「依 ItemName/ItemType」的年度統計，可加此索引
CREATE INDEX IX_Life_ElecBillDetail_Item
ON [dbo].[Life_ElectricityBillDetail] ([ItemName], [ItemType]);
GO


### 3.1 固定欄位（電號基本資料）

> 這些欄位來自使用者預先設定的電號基本資訊，目前先伴隨每筆紀錄存放，未來若支援多電號可抽到獨立「電號設定」資料表。

- 用電地址 (Address)
  - 範例：新北市汐止區福山街60巷12號四樓
- 電號 (PowerNo)
  - 範例：16-36-6055-40-7
- 輪流停電組別 (BlackoutGroup)
  - 範例：C

### 3.2 每期主欄位（Life_ElectricityBillPeriod）

> 下列欄位由 PDF 解析或人工輸入/調整，對應到主表 `Life_ElectricityBillPeriod`。

- 計費期間相關：
  - BillingStartDate：計費起始日（由「114/12/10」等民國日期轉換為西元日期）。
  - BillingEndDate：計費結束日。
  - BillingDays：本期用電日數（例如 57）。
  - BillingPeriodText：原始文字描述，例如「114/12/10 至 115/02/04（共 57 天）」。

- 日期與用電資訊：
  - ReadOrDebitDate：抄表/扣款日。
  - Kwh：計費度數（本期用電度數）。
  - KwhPerDay：日平均度數。
  - AvgPricePerKwh：當期每度平均電價（若帳單無此欄位，可用 TotalAmount / Kwh 計算）。

- 金額與發票：
  - TotalAmount：繳費總金額（最後應繳金額）。
  - InvoiceAmount：發票金額（通常應等於 TotalAmount，作為 cross-check）。
  - InvoicePeriod：發票期別，例如「115年03-04月」。
  - InvoiceNo：發票號碼，例如「ZD-31664747」。

- 其他延伸欄位：
  - TariffType：電價種類，例如「表燈 非營業用 / 非時間電價」。
  - SharedMeterHouseholdCount：公共用電分攤戶數。
  - RawDetailJson：完整電費明細之 JSON 字串（初期用來支撐明細畫面與後續轉換）。

---

## 4. 明細欄位與 RawDetailJson

從目前的解析結果，可以取得帳單內的費用細項，例如：

- 流動電費：3,377.6 元
- 停電扣減金額：-3.3 元
- 公共設施電費：29.0 元
- 節電獎勵：-84.0 元
- 電子帳單優惠：-10.0 元
- 繳費總金額：3,309 元

在初期版本中：

- 這些細項不直接進入統計邏輯，只影響「明細畫面」顯示。
- 建議統一存成 `RawDetailJson`，例如：

```json
{
  "Items": [
    { "Name": "流動電費", "Amount": 3377.6 },
    { "Name": "停電扣減金額", "Amount": -3.3 },
    { "Name": "公共設施電費", "Amount": 29.0 },
    { "Name": "節電獎勵", "Amount": -84.0 },
    { "Name": "電子帳單優惠", "Amount": -10.0 }
  ]
}
```

日後若要做「費用結構分析」，可根據 RawDetailJson 轉換出多筆明細資料，寫入 `Life_ElectricityBillDetail`。

---

## 5. 前端畫面規格

### 5.1 每期電費紀錄 – 列表頁

路由建議：`/life/utility/electricity/period-records`

- 篩選條件：
    - 年份（必填）。
    - 月份（選填）。
    - 電號（未來支援多電號時使用）。
- 列表欄位：
    - 計費期間（可顯示 BillingPeriodText 或 StartDate~EndDate）。
    - 抄表/扣款日。
    - 計費度數（Kwh）。
    - 日平均度數（KwhPerDay）。
    - 當期每度平均電價（AvgPricePerKwh）。
    - 繳費總金額（TotalAmount）。
    - 發票期別（InvoicePeriod）。
    - 發票號碼（InvoiceNo）。
    - 操作：明細 / 修改。
- 操作按鈕：
    - 上傳 PDF：
        - 選擇台電電費 PDF 檔，上傳後觸發後端解析流程。
    - 明細：
        - 彈出對話框顯示完整電費資訊（見 5.2）。
    - 修改：
        - 彈出編輯表單，可修改上述主要欄位。


### 5.2 每期電費紀錄 – 明細頁/明細彈窗

顯示區塊建議：

1. 基本資訊
    - 用戶名稱（若可取得）。
    - 用電地址。
    - 電號。
    - 輪流停電組別。
2. 計費資訊
    - 計費期間（起始日、結束日、天數）。
    - 抄表/扣款日。
    - 計費度數。
    - 日平均度數。
    - 當期每度平均電價。
3. 金額資訊
    - 流動電費。
    - 停電扣減金額。
    - 公共設施電費。
    - 節電獎勵。
    - 電子帳單優惠。
    - 繳費總金額（TotalAmount）。
4. 發票資訊
    - 發票期別。
    - 發票號碼。
    - 發票金額（InvoiceAmount）。
5. 其他
    - 電價種類。
    - 公共用電分攤戶數。
    - 按鈕：開啟原始 PDF（若有保留檔案路徑或檔名）。

---

## 6. 每期電費儀表板規格

路由建議：`/life/utility/electricity/dashboard`

### 6.1 功能與視角

- 依「年」為主要視角。
- 對該年每個月份顯示：
    - 計費度數總和（ΣKwh）。
    - 繳費總金額總和（ΣTotalAmount）。

未來若拆出明細表，可新增以 ItemName / ItemType 為維度的費用結構分析。

### 6.2 篩選條件與歸屬月份策略

- 篩選條件：
    - 年份（必填）。
    - 電號。
- 歸屬月份策略（簡化版）：
    - 以 BillingEndDate（計費結束日）所屬的月份作為該帳單的統計月份。
    - 例：114/12/10–115/02/04，結束日為 115/02/04 ⇒ 歸屬 115 年 2 月。
- 聚合計算：
    - 每月計費度數：該月所有紀錄之 Kwh 合計。
    - 每月繳費總金額：該月所有紀錄之 TotalAmount 合計。

若未來需要更精準的按日拆分，可再改為按天數比例拆分度數/金額，但第一版先採結束日歸屬。

### 6.3 圖表與表格

- 圖表：
    - X 軸：月份（1–12）。
    - Y 軸：
        - 左側：每月計費度數（柱狀圖）。
        - 右側：每月繳費總金額（折線圖）。
    - 互動：
        - 滑鼠 hover 顯示該月度數與金額。
        - 點擊某月份可帶出該月的「每期電費紀錄列表」。
- 表格：
    - 欄位：
        - 月份。
        - 計費度數合計。
        - 繳費總金額合計。
        - 該月帳單筆數。
    - 每列可展開查看該月份下的各期別，並提供「明細」、「修改」連結。

---

## 7. 後端 API 草案

### 7.1 上傳電費 PDF 並解析

```http
POST /api/life/utility/electricity/upload
Content-Type: multipart/form-data
Body: file = (台電 PDF 檔案，上限 5 MB，僅接受 .pdf / .jpg / .jpeg / .png)
```

檔案驗證：
- 大小上限：5 MB，超過回傳 400。
- 格式白名單：`.pdf`、`.jpg`、`.jpeg`、`.png`，不符回傳 400。

流程：

1. 以密碼 `0928284285` 解密 PDF。
2. 解析出主表欄位與細項，組成 DTO。
3. 儲存到 `Life_ElectricityBillPeriod`（含 RawDetailJson），並回傳解析結果供前端顯示。

### 7.2 取得每期電費紀錄列表

```http
GET /api/life/utility/electricity/period-records?year=2025&month=2&powerNo=16-36-6055-40-7
```

回傳欄位：對應列表頁所需欄位。

### 7.3 取得單筆明細 / 更新紀錄

```http
GET /api/life/utility/electricity/period-records/{id}
PUT /api/life/utility/electricity/period-records/{id}
```

- GET：回傳主表欄位 + RawDetailJson。
- PUT：接受可修改欄位（計費期間、抄表/扣款日、Kwh、KwhPerDay、AvgPricePerKwh、TotalAmount、InvoicePeriod、InvoiceNo 等）並更新。


### 7.4 儀表板資料

```http
GET /api/life/utility/electricity/dashboard?year=2025&powerNo=16-36-6055-40-7
```

回傳每個月份的聚合結果：

- 月份。
- KwhTotal。
- AmountTotal。
- BillCount。

---

## 8. 民國年轉西元的處理建議

台電帳單日期以民國年表示（例如 114/12/10），系統儲存時需轉為西元日期。

### 8.1 轉換邏輯

- 民國年 + 1911 = 西元年。
    - 例：民國 114 年 ⇒ 西元 2025 年。
- 日期字串：
    - 原始格式：`YYY/MM/DD`。
    - 轉換步驟：

1. 拆解字串為 YearMinguo、Month、Day。
2. 計算 YearAD = YearMinguo + 1911。
3. 組合為 `YearAD-Month-Day`。
4. 轉為 `DATE` 型別。


### 8.2 實作建議

- 在後端建立一個共用工具函式，例如：
    - C\#：
        - `DateTime ConvertMinguoToAd(string minguoDateString)`
    - T-SQL（如需要在 DB 端處理）：
        - 建一個 scalar function，負責將 `N'114/12/10'` 轉為 `2025-12-10`。
- 所有從 PDF 抽取的日期欄位（計費期間起迄、抄表日、扣款日）都先轉為西元 DateTime，再寫入 `DATE` 欄位。

---

## 9. 電費明細是否拆表之設計策略

本節說明「電費明細」是否拆成獨立資料表，對實作成本與儀表板分析能力的影響，並給出分階段的設計建議。

### 9.1 不拆明細表的情境（僅主表 + RawDetailJson）

- 儀表板需求：只需「計費度數」與「繳費總金額」的年/月統計。
- 實作方式：
    - 主表 `Life_ElectricityBillPeriod` 存所有主欄位。
    - `RawDetailJson` 存完整明細。
    - 儀表板只依主表 `Kwh` 與 `TotalAmount` 聚合。
    - 明細畫面從 `RawDetailJson` 渲染出細項表格。
- 優點：
    - 結構簡單、開發速度快。
    - 已足以滿足目前的年/月用電與金額趨勢需求。
- 限制：
    - 無法直接針對各種優惠/扣減做年度或長期統計。


### 9.2 拆出獨立明細表的情境（主表 + 明細表）

- 適用情境：
    - 需要分析「節電獎勵」等優惠的長期累積效果。
    - 需要分析「流動電費 vs 各種折扣」的費用構成。
    - 需要「費用結構儀表板」。
- 實作方式：
    - 新增 `Life_ElectricityBillDetail`：
        - PeriodId 對應主表 Id。
        - ItemName：流動電費 / 節電獎勵 / 電子帳單優惠…。
        - ItemType：CHARGE / DISCOUNT / OTHER。
        - Amount：金額（正負皆可）。
    - 儀表板可以依 ItemName / ItemType 做聚合統計：
        - 各項優惠每年累計金額。
        - 費用構成堆疊圖。


### 9.3 分階段導入建議

- 第一階段：
    - 以主表 + RawDetailJson 為主，完成：
        - PDF 解析與匯入。
        - 每期電費紀錄列表與明細。
        - 基本年/月儀表板（度數 + 總金額）。
- 第二階段（確定有費用結構分析需求）：
    - 新增 `Life_ElectricityBillDetail`。
    - 寫轉換程式，從 RawDetailJson 填入明細表。
    - 新增「優惠累積」、「費用結構」相關儀表板。

---
```


