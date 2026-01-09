using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.Images;
using InvoiceAssistant.Core.Service.Processors;
using SkiaSharp;
using Tesseract;

namespace InvoiceAssistant.Core.Service.Extractors;

public class PdfToImageWithTextPositionExtractor : IInfoExtractor
{
    public PdfProcessor? PdfEngine { get; set; }

    public void Dispose()
    {
    }
    public InvoiceInfo? GetInfo(string filePath, ProcessConfig processConfig)
    {
        Console.WriteLine(nameof(PdfToImageWithTextPositionExtractor) + " " + nameof(GetInfo));
        var ieu = new InfoExtractUnit();

        var imgs = PdfEngine!.ExtractImages(filePath);
        InvoiceInfo? ret = null;
        foreach (var img in imgs)
        {
            using var pix = MyImageConverter.SKBitmapToPix(img);
            ret = ieu.ExtractByImg(pix, processConfig);
            if (ret != null) break;
        }
        imgs.ForEach(t => t.Dispose());
        processConfig.CopyTo(ret);

        return ret;
    }
}
