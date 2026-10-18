using System.Text.RegularExpressions;
using InvoiceAssistant.Core.Data;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;
using Sdcb.PaddleOCR.Models.Local;

namespace InvoiceAssistant.Core.Service.ExtractUnits;

public class PaddleOCRImageInfoExtractUnit : IImageInfoExtractUnit
{
    private static readonly System.Reflection.PropertyInfo[] Props =
        typeof(InvoiceInfo).GetProperties();

    readonly PaddleOcrAll paddleOcrAll;
    private readonly ILogger<PaddleOCRImageInfoExtractUnit> logger;

    // ---- 缓存 ----
    // 一张图在一次识别流程中会被多份配置反复判定。全图 OCR 只能做一次，后续配置复用识别结果，
    // 并按配置区域从结果中筛文本，避免“每个 Xtractor 都裁剪子图再跑一次 OCR”。
    private readonly object _cacheLock = new();
    private string? _cachedPath;
    private DateTime _cachedStamp;
    private Mat? _cachedMat;          // 由本缓存持有，切换文件时释放
    private Mat? _ocrMat;             // OCR 结果对应的 Mat（引用比较）
    private OcrSnapshot? _ocr;

    /// <summary>
    /// 一次 OCR 的完整结果。Regions 必须物化成 List：
    /// PaddleOcrResult.Regions 底层是一次性迭代器，缓存后二次枚举会变空，
    /// 曾导致“按区域筛文本永远为空 → 反复裁剪重识别 → 单页 78 秒”。
    /// </summary>
    private sealed class OcrSnapshot
    {
        public required IReadOnlyList<PaddleOcrResultRegion> Regions { get; init; }
        public required string Text { get; init; }
    }

    public PaddleOCRImageInfoExtractUnit(ILogger<PaddleOCRImageInfoExtractUnit> logger)
    {
        FullOcrModel model = LocalFullModels.ChineseV5;
        paddleOcrAll = new(model, PaddleDevice.Mkldnn())
        {
            AllowRotateDetection = false, /* 允许识别有角度的文字 */
            Enable180Classification = false, /* 允许识别旋转角度大于90度的文字 */
        };
        this.logger = logger;
    }

    public void Dispose()
    {
        _cachedMat?.Dispose();
        _cachedMat = null;
        paddleOcrAll.Dispose();
    }

    private static DateTime SafeGetLastWriteTime(string filePath)
    {
        try
        {
            return File.GetLastWriteTimeUtc(filePath);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// 同一文件只读盘一次；文件变化才重新加载。返回的 Mat 归缓存所有，调用方不要 Dispose。
    /// </summary>
    private Mat? GetOrLoadMat(string filePath)
    {
        var stamp = SafeGetLastWriteTime(filePath);
        lock (_cacheLock)
        {
            if (_cachedMat != null && _cachedPath == filePath && _cachedStamp == stamp)
            {
                return _cachedMat;
            }

            _cachedMat?.Dispose();
            _cachedMat = Cv2.ImRead(filePath, ImreadModes.Color);
            _cachedPath = filePath;
            _cachedStamp = stamp;

            // 图像换了，之前的 OCR 结果作废
            _ocrMat = null;
            _ocr = null;

            if (_cachedMat == null || _cachedMat.Empty())
            {
                logger.LogWarning("无法读取图片：{File}", filePath);
                _cachedMat?.Dispose();
                _cachedMat = null;
                return null;
            }
            return _cachedMat;
        }
    }

    /// <summary>
    /// 全图 OCR 只跑一次，结果按 Mat 引用缓存复用。
    /// </summary>
    private OcrSnapshot GetFullOcr(Mat mat)
    {
        if (_ocr != null && ReferenceEquals(_ocrMat, mat))
        {
            return _ocr;
        }
        var result = paddleOcrAll.Run(mat);
        var snapshot = new OcrSnapshot
        {
            Regions = result.Regions.ToList(),   // 立刻物化，避免一次性迭代器被耗尽
            Text = result.Text,
        };
        _ocrMat = mat;
        _ocr = snapshot;
        return snapshot;
    }

    private static (float L, float T, float R, float B) GetRoi(ExtractMetadata metadata, int w, int h)
    {
        return metadata.ScaleOrAbsolutePosition
            ? (w * metadata.Left, h * metadata.Top, w * metadata.Right, h * metadata.Bottom)
            : (metadata.Left, metadata.Top, metadata.Right, metadata.Bottom);
    }

    /// <summary>
    /// 从整页 OCR 结果中筛出落在配置区域内的文本，替代“裁剪子图再 OCR”。
    ///
    /// requireCenterInside = true 时只取“文字框中心落在区域内”的结果（严格），
    /// 可避免把紧贴区域边缘的相邻单元格（如表头）误算进来；取不到再放宽为“有交集”。
    /// </summary>
    private static string GetTextInRoi(IReadOnlyList<PaddleOcrResultRegion> regions, ExtractMetadata metadata, int w, int h,
        bool requireCenterInside = false)
    {
        var (l, t, r, b) = GetRoi(metadata, w, h);
        var hits = new List<PaddleOcrResultRegion>();
        foreach (var region in regions)
        {
            var c = region.Rect.Center;
            var halfW = region.Rect.Size.Width / 2f;
            var halfH = region.Rect.Size.Height / 2f;
            var rl = c.X - halfW;
            var rr = c.X + halfW;
            var rt = c.Y - halfH;
            var rb = c.Y + halfH;

            if (requireCenterInside)
            {
                if (c.X < l || c.X > r || c.Y < t || c.Y > b) continue;
            }
            else
            {
                // 包围盒不相交则跳过
                if (rr < l || rl > r || rb < t || rt > b) continue;
            }
            hits.Add(region);
        }
        if (hits.Count == 0) return string.Empty;
        return string.Join("\n", hits
            .OrderBy(x => x.Rect.Center.Y)
            .ThenBy(x => x.Rect.Center.X)
            .Select(x => x.Text));
    }

    private Rect GetRoiRect(Mat mat, ExtractMetadata metadata)
    {
        var rect = metadata.ScaleOrAbsolutePosition ?
        metadata.GetRoiRect(mat.Width, mat.Height) :
        metadata.GetRoiRect();
        var ret = Rect.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
        return ret;
    }

    private Mat GetSub(Mat mat, ExtractMetadata metadata)
    {
        var roiRect = GetRoiRect(mat, metadata!);
        // 配置坐标越界时钳制到图像范围内，避免 new Mat(mat, rect) 抛出异常
        var x = Math.Clamp(roiRect.X, 0, Math.Max(0, mat.Width - 1));
        var y = Math.Clamp(roiRect.Y, 0, Math.Max(0, mat.Height - 1));
        var w = Math.Min(roiRect.Width, mat.Width - x);
        var h = Math.Min(roiRect.Height, mat.Height - y);
        if (w <= 0 || h <= 0)
        {
            // 区域无效时退回整图，保证不中断
            return new Mat(mat, new Rect(0, 0, mat.Width, mat.Height));
        }
        return new Mat(mat, new Rect(x, y, w, h));
    }

    private static bool MatchRegex(string txt, string pattern)
    {
        // Singleline：区域文本可能由多行拼接而成，让 "." 能跨行（"至(.+)$" 这类写法才可靠）
        return !string.IsNullOrWhiteSpace(txt)
            && Regex.IsMatch(txt, pattern, RegexOptions.Singleline);
    }

    /// <summary>
    /// 取一个区域内的文字：先严格（文字框中心在区域内）、再放宽（有交集）、最后裁剪子图单独 OCR。
    ///
    /// 关键细节：OCR 会把同一单元格的文字拆成多行（"2026-04-" / "19至2026-" / "04-30"），
    /// 因此匹配前必须先把换行去掉，否则日期、地名这类连续文本永远拼不起来。
    /// 裁剪兜底只在“区域内完全没有文字”时才做，避免为每份不相干的单据反复跑 OCR。
    /// </summary>
    private string GetRegionText(IReadOnlyList<PaddleOcrResultRegion> regions, Mat mat, ExtractMetadata metadata,
        string pattern, string? label, Func<bool>? allowCrop = null)
    {
        var strict = Normalize(GetTextInRoi(regions, metadata, mat.Width, mat.Height, true));
        if (MatchRegex(strict, pattern))
        {
            LogRegion(label, metadata, mat, strict, "中心命中");
            return strict;
        }

        var loose = Normalize(GetTextInRoi(regions, metadata, mat.Width, mat.Height));
        if (MatchRegex(loose, pattern))
        {
            LogRegion(label, metadata, mat, loose, "相交命中");
            return loose;
        }

        if (!string.IsNullOrWhiteSpace(loose))
        {
            // 区域里确实识别出了字，只是不符合该配置的正则 —— 说明区域/正则与版式不匹配，
            // 再裁剪一次也不会有不同结果，直接返回让上层判定“未命中”。
            LogRegion(label, metadata, mat, loose, "未匹配");
            return loose;
        }

        if (allowCrop == null || !allowCrop())
        {
            // 整页已识别出充分文字，空白区域大概率真的没有内容，不做昂贵的裁剪重识别
            return string.Empty;
        }

        using var sub = GetSub(mat, metadata);
        var crop = Normalize(paddleOcrAll.Run(sub).Text);
        logger.LogDebug("[OCR-裁剪] {Label} → {Text}", label, OneLine(crop));
        return crop;
    }

    /// <summary>OCR 文本归一化：去掉换行与首尾空白。</summary>
    private static string Normalize(string? s)
        => string.IsNullOrEmpty(s) ? string.Empty : s.ReplaceLineEndings("").Trim();

    private static string OneLine(string? s)
        => string.IsNullOrEmpty(s) ? "" : s.ReplaceLineEndings("⏎");

    private void LogRegion(string? label, ExtractMetadata metadata, Mat mat, string txt, string how)
    {
        if (!logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug)) return;
        var rect = metadata.ScaleOrAbsolutePosition
            ? metadata.GetRoiRect(mat.Width, mat.Height)
            : metadata.GetRoiRect();
        logger.LogDebug("[OCR-区域] {Label} {How} rect=({L},{T},{W}x{H}) → {Text}",
            label, how, rect.Left, rect.Top, rect.Width, rect.Height, OneLine(txt));
    }

    private Mat? _cropMat;
    private int _cropCount;

    /// <summary>
    /// 单个页面限制裁剪重识别的次数。返回 false 表示本页配额已用完，直接按“区域无文字”处理。
    /// </summary>
    private bool TryReserveCrop(Mat mat)
    {
        if (!ReferenceEquals(_cropMat, mat))
        {
            _cropMat = mat;
            _cropCount = 0;
        }
        if (_cropCount >= ExtractionOptions.MaxCropFallbacksPerPage) return false;
        _cropCount++;
        return true;
    }

    public async Task<InvoiceInfo?> ExtractByImg(Mat mat, ProcessConfig processConfig)
    {
        if (mat == null || mat.Empty()) return null;
        InvoiceInfo? ret = null;

        var result = GetFullOcr(mat);
        // 用“整页识别出的字符数”而不是文字块数量判断：Paddle 有时会把整张表单合成一个大块，
        // 块数少但内容其实已经读出来了，此时再裁剪重识别纯属浪费。
        var pageChars = result.Regions.Sum(r => r.Text?.Length ?? 0);
        var matcherAllowCrop = pageChars < 20;
        logger.LogDebug("[OCR] {Title} 整页识别 {Count} 块 / {Chars} 字，允许裁剪={Allow}",
            processConfig.Title, result.Regions.Count(), pageChars, matcherAllowCrop);

        // Matcher：先严格、再放宽；只有整页几乎没读出字时才裁剪
        var txt = GetRegionText(result.Regions, mat, processConfig.Matcher!, processConfig.Matcher!.RegexPattern!,
            "Matcher", () => matcherAllowCrop);
        if (!MatchRegex(txt, processConfig.Matcher!.RegexPattern!))
        {
            return ret;
        }
        logger.LogDebug("{Pattern} -> {Text}", processConfig.Matcher!.RegexPattern, OneLine(txt));
        ret = new InvoiceInfo();
        if (processConfig.Xtractors != null)
        {
            var xtractors = processConfig.Xtractors.Where(t => t.Enable != false).ToList();
            foreach (var xtractor in xtractors)
            {
                var prop = xtractor.RegexMapToProperties?.FirstOrDefault()?.PropertyName ?? "?";
                // 裁剪重识别只对“小区域”开放：大区域的裁剪相当于再跑一遍整页 OCR（约 10 秒）
                var areaRatio = Math.Abs((xtractor.Right - xtractor.Left) * (xtractor.Bottom - xtractor.Top));
                txt = GetRegionText(result.Regions, mat, xtractor, xtractor.RegexPattern!, prop,
                    () => areaRatio <= 0.2 && TryReserveCrop(mat));
                if (string.IsNullOrWhiteSpace(txt)) continue;
                var match = Regex.Match(txt, xtractor.RegexPattern!, RegexOptions.Singleline);
                if (!match.Success) continue;
                if (false == ret.SetValue(Props, match, xtractor.RegexMapToProperties!))
                {
                    logger.LogDebug("{Title} set value failed", processConfig.Title);
                }
            }
        }
        await Task.CompletedTask;
        return ret;
    }

    public async Task<InvoiceInfo?> ExtractByImg(string filePath, ProcessConfig processConfig)
    {
        var mat = GetOrLoadMat(filePath);
        if (mat == null) return null;
        return await ExtractByImg(mat, processConfig);
    }

    public async Task<InvoiceInfo?> ExtractByImgOnlyText(Mat mat, ProcessConfig processConfig)
    {
        if (mat == null || mat.Empty()) return null;
        var result = GetFullOcr(mat);
        InvoiceInfo? ret = null;
        var txt = result.Text.Replace(" ", "");
        if (!MatchRegex(txt, processConfig.Matcher!.RegexPattern!))
        {
            return ret;
        }
        logger.LogDebug("{Pattern} -> [all text]", processConfig.Matcher!.RegexPattern);
        ret = new InvoiceInfo();
        if (processConfig.Xtractors != null)
        {
            var xtractors = processConfig.Xtractors.Where(t => t.Enable != false).ToList();
            foreach (var xtractor in xtractors)
            {
                var match = Regex.Match(txt, xtractor.RegexPattern!, RegexOptions.Singleline);
                if (!match.Success) continue;
                if (false == ret.SetValue(Props, match, xtractor.RegexMapToProperties!))
                {
                    logger.LogDebug("{Title} set value failed", processConfig.Title);
                }
            }
        }
        await Task.CompletedTask;
        return ret;
    }

    public async Task<InvoiceInfo?> ExtractByImgOnlyText(string filePath, ProcessConfig processConfig)
    {
        var mat = GetOrLoadMat(filePath);
        if (mat == null) return null;
        return await ExtractByImgOnlyText(mat, processConfig);
    }

    public async Task OutputTestResult(Mat mat)
    {
        var result = GetFullOcr(mat);
        foreach (var region in result.Regions)
        {
            Console.WriteLine($"Text: {region.Text}, Score: {region.Score}, RectCenter: {region.Rect.Center}, RectSize:    {region.Rect.Size}, Angle: {region.Rect.Angle}");
        }
        await Task.CompletedTask;
    }
}
