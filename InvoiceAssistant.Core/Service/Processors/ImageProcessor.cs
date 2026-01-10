using System.Drawing;
using System.Drawing.Imaging.Effects;
using InvoiceAssistant.Core.Service.Images;
using SkiaSharp;
using Tesseract;

namespace InvoiceAssistant.Core.Service.Processors;

public class ImageProcessor : IImageProcessor
{
    public string? GetBoxText(TesseractEngine engine, SKBitmap img, SKRectI rectI)
    {
        using SKBitmap sub = new();
        if (!img.ExtractSubset(sub, rectI))
        {
            return null;
        }

        using var stream = File.OpenWrite("sub.png");
        sub.Encode(SKEncodedImageFormat.Png, 100).SaveTo(stream);

        using var pix = MyImageConverter.SKBitmapToPix(sub);
        using var page = engine.Process(pix);
        var txt = page.GetText();
        return txt;
    }

}