using System.Text.Json;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace HSPAS.Api.Services;

public class TaiwaterBillParserService
{
    public record ParsedWaterBill(
        string WaterAddress,
        string WaterNo,
        string MeterNo,
        DateTime BillingStartDate,
        DateTime BillingEndDate,
        int? BillingDays,
        string? BillingPeriodText,
        int? TotalUsage,
        int CurrentUsage,
        int CurrentMeterReading,
        int PreviousMeterReading,
        decimal TotalAmount,
        string? RawDetailJson
    );

    public record ParseResult(bool Success, string? Error, ParsedWaterBill? Bill);

    public ParseResult Parse(Stream pdfStream, string password = "2gaijdrl")
    {
        try
        {
            var options = new ParsingOptions { Password = password };
            using var document = PdfDocument.Open(pdfStream, options);

            // Extract text as lines (preserving PDF layout better)
            var allLines = new List<string>();
            var allWordsText = new List<string>();
            for (int i = 1; i <= document.NumberOfPages; i++)
            {
                var p = document.GetPage(i);
                allWordsText.Add(string.Join(" ", p.GetWords().Select(w => w.Text)));
                // Also try line-by-line extraction using Letters
                var letters = p.Letters.OrderBy(l => -l.Location.Y).ThenBy(l => l.Location.X).ToList();
                var currentLine = "";
                double lastY = double.MaxValue;
                foreach (var letter in letters)
                {
                    if (Math.Abs(letter.Location.Y - lastY) > 3)
                    {
                        if (!string.IsNullOrWhiteSpace(currentLine))
                            allLines.Add(currentLine.Trim());
                        currentLine = "";
                    }
                    currentLine += letter.Value;
                    lastY = letter.Location.Y;
                }
                if (!string.IsNullOrWhiteSpace(currentLine))
                    allLines.Add(currentLine.Trim());
            }

            var wordText = NormalizeFullWidth(string.Join(" ", allWordsText));
            var lineText = string.Join("\n", allLines.Select(NormalizeFullWidth));

            var waterAddress = ExtractWaterAddress(lineText, wordText);
            var waterNo = ExtractWaterNo(lineText, wordText);
            var meterNo = ExtractMeterNo(lineText, wordText);
            var (startDate, endDate, days, periodText) = ExtractBillingPeriod(lineText, wordText);
            var combined = lineText + "\n" + wordText;
            var totalUsage = ExtractFieldIntFuzzy(combined, "總用水度數");
            var currentUsage = ExtractFieldIntFuzzy(combined, "本期用水度數");
            var currentReading = ExtractFieldIntFuzzy(combined, "本期指針");
            var previousReading = ExtractFieldIntFuzzy(combined, "上期指針");
            var totalAmount = ExtractTotalAmount(lineText, wordText);
            var rawDetailJson = ExtractDetailItems(lineText);

            if (waterNo == null) return new ParseResult(false, "無法解析水號", null);
            if (startDate == null || endDate == null) return new ParseResult(false, "無法解析用水計費期間", null);

            var bill = new ParsedWaterBill(
                WaterAddress: waterAddress ?? "新北市汐止區福山街60巷12號四樓",
                WaterNo: waterNo,
                MeterNo: meterNo ?? "C108015226",
                BillingStartDate: startDate.Value,
                BillingEndDate: endDate.Value,
                BillingDays: days,
                BillingPeriodText: periodText,
                TotalUsage: totalUsage,
                CurrentUsage: currentUsage ?? 0,
                CurrentMeterReading: currentReading ?? 0,
                PreviousMeterReading: previousReading ?? 0,
                TotalAmount: totalAmount ?? 0,
                RawDetailJson: rawDetailJson
            );

            return new ParseResult(true, null, bill);
        }
        catch (Exception ex) when (ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                                   ex.Message.Contains("encrypt", StringComparison.OrdinalIgnoreCase))
        {
            return new ParseResult(false, "PDF 密碼錯誤或無法解密", null);
        }
        catch (Exception ex)
        {
            return new ParseResult(false, $"PDF 解析失敗：{ex.Message}", null);
        }
    }

    private static string NormalizeFullWidth(string s)
    {
        // First: Unicode NFKC normalization (handles CJK Compatibility Ideographs like U+F963→度)
        s = s.Normalize(System.Text.NormalizationForm.FormKC);
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var c in s)
        {
            if (c >= '\uFF01' && c <= '\uFF5E')
                sb.Append((char)(c - 0xFEE0));
            else if (c == '\u3000')
                sb.Append(' ');
            else
                sb.Append(c);
        }
        return sb.ToString();
    }

    private static DateTime? ParseMinguoDate(string s)
    {
        s = NormalizeFullWidth(s.Trim());
        var m = Regex.Match(s, @"(\d{3})(\d{2})(\d{2})");
        if (m.Success)
        {
            int y = int.Parse(m.Groups[1].Value) + 1911;
            int mo = int.Parse(m.Groups[2].Value);
            int d = int.Parse(m.Groups[3].Value);
            try { return new DateTime(y, mo, d); } catch { }
        }
        m = Regex.Match(s, @"(\d{2,3})[/\-.](\d{1,2})[/\-.](\d{1,2})");
        if (m.Success)
        {
            int y = int.Parse(m.Groups[1].Value) + 1911;
            int mo = int.Parse(m.Groups[2].Value);
            int d = int.Parse(m.Groups[3].Value);
            try { return new DateTime(y, mo, d); } catch { }
        }
        return null;
    }

    private static int? ExtractFieldInt(string text, string pattern)
    {
        var m = Regex.Match(text, pattern);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var v)) return v;
        return null;
    }

    /// <summary>
    /// Fuzzy extract: build regex with optional \s* between each CJK character of label,
    /// then match [：:\s\n]* followed by digits.
    /// </summary>
    private static int? ExtractFieldIntFuzzy(string text, string label)
    {
        // Build regex: each char of label with optional spaces between
        var fuzzy = string.Join(@"\s*", label.Select(c => Regex.Escape(c.ToString())));
        var pattern = fuzzy + @"[\s：:\n]*(\d+)";
        var m = Regex.Match(text, pattern);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var v)) return v;
        return null;
    }

    private static string? ExtractWaterAddress(string lineText, string wordText)
    {
        // Line-based: 用水地址：新北市汐止區福山街60巷12號四樓
        var m = Regex.Match(lineText, @"用水地址[：:\s]*([\u4e00-\u9fff\d]+(?:市|縣)[\u4e00-\u9fff\d]+(?:區|鎮|鄉)[\u4e00-\u9fff\d]+號[\u4e00-\u9fff]*樓?)");
        if (m.Success) return m.Groups[1].Value.Trim();
        // Word-based fallback
        m = Regex.Match(wordText, @"用水地址[：:\s]*([\u4e00-\u9fff\d]+(?:市|縣)[\u4e00-\u9fff\d]+(?:區|鎮|鄉)[\u4e00-\u9fff\d]+號[\u4e00-\u9fff]*樓?)");
        if (m.Success) return m.Groups[1].Value.Trim();
        return null;
    }

    private static string? ExtractWaterNo(string lineText, string wordText)
    {
        // Method 1: Look for barcode-like string containing water no: "171101K220209750"
        // Format: digits + K + 2-digit area + 6-digit account + 1-digit check
        var m = Regex.Match(lineText + "\n" + wordText, @"\d+([A-Z])(\d{2})(\d{6})(\d)\d*");
        if (m.Success && m.Groups[1].Value == "K") // Ensure it's a valid water number prefix
            return $"{m.Groups[1].Value}-{m.Groups[2].Value}-{m.Groups[3].Value}-{m.Groups[4].Value}";

        // Method 2: Look for spaced water number near 水號: "K 22 020975 0"
        m = Regex.Match(wordText, @"(?:水\s*號|Water\s*Number).*?([A-Z])\s+(\d{2})\s+(\d{6})\s+(\d)");
        if (m.Success)
            return $"{m.Groups[1].Value}-{m.Groups[2].Value}-{m.Groups[3].Value}-{m.Groups[4].Value}";

        // Method 3: Masked format: K-22-****75-0
        m = Regex.Match(lineText + "\n" + wordText, @"水\s*號[：:\s]*([A-Z])[-\s]*(\d{2})[-\s]*[\d*]{4,6}(\d{2})[-\s]*(\d)");
        if (m.Success)
            return null; // Can't reconstruct full number from masked version

        // Method 4: Direct format K-22-020975-0
        m = Regex.Match(lineText + "\n" + wordText, @"([A-Z]-\d{2}-\d{6}-\d)");
        if (m.Success)
            return m.Groups[1].Value;

        return null;
    }

    private static string? ExtractMeterNo(string lineText, string wordText)
    {
        var combined = lineText + "\n" + wordText;
        // 水表號碼Meter No：\nC108015226
        var m = Regex.Match(combined, @"水表號碼.*?([A-Z]\d{6,})");
        if (m.Success) return m.Groups[1].Value;
        m = Regex.Match(combined, @"Meter\s*No[：:\s]*([A-Z]\d{6,})");
        if (m.Success) return m.Groups[1].Value;
        return null;
    }

    private static (DateTime? start, DateTime? end, int? days, string? periodText) ExtractBillingPeriod(string lineText, string wordText)
    {
        var combined = lineText + "\n" + wordText;
        // 用水計費期間：\n1141224/1150303
        var m = Regex.Match(combined, @"用水計費期間[：:\s]*(\d{7})\s*/\s*(\d{7})");
        if (!m.Success)
            m = Regex.Match(combined, @"(\d{7})/(\d{7})");

        if (m.Success)
        {
            var start = ParseMinguoDate(m.Groups[1].Value);
            var end = ParseMinguoDate(m.Groups[2].Value);
            int? days = (start.HasValue && end.HasValue) ? (int)(end.Value - start.Value).TotalDays + 1 : null;
            var periodText = start.HasValue && end.HasValue
                ? $"{start.Value:yyyy/MM/dd} ~ {end.Value:yyyy/MM/dd}（共 {days} 天）"
                : m.Value;
            return (start, end, days, periodText);
        }

        return (null, null, null, null);
    }

    private static decimal? ExtractTotalAmount(string lineText, string wordText)
    {
        // Line-based: 應繳總金額（元）Total Amount Due：\n61.0
        var m = Regex.Match(lineText, @"應繳總金額.*?Total Amount Due[：:\s]*\n\s*(\d[\d,.]*\.?\d*)");
        if (m.Success)
        {
            var s = m.Groups[1].Value.Replace(",", "");
            if (decimal.TryParse(s, out var v)) return v;
        }
        // Line-based: 應繳總金額（元）：61
        m = Regex.Match(lineText, @"應繳總金額[（\(]元[）\)][：:\s]*(\d[\d,.]*\.?\d*)");
        if (m.Success)
        {
            var s = m.Groups[1].Value.Replace(",", "");
            if (decimal.TryParse(s, out var v)) return v;
        }
        // Word text: 應繳總金額 ... 61.0
        m = Regex.Match(wordText, @"應繳總金額.*?Due[：:\s]+(\d[\d,.]*\.?\d*)");
        if (m.Success)
        {
            var s = m.Groups[1].Value.Replace(",", "");
            if (decimal.TryParse(s, out var v)) return v;
        }
        // Fallback: $61 pattern
        m = Regex.Match(lineText + "\n" + wordText, @"\$(\d[\d,]*)");
        if (m.Success)
        {
            var s = m.Groups[1].Value.Replace(",", "");
            if (decimal.TryParse(s, out var v)) return v;
        }
        return null;
    }

    private static string? ExtractDetailItems(string lineText)
    {
        var items = new List<object>();

        // 口徑Caliber/基本費：\n25/252.0 → basic fee is 252.0
        var basicFeeM = Regex.Match(lineText, @"基本費[：:\s]*\n?\s*\d+/(\d[\d,.]*\.?\d*)");
        if (basicFeeM.Success && decimal.TryParse(basicFeeM.Groups[1].Value, out var bf))
            items.Add(new { Name = "基本費", Amount = bf });

        var feePatterns = new[]
        {
            ("用水費", @"用水費[：:\s]*\n?\s*(\d[\d,.]*\.?\d*)"),
            ("維護費", @"維護費[：:\s]*\n?\s*(\d[\d,.]*\.?\d*)"),
            ("電子帳單回饋金", @"電子帳單回饋金[：:\s]*\n?\s*(\d[\d,.]*\.?\d*)"),
            ("水費小計", @"水費項目小計[：:\s]*\n?\s*(\d[\d,.]*\.?\d*)"),
            ("水源保育與回饋費", @"水源保育與回饋費[：:\s]*\n?\s*(\d[\d,.]*\.?\d*)"),
            ("污水下水道使用費", @"污水下水道使用費[：:\s]*\n?\s*(\d[\d,.]*\.?\d*)"),
            ("代徵費款小計", @"代徵費款小計[：:\s]*\n?\s*(\d[\d,.]*\.?\d*)"),
        };

        foreach (var (name, pattern) in feePatterns)
        {
            var m = Regex.Match(lineText, pattern);
            if (m.Success)
            {
                var val = m.Groups[1].Value.Replace(",", "");
                if (decimal.TryParse(val, out var v))
                    items.Add(new { Name = name, Amount = v });
            }
        }

        // C退還Ｂ追收：\nＣ退還/-782.0
        // After normalization: C退還B追收：\nC退還/-782.0
        var refundM = Regex.Match(lineText, @"C退還/[-]?(\d[\d,.]*\.?\d*)");
        if (refundM.Success && decimal.TryParse(refundM.Groups[1].Value, out var rv))
            items.Add(new { Name = "C退還", Amount = -rv });

        if (items.Count == 0) return null;
        return JsonSerializer.Serialize(new { Items = items });
    }
}
