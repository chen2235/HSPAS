# 美股交易紀錄管理（US Stock Trade Records）

> **模組代碼**：US_TRADES
> **路由**：`/us/trades`
> **選單位置**：股票損益紀錄 → 美股投資 → 美股交易紀錄管理

---

## 1. 功能概述

管理美股（複委託）交易紀錄，支援：
- 上傳國泰證券「海外股票交易明細」PDF 日對帳單，自動解析美股交易（含密碼解密）
- 手動新增美股交易紀錄（買進 BUY / 賣出 SELL / 股利 DIVIDEND）
- 查詢、修改、刪除美股交易紀錄
- 批次匯入解析後的交易明細

---

## 2. 資料庫設計

### 2.1 資料表 `US_TradeRecord`

| 欄位 | 型別 | 說明 |
|------|------|------|
| Id | bigint IDENTITY PK | 自動編號 |
| TradeDate | date | 交易日期 |
| SettlementDate | date NULL | 交割日期 |
| StockSymbol | nvarchar(20) | 美股代號（如 NVDA, AAPL） |
| StockName | nvarchar(100) | 股票名稱（如 NVIDIA Corp） |
| Market | nvarchar(20) | 交易市場（如「美國」） |
| Action | nvarchar(10) | BUY / SELL / DIVIDEND |
| Currency | nvarchar(5) | 交易幣別（USD） |
| Quantity | decimal(19,6) | 股數（美股支援零股，最小 0.000001） |
| Price | decimal(19,6) | 單股成交價（USD） |
| Amount | decimal(19,4) | 成交金額 = Quantity × Price（USD） |
| Fee | decimal(19,4) | 手續費（USD） |
| Tax | decimal(19,4) | 交易稅費（USD） |
| NetAmount | decimal(19,4) | 應收/付金額（USD），BUY 為負、SELL 為正 |
| SettlementCurrency | nvarchar(5) NULL | 實際交割幣別（USD/TWD） |
| ExchangeRate | decimal(19,4) NULL | 匯率（TWD/USD） |
| NetAmountTwd | decimal(19,4) NULL | 實際應收/付金額（TWD） |
| TradeRef | nvarchar(20) NULL | 交易序號（對帳單上的編號） |
| Note | nvarchar(500) NULL | 備註 |
| CreateTime | datetime2 DEFAULT SYSUTCDATETIME() | 建立時間 |

### 2.2 索引

- `IX_US_TradeRecord_StockSymbol` (StockSymbol)
- `IX_US_TradeRecord_TradeDate` (TradeDate DESC)

---

## 3. 後端 API

### 3.1 PDF 解析服務 `UsCathayStatementParserService`

解析國泰證券「客戶日買賣報告書」中的海外股票交易明細：
- 使用 UglyToad.PdfPig 解密 PDF（密碼預設 A120683373）
- 解析欄位：交易序號、商品代號/名稱、市場、交易種類、幣別、股數、價格、成交金額、手續費、交易稅費、應收付金額、交割日、交割幣別、匯率、實際應收付金額
- 交易方向判斷：應收/付金額為負 → BUY，為正 → SELL
- 支援解析 Subtotal 行確認合計

### 3.2 API 端點

| 方法 | 路徑 | 說明 |
|------|------|------|
| POST | `/api/us/trades` | 新增美股交易紀錄 |
| GET | `/api/us/trades` | 查詢美股交易紀錄（支援 symbol/from/to 篩選） |
| PUT | `/api/us/trades/{id}` | 修改美股交易紀錄 |
| DELETE | `/api/us/trades/{id}` | 刪除美股交易紀錄 |
| POST | `/api/us/trades/cathay-statement/parse` | 上傳並解析國泰證美股日對帳單 PDF |
| POST | `/api/us/trades/batch` | 批次新增多筆美股交易紀錄 |

### 3.3 NetAmount 計算邏輯

```
BUY:      NetAmount = -(Amount + Fee + Tax)
SELL:     NetAmount = +(Amount - Fee - Tax)
DIVIDEND: NetAmount = +(Amount)
```

---

## 4. 前端頁面 `/us/trades`

### 4.1 匯入國泰證美股日對帳單

- 檔案選擇（.pdf）、密碼輸入框（預設 A120683373）、「解析對帳單」按鈕
- 解析成功後顯示「待確認交易明細表格」：
  - 欄位：交易日期、代號、名稱、市場、交易別、幣別、股數、價格、成交金額、手續費、稅費、應收付額(USD)、交割日、匯率、應收付額(TWD)
  - 每列可移除、可編輯代號與備註
- 「確認新增」按鈕呼叫批次 API

### 4.2 手動新增交易表單

- 交易日期（預設今天）、股票代號（如 NVDA）、股票名稱
- 動作（買進/賣出/股利）、幣別（預設 USD）、股數（支援小數）、單股成交價
- 手續費、交易稅費、備註
- 交割日期（選填）、匯率（選填）

### 4.3 查詢交易紀錄

- 篩選：股票代號、起始日期、結束日期
- 結果表格含排序功能
- 每列「修改」按鈕開啟 Modal 編輯/刪除

---

## 5. 前端 HTML/JS 檔案

- **HTML**: `pages/us-trades.html`
- **JS**: `js/us-trades.js`
- **路由 hash**: `us/trades`
- **HSPAS.registerPage**: `'us/trades'`
