using System;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;

namespace InvoiceAssistant.App.Service;

public static class SKBitmapExtensions
{
    public static Bitmap? ConvertSKBitmapToAvaloniaBitmapViaStream(this SKBitmap skBitmap)
    {
        if (skBitmap == null)
            return null;

        // 确保是支持的格式
        using var tempBitmap = skBitmap.Copy(SKColorType.Bgra8888);
        using var pixmap = tempBitmap.PeekPixels();
        // 编码为 PNG 内存流
        using var image = SKImage.FromPixels(pixmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream(data.ToArray());
        return new Bitmap(stream);
    }
}
