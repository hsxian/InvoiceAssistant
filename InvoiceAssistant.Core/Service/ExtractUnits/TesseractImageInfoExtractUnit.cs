using System.Text.RegularExpressions;
using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.Images;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using SkiaSharp;
using Tesseract;

namespace InvoiceAssistant.Core.Service.ExtractUnits;

public class TesseractImageInfoExtractUnit(ILogger<TesseractImageInfoExtractUnit> logger) : IImageInfoExtractUnit
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }
    private string? GetTxtByImg(TesseractEngine engie, Pix pix, ExtractMetadata metadata)
    {
        var box = metadata.ScaleOrAbsolutePosition ?
         new SKRectI((int)(pix.Width * metadata.Left), (int)(pix.Height * metadata.Top),
         (int)(pix.Width * metadata.Right), (int)(pix.Height * metadata.Bottom))
        : new SKRectI((int)metadata.Left, (int)metadata.Top, (int)metadata.Right, (int)metadata.Bottom);

        var rect = new Tesseract.Rect(box.Left, box.Top, box.Width, box.Height);

#if DEBUG
        pix.Save("iii.png");

        using var bim = SKBitmap.Decode("iii.png");
        using SKBitmap sub = new();
        if (bim.ExtractSubset(sub, box))
        {
            using var st = File.OpenWrite("sub.png");
            sub.Encode(st, SKEncodedImageFormat.Png, 100);
        }
#endif

        using var page = engie.Process(pix, rect);
        return page.GetText().Replace(" ", "").Trim();
    }
    public async Task<InvoiceInfo?> ExtractByImg(Pix pix, ProcessConfig processConfig)
    {
        using var engie = new TesseractEngine("tessdata", "chi_sim", EngineMode.Default);
        InvoiceInfo? ret = null;
        var txt = GetTxtByImg(engie, pix, processConfig.Matcher!);
        logger.LogInformation($"{processConfig.Matcher!.RegexPattern} -> {txt}");
        if (string.IsNullOrWhiteSpace(txt) || !Regex.Match(txt, processConfig.Matcher!.RegexPattern!).Success)
        {
            return ret;
        }
        ret = new InvoiceInfo();
        var props = typeof(InvoiceInfo).GetProperties();
        if (processConfig.Xtractors != null)
        {
            var xtractors = processConfig.Xtractors.Where(t => t.Enable != false).ToList();
            foreach (var xtractor in xtractors)
            {
                txt = GetTxtByImg(engie, pix, xtractor);
                logger.LogInformation($"{xtractor.RegexPattern} -> {txt}");
                if (string.IsNullOrWhiteSpace(txt)) continue;
                var match = Regex.Match(txt, xtractor.RegexPattern!);
                if (!match.Success) continue;
                ret.SetValue(props, match, xtractor.RegexMapToProperties!);
            }
        }
        await Task.CompletedTask;
        return ret;
    }
    public async Task<InvoiceInfo?> ExtractByImg(Mat mat, ProcessConfig processConfig)
    {
        using var pix = MyImageConverter.MatToPix(mat);
        return await ExtractByImg(pix, processConfig);
    }

    public async Task<InvoiceInfo?> ExtractByImg(string filePath, ProcessConfig processConfig)
    {
        using var pix = Pix.LoadFromFile(filePath);
        using var gray = pix.ConvertRGBToGray();
        return await ExtractByImg(gray, processConfig);
    }
    public async Task<InvoiceInfo?> ExtractByImgOnlyText(Mat mat, ProcessConfig processConfig)
    {
        using var pix = MyImageConverter.MatToPix(mat);
        return await ExtractByImgOnlyText(pix, processConfig);
    }

    public Task OutputTestResult(Mat mat)
    {
        throw new NotImplementedException();
    }
    public async Task<InvoiceInfo?> ExtractByImgOnlyText(Pix pix, ProcessConfig processConfig)
    {
        InvoiceInfo? ret = null;
        using var engie = new TesseractEngine("tessdata", "chi_sim", EngineMode.Default);
        using var gray = pix.ConvertRGBToGray();
        var txt = engie.Process(gray, PageSegMode.SparseText).GetText().Replace(" ", "");
        if (!Regex.Match(txt, processConfig.Matcher!.RegexPattern!).Success)
        {
            return ret;
        }
        ret = new InvoiceInfo();
        var props = typeof(InvoiceInfo).GetProperties();
        if (processConfig.Xtractors != null)
        {
            var xtractors = processConfig.Xtractors.Where(t => t.Enable != false).ToList();
            foreach (var xtractor in xtractors)
            {
                var match = Regex.Match(txt, xtractor.RegexPattern!);
                if (!match.Success) continue;
                ret.SetValue(props, match, xtractor.RegexMapToProperties!);
            }
        }
        await Task.CompletedTask;
        return ret;
    }
    public async Task<InvoiceInfo?> ExtractByImgOnlyText(string filePath, ProcessConfig processConfig)
    {
        using var pix = Pix.LoadFromFile(filePath);
        using var gray = pix.ConvertRGBToGray();
        return await ExtractByImgOnlyText(gray, processConfig);
    }
}
