using Docnet.Core;
using Docnet.Core.Models;
using Docnet.Core.Readers;
using Ghostscript.NET;
using Ghostscript.NET.Rasterizer;
using InvoiceAssistant.Core.Service.Images;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using SkiaSharp;
using System.Runtime.InteropServices;

namespace InvoiceAssistant.Core.Service.Processors;

public class PdfProcessor(ILogger<PdfProcessor> logger) : IPdfProcessor
{
    private readonly ILogger<PdfProcessor> logger = logger;

    protected IDocLib DocNet { get; } = DocLib.Instance;

    /// <summary>
    /// 是否把每个字符的坐标打印到控制台。默认关闭；开启后处理大批 PDF 会因巨量控制台 I/O 变得极慢。
    /// </summary>
    public static bool DumpCharacters { get; set; }

    // ---- 页面快照缓存：同一份文件在多份配置之间只解析一次 ----
    private readonly object _snapshotLock = new();
    private string? _snapshotPath;
    private DateTime _snapshotStamp;
    private IReadOnlyList<PdfPageSnapshot>? _snapshots;

    // ---- 栅格化缓存：OCR 回退时同一份 PDF 只渲染一次，多份配置共用 ----
    private readonly object _imgLock = new();
    private string? _imgPath;
    private DateTime _imgStamp;
    private int _imgDpi;
    private List<Mat>? _imgs;

    public async Task<IReadOnlyList<PdfPageSnapshot>> GetPageSnapshots(string filePath, int dpi = 300)
    {
        var stamp = SafeGetLastWriteTime(filePath);
        lock (_snapshotLock)
        {
            if (_snapshots != null && _snapshotPath == filePath && _snapshotStamp == stamp)
            {
                return _snapshots;
            }
        }

        var list = new List<PdfPageSnapshot>();
        using (var docReader = DocNet.GetDocReader(filePath, new PageDimensions(dpi / 72.0)))
        {
            var count = docReader.GetPageCount();
            for (int i = 0; i < count; i++)
            {
                using var pageReader = docReader.GetPageReader(i);
                var chars = pageReader.GetCharacters().ToList();
                if (DumpCharacters)
                {
                    foreach (var character in chars)
                    {
                        Console.WriteLine($"{character.Char} {character.Box.Left} {character.Box.Top} {character.Box.Right} {character.Box.Bottom}");
                    }
                }
                list.Add(new PdfPageSnapshot
                {
                    Width = pageReader.GetPageWidth(),
                    Height = pageReader.GetPageHeight(),
                    Characters = chars,
                    Text = pageReader.GetText(),
                });
            }
        }

        lock (_snapshotLock)
        {
            _snapshotPath = filePath;
            _snapshotStamp = stamp;
            _snapshots = list;
        }
        await Task.CompletedTask;
        return list;
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

    public void Dispose()
    {
        lock (_imgLock)
        {
            if (_imgs != null)
            {
                foreach (var m in _imgs) m.Dispose();
                _imgs = null;
            }
        }
        DocNet.Dispose();
    }

    /// <summary>
    /// 取（并缓存）整份 PDF 的页面位图，供 OCR 使用。
    /// 同一份文件在多个配置之间只渲染一次——Ghostscript 渲染是 OCR 路径里最贵的一步。
    /// </summary>
    public async Task<List<Mat>> GetMatImages(string filePath, int dpi = 300)
    {
        var stamp = SafeGetLastWriteTime(filePath);
        lock (_imgLock)
        {
            if (_imgs != null && _imgPath == filePath && _imgStamp == stamp && _imgDpi == dpi)
            {
                return _imgs;
            }
        }

        var imgs = await ExtractMatImages(filePath, dpi);
        lock (_imgLock)
        {
            if (_imgs != null)
            {
                foreach (var m in _imgs) m.Dispose();
            }
            _imgs = imgs;
            _imgPath = filePath;
            _imgStamp = stamp;
            _imgDpi = dpi;
        }
        return imgs;
    }

    public async Task<List<SKBitmap>> ExtractImages(string filePath, int dpi = 300)
    {
        var ret = new List<SKBitmap>();
        // 创建 Rasterizer 实例
        using var rasterizer = new GhostscriptRasterizer();
        // 初始化（自动查找系统 gs）
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var stream = File.Open(filePath, FileMode.Open);//Or go with using
            var lv = GhostscriptVersionInfo.GetLastInstalledVersion(GhostscriptLicense.GPL | GhostscriptLicense.AFPL, GhostscriptLicense.GPL);
            rasterizer.Open(stream, lv, true);
        }
        else
        {
            rasterizer.Open(filePath);
        }

        int pageCount = rasterizer.PageCount;

        // Ghostscript 的 PageCount 偶尔会比实际页数多（实测单页 PDF 报 2 页），
        // 多出来的空白页会让每份配置都白跑一次 OCR。用 Docnet 的真实页数取小值兜住。
        var realCount = GetDocPageCount(filePath);
        if (realCount > 0 && realCount < pageCount)
        {
            logger.LogDebug("{File} Ghostscript 报 {Gs} 页，实际 {Real} 页，按实际页数处理。",
                Path.GetFileName(filePath), pageCount, realCount);
            pageCount = realCount;
        }

        // 设置 DPI（建议 300+ 用于 OCR）

        for (int i = 1; i <= pageCount; i++)
        {
            // 渲染第 i 页为图像
            var img = rasterizer.GetPage(dpi, i);

            ret.Add(img);
        }
        await Task.CompletedTask;
        return ret;
    }
    /// <summary>用 Docnet 取真实页数；失败返回 0，表示“不确定，按 Ghostscript 的结果来”。</summary>
    private int GetDocPageCount(string filePath)
    {
        try
        {
            using var docReader = DocNet.GetDocReader(filePath, new PageDimensions(1));
            return docReader.GetPageCount();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "读取 PDF 页数失败：{File}", filePath);
            return 0;
        }
    }

    private List<byte[]> ExtractImages2(string filePath, int dpi = 300)    {
        using var docReader = DocNet.GetDocReader(filePath, new PageDimensions(dpi / 72.0));
        var count = docReader.GetPageCount();
        var ret = new List<byte[]>();
        for (int i = 0; i < count; i++)
        {
            using var pageReader = docReader.GetPageReader(i);
            pageReader.GetImage();
            var text = pageReader.GetImage();
            // logger.LogInformation(text);
            ret.Add(text);
        }
        return ret;
    }

    /// <summary>
    /// 提取整页纯文本。直接从页面快照复用，避免重复解析文档。
    /// </summary>
    public async Task<List<string>> ExtractText(string filePath, int dpi = 300)
    {
        var pages = await GetPageSnapshots(filePath, dpi);
        return pages.Select(t => t.Text).ToList();
    }

    public async Task ForeachPdfPage(string filePath, Action<IPageReader> action, int dpi = 300)
    {
        using var docReader = DocNet.GetDocReader(filePath, new PageDimensions(dpi / 72.0));
        var count = docReader.GetPageCount();
        for (int i = 0; i < count; i++)
        {
            using var pageReader = docReader.GetPageReader(i);
            action(pageReader);
        }
        await Task.CompletedTask;
    }
    public IEnumerable<Character> FilterIntersect(IEnumerable<Character> characters, BoundBox bound)
    {
        var charactersInArea = characters.Where(c =>
                        c.Box.Left <= bound.Right &&   // 字符框的左边界在目标框右边界的左侧
                        c.Box.Right >= bound.Left &&   // 字符框的右边界在目标框左边界的右侧
                        c.Box.Top <= bound.Bottom &&   // 字符框的上边界在目标框下边界的上方
                        c.Box.Bottom >= bound.Top      // 字符框的下边界在目标框上边界的下方
                    )
                   //.OrderBy(c => c.Box.Top).ThenBy(c => c.Box.Left)
                   ; // 按位置排序

        return charactersInArea;
    }
    public IEnumerable<Character> FilterContains(IEnumerable<Character> characters, BoundBox bound)
    {
        var charactersInArea = characters.Where(c =>
                       c.Box.Left >= bound.Left &&
                       c.Box.Top >= bound.Top &&
                       c.Box.Right <= bound.Right &&
                       c.Box.Bottom <= bound.Bottom
                   )
                   //.OrderBy(c => c.Box.Top).ThenBy(c => c.Box.Left)
                   ; // 按位置排序

        return charactersInArea;
    }
    public string GetText(IEnumerable<Character> characters, BoundBox bound)
    {
        return string.Concat(FilterIntersect(characters, bound).Select(t => t.Char));
    }

    public async Task<List<Mat>> ExtractMatImages(string filePath, int dpi = 300)
    {
        List<Mat> ret = [];
        var sks = await ExtractImages(filePath, dpi);
        foreach (var item in sks)
        {
            var mat = MyImageConverter.SKBitmapToMat(item);
            ret.Add(mat);
            item.Dispose();
        }
        return ret;
    }
}
