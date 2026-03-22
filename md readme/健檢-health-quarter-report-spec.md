<img src="https://r2cdn.perplexity.ai/pplx-full-logo-primary-dark%402x.png" style="height:64px;margin-right:32px"/>

# 請分析 廖內科健檢報告-2.jpg，之後每三個月會上傳jpg，要分析裡面的資料並寫到我的專案中(skill.md)將分析欄位呈現出來讓我確認，沒問題後寫到DB中，在"每三個月報告儀表板“去與之前的資料去比對。

以上是我的需求說明，請產生出另一個md檔案，以便claude去開發

先給你結論：我已看懂這張健檢單主要欄位與你 skill.md 裡「健康管理紀錄」的設計方向，下面先產出一份新的 markdown 規格檔草稿，給你拿去丟給 Claude 開發用。[^1]

***

## 檔名建議

`health-quarter-report-spec.md`

***

## 1. 任務說明

- 本文件是「鴻仁生活紀錄系統」中「每三個月報告紀錄上傳 / 儀表板」的開發規格補充檔。
- 目標：
    - 針對「廖內科健檢報告」類型的 JPG 影像，每三個月上傳一次。
    - 由後端或 AI 服務解析報告上的各項檢驗數值，寫入資料庫。
    - 在「每三個月報告儀表板」中，將最新一次結果與歷史紀錄做趨勢與異常比較。

***

## 2. 目前樣本報告重點欄位（Liao-Nei-Ke-Jian-Jian-Bao-Gao-2）

> 以下欄位名稱與單位先依這張報告肉眼辨識的樣子命名，之後可再調整成你習慣的英文欄位名。

### 2.1 檢驗項目與值（依畫面由上而下）

- T-Cholesterol（總膽固醇）
    - 測值：168 mg/dL（在參考範圍內）
- Triglyceride（三酸甘油脂）
    - 測值：171 mg/dL（高於 <150，報告上有 ↑ 標記）
- HDL（高密度脂蛋白）
    - 測值：36 mg/dL（男 >40；此值略低，有 ↓/異常標記）
- SGPT (ALT)（肝功能）
    - 測值：36 U/L（報告參考值 ≤41）
- Creatinine（肌酐，腎功能）
    - 測值：0.93 mg/dL（參考 0.64–1.27）
- Uric Acid（尿酸）
    - 測值：6.5 mg/dL（參考 3.4–7.0）
- MDRD EGFR（eGFR 公式 1）
    - 測值：84.99 mL/min/1.73m²（參考 >60）
- CKD‑EPI EGFR（eGFR 公式 2）
    - 測值：98.19 mL/min/1.73m²（參考 >60）
- AC SUGAR（空腹血糖）
    - 測值：143 mg/dL（報告參考 70–100，有 ↑ 標記）
- HBA1C（糖化血色素）
    - 測值：7.0%（參考 4.0–5.6，有 ↑ 標記）
- 其他資訊（之後 DB 也要存）
    - 報告日期：115/01/22（民國年，需要轉西元）
    - 檢驗醫療院所：廖內科（報告上印章）

> 註：實際實作時，建議先用固定欄位開發，不走「任意欄位 K/V」。之後如果有其他醫院版本，再擴充欄位。

***

## 3. 資料庫設計（健康季報告）

### 3.1 主表：QuarterHealthReport

用來存每一次「每三個月健檢」的總表。

```sql
CREATE TABLE [dbo].[QuarterHealthReport] (
    [Id]              BIGINT        IDENTITY(1,1) PRIMARY KEY,
    [ReportDate]      DATE          NOT NULL,        -- 轉成西元，例如 2026-01-22
    [HospitalName]    NVARCHAR(100) NULL,            -- 例如 N'廖內科'
    [SourceFileName]  NVARCHAR(260) NULL,            -- 上傳的原始檔名
    [SourceFilePath]  NVARCHAR(500) NULL,            -- 伺服器儲存路徑或雲端 URL
    [OcrJsonRaw]      NVARCHAR(MAX) NULL,            -- AI / OCR 原始輸出 JSON（保留除錯用）
    [CreatedAt]       DATETIME2     NOT NULL CONSTRAINT DF_QHR_CreatedAt DEFAULT (SYSDATETIME()),
    [UpdatedAt]       DATETIME2     NULL
);
```


### 3.2 明細表：QuarterHealthReportDetail

如果未來會有其他醫院、或欄位變動，可以改成「Key/Value 模式」。但依目前需求，建議先定義固定欄位表，比較方便做報表與比較。

```sql
CREATE TABLE [dbo].[QuarterHealthReportDetail] (
    [Id]                 BIGINT       IDENTITY(1,1) PRIMARY KEY,
    [ReportId]           BIGINT       NOT NULL FOREIGN KEY REFERENCES [dbo].[QuarterHealthReport]([Id]),

    -- 脂質
    [TCholesterol]       DECIMAL(6,2) NULL,  -- mg/dL
    [Triglyceride]       DECIMAL(6,2) NULL,  -- mg/dL
    [HDL]                DECIMAL(6,2) NULL,  -- mg/dL

    -- 肝功能
    [SGPT_ALT]           DECIMAL(6,2) NULL,  -- U/L

    -- 腎功能
    [Creatinine]         DECIMAL(6,2) NULL,  -- mg/dL
    [UricAcid]           DECIMAL(6,2) NULL,  -- mg/dL
    [MDRD_EGFR]          DECIMAL(6,2) NULL,  -- mL/min/1.73m2
    [CKDEPI_EGFR]        DECIMAL(6,2) NULL,  -- mL/min/1.73m2

    -- 血糖
    [AcSugar]            DECIMAL(6,2) NULL,  -- mg/dL
    [Hba1c]              DECIMAL(4,2) NULL,  -- %

    -- 異常旗標（方便 dashboard 顯示）
    [TriglycerideHigh]   BIT           NULL,
    [HDLLow]             BIT           NULL,
    [AcSugarHigh]        BIT           NULL,
    [Hba1cHigh]          BIT           NULL,

    [CreatedAt]          DATETIME2     NOT NULL CONSTRAINT DF_QHRD_CreatedAt DEFAULT (SYSDATETIME())
);
```


***

## 4. 後端 API 規格

### 4.1 上傳季報告 JPG

- Method: `POST /api/health/checkup/qtr/upload`
- Content-Type: `multipart/form-data`
- Request:
    - `file`: 單一 PDF / JPG / PNG 檔（上限 5 MB）
    - `reportDate`（可選）：`YYYY-MM-DD`，若沒傳由系統或 AI 從報告上推測
    - `hospitalName`（可選）：預設 `廖內科`，可手動覆寫
- 檔案驗證：
    - 大小上限：5 MB，超過回傳 400
    - 格式白名單：`.pdf`、`.jpg`、`.jpeg`、`.png`，不符回傳 400
- Flow（邏輯說明給 Claude）：

1. 儲存上傳檔案到指定路徑（例如 `/data/health/qtr/`）。
2. 呼叫內部 OCR/AI 服務（未在本文件細部規格，可以由你或我之後補一份 prompt/skill 給它）。
3. 解析出各檢驗項目與數值，組成 `QuarterHealthReport` + `QuarterHealthReportDetail` 寫入 DB。
4. 回傳解析結果給前端做立即確認。
- Response 範例（JSON）：

```json
{
  "reportId": 123,
  "reportDate": "2026-01-22",
  "hospitalName": "廖內科",
  "values": {
    "tCholesterol": 168,
    "triglyceride": 171,
    "hdl": 36,
    "sgptAlt": 36,
    "creatinine": 0.93,
    "uricAcid": 6.5,
    "mdrdEgfr": 84.99,
    "ckdepiEgfr": 98.19,
    "acSugar": 143,
    "hba1c": 7.0
  },
  "flags": {
    "triglycerideHigh": true,
    "hdlLow": true,
    "acSugarHigh": true,
    "hba1cHigh": true
  }
}
```


### 4.2 取得單次報告明細

- `GET /api/health/checkup/qtr/{reportId}`
- 回傳 `QuarterHealthReport` + `QuarterHealthReportDetail`。


### 4.3 取得歷史報告列表（給儀表板用）

- `GET /api/health/checkup/qtr/list?from=YYYY-MM-DD&to=YYYY-MM-DD`
- 回傳依 `ReportDate` 排序的清單，每筆包含：
    - `reportId`, `reportDate`, `hospitalName`
    - 主要數值（至少：Triglyceride, HDL, AcSugar, Hba1c）
    - 異常旗標

***

## 5. 前端 – 每三個月報告儀表板

### 5.1 頁面路由

- URL：`/health/checkup/qtr/dashboard`
- 導覽列：已在原 skill.md 的 `MenuFunction` 初始資料中有 `HEALTH_CHECKUP_QTR_DASH`。[^1]


### 5.2 畫面區塊

1. 報告選擇與摘要
    - 左側：下拉選單或時間軸，選擇某一次報告（`ReportDate`）。
    - 右側：顯示該次報告主要數值與是否異常（用紅/綠標示）。
2. 趨勢圖
    - Triglyceride 趨勢（折線圖，橫軸：日期，縱軸：mg/dL，標上 150 這條參考線）。
    - HDL 趨勢（標上 40 參考線）。
    - AC SUGAR 與 HBA1C 趨勢。
3. 與上一季比較卡片
    - 對每一個重點指標顯示：
        - 本次值
        - 上一次值
        - 差異（↑/↓）

***

## 6. AI / OCR 解析需求（給 Claude 或其它 AI 用）

1. 輸入：單張健檢報告 JPG。
2. 輸出：固定 JSON 結構，例如：
```json
{
  "reportDate": "115/01/22",
  "hospitalName": "廖內科",
  "items": {
    "TCholesterol": { "value": 168, "unit": "mg/dL", "isHigh": false, "isLow": false },
    "Triglyceride": { "value": 171, "unit": "mg/dL", "isHigh": true, "isLow": false },
    "HDL":          { "value": 36,  "unit": "mg/dL", "isHigh": false, "isLow": true },
    "SGPT_ALT":     { "value": 36,  "unit": "U/L" },
    "Creatinine":   { "value": 0.93,"unit": "mg/dL" },
    "UricAcid":     { "value": 6.5, "unit": "mg/dL" },
    "MDRD_EGFR":    { "value": 84.99,"unit": "mL/min" },
    "CKDEPI_EGFR":  { "value": 98.19,"unit": "mL/min" },
    "AcSugar":      { "value": 143, "unit": "mg/dL", "isHigh": true },
    "Hba1c":        { "value": 7.0, "unit": "%"    , "isHigh": true }
  }
}
```

3. 後端接到 JSON 後：
    - 轉換民國日期 → 西元日期。
    - 將 `value` 寫入 `QuarterHealthReportDetail` 對應欄位。
    - `isHigh` / `isLow` 寫入旗標欄位。

***

## 7. 與 skill.md 既有內容的整合點

- 本文件新增的資料表與 API，皆屬於 `健康管理紀錄 / 健檢報告紀錄 / 每三個月報告...` 模組，與 skill.md 既有的 `HEALTH_ROOT`、`HEALTH_CHECKUP_QTR_UP`、`HEALTH_CHECKUP_QTR_DASH` 完全對齊。[^1]
- 開發時，請維持與現有專案相同的架構：
    - Controller：`HealthCheckupQuarterController`（範例名稱）
    - Service：`IQuarterHealthReportService` + 實作
    - Repository / EF Core Entity 對應上述兩張表。

***

你確認這份 spec 內容沒有問題後，我再幫你把 DB schema + API 規格補進原本的 `skill.md` 裡的「健康管理紀錄」章節版本，讓未來每季上傳 JPG 時可以照這一套流程走。

你比較偏好「固定欄位表」方式，還是希望我改成「通用 Key/Value + 指標定義表」再幫你調整一次？
<span style="display:none">[^2]</span>

<div align="center">⁂</div>

[^1]: skill.md

[^2]: Liao-Nei-Ke-Jian-Jian-Bao-Gao-2.jpg

