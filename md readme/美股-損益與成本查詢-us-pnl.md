# 美股損益與成本查詢（US Stock PnL & Cost Analysis）

> **模組代碼**：US_PNL
> **路由**：`/us/pnl`
> **選單位置**：股票損益紀錄 → 美股投資 → 美股損益與成本查詢

---

## 1. 功能概述

查詢美股投資組合的損益與成本分析：
- 整體美股投資組合摘要（總成本、總市值、未實現損益、報酬率，以 USD 計）
- 各檔美股持股損益明細（平均成本、現價、未實現損益、報酬率）
- 標題列可排序
- 報酬率顏色標示：≥100% 藍色、1%~99% 黑色、≤0% 紅色

---

## 2. 後端 API

### 2.1 美股投資組合服務 `IUsPortfolioService`

```csharp
public interface IUsPortfolioService
{
    Task<UsHoldingsSummary> GetHoldingsAsync(CancellationToken ct = default);
    Task<UsStockUnrealized?> GetStockUnrealizedAsync(string symbol, CancellationToken ct = default);
    Task<UsPortfolioSummary> GetUnrealizedSummaryAsync(CancellationToken ct = default);
}
```

### 2.2 持股計算邏輯

- 從 `US_TradeRecord` 取所有交易
- 各 Symbol 分組計算：BUY 累加 Quantity、SELL 累減 Quantity → CurrentQty
- 總成本 = BUY 的 (Amount + Fee + Tax) 累加
- 平均成本 = TotalCost / CurrentQty
- 市值 = CurrentQty × 最新價格（從外部 API 或手動輸入的最近一筆價格）

### 2.3 最新價格取得

由於美股無法像台股從 TWSE 自動抓取，最新價格取自：
1. 最近一筆同 Symbol 的 SELL 交易價格
2. 最近一筆同 Symbol 的 BUY 交易價格
3. 若無交易記錄，顯示「--」

> **未來擴充**：可串接免費 API（如 Yahoo Finance）自動取得即時報價

### 2.4 API 端點

| 方法 | 路徑 | 說明 |
|------|------|------|
| GET | `/api/us/portfolio/holdings` | 美股持股總覽（含市值、比例） |
| GET | `/api/us/portfolio/stock/{symbol}/unrealized` | 單檔美股未實現損益 |
| GET | `/api/us/portfolio/unrealized-summary` | 整體美股未實現損益摘要 |

### 2.5 DTO

```csharp
public class UsHoldingItem
{
    public string StockSymbol { get; set; }
    public string StockName { get; set; }
    public decimal Quantity { get; set; }       // 可能有小數（零股）
    public decimal? LastPrice { get; set; }      // USD
    public decimal MarketValue { get; set; }     // USD
    public decimal WeightRatio { get; set; }
}

public class UsStockUnrealized
{
    public string StockSymbol { get; set; }
    public string StockName { get; set; }
    public decimal CurrentQty { get; set; }
    public decimal AvgCost { get; set; }         // USD
    public decimal? LastPrice { get; set; }       // USD
    public decimal MarketValue { get; set; }      // USD
    public decimal TotalCost { get; set; }        // USD
    public decimal UnrealizedPnL { get; set; }    // USD
    public decimal UnrealizedReturn { get; set; } // 比率
}
```

---

## 3. 前端頁面 `/us/pnl`

### 3.1 整體投資組合摘要

- 4 欄摘要列：總成本(USD)、總市值(USD)、未實現損益(USD)、報酬率(%)
- 損益/報酬率顏色：≥1 藍色+正號、<1 紅色

### 3.2 持股損益明細表格

| 欄位 | 排序 | 說明 |
|------|------|------|
| 代號 | Y | StockSymbol |
| 名稱 | Y | StockName |
| 持股 | Y | CurrentQty（小數 6 位） |
| 平均成本 | Y | AvgCost (USD) |
| 現價 | Y | LastPrice (USD) |
| 總成本 | Y | TotalCost (USD) |
| 市值 | Y | MarketValue (USD) |
| 未實現損益 | Y | UnrealizedPnL (USD) |
| 報酬率 | Y | UnrealizedReturn % |

- 整列顏色：報酬率 ≥100% 藍色、≤0% 紅色
- 金額顯示 USD 前綴（如 $185.41）
- 預設按報酬率降序排列

---

## 4. 前端 HTML/JS 檔案

- **HTML**: `pages/us-pnl.html`
- **JS**: `js/us-pnl.js`
- **路由 hash**: `us/pnl`
- **HSPAS.registerPage**: `'us/pnl'`
