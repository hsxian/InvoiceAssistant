using System.Reflection;
using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.ExtractUnits;
using InvoiceAssistant.Core.Service.Processors;
using Microsoft.Extensions.Logging;

namespace InvoiceAssistant.Core.Service;

/// <summary>
/// PDF 的统一抽取策略：文本层优先，必要时回退 OCR。
///
/// 决策依据不再是“整份 PDF 有没有字”，而是“配置关心的那块区域有没有可用文字”：
/// ① 区域有可读文字 → 走文本层；抽到的字段齐全就直接返回（毫秒级）。
/// ② 区域有可读文字但字段不全 → 再跑一次 OCR，取字段更全的结果。
/// ③ 区域文本为空（扫描件）或大量乱码（坏字体） → 直接 OCR。
/// ④ 区域文本可读、但正则不匹配 → 判定“不是这类单据”，不做昂贵的 OCR。
///
/// 这样既能补上“扫描件必须 OCR”的漏单，又不会让电子版 PDF 白白多付 OCR 的代价。
/// </summary>
public static class PdfHybridExtraction
{
    private static readonly PropertyInfo[] Props = typeof(InvoiceInfo).GetProperties();

    public static async Task<InvoiceInfo?> Extract(
        IPdfProcessor pdfProcessor,
        IPdfInfoExtractUnit pdfUnit,
        IImageInfoExtractUnit? imgUnit,
        ILogger logger,
        string filePath,
        ProcessConfig cfg)
    {
        var name = Path.GetFileName(filePath);
        var expected = ExpectedFields(cfg);

        // ① 先判断文本层在“配置关心的区域”里是否真的可用
        var textUsable = false;
        if (ExtractionOptions.EnableTextLayer && cfg.Matcher != null)
        {
            var pages = await pdfProcessor.GetPageSnapshots(filePath);
            textUsable = PdfTextRegion.HasUsableText(
                pages, cfg.Matcher,
                ExtractionOptions.MinRoiReadableChars,
                ExtractionOptions.MinReadableRatio);
        }

        InvoiceInfo? byText = null;
        if (textUsable)
        {
            byText = await pdfUnit.ExtractByPdf(filePath, cfg);
            if (byText != null)
            {
                byText.ExtractSource = "text";
                var filled = FilledCount(byText, expected);
                if (filled >= expected.Count)
                {
                    logger.LogDebug("[text] {File} →「{Title}」字段 {Filled}/{Expected}",
                        name, cfg.Title, filled, expected.Count);
                    return byText;
                }
                logger.LogDebug("[text] {File} →「{Title}」字段不全 {Filled}/{Expected}，尝试 OCR 兜底",
                    name, cfg.Title, filled, expected.Count);
            }
            else
            {
                // 区域文字可读却匹配不上：绝大多数情况是“这份单据不属于这类”，
                // OCR 不会让正则突然匹配，所以这里不做渲染，避免为每张不相干的单据付 OCR 成本。
                logger.LogDebug("[text] {File} 区域文字可读但未匹配「{Title}」，跳过", name, cfg.Title);
                return null;
            }
        }
        else if (ExtractionOptions.EnableTextLayer)
        {
            logger.LogDebug("[ocr] {File} 文本层不可用（扫描件或乱码），改走 OCR", name);
        }

        // ② 需要 OCR：没有 OCR 能力（纯文本抽取器）或显式关闭时，返回已有的部分结果
        if (imgUnit == null || !ExtractionOptions.EnableOcrFallback)
        {
            return byText;
        }

        var byOcr = await ExtractByOcr(pdfProcessor, imgUnit, logger, filePath, cfg);
        if (byOcr != null)
        {
            byOcr.ExtractSource = "ocr";
            logger.LogDebug("[ocr] {File} →「{Title}」字段 {Filled}/{Expected}",
                name, cfg.Title, FilledCount(byOcr, expected), expected.Count);
        }

        if (byText == null) return byOcr;
        if (byOcr == null) return byText;
        if (!ExtractionOptions.PreferMoreCompleteResult) return byText;

        // 两条路都有结果时取字段更全的；字段数相同优先文本层（没有识别误差）
        return FilledCount(byOcr, expected) > FilledCount(byText, expected) ? byOcr : byText;
    }

    private static async Task<InvoiceInfo?> ExtractByOcr(
        IPdfProcessor pdfProcessor,
        IImageInfoExtractUnit imgUnit,
        ILogger logger,
        string filePath,
        ProcessConfig cfg)
    {
        var imgs = await pdfProcessor.GetMatImages(filePath, ExtractionOptions.RenderDpi);
        logger.LogDebug("[ocr] {File} 渲染页数 {Count}", Path.GetFileName(filePath), imgs.Count);
        foreach (var img in imgs)
        {
            // 空白页（如审批单的尾页）直接跳过：识别不出内容，还会触发一次裁剪重识别
            if (PdfImageUtils.IsNearlyBlank(img))
            {
                logger.LogDebug("[ocr] {File} 跳过空白页", Path.GetFileName(filePath));
                continue;
            }
            var ret = await imgUnit.ExtractByImg(img, cfg);
            if (ret != null) return ret;
        }
        return null;
    }

    /// <summary>配置期望抽取到的字段名（去重）。用于衡量一条结果“抽全了没有”。</summary>
    public static List<string> ExpectedFields(ProcessConfig cfg)
    {
        var list = new List<string>();
        if (cfg.Xtractors == null) return list;
        foreach (var xt in cfg.Xtractors)
        {
            if (xt.Enable == false || xt.RegexMapToProperties == null) continue;
            foreach (var map in xt.RegexMapToProperties)
            {
                var n = map.PropertyName;
                if (!string.IsNullOrWhiteSpace(n) && !list.Contains(n)) list.Add(n);
            }
        }
        return list;
    }

    /// <summary>一条结果里实际填上的字段数（字符串非空、日期非默认、金额非 0）。</summary>
    public static int FilledCount(InvoiceInfo info, IReadOnlyList<string> expected)
    {
        var n = 0;
        foreach (var name in expected)
        {
            var p = Props.FirstOrDefault(t => t.Name == name);
            if (p == null) continue;
            var v = p.GetValue(info);
            if (p.PropertyType == typeof(string))
            {
                if (!string.IsNullOrWhiteSpace(v as string)) n++;
            }
            else if (p.PropertyType == typeof(DateTime))
            {
                if (v is DateTime d && d != default) n++;
            }
            else if (p.PropertyType == typeof(double))
            {
                if (v is double d && d != 0) n++;
            }
        }
        return n;
    }

    public static string Describe(InvoiceInfo? info, ProcessConfig cfg)
    {
        if (info == null) return "未识别";
        var expected = ExpectedFields(cfg);
        return $"{cfg.Title}（{info.ExtractSource ?? "?"}，字段 {FilledCount(info, expected)}/{expected.Count}）";
    }
}
