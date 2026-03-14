// See https://aka.ms/new-console-template for more information
using InvoiceAssistant.Core.Service;
using InvoiceAssistant.Core.Service.Processors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

class MainHostedService(IInfoExtractAssembly infoExtractAssembly,
IRenameProcessor renameProcessor,
IConfiguration configuration) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var pch = configuration.GetSection("ProcessConfigurationHome").Get<string>()!;
        var rfh = configuration.GetSection("RawFilesHome").Get<string>()!;
        infoExtractAssembly.PdfExtensions = configuration.GetSection("PdfExtensions").Get<List<string>>()!;
        infoExtractAssembly.ImageExtensions = configuration.GetSection("ImageExtensions").Get<List<string>>()!;
        RenameProcessor.InvoiceOwner = configuration.GetSection("InvoiceOwner").Get<string>()!;
        var configs = await infoExtractAssembly.GetProcessConfigs(pch);
        var infos = await infoExtractAssembly.Extract(rfh, configs);
        renameProcessor.TryGroup(infos);
        // foreach (var item in infos)
        // {
        //     renameProcessor.Rename(item);
        // }
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