using System.Globalization;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace HSPAS.Api.Services;

public class CathayStatementParserService
{
    public record ParsedTradeItem(
        string TradeDate,
        string StockId,
        string StockName,
        string Action,
        int Quantity,
        decimal Price,
        decimal Fee,
        decimal Tax,
        decimal OtherCost,
        decimal NetAmount,
        string CustomerReceivablePayableRaw
    );

    public record ParseResult(bool Success, string? Error, List<ParsedTradeItem> Items);

    public ParseResult Parse(Stream pdfStream, string password)
    {
        try
        {
            var options = new ParsingOptions { Password = password };
            using var document = PdfDocument.Open(pdfStream, options);

            // Collect all words from all pages with positions
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

            // Extract trade date
            var tradeDate = ExtractTradeDate(allWords);
            if (tradeDate == null)
                return new ParseResult(false, "無法解析對帳單日期，請確認檔案格式是否為國泰證券日對帳單。", []);

            // Extract trade items
            var items = ExtractTradeItems(allWords, tradeDate);
            if (items.Count == 0)
                return new ParseResult(false, "未在對帳單中找到交易明細，請確認檔案內容。", []);

            return new ParseResult(true, null, items);
        }
        catch (Exception ex) when (ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("encrypt", StringComparison.OrdinalIgnoreCase))
        {
            return new ParseResult(false, "PDF 密碼錯誤，請確認密碼是否正確。", []);
        }
        catch (Exception ex)
        {
            return new ParseResult(false, $"檔案解析失敗：{ex.Message}", []);
        }
    }

    private record WordInfo(string Text, double X, double Y, int Page);

    private static string? ExtractTradeDate(List<WordInfo> words)
    {
        // Look for "2026/02/24" pattern (the trade date line)
        // It appears after "成交日期" header
        var dateWord = words.FirstOrDefault(w =>
            Regex.IsMatch(w.Text, @"^\d{4}/\d{1,2}/\d{1,2}$"));
        if (dateWord != null)
        {
            var parts = dateWord.Text.Split('/');
            return $"{parts[0]}-{int.Parse(parts[1]):D2}-{int.Parse(parts[2]):D2}";
        }

        // Fallback: ROC date "115年2月24日"
        var fullText = string.Join(" ", words.Select(w => w.Text));
        var match = Regex.Match(fullText, @"(\d{2,3})年(\d{1,2})月(\d{1,2})日");
        if (match.Success)
        {
            var rocYear = int.Parse(match.Groups[1].Value);
            var month = int.Parse(match.Groups[2].Value);
            var day = int.Parse(match.Groups[3].Value);
            return $"{rocYear + 1911:D4}-{month:D2}-{day:D2}";
        }

        return null;
    }

    private static List<ParsedTradeItem> ExtractTradeItems(List<WordInfo> words, string tradeDate)
    {
        var items = new List<ParsedTradeItem>();

        // Find the "商品名稱" header word to determine column X positions and the start of trade data
        var headerWord = words.FirstOrDefault(w => w.Text == "商品名稱");
        if (headerWord == null) return items;

        // Find "總合計" to determine end of trade data
        var summaryWord = words.FirstOrDefault(w => w.Text == "總合計");
        if (summaryWord == null) return items;

        double headerY = headerWord.Y;
        double summaryY = summaryWord.Y;

        // Determine column X ranges from header words at the same Y level
        // Headers: 商品名稱(~30) 類別(~65) 成交股數(~87) 單價(~123) 成交金額(~158) 手續費(~205) 交易稅(~233)
        // 客戶應收付額 is at a different Y but X≈276-283

        // Get all words between header and summary (Y between summaryY and headerY)
        // In PDF, Y increases upward, so trade rows have headerY > tradeY > summaryY
        var tradeWords = words
            .Where(w => w.Y < headerY && w.Y > summaryY)
            .OrderByDescending(w => w.Y)
            .ThenBy(w => w.X)
            .ToList();

        // Group words by Y coordinate (with tolerance of 5 units)
        var rows = GroupByY(tradeWords, 5.0);

        // Find action words (集買/集賣/現買/現賣) to identify trade rows
        // Each trade has a main row with stock data and may have a separate row for 客戶應收付額
        var actionKeywords = new[] { "集買", "集賣", "現買", "現賣" };

        // Find all main trade rows (containing action keyword)
        var tradeRows = new List<(double Y, List<WordInfo> Words)>();
        var amountRows = new List<(double Y, List<WordInfo> Words)>();

        foreach (var row in rows)
        {
            if (row.Words.Any(w => actionKeywords.Contains(w.Text)))
                tradeRows.Add(row);
            else if (row.Words.Any(w => Regex.IsMatch(w.Text, @"^[+-][\d,]+$")))
                amountRows.Add(row);
        }

        // Match each trade row with its customer amount row (the row just above it)
        foreach (var tradeRow in tradeRows)
        {
            var wordsInRow = tradeRow.Words.OrderBy(w => w.X).ToList();

            // Extract fields by position order
            string stockName = "", action = "";
            int quantity = 0;
            decimal price = 0, amount = 0, fee = 0, tax = 0;
            string customerAmountRaw = "";

            // The first word is usually the stock name
            int idx = 0;
            if (idx < wordsInRow.Count)
            {
                stockName = wordsInRow[idx].Text;
                idx++;
            }

            // Action (集買/集賣 etc.)
            if (idx < wordsInRow.Count && actionKeywords.Contains(wordsInRow[idx].Text))
            {
                action = wordsInRow[idx].Text;
                idx++;
            }

            // Quantity
            if (idx < wordsInRow.Count)
            {
                quantity = (int)ParseNumber(wordsInRow[idx].Text);
                idx++;
            }

            // Price
            if (idx < wordsInRow.Count)
            {
                price = ParseDecimal(wordsInRow[idx].Text);
                idx++;
            }

            // Amount (成交金額)
            if (idx < wordsInRow.Count)
            {
                amount = ParseDecimal(wordsInRow[idx].Text);
                idx++;
            }

            // Fee
            if (idx < wordsInRow.Count)
            {
                fee = ParseDecimal(wordsInRow[idx].Text);
                idx++;
            }

            // Tax
            if (idx < wordsInRow.Count)
            {
                tax = ParseDecimal(wordsInRow[idx].Text);
                idx++;
            }

            // Find the customer amount from the row just above this trade row
            // (higher Y value, closest to this row's Y)
            var amountRow = amountRows
                .Where(r => r.Y > tradeRow.Y && r.Y < tradeRow.Y + 20)
                .OrderBy(r => r.Y)
                .FirstOrDefault();

            if (amountRow.Words != null)
            {
                var amtWord = amountRow.Words
                    .FirstOrDefault(w => Regex.IsMatch(w.Text, @"^[+-][\d,]+$"));
                if (amtWord != null)
                    customerAmountRaw = amtWord.Text;
            }

            // Fallback: if no separate amount row, look for +/- amount in remaining words of trade row
            if (string.IsNullOrEmpty(customerAmountRaw) && idx < wordsInRow.Count)
            {
                var amtWord = wordsInRow.Skip(idx)
                    .FirstOrDefault(w => Regex.IsMatch(w.Text, @"^[+-][\d,]+$"));
                if (amtWord != null)
                    customerAmountRaw = amtWord.Text;
            }

            // Determine action and netAmount
            bool isSell = action.Contains("賣");
            decimal netAmount;
            if (!string.IsNullOrEmpty(customerAmountRaw))
            {
                bool isPositive = customerAmountRaw.StartsWith("+");
                netAmount = ParseDecimal(customerAmountRaw.TrimStart('+').TrimStart('-'));
                if (!isPositive) netAmount = -netAmount;
                // Override action based on customer amount sign if needed
                isSell = isPositive;
            }
            else
            {
                // Calculate from known values
                netAmount = isSell
                    ? amount - fee - tax
                    : -(amount + fee + tax);
            }

            items.Add(new ParsedTradeItem(
                TradeDate: tradeDate,
                StockId: "",
                StockName: stockName,
                Action: isSell ? "SELL" : "BUY",
                Quantity: quantity,
                Price: price,
                Fee: fee,
                Tax: tax,
                OtherCost: 0,
                NetAmount: netAmount,
                CustomerReceivablePayableRaw: customerAmountRaw
            ));
        }

        return items;
    }

    private static List<(double Y, List<WordInfo> Words)> GroupByY(List<WordInfo> words, double tolerance)
    {
        var groups = new List<(double Y, List<WordInfo> Words)>();

        foreach (var word in words)
        {
            var existing = groups.FindIndex(g => Math.Abs(g.Y - word.Y) <= tolerance);
            if (existing >= 0)
            {
                groups[existing].Words.Add(word);
            }
            else
            {
                groups.Add((word.Y, new List<WordInfo> { word }));
            }
        }

        return groups.OrderByDescending(g => g.Y).ToList();
    }

    private static long ParseNumber(string s)
    {
        s = s.Replace(",", "").Replace(" ", "").Trim();
        return long.TryParse(s, out var v) ? v : 0;
    }

    private static decimal ParseDecimal(string s)
    {
        s = s.Replace(",", "").Replace(" ", "").Trim();
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }
}
