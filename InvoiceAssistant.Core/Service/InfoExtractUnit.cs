using System.Reflection;
using System.Text.RegularExpressions;
using Docnet.Core.Models;
using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.Images;
using InvoiceAssistant.Core.Service.Processors;
using SkiaSharp;
using Tesseract;

namespace InvoiceAssistant.Core.Service;

public class InfoExtractUnit
{
    private void SetValue(InvoiceInfo ret, PropertyInfo[] props, Match match,
   IEnumerable<RegexMapToPropertyMetadata> regexMapToProperties)
    {
        foreach (var item in regexMapToProperties!)
        {
            var prop = props.First(t => t.Name == item.PropertyName);
            SetValue(ret, prop, match.Groups[item.GroupsIndex].Value.ReplaceLineEndings().Trim());
        }


    }
    private void SetValue(object obj, PropertyInfo prop, string v)
    {
        if (prop.PropertyType == typeof(string))
        {
            prop.SetValue(obj, v);
        }
        else if (prop.PropertyType == typeof(double))
        {
            var d = double.Parse(v);
            prop.SetValue(obj, d);
        }
        else if (prop.PropertyType == typeof(DateTime))
        {
            var d = DateTime.Parse(v);
            prop.SetValue(obj, d);
        }
    }
    public InvoiceInfo? ExtractByPdfOnlyText(PdfProcessor pdfProcessor, string filePath, string matcherPattern, List<ExtractMetadata> xtractors)
    {
        InvoiceInfo? ret = null;
        pdfProcessor.ForeachPdfPage(filePath, page =>
        {
            var txt = page.GetText();
            if (!Regex.Match(txt, matcherPattern).Success)
            {
                return;
            }
            ret = new InvoiceInfo();
            var props = typeof(InvoiceInfo).GetProperties();
            foreach (var xtractor in xtractors)
            {
                var match = Regex.Match(txt, xtractor.RegexPattern!);
                if (!match.Success) continue;
                SetValue(ret, props, match, xtractor.RegexMapToProperties!);
            }
        });
        return ret;
    }
    private string GetTxtByPdf(PdfProcessor pdfProcessor, IEnumerable<Character> characters, int pw, int ph, ExtractMetadata metadata)
    {
        var box = metadata.ScaleOrAbsolutePosition ?
            new BoundBox((int)(pw * metadata.Left), (int)(ph * metadata.Top),
            (int)(pw * metadata.Right), (int)(ph * metadata.Bottom))
            : new BoundBox((int)metadata.Left, (int)metadata.Top, (int)metadata.Right, (int)metadata.Bottom);

        var txt = pdfProcessor.GetText(characters, box).ReplaceLineEndings("").Trim();
        return txt;
    }
    public InvoiceInfo? ExtractByPdf(PdfProcessor pdfProcessor, string filePath, ExtractMetadata matcher, List<ExtractMetadata> xtractors)
    {
        InvoiceInfo? ret = null;
        pdfProcessor.ForeachPdfPage(filePath, page =>
        {
            var characters = page.GetCharacters();

            var txt = GetTxtByPdf(pdfProcessor, characters, page.GetPageWidth(), page.GetPageHeight(), matcher);
            Console.WriteLine($"{matcher.RegexPattern} -> {txt}");
            var match = Regex.Match(txt, matcher.RegexPattern!);
            if (!match.Success)
            {
                return;
            }
            ret = new InvoiceInfo();
            var props = typeof(InvoiceInfo).GetProperties();
            foreach (var xtractor in xtractors)
            {
                txt = GetTxtByPdf(pdfProcessor, characters, page.GetPageWidth(), page.GetPageHeight(), xtractor);
                Console.WriteLine($"{xtractor.RegexPattern} -> {txt}");
                match = Regex.Match(txt, xtractor.RegexPattern!);
                if (!match.Success) continue;
                SetValue(ret, props, match, xtractor.RegexMapToProperties!);
            }
        });
        return ret;
    }
    public InvoiceInfo? ExtractByImgOnlyText(string filePath, string matcherPattern, List<ExtractMetadata> xtractors)
    {
        InvoiceInfo? ret = null;
        using var engie = new TesseractEngine("tessdata", "chi_sim", EngineMode.Default);
        using var pix = Pix.LoadFromFile(filePath);
        using var gray = pix.ConvertRGBToGray();
        var txt = engie.Process(gray, PageSegMode.SparseText).GetText().Replace(" ", "");
        if (!Regex.Match(txt, matcherPattern).Success)
        {
            return ret;
        }
        ret = new InvoiceInfo();
        var props = typeof(InvoiceInfo).GetProperties();
        foreach (var xtractor in xtractors)
        {
            var match = Regex.Match(txt, xtractor.RegexPattern!);
            if (!match.Success) continue;
            SetValue(ret, props, match, xtractor.RegexMapToProperties!);
        }
        return ret;
    }
    private string? GetTxtByImg(TesseractEngine engie, Pix pix, ExtractMetadata metadata)
    {
        var box = metadata.ScaleOrAbsolutePosition ?
         new SKRectI((int)(pix.Width * metadata.Left), (int)(pix.Height * metadata.Top),
         (int)(pix.Width * metadata.Right), (int)(pix.Height * metadata.Bottom))
        : new SKRectI((int)metadata.Left, (int)metadata.Top, (int)metadata.Right, (int)metadata.Bottom);

        var rect = new Rect(box.Left, box.Top, box.Width, box.Height);

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
    public InvoiceInfo? ExtractByImg(string filePath, ExtractMetadata matcher, List<ExtractMetadata> xtractors)
    {
        using var pix = Pix.LoadFromFile(filePath);
        return ExtractByImg(pix, matcher, xtractors);
    }
    public InvoiceInfo? ExtractByImg(Pix pix, ExtractMetadata matcher, List<ExtractMetadata> xtractors)
    {
        using var engie = new TesseractEngine("tessdata", "chi_sim", EngineMode.Default);
        InvoiceInfo? ret = null;
        using var gray = pix.ConvertRGBToGray();
        var txt = GetTxtByImg(engie, gray, matcher);
        Console.WriteLine($"{matcher.RegexPattern} -> {txt}");
        if (string.IsNullOrWhiteSpace(txt) || !Regex.Match(txt, matcher.RegexPattern!).Success)
        {
            return ret;
        }
        ret = new InvoiceInfo();
        var props = typeof(InvoiceInfo).GetProperties();
        foreach (var xtractor in xtractors)
        {
            txt = GetTxtByImg(engie, gray, xtractor);
            Console.WriteLine($"{xtractor.RegexPattern} -> {txt}");
            if (string.IsNullOrWhiteSpace(txt)) continue;
            var match = Regex.Match(txt, xtractor.RegexPattern!);
            if (!match.Success) continue;
            SetValue(ret, props, match, xtractor.RegexMapToProperties!);
        }
        return ret;
    }
}
