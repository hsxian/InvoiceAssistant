using Docnet.Core.Models;
using Docnet.Core.Readers;
using OpenCvSharp;
using SkiaSharp;

namespace InvoiceAssistant.Core.Service.Processors;

public interface IPdfProcessor : IDisposable
{
    Task<List<SKBitmap>> ExtractImages(string filePath, int dpi = 300);
    Task<List<Mat>> ExtractMatImages(string filePath, int dpi = 300);
    /// <summary>
    /// 取整份 PDF 的页面位图（带缓存）。OCR 回退时同一份文件只渲染一次。
    /// </summary>
    Task<List<Mat>> GetMatImages(string filePath, int dpi = 300);
    Task<List<string>> ExtractText(string filePath, int dpi = 300);
    /// <summary>
    /// 获取 PDF 各页快照（文本层字符 + 尺寸 + 整页文本）。
    /// 内部按文件路径与修改时间缓存，同一份文件多次调用只解析一次。
    /// </summary>
    Task<IReadOnlyList<PdfPageSnapshot>> GetPageSnapshots(string filePath, int dpi = 300);
    IEnumerable<Character> FilterContains(IEnumerable<Character> characters, BoundBox bound);
    IEnumerable<Character> FilterIntersect(IEnumerable<Character> characters, BoundBox bound);
    Task ForeachPdfPage(string filePath, Action<IPageReader> action, int dpi = 300);
    string GetText(IEnumerable<Character> characters, BoundBox bound);
}
