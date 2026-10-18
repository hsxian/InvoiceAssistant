using Docnet.Core.Models;

namespace InvoiceAssistant.Core.Service.Processors;

/// <summary>
/// 单页 PDF 的一次性解析结果（文本层字符、页面尺寸、整页纯文本）。
/// 同一份 PDF 会被多份配置反复判定，缓存快照可避免重复打开/解析文档。
/// </summary>
public sealed class PdfPageSnapshot
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required IReadOnlyList<Character> Characters { get; init; }

    /// <summary>整页纯文本（Docnet 文本层）。扫描件通常为空。</summary>
    public required string Text { get; init; }
}
