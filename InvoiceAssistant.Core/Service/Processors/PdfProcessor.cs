using Docnet.Core;
using Docnet.Core.Models;
using Docnet.Core.Readers;
using Ghostscript.NET;
using Ghostscript.NET.Rasterizer;
using InvoiceAssistant.Core.Service.Images;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using SkiaSharp;

namespace InvoiceAssistant.Core.Service.Processors;

public class PdfProcessor(ILogger<PdfProcessor> logger) : IPdfProcessor
{
    private readonly ILogger<PdfProcessor> logger = logger;

    protected IDocLib DocNet { get; } = DocLib.Instance;

    public void Dispose()
    {
        DocNet.Dispose();
    }

    public async Task<List<SKBitmap>> ExtractImages(string filePath, int dpi = 300)
    {
        var ret = new List<SKBitmap>();
        // 创建 Rasterizer 实例
        using var rasterizer = new GhostscriptRasterizer();
        // 初始化（自动查找系统 gs）
#if Windows
        using var stream = File.Open(filePath, FileMode.Open);//Or go with using
        var lv = GhostscriptVersionInfo.GetLastInstalledVersion(GhostscriptLicense.GPL | GhostscriptLicense.AFPL, GhostscriptLicense.GPL);
        rasterizer.Open(stream, lv, true);
#else
        rasterizer.Open(filePath);
#endif

        int pageCount = rasterizer.PageCount;

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
    private List<byte[]> ExtractImages2(string filePath, int dpi = 300)
    {
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
    public async Task<List<string>> ExtractText(string filePath, int dpi = 300)
    {
        using var docReader = DocNet.GetDocReader(filePath, new PageDimensions(dpi / 72.0));
        var count = docReader.GetPageCount();
        var ret = new List<string>();
        for (int i = 0; i < count; i++)
        {
            using var pageReader = docReader.GetPageReader(i);
            var text = pageReader.GetText();
            //  logger.LogInformation(text);
            ret.Add(text);
        }
        await Task.CompletedTask;
        return ret;
    }
    public async Task ForeachPdfPage(string filePath, Action<IPageReader> action, int dpi = 300)
    {
        using var docReader = DocNet.GetDocReader(filePath, new PageDimensions(dpi / 72.0));
        var count = docReader.GetPageCount();
        var ret = new List<string>();
        for (int i = 0; i < count; i++)
        {
            using var pageReader = docReader.GetPageReader(i);
            var characters = pageReader.GetCharacters();
#if DEBUG
            foreach (var character in characters)
            {
                Console.WriteLine($"{character.Char} {character.Box.Left} {character.Box.Top} {character.Box.Right} {character.Box.Bottom}");
            }
#endif
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