using System.Text.RegularExpressions;
using HSPAS.Api.Services.Interfaces;
using Tesseract;

namespace HSPAS.Api.Services;

public class HealthReportOcrService : IHealthReportOcrService
{
    private readonly ILogger<HealthReportOcrService> _logger;
    private readonly string _tessDataPath;

    // 每項檢驗的合理數值範圍（排除 OCR 亂碼數字）
    private static readonly Dictionary<string, (decimal min, decimal max)> ValueRanges = new()
    {
        ["TCholesterol"] = (80, 400),
        ["Triglyceride"] = (30, 1000),
        ["HDL"] = (10, 120),
        ["SGPT"] = (3, 300),
        ["Creatinine"] = (0.1m, 20),
        ["UricAcid"] = (1, 15),
        ["MDRD_EGFR"] = (10, 200),
        ["CKDEPI_EGFR"] = (10, 200),
        ["AcSugar"] = (40, 600),
        ["Hba1c"] = (3, 20),
    };

    public HealthReportOcrService(ILogger<HealthReportOcrService> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _tessDataPath = Path.Combine(env.ContentRootPath, "tessdata");
    }

    public Task<OcrParseResult> ParseImageAsync(Stream imageStream, string fileName, CancellationToken ct = default)
    {
        var result = new OcrParseResult();

        try
        {
            using var ms = new MemoryStream();
            imageStream.CopyTo(ms);
            var imageBytes = ms.ToArray();

            // 1. 四方向旋轉找最佳角度
            var bestAngle = FindBestRotation(imageBytes);
            _logger.LogInformation("OCR 最佳旋轉角度：{Angle}°", bestAngle);
            var rotatedBytes = RotateImage(imageBytes, bestAngle);

            // 2. 多 PSM 模式辨識，合併取聯集
            var engText3 = RunOcr(rotatedBytes, "eng", PageSegMode.Auto);
            var engText6 = RunOcr(rotatedBytes, "eng", PageSegMode.SingleBlock);
            var chiText3 = RunOcr(rotatedBytes, "chi_tra", PageSegMode.Auto);
            var chiText6 = RunOcr(rotatedBytes, "chi_tra", PageSegMode.SingleBlock);

            var engText = engText3 + "\n" + engText6;
            var chiText = chiText3 + "\n" + chiText6;
            var combinedText = engText + "\n" + chiText;
            result.RawText = combinedText;

            _logger.LogInformation("OCR ENG（前 600 字）：\n{T}", Truncate(engText3, 600));

            // 3. 逐行解析（核心）
            result.Values = ParseByLine(combinedText);

            // 4. 異常旗標
            result.Flags = new QuarterReportFlags
            {
                TriglycerideHigh = result.Values.Triglyceride > 150,
                HDLLow = result.Values.HDL < 40,
                AcSugarHigh = result.Values.AcSugar > 100,
                Hba1cHigh = result.Values.Hba1c > 5.6m
            };

            result.DetectedReportDate = DetectReportDate(combinedText);
            result.DetectedHospitalName = DetectHospitalName(chiText);
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR 解析失敗：{FileName}", fileName);
            result.Success = false;
            result.ErrorMessage = $"OCR 解析失敗：{ex.Message}";
        }

        return Task.FromResult(result);
    }

    // ==================== 逐行解析（核心邏輯） ====================

    /// <summary>
    /// 逐行掃描 OCR 文字，找含關鍵字的行，從該行提取合理範圍內的數值。
    /// 避免非貪婪 regex 跨行抓到 OCR 亂碼數字。
    /// </summary>
    private QuarterReportValues ParseByLine(string allText)
    {
        var values = new QuarterReportValues();
        var lines = allText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length < 3) continue;

            // T-Cholesterol
            if (values.TCholesterol == null && IsMatch(line, "T-Cholesterol", "T.Cholesterol", "總膽固醇"))
                values.TCholesterol = PickBestNumber(line, "TCholesterol");

            // Triglyceride
            if (values.Triglyceride == null && IsMatch(line, "Triglyceride", "三酸甘油脂", "三酸甘油酯"))
                values.Triglyceride = PickBestNumber(line, "Triglyceride");

            // HDL
            if (values.HDL == null && IsMatch(line, "HDL", "高密度") && !IsMatch(line, "Cholesterol", "T-Cho", "T.Cho"))
                values.HDL = PickBestNumber(line, "HDL");

            // SGPT (ALT)
            if (values.SGPT_ALT == null && IsMatch(line, "SGPT", "ALT", "GPT", "肝功能", "麩丙酮"))
                values.SGPT_ALT = PickBestNumber(line, "SGPT");

            // Creatinine
            if (values.Creatinine == null && IsMatch(line, "Creatinine", "肌酸酐", "肌酐"))
                values.Creatinine = PickBestNumber(line, "Creatinine");

            // Uric Acid
            if (values.UricAcid == null && IsMatch(line, "Uric", "尿酸", "尿 酸"))
                values.UricAcid = PickBestNumber(line, "UricAcid");

            // MDRD eGFR
            if (values.MDRD_EGFR == null && IsMatch(line, "MDRD") && !IsMatch(line, "CKD"))
                values.MDRD_EGFR = PickBestNumber(line, "MDRD_EGFR");

            // CKD-EPI eGFR
            if (values.CKDEPI_EGFR == null && IsMatch(line, "CKD", "CKD-EPI", "CKD.EPI"))
                values.CKDEPI_EGFR = PickBestNumber(line, "CKDEPI_EGFR");

            // AC SUGAR
            if (values.AcSugar == null && IsMatch(line, "AC SUGAR", "ACSUGAR", "AC S", "Ac s", "飯前血糖", "空腹血糖"))
                values.AcSugar = PickBestNumber(line, "AcSugar");

            // HbA1c
            if (values.Hba1c == null && IsMatch(line, "HbA1c", "HBA1C", "HbA1", "HBA 1", "HBAi", "糖化血色素", "種化血色素", "薩化血色素"))
                values.Hba1c = PickBestNumber(line, "Hba1c");
        }

        LogParsedValues(values);
        return values;
    }

    /// <summary>行內是否含有任一關鍵字（不區分大小寫）</summary>
    private static bool IsMatch(string line, params string[] keywords)
    {
        foreach (var kw in keywords)
        {
            if (line.Contains(kw, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 從一行中提取所有數值，選出落在合理範圍內的最佳值。
    /// 策略：優先選有小數點的數值（檢驗值通常有小數），再選整數。
    /// </summary>
    private decimal? PickBestNumber(string line, string itemKey)
    {
        if (!ValueRanges.TryGetValue(itemKey, out var range))
            return null;

        // 把全形句點 ﹒ 和全形逗號替換為半形
        var normalized = line
            .Replace('﹒', '.')
            .Replace('．', '.')
            .Replace('﹐', ',');

        // 提取所有數字（含小數）
        var matches = Regex.Matches(normalized, @"(\d+\.\d+|\d+)");
        var candidates = new List<(decimal value, bool hasDecimal)>();

        foreach (Match m in matches)
        {
            if (decimal.TryParse(m.Value, out var num) && num >= range.min && num <= range.max)
            {
                candidates.Add((num, m.Value.Contains('.')));
            }
        }

        if (candidates.Count == 0) return null;

        // 優先選有小數點的（檢驗值通常有小數位），否則選第一個合理值
        var withDecimal = candidates.Where(c => c.hasDecimal).ToList();
        var picked = withDecimal.Count > 0 ? withDecimal[0].value : candidates[0].value;

        _logger.LogInformation("  {Item}: 候選={Cands} → 選定={Picked}",
            itemKey,
            string.Join(", ", candidates.Select(c => c.value)),
            picked);

        return picked;
    }

    private void LogParsedValues(QuarterReportValues v)
    {
        _logger.LogInformation(
            "解析結果: T-Chol={TC}, TG={TG}, HDL={HDL}, SGPT={SGPT}, Cr={Cr}, UA={UA}, MDRD={MDRD}, CKD={CKD}, AcS={AcS}, A1c={A1c}",
            v.TCholesterol, v.Triglyceride, v.HDL, v.SGPT_ALT,
            v.Creatinine, v.UricAcid, v.MDRD_EGFR, v.CKDEPI_EGFR,
            v.AcSugar, v.Hba1c);
    }

    // ==================== 影像前處理 ====================

    private int FindBestRotation(byte[] imageBytes)
    {
        var scores = new Dictionary<int, int>();

        foreach (var angle in new[] { 0, 90, 180, 270 })
        {
            var rotated = RotateImage(imageBytes, angle);
            var text = RunOcr(rotated, "eng", PageSegMode.Auto);
            var score = CountAlphanumeric(text);
            scores[angle] = score;
            _logger.LogInformation("旋轉 {Angle}° 評分={Score}", angle, score);
        }

        var score0 = scores[0];
        var bestAngle = 0;
        var bestScore = score0;

        // 只有在其他角度的分數顯著優於 0°（超過 20%）時才旋轉
        foreach (var kvp in scores)
        {
            if (kvp.Key == 0) continue;
            if (kvp.Value > bestScore && kvp.Value > score0 * 1.2)
            {
                bestScore = kvp.Value;
                bestAngle = kvp.Key;
            }
        }

        return bestAngle;
    }

    private byte[] RotateImage(byte[] imageBytes, int angle)
    {
        if (angle == 0) return imageBytes;

        using var pix = Pix.LoadFromMemory(imageBytes);
        Pix rotated;

        switch (angle)
        {
            case 90:
                rotated = pix.Rotate90(1);
                break;
            case 180:
                var tmp = pix.Rotate90(1);
                rotated = tmp.Rotate90(1);
                tmp.Dispose();
                break;
            case 270:
                rotated = pix.Rotate90(-1);
                break;
            default:
                return imageBytes;
        }

        using (rotated)
        {
            return PixToBytes(rotated);
        }
    }

    private byte[] PixToBytes(Pix pix)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"ocr_{Guid.NewGuid():N}.png");
        try
        {
            pix.Save(tempPath, ImageFormat.Png);
            return File.ReadAllBytes(tempPath);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private static int CountAlphanumeric(string text) => text.Count(char.IsLetterOrDigit);
    private static string Truncate(string s, int max) => s.Length > max ? s[..max] : s;

    // ==================== OCR 執行 ====================

    private string RunOcr(byte[] imageBytes, string language, PageSegMode psm)
    {
        try
        {
            using var engine = new TesseractEngine(_tessDataPath, language, EngineMode.Default);
            using var pix = Pix.LoadFromMemory(imageBytes);
            using var page = engine.Process(pix, psm);
            return page.GetText();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OCR {Lang} PSM={Psm} 失敗", language, psm);
            return string.Empty;
        }
    }

    // ==================== 日期 / 院所偵測 ====================

    private static string? DetectReportDate(string text)
    {
        var m = Regex.Match(text, @"報[告吿]\s*日\s*期\D{0,10}?(\d{2,3})\s*[/\-\.]\s*(\d{1,2})\s*[/\-\.]\s*(\d{1,2})");
        if (m.Success) { var dt = ParseRocDate(m); if (dt != null) return dt; }

        // 就醫日期
        m = Regex.Match(text, @"就\s*醫\s*日\s*期\D{0,10}?(\d{2,3})\s*[/\-\.]\s*(\d{1,2})\s*[/\-\.]\s*(\d{1,2})");
        if (m.Success) { var dt = ParseRocDate(m); if (dt != null) return dt; }

        foreach (Match match in Regex.Matches(text, @"(\d{2,3})\s*[/\-]\s*(\d{1,2})\s*[/\-]\s*(\d{1,2})"))
        {
            var dt = ParseRocDate(match);
            if (dt != null) return dt;
        }

        var ad = Regex.Match(text, @"(20\d{2})[/\-](\d{1,2})[/\-](\d{1,2})");
        if (ad.Success)
            return $"{ad.Groups[1].Value}-{ad.Groups[2].Value.PadLeft(2, '0')}-{ad.Groups[3].Value.PadLeft(2, '0')}";

        return null;
    }

    private static string? ParseRocDate(Match match)
    {
        if (int.TryParse(match.Groups[1].Value, out var y) &&
            int.TryParse(match.Groups[2].Value, out var mon) &&
            int.TryParse(match.Groups[3].Value, out var day) &&
            y >= 80 && y <= 200 && mon >= 1 && mon <= 12 && day >= 1 && day <= 31)
        {
            try { return new DateTime(y + 1911, mon, day).ToString("yyyy-MM-dd"); } catch { }
        }
        return null;
    }

    private static string? DetectHospitalName(string chiText)
    {
        var patterns = new[]
        {
            @"(廖\s*內\s*科\S{0,6})",
            @"([\u4e00-\u9fff]{2,10}(?:醫院|診所|內科|外科|小兒科|家醫科|健檢中心))",
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(chiText, pattern);
            if (match.Success)
                return match.Groups[1].Value
                    .Replace(" ", "")
                    .TrimEnd('：', ':', ' ', '﹕');
        }

        return null;
    }
}
