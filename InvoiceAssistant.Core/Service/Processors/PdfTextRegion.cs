using System.Globalization;
using Docnet.Core.Models;
using InvoiceAssistant.Core.Data;

namespace InvoiceAssistant.Core.Service.Processors;

/// <summary>
/// 文本层按配置区域取字、以及“该区域是否真的有可读文字”的判定。
/// 统一放在这里，文本抽取与 OCR 回退决策共用同一套坐标换算，避免两处口径不一致。
/// </summary>
public static class PdfTextRegion
{
    /// <summary>把配置里的归一化/绝对坐标换算成页面点坐标。</summary>
    public static BoundBox ToBoundBox(ExtractMetadata m, int pageWidth, int pageHeight)
    {
        return m.ScaleOrAbsolutePosition
            ? new BoundBox((int)(pageWidth * m.Left), (int)(pageHeight * m.Top),
                           (int)(pageWidth * m.Right), (int)(pageHeight * m.Bottom))
            : new BoundBox((int)m.Left, (int)m.Top, (int)m.Right, (int)m.Bottom);
    }

    /// <summary>与区域有交集的字符（保持 PDF 里的原始顺序）。</summary>
    public static IEnumerable<Character> CharactersIn(PdfPageSnapshot page, ExtractMetadata m)
    {
        var b = ToBoundBox(m, page.Width, page.Height);
        return page.Characters.Where(c =>
            c.Box.Left <= b.Right && c.Box.Right >= b.Left &&
            c.Box.Top <= b.Bottom && c.Box.Bottom >= b.Top);
    }

    /// <summary>区域文本：与区域有交集的字符按原序拼接并去掉换行。</summary>
    public static string GetText(PdfPageSnapshot page, ExtractMetadata m)
        => string.Concat(CharactersIn(page, m).Select(c => c.Char)).ReplaceLineEndings("").Trim();

    /// <summary>
    /// 该字符是否算“可读内容”。用于区分真实文字与乱码：
    /// 控制符、代理项、私用区、未分配码位、替换符、纯符号/空白都不计入。
    /// </summary>
    public static bool IsReadable(char ch)
    {
        if (ch == '\uFFFD' || char.IsWhiteSpace(ch)) return false;
        var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
        if (cat is UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.Surrogate
            or UnicodeCategory.PrivateUse or UnicodeCategory.OtherNotAssigned)
        {
            return false;
        }
        return char.IsLetterOrDigit(ch);
    }

    /// <summary>区域内可读字符数。</summary>
    public static int CountReadable(PdfPageSnapshot page, ExtractMetadata m)
        => CharactersIn(page, m).Count(c => IsReadable(c.Char));

    /// <summary>区域内“可读字符 / 非空白字符”的比例。无字符时返回 0。</summary>
    public static double ReadableRatio(PdfPageSnapshot page, ExtractMetadata m)
    {
        var chars = CharactersIn(page, m).Where(c => !char.IsWhiteSpace(c.Char)).ToList();
        if (chars.Count == 0) return 0;
        return chars.Count(c => IsReadable(c.Char)) / (double)chars.Count;
    }

    /// <summary>
    /// 页面上该区域是否存在可用文本。扫描件的文本层为空；坏字体的文本层则是大量乱码，
    /// 两种都判为“不可用”，交给 OCR。
    /// </summary>
    public static bool HasUsableText(PdfPageSnapshot page, ExtractMetadata m,
        int minReadableChars = 2, double minRatio = 0.5)
    {
        var readable = CountReadable(page, m);
        if (readable < minReadableChars) return false;
        return ReadableRatio(page, m) >= minRatio;
    }

    /// <summary>任一分页满足条件即认为文本层可用。</summary>
    public static bool HasUsableText(IEnumerable<PdfPageSnapshot> pages, ExtractMetadata m,
        int minReadableChars = 2, double minRatio = 0.5)
        => pages.Any(p => HasUsableText(p, m, minReadableChars, minRatio));
}
