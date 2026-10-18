namespace InvoiceAssistant.Core.Service;

/// <summary>
/// PDF 抽取策略开关：文本层与“渲染成图 + OCR”之间的自适应取舍。
///
/// 背景：电子版 PDF 的文本层又快又准，但扫描件（打印成 PDF、图片型 PDF）没有文本层，
/// 或文本层因字体内嵌/编码问题而不可读。固定“只用文本层”会漏单，固定“一律 OCR”会慢几十倍。
/// 因此默认策略是：能用文本层就用；文本层缺失/乱码/字段不全时才回退 OCR。
/// </summary>
public static class ExtractionOptions
{
    /// <summary>是否启用文本层路径（关闭后所有 PDF 直接走 OCR）。</summary>
    public static bool EnableTextLayer { get; set; } = true;

    /// <summary>文本层抽不到、或抽到但字段不全时，是否回退“渲染 + OCR”。</summary>
    public static bool EnableOcrFallback { get; set; } = true;

    /// <summary>OCR 渲染 DPI。300 兼顾表格类单据的行线清晰度；调低可提速但会掉识别率。</summary>
    public static int RenderDpi { get; set; } = 300;

    /// <summary>判定“该区域有可用文字”所需的最少可读字符数。</summary>
    public static int MinRoiReadableChars { get; set; } = 2;

    /// <summary>区域文本可读率低于该值时，视为“文本层乱码”，改走 OCR。</summary>
    public static double MinReadableRatio { get; set; } = 0.5;

    /// <summary>文本层与 OCR 都拿到结果时，字段更全者胜；字段数相同则优先文本层（更准）。</summary>
    public static bool PreferMoreCompleteResult { get; set; } = true;

    /// <summary>
    /// 单个页面最多允许几次“裁剪后单独 OCR”。
    /// 裁剪一块大区域等于又跑一遍 OCR（约 10 秒），必须设上限，否则遇到版式不符的配置会成倍放大耗时。
    /// </summary>
    public static int MaxCropFallbacksPerPage { get; set; } = 3;
}
