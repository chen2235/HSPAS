
## 一、每期水費紀錄／儀表板專用 md（建議檔名：`life-utility-water.md`）

```markdown
# 生活計帳 – 每期水費紀錄與儀表板規格 (life-utility-water.md)

本文檔為「鴻仁生活紀錄系統」中，生活計帳模組底下「每期水電費瓦斯紀錄」之子功能：「每期水費紀錄」與「每期水費儀表板」之詳細規格。畫面與互動風格請參考「每期電費紀錄／每期電費儀表板」。

---

## 1. 需求背景與目標

- 背景：
  - 臺北自來水事業處提供水費電子通知單（PDF），內容包含用水期間、指針、用水度數與應繳金額。
  - 使用者希望集中管理歷史水費資料，按年與期別（同期間）分析水費金額與用水度數。
- 目標：
  - 提供「每期水費紀錄」功能，支援上傳自來水處 PDF 並自動解析欄位，使用者可以檢視與修改。
  - 提供「每期水費儀表板」功能，依年與水費期別（同期間）視覺化呈現總用水度數與應繳總金額。
  - 畫面風格、操作習慣與「每期電費紀錄／儀表板」保持一致，降低使用者學習成本。[file:34]

---

## 2. 系統流程概述

### 2.1 每期水費紀錄 – 上傳與解析流程

1. 使用者於前端選擇「每期水費紀錄」畫面（路由：`/life/utility/water/period-records`）。
2. 點選「上傳水費 PDF」：
   - 初始階段先支援單檔上傳，未來可擴充為多檔上傳批次匯入。
3. 後端以固定密碼 `2gaijdrl` 解密 PDF，進行內容解析。
4. 從水費電子通知單中抽取欄位，建立一筆「暫存水費紀錄」：
   - 固定欄位：用水地址、用水號碼（電號）、水表號碼。
   - 每期欄位：用水計費期間、總用水度數、本期用水度數、本期指針、上期指針、應繳總金額。
5. 前端列表顯示解析結果，提供「明細」、「修改」操作。
6. 使用者確認資料無誤後儲存，即成為正式紀錄，供儀表板使用。

若解析失敗或欄位缺漏：

- 顯示錯誤訊息與部分成功解析的欄位。
- 使用者可選擇放棄該 PDF，或以「新增/修改」方式補齊欄位後儲存。

---

## 3. 欄位設計

### 3.1 固定欄位（用水基本資料）

> 這些欄位由使用者預先設定，目前先伴隨每筆紀錄存放；未來若支援多戶水號，可抽成獨立「用水戶設定」資料表。

- 用水地址 (WaterAddress)
  - 新北市汐止區福山街60巷12號四樓
- 用水號碼／用戶編號 (WaterNo)
  - K-22-020975-0
- 水表號碼 (MeterNo)
  - C108015226

### 3.2 每期主欄位（Life_WaterBillPeriod）

> 下列欄位由水費 PDF 解析或人工輸入/調整，對應到主表 `Life_WaterBillPeriod`。

- 用水計費期間相關：
  - BillingStartDate：用水計費起始日。
  - BillingEndDate：用水計費結束日。
  - BillingDays：用水天數（如果通知單上有，則解析；若無，可由起訖日計算）。
  - BillingPeriodText：原始文字描述，例如「2025/01/01 至 2025/02/28」。

- 用水度數與指針：
  - TotalUsage：總用水度數（若通知單有提供「累計」或「合計」，可用此欄位；若無，可等同本期用水度數）。
  - CurrentUsage：本期用水度數（本期扣款對應的用水量）。
  - CurrentMeterReading：本期指針。
  - PreviousMeterReading：上期指針。

- 金額相關：
  - TotalAmount：應繳總金額（含水費、污水處理費等全部合計）。
  - InvoiceAmount（可選）：若有發票或收據金額欄位，可用此欄位 cross-check。

- 其他延伸欄位：
  - TariffType（可選）：用水類別（住家、商業等），視通知單是否有欄位而定。
  - RawDetailJson（可選）：若未來想記錄各細項（例如水費、污水處理費、折扣等），可用 JSON 儲存，第一版可先預留。

---

## 4. 資料庫 schema 建議（MSSQL）

### 4.1 每期水費主表：Life_WaterBillPeriod

```sql
CREATE TABLE [dbo].[Life_WaterBillPeriod] (
    [Id]                    BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,

    -- 用水戶基本資訊（固定欄位）
    [WaterAddress]          NVARCHAR(200)   NOT NULL,      -- 用水地址
    [WaterNo]               VARCHAR(20)     NOT NULL,      -- 用水號碼／用戶編號，例如 K-22-020975-0
    [MeterNo]               VARCHAR(30)     NOT NULL,      -- 水表號碼，例如 C108015226

    -- 用水計費期間
    [BillingStartDate]      DATE            NOT NULL,      -- 用水計費起始日
    [BillingEndDate]        DATE            NOT NULL,      -- 用水計費結束日
    [BillingDays]           INT             NULL,          -- 用水天數（可選）
    [BillingPeriodText]     NVARCHAR(100)   NULL,          -- 原始文字，例如「2025/01/01 至 2025/02/28」

    -- 用水度數與指針
    [TotalUsage]            INT             NULL,          -- 總用水度數（若有）
    [CurrentUsage]          INT             NOT NULL,      -- 本期用水度數
    [CurrentMeterReading]   INT             NOT NULL,      -- 本期指針
    [PreviousMeterReading]  INT             NOT NULL,      -- 上期指針

    -- 金額相關
    [TotalAmount]           DECIMAL(19,2)   NOT NULL,      -- 應繳總金額
    [InvoiceAmount]         DECIMAL(19,2)   NULL,          -- 收據/發票金額（如有）

    -- 其他資訊
    [TariffType]            NVARCHAR(100)   NULL,          -- 用水類別（住家/商業等）
    [RawDetailJson]         NVARCHAR(MAX)   NULL,          -- 預留：細項明細 JSON（如水費/污水費等）

    -- 系統管理欄位
    [Remark]                NVARCHAR(200)   NULL,
    [CreateTime]            DATETIME2(0)    NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdateTime]            DATETIME2(0)    NULL,
    [CreateUser]            NVARCHAR(50)    NULL,
    [UpdateUser]            NVARCHAR(50)    NULL
);
GO

-- 依「用水號碼 + 計費結束日」查詢水費紀錄（by 年 / 期）
CREATE INDEX IX_Life_WaterBillPeriod_WaterNo_EndDate
ON [dbo].[Life_WaterBillPeriod] ([WaterNo], [BillingEndDate]);
GO
```

> 水費目前沒有像電費那樣需要「費用結構分析」的需求，所以暫不拆明細表。若未來需要分析各種費用構成（例如水費 vs 污水處理費），可比照電費的 `Life_ElectricityBillDetail` 再新增 `Life_WaterBillDetail`。[file:34]

---

## 5. 前端畫面規格

### 5.1 每期水費紀錄 – 列表頁

路由建議：`/life/utility/water/period-records`
風格：沿用「每期電費紀錄」頁面的表格與按鈕樣式。

- 篩選條件：
    - 年份（必填）。
    - 期別（選填）：可用「起始月/結束月」或直接用日期區間。
    - 用水號碼（未來如有多戶水號）。
- 列表欄位：
    - 用水計費期間（顯示 BillingPeriodText 或 StartDate~EndDate）。
    - 總用水度數（TotalUsage，若無則顯示「-」或等於 CurrentUsage）。
    - 本期用水度數（CurrentUsage）。
    - 本期指針（CurrentMeterReading）。
    - 上期指針（PreviousMeterReading）。
    - 應繳總金額（TotalAmount）。
    - 操作：明細 / 修改。
- 操作按鈕：
    - 上傳 PDF：
        - 選擇自來水處水費 PDF 檔，上傳後觸發後端解析流程。
    - 明細：
        - 彈出對話框顯示完整水費資訊（見 5.2）。
    - 修改：
        - 彈出編輯表單，可修改上述主要欄位。


### 5.2 每期水費紀錄 – 明細頁/明細彈窗

顯示區塊建議（對齊電費明細畫面結構）：

1. 基本資訊
    - 用水地址。
    - 用水號碼（WaterNo）。
    - 水表號碼（MeterNo）。
2. 計費資訊
    - 用水計費期間（起始日、結束日、天數）。
    - 總用水度數。
    - 本期用水度數。
    - 本期指針。
    - 上期指針。
3. 金額資訊
    - 應繳總金額（TotalAmount）。
    - （如 RawDetailJson 有細項，可以列出水費、污水處理費、其他調整，第一版可以先不做。）
4. 其他
    - 用水類別（TariffType，若有）。
    - 按鈕：開啟原始 PDF（若有保留檔名或檔案位置）。

---

## 6. 每期水費儀表板規格

路由建議：`/life/utility/water/dashboard`
風格：沿用「每期電費儀表板」，只是指標改成「總用水度數 + 應繳總金額」。

### 6.1 功能與視角

- 依「年」為主要視角。
- 對該年各「水費期別」顯示：
    - 總用水度數。
    - 應繳總金額。

> 你提到「by 年與同期間水費金與總用水度數去呈現」，可以解釋為：
> - 同一年內的各筆水費期別（通常是雙月或固定週期），逐筆顯示其期別範圍 + 總用水度數 + 應繳總金額。

### 6.2 篩選條件

- 年份（必填）。
- 用水號碼（WaterNo）。


### 6.3 聚合與呈現策略

水費不像電費有明確「月份」欄位而是「用水計費期間」，這裡採「每期一列」的做法：

- 歸屬期別：
    - 直接以 `BillingStartDate ~ BillingEndDate` 作為一個期別顯示，不強行歸屬到單一月份。
- 儀表板中的列表與圖表：
    - 列表：每一筆 `Life_WaterBillPeriod` 為一列。
    - 圖表：X 軸可以用「期別序號」或「計費結束日」(BillingEndDate) 排序。

聚合計算：

- 每期總用水度數：取 `TotalUsage`，若為 NULL 則退回 `CurrentUsage`。
- 每期應繳總金額：取 `TotalAmount`。


### 6.4 圖表與表格

- 圖表（建議）：
    - X 軸：各期的 BillingEndDate（或簡化顯示為「第1期、第2期…」）。
    - Y 軸：
        - 左側：總用水度數（柱狀圖）。
        - 右側：應繳總金額（折線圖）。
    - 互動：
        - 滑鼠 hover：顯示期別的計費期間、總用水度數、應繳總金額。
        - 點擊某期：帶出該期在「每期水費紀錄」頁面的那一筆紀錄（或打開明細）。
- 表格：
    - 欄位：
        - 計費期間。
        - 總用水度數。
        - 應繳總金額。
        - 本期指針。
        - 上期指針。
    - 每列提供「明細」連結，回到 `Life_WaterBillPeriod` 的明細彈窗。

---

## 7. 後端 API 草案

### 7.1 上傳水費 PDF 並解析

```http
POST /api/life/utility/water/upload
Content-Type: multipart/form-data
Body: file = (水費 PDF 檔案，上限 5 MB，僅接受 .pdf / .jpg / .jpeg / .png)
```

檔案驗證：
- 大小上限：5 MB，超過回傳 400。
- 格式白名單：`.pdf`、`.jpg`、`.jpeg`、`.png`，不符回傳 400。

流程：

1. 以密碼 `2gaijdrl` 解密 PDF。
2. 解析出主表欄位，組成 DTO。
3. 儲存到 `Life_WaterBillPeriod`，並回傳解析結果給前端顯示。

### 7.2 取得每期水費紀錄列表

```http
GET /api/life/utility/water/period-records?year=2025&waterNo=K-22-020975-0
```

- 可選參數：`from`, `to`（日期區間）。
- 回傳欄位：對應列表頁所需欄位。


### 7.3 取得單筆明細 / 更新紀錄

```http
GET /api/life/utility/water/period-records/{id}
PUT /api/life/utility/water/period-records/{id}
```

- GET：回傳主表欄位 + RawDetailJson（如有）。
- PUT：允許更新下列欄位：
    - 用水計費期間（BillingStartDate, BillingEndDate, BillingDays）。
    - TotalUsage。
    - CurrentUsage。
    - CurrentMeterReading。
    - PreviousMeterReading。
    - TotalAmount。


### 7.4 儀表板資料

```http
GET /api/life/utility/water/dashboard?year=2025&waterNo=K-22-020975-0
```

回傳每一期的統計結果：

- BillingStartDate。
- BillingEndDate。
- TotalUsage。
- TotalAmount。
- PeriodIndex（可選；該年中的第幾期，用於前端畫圖排序）。

---

## 8. 實作備註

- 畫面與互動請沿用「每期電費紀錄／儀表板」：
    - 列表風格、按鈕位置、彈窗樣式統一。
    - 這樣使用者從電費切到水費幾乎不用重新學習。
- 水費 PDF 的欄位名稱與版面可能會隨時間變動：
    - 建議像電費一樣，把「欄位映射規則」封裝在解析 Service 裡，一旦版面更新只需要調整解析邏輯即可。

```

***






