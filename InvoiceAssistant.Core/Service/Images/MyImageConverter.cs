using OpenCvSharp;
using SkiaSharp;
using Tesseract;
namespace InvoiceAssistant.Core.Service.Images;

public static class MyImageConverter
{
    // 1. Mat 转换为 SKImage
    public static SKImage MatToSKImage(this Mat mat)
    {
        // 将Mat转换为字节数组
        byte[] imageBytes = mat.ToBytes(".png");
        // 使用SKImage从字节数组创建图像
        return SKImage.FromEncodedData(imageBytes);
    }

    // 2. Mat 转换为 SKBitmap
    public static SKBitmap MatToSKBitmap(this Mat mat)
    {
        using var image = MatToSKImage(mat);
        return SKBitmap.FromImage(image);
    }

    // 3. Mat 转换为 Pix
    public static Pix MatToPix(this Mat mat)
    {
        // 将Mat转换为PNG格式的字节数组
        byte[] imageBytes = mat.ToBytes(".png");
        // 使用Pix加载从字节数组
        return Pix.LoadFromMemory(imageBytes);
    }

    // 4. SKImage 转换为 Mat
    public static Mat SKImageToMat(this SKImage skImage)
    {
        // 将SKImage编码为PNG格式的字节数组
        using var data = skImage.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = data.AsStream();
        byte[] bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        // 从字节数组创建Mat
        return Cv2.ImDecode(bytes, ImreadModes.Color);
    }

    // 5. SKBitmap 转换为 Mat
    public static Mat SKBitmapToMat(this SKBitmap skBitmap)
    {
        using var image = SKImage.FromBitmap(skBitmap);
        return SKImageToMat(image);
    }

    // 6. SKBitmap 转换为 Pix
    public static Pix SKBitmapToPix(this SKBitmap skBitmap)
    {
        using var image = SKImage.FromBitmap(skBitmap);
        // 将SKImage编码为PNG格式的字节数组
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        byte[] bytes = data.ToArray();
        return Pix.LoadFromMemory(bytes);
    }

    // 7. SKImage 转换为 Pix
    public static Pix SKImageToPix(this SKImage skImage)
    {
        using var data = skImage.Encode(SKEncodedImageFormat.Png, 100);
        byte[] bytes = data.ToArray();
        return Pix.LoadFromMemory(bytes);
    }

    // // 8. Pix 转换为 Mat
    // public static Mat PixToMat(Pix pix)
    // {
    //     // 将Pix保存为字节数组
    //     byte[] imageBytes;
    //     using (var memoryStream = new MemoryStream())
    //     {
    //         pix.Save(memoryStream);
    //         imageBytes = memoryStream.ToArray();
    //     }
    //     // 从字节数组创建Mat
    //     return Cv2.ImDecode(imageBytes, ImreadModes.Color);
    // }

    // // 9. Pix 转换为 SKImage
    // public static SKImage PixToSKImage(Pix pix)
    // {
    //     byte[] imageBytes;
    //     using (var memoryStream = new MemoryStream())
    //     {
    //         pix.SaveToPng(memoryStream);
    //         imageBytes = memoryStream.ToArray();
    //     }
    //     return SKImage.FromEncodedData(imageBytes);
    // }
    // public static SKBitmap PixToSKBitmap(Pix pix)
    // {
    //     // 1. 获取 Pix 的图像信息
    //     var width = pix.Width;
    //     var height = pix.Height;

    //     // 2. 根据 Pix 的深度（depth）确定 SkiaSharp 的颜色格式
    //     SKColorType colorType;
    //     if (pix.Depth == 1)
    //     {
    //         // 1位二值图
    //         colorType = SKColorType.Gray8;
    //     }
    //     else if (pix.Depth == 8)
    //     {
    //         // 8位灰度图
    //         colorType = SKColorType.Gray8;
    //     }
    //     else if (pix.Depth == 32)
    //     {
    //         // 32位彩色图（通常为 ARGB）
    //         colorType = SKColorType.Bgra8888;
    //     }
    //     else
    //     {
    //         throw new NotSupportedException($"Unsupported Pix depth: {pix.Depth}");
    //     }

    //     // 3. 创建 SKImageInfo
    //     // var info = new SKImageInfo(width, height, colorType, SKAlphaType.Premul);
    //     var data = pix.GetData();
    //     // // 4. 获取 Pix 的数据指针
    //     // IntPtr dataPtr = data.Data;
    //     // // 注意：需要根据实际情况计算每行的字节数（rowBytes）
    //     // int rowBytes = pix.GetData().WordsPerLine * 4; // wpl 是字/行，通常需要乘以4得到字节数
    //     // 5. 使用 FromPixels 创建 SKImage
    //     var newInfo = new SKImageInfo(width, height, colorType);
    //     var bitmap = new SKBitmap();
    //     bitmap.InstallPixels(newInfo, data.Data);
    //     return bitmap;
    // }
    /// <summary>
    /// 把像素字节数组转图片
    /// </summary>
    /// <param name="buffer">像素数组，例如: [r, g, b, a, r, g, b, a ...]</param>
    /// <param name="width">图片的宽度，例如: 512</param>
    /// <param name="height">图片的高度，例如: 1024</param>
    /// <param name="format">指定像素数组的组成方式，例如：SKColorType.Rgba8888</param>
    /// <returns></returns>
    public static SKBitmap Decode(byte[] buffer, int width, int height, SKColorType format)
    {
        var data = SKData.CreateCopy(buffer);
        var newInfo = new SKImageInfo(width, height, format);
        var bitmap = new SKBitmap();
        bitmap.InstallPixels(newInfo, data.Data);
        return bitmap;
    }
    // 10. Pix 转换为 SKBitmap
    // public static SKBitmap PixToSKBitmap(Pix pix)
    // {
    //     using (var image = PixToSKImage(pix))
    //     {
    //         return SKBitmap.FromImage(image);
    //     }
    // }
}