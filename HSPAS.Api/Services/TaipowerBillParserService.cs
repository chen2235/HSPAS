using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace HSPAS.Api.Services;

public class TaipowerBillParserService
{
    public record ParsedBill(
        string Address,
        string PowerNo,
        string? BlackoutGroup,
        DateTime BillingStartDate,
        DateTime BillingEndDate,
        int BillingDays,
        string? BillingPeriodText,
        DateTime ReadOrDebitDate,
        int Kwh,
        decimal? KwhPerDay,
        decimal? AvgPricePerKwh,
        decimal TotalAmount,
        decimal? InvoiceAmount,
        string? TariffType,
        int? SharedMeterHouseholdCount,
        string? InvoicePeriod,
        string? InvoiceNo,
        string? RawDetailJson
    );

    public record ParseResult(bool Success, string? Error, ParsedBill? Bill);

    /// <summary>
    /// Extract raw text from PDF (for debugging).
    /// </summary>
    public string ExtractRawText(Stream pdfStream, string password = "0928284285")
    {
        var options = new ParsingOptions { Password = password };
        using var document = PdfDocument.Open(pdfStream, options);
        var textParts = new List<string>();
        for (int i = 1; i <= document.NumberOfPages; i++)
        {
            var p = document.GetPage(i);
            textParts.Add(string.Join(" ", p.GetWords().Select(w => w.Text)));
        }
        return string.Join(" ", textParts);
    }

    public ParseResult Parse(Stream pdfStream, string password = "0928284285")
    {
        try
        {
            var options = new ParsingOptions { Password = password };
            using var document = PdfDocument.Open(pdfStream, options);

            // Extract all text from all pages
            var textParts = new List<string>();
            for (int i = 1; i <= document.NumberOfPages; i++)
            {
                var p = document.GetPage(i);
                textParts.Add(string.Join(" ", p.GetWords().Select(w => w.Text)));
            }
            var text = string.Join(" ", textParts);

            // Parse each field using regex
            var address = ExtractAddress(text);
            var powerNo = ExtractPowerNo(text);
            var blackoutGroup = ExtractBlackoutGroup(text);
            var (startDate, endDate, days, periodText) = ExtractBillingPeriod(text);
            var readDate = ExtractReadOrDebitDate(text);
            var kwh = ExtractKwh(text);
            var kwhPerDay = ExtractKwhPerDay(text);
            var avgPrice = ExtractAvgPrice(text);
            var totalAmount = ExtractTotalAmount(text);
            var tariffType = ExtractTariffType(text);
            var sharedCount = ExtractSharedCount(text);
            var (invoicePeriod, invoiceNo, invoiceAmount) = ExtractInvoiceInfo(text);
            var rawDetailJson = ExtractDetailItems(text);

            // Validate required fields
            if (powerNo == null) return new ParseResult(false, "無法解析電號", null);
            if (startDate == null || endDate == null) return new ParseResult(false, "無法解析計費期間", null);
            if (readDate == null) return new ParseResult(false, "無法解析抄表/扣款日", null);

            var bill = new ParsedBill(
                Address: address ?? "新北市汐止區福山街60巷12號四樓",
                PowerNo: powerNo,
                BlackoutGroup: blackoutGroup,
                BillingStartDate: startDate.Value,
                BillingEndDate: endDate.Value,
                BillingDays: days ?? (endDate.Value - startDate.Value).Days,
                BillingPeriodText: periodText,
                ReadOrDebitDate: readDate.Value,
                Kwh: kwh ?? 0,
                KwhPerDay: kwhPerDay,
                AvgPricePerKwh: avgPrice,
                TotalAmount: totalAmount ?? 0,
                InvoiceAmount: invoiceAmount,
                TariffType: tariffType,
                SharedMeterHouseholdCount: sharedCount,
                InvoicePeriod: invoicePeriod,
                InvoiceNo: invoiceNo,
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

    /// <summary>
    /// Convert minguo date (YYY/MM/DD) to DateTime.
    /// </summary>
    private static DateTime? ParseMinguoDate(string s)
    {
        // Handle full-width digits too
        s = NormalizeFullWidth(s);
        var m = Regex.Match(s.Trim(), @"(\d{2,3})[/\-.](\d{1,2})[/\-.](\d{1,2})");
        if (!m.Success) return null;
        int y = int.Parse(m.Groups[1].Value) + 1911;
        int mo = int.Parse(m.Groups[2].Value);
        int d = int.Parse(m.Groups[3].Value);
        try { return new DateTime(y, mo, d); } catch { return null; }
    }

    /// <summary>
    /// Convert full-width ASCII characters (U+FF01–U+FF5E) to half-width,
    /// and ideographic space (U+3000) to normal space.
    /// </summary>
    private static string NormalizeFullWidth(string s)
    {
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

    private static string? ExtractAddress(string text)
    {
        text = NormalizeFullWidth(text);
        // Match: 用電地址：新北市汐止區福山街60巷12號 ... 四樓
        // The floor (四樓) may be separated by other text (e.g. "官 網 電 子 發 票 平 台 四樓")
        var m = Regex.Match(text, @"用電地址[：:\s]*([\u4e00-\u9fff\d]+(?:市|縣)[\u4e00-\u9fff\d]+(?:區|鎮|鄉)[\u4e00-\u9fff\d]+號)");
        if (m.Success)
        {
            var addr = m.Groups[1].Value.Trim();
            // Look for floor number within ~100 chars after the address
            var afterAddr = text.Substring(m.Index + m.Length, Math.Min(200, text.Length - m.Index - m.Length));
            var fm = Regex.Match(afterAddr, @"([\u4e00-\u9fff]+樓)");
            if (fm.Success) addr += fm.Groups[1].Value;
            return addr;
        }
        // Fallback: first address-like match
        m = Regex.Match(text, @"([\u4e00-\u9fff]+(?:市|縣)[\u4e00-\u9fff]+(?:區|鎮|鄉)[\u4e00-\u9fff\d]+號[\u4e00-\u9fff]*樓?)");
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static string? ExtractPowerNo(string text)
    {
        text = NormalizeFullWidth(text);
        var m = Regex.Match(text, @"(\d{2}-\d{2}-\d{4}-\d{2}-\d{1,2})");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string? ExtractBlackoutGroup(string text)
    {
        text = NormalizeFullWidth(text);
        var m = Regex.Match(text, @"輪流停電組別\s*([A-Z])");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static (DateTime? start, DateTime? end, int? days, string? periodText) ExtractBillingPeriod(string text)
    {
        text = NormalizeFullWidth(text);
        // Pattern: 計費期間：114/12/10至115/02/04 or with various separators
        var m = Regex.Match(text, @"計費期間[：:\s]*(\d{2,3}/\d{1,2}/\d{1,2})\s*(?:至|~|-|到)\s*(\d{2,3}/\d{1,2}/\d{1,2})");
        if (!m.Success)
        {
            // Try alternative: look for two dates near "計費期間"
            m = Regex.Match(text, @"(\d{2,3}/\d{1,2}/\d{1,2})至(\d{2,3}/\d{1,2}/\d{1,2})");
        }
        if (!m.Success) return (null, null, null, null);

        var start = ParseMinguoDate(m.Groups[1].Value);
        var end = ParseMinguoDate(m.Groups[2].Value);
        int? days = (start.HasValue && end.HasValue) ? (int)(end.Value - start.Value).TotalDays + 1 : null;

        // Try extract explicit days number from "本期 57 1221 21.42" pattern
        var dm = Regex.Match(text, @"用電日數[：:\s]*(\d+)");
        if (!dm.Success) dm = Regex.Match(text, @"本期\s+(\d{2,3})\s+\d{3,5}\s+\d+\.\d+");
        if (dm.Success) days = int.Parse(dm.Groups[1].Value);

        var periodText = m.Value;
        if (days.HasValue) periodText = $"{m.Groups[1].Value} 至 {m.Groups[2].Value}（共 {days} 天）";

        return (start, end, days, periodText);
    }

    private static DateTime? ExtractReadOrDebitDate(string text)
    {
        text = NormalizeFullWidth(text);
        // PdfPig text: "本次抄表日/扣款日：115/02/05；115/03/05"
        // The format has two dates separated by ；, the second one is the actual debit date
        var m = Regex.Match(text, @"本次抄表日/扣款日[：:\s]*(\d{2,3}/\d{1,2}/\d{1,2})\s*[；;]\s*(\d{2,3}/\d{1,2}/\d{1,2})");
        if (m.Success) return ParseMinguoDate(m.Groups[2].Value);
        // Single date pattern
        m = Regex.Match(text, @"本次抄表日/扣款日[：:\s]*(\d{2,3}/\d{1,2}/\d{1,2})");
        if (m.Success) return ParseMinguoDate(m.Groups[1].Value);
        // Fallback: 扣款日
        m = Regex.Match(text, @"扣款日[：:\s]*(\d{2,3}/\d{1,2}/\d{1,2})");
        if (m.Success) return ParseMinguoDate(m.Groups[1].Value);
        return null;
    }

    private static int? ExtractKwh(string text)
    {
        text = NormalizeFullWidth(text);
        // In the PDF text, the pattern is: "WR01-99010120****** 40 1221 8"
        // where 40=底度, 1221=計費度數(kWh), 8=分攤戶數
        // Look for a 3-4 digit number between smaller numbers after the meter info
        var m = Regex.Match(text, @"WR\d+-\d+\*+\s+\d+\s+(\d{3,5})\s+\d+");
        if (m.Success) return int.Parse(m.Groups[1].Value);

        // Try: look for 度數 and then find a 3-5 digit number nearby
        m = Regex.Match(text, @"經常度數\s+.*?公共用電分攤戶數\s+.*?\d+\s+(\d{3,5})\s+\d+");
        if (m.Success) return int.Parse(m.Groups[1].Value);

        // Fallback: pattern with 底度 number kwh number
        m = Regex.Match(text, @"\b(\d{3,5})\b\s+\d{1,2}\s+流動電費");
        if (m.Success) return int.Parse(m.Groups[1].Value);

        return null;
    }

    private static decimal? ExtractKwhPerDay(string text)
    {
        text = NormalizeFullWidth(text);
        // PdfPig text: "本期 57 1221 21.42" (days, kwh, kwhPerDay)
        var m = Regex.Match(text, @"本期\s+\d+\s+\d+\s+(\d+\.\d+)");
        if (m.Success && decimal.TryParse(m.Groups[1].Value, out var v)) return v;
        // Fallback: 日平均度數 pattern
        m = Regex.Match(text, @"日平均度數.*?(\d+\.\d+)");
        if (m.Success && decimal.TryParse(m.Groups[1].Value, out var v2)) return v2;
        return null;
    }

    private static decimal? ExtractAvgPrice(string text)
    {
        text = NormalizeFullWidth(text);
        // PdfPig text: "當期每度平均電價 2.72元"
        var m = Regex.Match(text, @"當期每度平均電價\s+(\d+\.\d+)\s*元");
        if (m.Success && decimal.TryParse(m.Groups[1].Value, out var v)) return v;
        // Fallback: last decimal+元 before 流動電費計算式
        m = Regex.Match(text, @"(\d+\.\d{1,2})\s*元\s+流動電費計算式");
        if (m.Success && decimal.TryParse(m.Groups[1].Value, out var v2)) return v2;
        return null;
    }

    private static decimal? ExtractDecimal(string text, string pattern)
    {
        text = NormalizeFullWidth(text);
        var m = Regex.Match(text, pattern);
        if (m.Success && decimal.TryParse(m.Groups[1].Value, out var v)) return v;
        return null;
    }

    private static decimal? ExtractTotalAmount(string text)
    {
        text = NormalizeFullWidth(text);
        // Try: ****3309 元 pattern (full-width stars + number) — most reliable
        var m = Regex.Match(text, @"[*]+\s*(\d[\d,]*)\s*元");
        if (m.Success)
        {
            var s = m.Groups[1].Value.Replace(",", "");
            if (decimal.TryParse(s, out var v)) return v;
        }
        // Pattern: 繳費總金額 then several amounts, the last one with comma is the total
        // "繳費總金額 3377.6 元 -3.3 元 29.0 元 -84.0 元 -10.0 元 3,309 元"
        m = Regex.Match(text, @"繳費總金額\s+.*?(\d[\d,]+)\s*元\s+載具");
        if (m.Success)
        {
            var s = m.Groups[1].Value.Replace(",", "");
            if (decimal.TryParse(s, out var v)) return v;
        }
        return null;
    }

    private static string? ExtractTariffType(string text)
    {
        text = NormalizeFullWidth(text);
        // PdfPig text: "電價種類： 表燈 非營業用" and "時間種類： 非時間電價"
        string? priceType = null, timeType = null;
        var m1 = Regex.Match(text, @"電價種類[：:\s]*(表燈\s*[\u4e00-\u9fff]+)");
        if (m1.Success) priceType = m1.Groups[1].Value.Trim();
        var m2 = Regex.Match(text, @"時間種類[：:\s]*([\u4e00-\u9fff]+電價)");
        if (m2.Success) timeType = m2.Groups[1].Value.Trim();
        if (priceType != null || timeType != null)
        {
            var parts = new List<string>();
            if (priceType != null) parts.Add(priceType);
            if (timeType != null) parts.Add(timeType);
            return string.Join(" ", parts);
        }
        // Fallback: direct match
        var m = Regex.Match(text, @"(表燈\s*[\u4e00-\u9fff\s]*(?:非時間電價|時間電價))");
        if (m.Success) return Regex.Replace(m.Groups[1].Value.Trim(), @"\s+", " ");
        return null;
    }

    private static int? ExtractSharedCount(string text)
    {
        text = NormalizeFullWidth(text);
        var m = Regex.Match(text, @"公共用電分攤戶數\s+(\d+)");
        if (m.Success) return int.Parse(m.Groups[1].Value);
        return null;
    }

    private static (string? period, string? no, decimal? amount) ExtractInvoiceInfo(string text)
    {
        text = NormalizeFullWidth(text);
        // 發票期別 and 發票號碼 are in a table-like structure
        // Pattern: 115年03-04月 ZD-31664747 3309
        string? period = null, no = null;
        decimal? amount = null;

        var m = Regex.Match(text, @"(\d{3}年\d{2}-\d{2}月)\s+([A-Z]{2}-\d+)\s+(\d+)");
        if (m.Success)
        {
            period = m.Groups[1].Value;
            no = m.Groups[2].Value;
            if (decimal.TryParse(m.Groups[3].Value, out var v)) amount = v;
        }
        else
        {
            // Try separate patterns
            var pm = Regex.Match(text, @"發票期別\s*([\d年\-月]+)");
            if (!pm.Success) pm = Regex.Match(text, @"(\d{3}年\d{2}-\d{2}月)");
            if (pm.Success) period = pm.Groups[1].Value;

            var nm = Regex.Match(text, @"([A-Z]{2}-\d{5,})");
            if (nm.Success) no = nm.Groups[1].Value;
        }

        return (period, no, amount);
    }

    private static string? ExtractDetailItems(string text)
    {
        text = NormalizeFullWidth(text);
        var items = new List<object>();

        // PdfPig text has items interleaved with amounts:
        // "流動電費 3377.6 元 ... 停電扣減金額 -3.3 元 ... 公共設施電費 29.0 元 ..."
        var feePatterns = new[]
        {
            ("流動電費", @"流動電費\s+(-?[\d,.]+)\s*元"),
            ("停電扣減金額", @"停電扣減金額\s+(-?[\d,.]+)\s*元"),
            ("公共設施電費", @"公共設施電費\s+(-?[\d,.]+)\s*元"),
            ("節電獎勵", @"節電獎勵\s+(-?[\d,.]+)\s*元"),
            ("電子帳單優惠減收金額", @"電子帳單優惠減收金額\s+(-?[\d,.]+)\s*元"),
            ("繳費總金額", @"繳費總金額\s+(-?[\d,.]+)\s*元"),
        };

        foreach (var (name, pattern) in feePatterns)
        {
            var m = Regex.Match(text, pattern);
            if (m.Success)
            {
                var val = m.Groups[1].Value.Replace(",", "");
                if (decimal.TryParse(val, out var v))
                {
                    items.Add(new { Name = name, Amount = v });
                }
            }
        }

        if (items.Count == 0) return null;
        return JsonSerializer.Serialize(new { Items = items });
    }
}
