// See https://aka.ms/new-console-template for more information
using InvoiceAssistant.Core.Service;
using InvoiceAssistant.Core.Service.Processors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

class MainHostedService(IInfoExtractAssembly infoExtractAssembly,
IRenameProcessor renameProcessor,
IConfiguration configuration,
ILogger<MainHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var pch = ResolvePath(configuration.GetSection("ProcessConfigurationHome").Get<string>());
        var rfh = ResolvePath(configuration.GetSection("RawFilesHome").Get<string>());

        if (string.IsNullOrWhiteSpace(rfh))
        {
            logger.LogError("RawFilesHome 未配置（appsettings.json），无法确定要扫描的目录，已退出。");
            return;
        }
        if (string.IsNullOrWhiteSpace(pch) || !Directory.Exists(pch))
        {
            logger.LogError("ProcessConfigurationHome 未配置或不存在：{Path}，已退出。", pch);
            return;
        }

        infoExtractAssembly.PdfExtensions = configuration.GetSection("PdfExtensions").Get<List<string>>() ?? [".pdf"];
        infoExtractAssembly.ImageExtensions = configuration.GetSection("ImageExtensions").Get<List<string>>() ?? [".png", ".jpg", ".jpeg"];
        RenameProcessor.InvoiceOwner = configuration.GetSection("InvoiceOwner").Get<string>() ?? string.Empty;

        logger.LogInformation("配置目录：{ConfigHome}", pch);
        logger.LogInformation("扫描目录：{RawHome}", rfh);
        logger.LogInformation("报销人：{Owner}", RenameProcessor.InvoiceOwner);

        var configs = await infoExtractAssembly.GetProcessConfigs(pch);
        logger.LogInformation("已加载 {Count} 份单据识别配置", configs.Count());
        var infos = await infoExtractAssembly.Extract(rfh, configs);
        renameProcessor.TryGroup(infos);
        logger.LogInformation("处理完成，共识别 {Count} 份单据。", infos.Count());
    }

    private static string? ResolvePath(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw;
        // 相对路径按程序基目录解析，便于 dotnet run 时直接可用
        return Path.IsPathRooted(raw) ? raw : Path.Combine(AppContext.BaseDirectory, raw);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
}
// var dir = "/2/";

// var pdfProcessor = new PdfProcessor();
// // 创建Tesseract引擎实例
// using var engine = new TesseractEngine("tessdata", "chi_sim", EngineMode.Default);

// var imgs = pdfProcessor.ExtractImages(dir + ".pdf");

// foreach (var item in imgs)
// {
//     using var mat = MyImageConverter.SKBitmapToMat(item);
//     using var bmat = ImageProcessor.Binarize(mat);
//     // 加载图像文件
//     using var img = MyImageConverter.MatToPix(bmat);
//     img.Save("ii.png");
//     // 处理图像并提取文本
//     using var page = engine.Process(img, PageSegMode.SparseText);
//     var text = page.GetText();
//     var ss = page.GetSegmentedRegions(PageIteratorLevel.Block);
//     var tt = ss[0].ToString();
//     Console.WriteLine("提取的文本: ");
//     Console.WriteLine(text);

//     var result = page.GetIterator();
//     result.Begin();

//     // 读取原始图片到SKBitmap
//     SKBitmap bitmap;
//     using (var stream = System.IO.File.OpenRead("ii.png"))
//     {
//         bitmap = SKBitmap.Decode(stream);
//     }

//     // 创建一个SKCanvas用于绘图
//     using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));
//     surface.Canvas.DrawBitmap(bitmap, 0, 0);

//     do
//     {
//         // 获取每个识别出的文字块的位置
//         if (result.IsAtBeginningOf(PageIteratorLevel.Block))
//         {
//             result.TryGetBoundingBox(PageIteratorLevel.Block, out var boundingBox);
//             // 在画布上绘制矩形
//             surface.Canvas.DrawRect(boundingBox.X1, boundingBox.Y1, boundingBox.Width, boundingBox.Height, new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Stroke, StrokeWidth = 2 });
//         }
//     } while (result.Next(PageIteratorLevel.Block));

//     // 保存绘制结果
//     using var image = surface.Snapshot();
//     using var data = image.Encode(SKEncodedImageFormat.Png, 100);
//     using var outputStream = System.IO.File.OpenWrite("iii.png");
//     data.SaveTo(outputStream);
// }