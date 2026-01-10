using SkiaSharp;
using Tesseract;

namespace InvoiceAssistant.Core.Service.Processors;

public interface IImageProcessor
{
    string? GetBoxText(TesseractEngine engine, SKBitmap img, SKRectI rectI);
}
