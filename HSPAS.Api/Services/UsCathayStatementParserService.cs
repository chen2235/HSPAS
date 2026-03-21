using System.Globalization;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace HSPAS.Api.Services;

public class UsCathayStatementParserService
{
    public record ParsedUsTrade(
        string TradeDate,
        string TradeRef,
        string StockSymbol,
        string StockName,
        string Market,
        string Action,
        string Currency,
        decimal Quantity,
        decimal Price,
        decimal Amount,
        decimal Fee,
        decimal Tax,
        decimal NetAmount,
        string? SettlementDate,
        string? SettlementCurrency,
        decimal? ExchangeRate,
        decimal? NetAmountTwd,
        string? CustomerReceivablePayableRaw
    );

    public record ParseResult(bool Success, List<ParsedUsTrade> Items, string? Error);

    private record WordInfo(string Text, double X, double Y, int Page);

    public ParseResult Parse(Stream pdfStream, string password)
    {
        try
        {
            var options = new ParsingOptions { Password = password };
            PdfDocument document;
            try
            {
                document = PdfDocument.Open(pdfStream, options);
            }
            catch
            {
                return new ParseResult(false, new(), "PDF 解密失敗，請確認密碼。");
            }

            var allWords = new List<WordInfo>();
            for (int i = 1; i <= document.NumberOfPages; i++)
            {
                var page = document.GetPage(i);
                foreach (var word in page.GetWords())
                {
                    allWords.Add(new WordInfo(
                        word.Text,
                        word.BoundingBox.Left,
                        word.BoundingBox.Bottom,
                        i
                    ));
                }
            }
            document.Dispose();

            var fullText = string.Join(" ", allWords.Select(w => w.Text));
            if (!fullText.Contains("海外股票交易明細") && !fullText.Contains("Offshore"))
                return new ParseResult(false, new(), "此 PDF 非海外股票交易明細格式。");

            // Extract trade date
            var tradeDateMatch = Regex.Match(fullText, @"(\d{4})年(\d{2})月(\d{2})日");
            var tradeDate = tradeDateMatch.Success
                ? $"{tradeDateMatch.Groups[1].Value}-{tradeDateMatch.Groups[2].Value}-{tradeDateMatch.Groups[3].Value}"
                : DateTime.Today.ToString("yyyy-MM-dd");

            // Group words by Y (tolerance 2.5)
            var rows = GroupByY(allWords, 2.5);

            var items = new List<ParsedUsTrade>();

            // Find trade reference rows (8-digit number like 00008862)
            for (int ri = 0; ri < rows.Count; ri++)
            {
                var row1 = rows[ri];
                var tradeRefWord = row1.Words.FirstOrDefault(w => Regex.IsMatch(w.Text, @"^\d{8}$"));
                if (tradeRefWord == null) continue;

                var tradeRef = tradeRefWord.Text;
                var r1 = row1.Words.OrderBy(w => w.X).ToList();

                // The trade data spans 3 rows:
                // Row 1 (this row):     TradeRef, Symbol/Name, Currency, Price, NetAmount
                // Row 2 (next row):     Market, Action, Qty, Amount, Fee, Tax, SettlementDate
                // Row 3 (row after):    SettlementCurrency, ExchangeRate, ActualNetAmount

                if (ri + 1 >= rows.Count) continue;
                var r2 = rows[ri + 1].Words.OrderBy(w => w.X).ToList();

                List<WordInfo>? r3 = null;
                if (ri + 2 < rows.Count && Math.Abs(rows[ri + 2].Y - row1.Y) < 20)
                    r3 = rows[ri + 2].Words.OrderBy(w => w.X).ToList();

                // === Row 1: TradeRef, Symbol/Name, Currency, Price, NetAmount ===
                string symbol = "", stockName = "";
                var symbolWord = r1.FirstOrDefault(w => Regex.IsMatch(w.Text, @"^[A-Z]{1,10}/"));
                if (symbolWord == null) continue;

                var slashIdx = symbolWord.Text.IndexOf('/');
                symbol = symbolWord.Text.Substring(0, slashIdx);
                stockName = symbolWord.Text.Substring(slashIdx + 1);

                // Append subsequent non-keyword words as part of stock name
                var afterSymbol = r1.Where(w => w.X > symbolWord.X + 1).OrderBy(w => w.X).ToList();
                foreach (var aw in afterSymbol)
                {
                    if (Regex.IsMatch(aw.Text, @"^(USD|HKD|JPY|GBP|SGD|EUR|KRW|AUD|CAD|TWD|\d)"))
                        break;
                    stockName += " " + aw.Text;
                }
                stockName = stockName.Trim();

                // Currency from Row 1 (first 3-letter uppercase after name)
                var currWord = r1.FirstOrDefault(w =>
                    Regex.IsMatch(w.Text, @"^[A-Z]{3}$") && w.X > symbolWord.X);
                var currency = currWord?.Text ?? "USD";

                // Price from Row 1 (number with many decimals, positioned after currency)
                var r1Nums = r1.Where(w => Regex.IsMatch(w.Text, @"^-?[\d.]+$") && w.Text != tradeRef)
                    .OrderBy(w => w.X).ToList();
                decimal price = r1Nums.Count >= 1 ? ParseDec(r1Nums[0].Text) : 0;
                decimal netAmount = r1Nums.Count >= 2 ? ParseDec(r1Nums[1].Text) : 0;

                // === Row 2: Market, Action, Qty, Amount, Fee, Tax, SettlementDate ===
                var marketWord = r2.FirstOrDefault(w =>
                    Regex.IsMatch(w.Text, @"^(美國|香港|日本|英國|新加坡|韓國|德國|澳洲|加拿大)$"));
                var market = marketWord?.Text ?? "美國";

                var actionWord = r2.FirstOrDefault(w =>
                    w.Text == "買進" || w.Text == "賣出" || w.Text == "除息" || w.Text == "除權" || w.Text == "配息");
                if (actionWord == null) continue;
                var action = actionWord.Text switch
                {
                    "買進" => "BUY",
                    "賣出" => "SELL",
                    _ => "DIVIDEND"  // 除息、除權、配息 → DIVIDEND
                };

                var r2Nums = r2.Where(w => Regex.IsMatch(w.Text, @"^-?[\d.]+$"))
                    .OrderBy(w => w.X).ToList();
                decimal quantity = r2Nums.Count >= 1 ? ParseDec(r2Nums[0].Text) : 0;
                decimal amount = r2Nums.Count >= 2 ? ParseDec(r2Nums[1].Text) : 0;
                decimal fee = r2Nums.Count >= 3 ? ParseDec(r2Nums[2].Text) : 0;
                decimal tax = r2Nums.Count >= 4 ? ParseDec(r2Nums[3].Text) : 0;

                var sdWord = r2.FirstOrDefault(w => Regex.IsMatch(w.Text, @"^\d{4}/\d{2}/\d{2}$"));
                string? settlementDate = sdWord?.Text.Replace("/", "-");

                // === Row 3: SettlementCurrency, ExchangeRate, ActualNetAmount ===
                string? settlementCurrency = null;
                decimal? exchangeRate = null;
                decimal? netAmountTwd = null;

                if (r3 != null)
                {
                    var sc = r3.FirstOrDefault(w => Regex.IsMatch(w.Text, @"^[A-Z]{3}$"));
                    settlementCurrency = sc?.Text;

                    var r3Nums = r3.Where(w => Regex.IsMatch(w.Text, @"^-?[\d.]+$"))
                        .OrderBy(w => w.X).ToList();
                    if (r3Nums.Count >= 1) exchangeRate = ParseDec(r3Nums[0].Text);
                    if (r3Nums.Count >= 2) netAmountTwd = ParseDec(r3Nums[1].Text);
                }

                items.Add(new ParsedUsTrade(
                    tradeDate, tradeRef, symbol, stockName, market, action, currency,
                    quantity, price, amount, fee, tax, netAmount,
                    settlementDate, settlementCurrency, exchangeRate, netAmountTwd,
                    netAmount.ToString("N2")
                ));

                // Skip the rows we just processed
                ri += (r3 != null ? 2 : 1);
            }

            if (items.Count == 0)
                return new ParseResult(false, new(), "未能解析到任何交易明細，請確認 PDF 格式。");

            return new ParseResult(true, items, null);
        }
        catch (Exception ex)
        {
            return new ParseResult(false, new(), $"解析錯誤：{ex.Message}");
        }
    }

    private static List<(double Y, List<WordInfo> Words)> GroupByY(List<WordInfo> words, double tolerance)
    {
        var groups = new List<(double Y, List<WordInfo> Words)>();
        foreach (var word in words.OrderByDescending(w => w.Y))
        {
            var existing = groups.FindIndex(g => Math.Abs(g.Y - word.Y) <= tolerance);
            if (existing >= 0)
                groups[existing].Words.Add(word);
            else
                groups.Add((word.Y, new List<WordInfo> { word }));
        }
        return groups.OrderByDescending(g => g.Y).ToList();
    }

    private static decimal ParseDec(string s)
    {
        s = s.Replace(",", "").Trim();
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }
}
