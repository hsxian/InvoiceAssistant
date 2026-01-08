// See https://aka.ms/new-console-template for more information
using InvoiceAssistant.Core.Service;
using InvoiceAssistant.Core.Service.Extractors;
using InvoiceAssistant.Core.Service.Images;
using InvoiceAssistant.Core.Service.Processors;
using SkiaSharp;
using Tesseract;

Console.WriteLine("Hello, World!");
new InfoExtractAssembly().Extract("/media/hsx/j/zhch/苏研院/报销/2025/251227");
return;


// 设置语言：例如简体中文 + 英文

var dir = "/media/hsx/j/zhch/苏研院/报销/2025/251112/";
// ocrInput.LoadImage(dir + "黄思贤-支付截图440元.jpg");
// ocrInput.LoadPdf(dir + "黄思贤-出差审批单.pdf");
// ocrInput.LoadPdf(dir + "黄思贤-出差审批单.pdf");

var pdfProcessor = new PdfProcessor();
// 创建Tesseract引擎实例
using var engine = new TesseractEngine("tessdata", "chi_sim", EngineMode.Default);

// // 加载图像文件
// using var img = Pix.LoadFromFile(dir + "黄思贤-支付截图440元.jpg");

// // 处理图像并提取文本
// using var page = engine.Process(img);

// var text = page.GetText();
// Console.WriteLine("提取的文本: ");
// Console.WriteLine(text);
pdfProcessor.ExtractText(dir + "黄思贤-出差审批单.pdf");
// var imgs2 = pdfProcessor.ExtractImages2(dir + "黄思贤-出差审批单.pdf");
var imgs = pdfProcessor.ExtractImages(dir + "黄思贤-出差审批单.pdf");

foreach (var item in imgs)
{
    using var mat = MyImageConverter.SKBitmapToMat(item);
    using var bmat = ImageProcessor.Binarize(mat);
    // 加载图像文件
    using var img = MyImageConverter.MatToPix(bmat);
    img.Save("ii.png");
    // 处理图像并提取文本
    using var page = engine.Process(img, PageSegMode.SparseText);
    var text = page.GetText();
    var ss = page.GetSegmentedRegions(PageIteratorLevel.Block);
    var tt = ss[0].ToString();
    Console.WriteLine("提取的文本: ");
    Console.WriteLine(text);

    var result = page.GetIterator();
    result.Begin();

    // 读取原始图片到SKBitmap
    SKBitmap bitmap;
    using (var stream = System.IO.File.OpenRead("ii.png"))
    {
        bitmap = SKBitmap.Decode(stream);
    }

    // 创建一个SKCanvas用于绘图
    using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));
    surface.Canvas.DrawBitmap(bitmap, 0, 0);

    do
    {
        // 获取每个识别出的文字块的位置
        if (result.IsAtBeginningOf(PageIteratorLevel.Block))
        {
            result.TryGetBoundingBox(PageIteratorLevel.Block, out var boundingBox);
            // 在画布上绘制矩形
            surface.Canvas.DrawRect(boundingBox.X1, boundingBox.Y1, boundingBox.Width, boundingBox.Height, new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Stroke, StrokeWidth = 2 });
        }
    } while (result.Next(PageIteratorLevel.Block));

    // 保存绘制结果
    using var image = surface.Snapshot();
    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
    using var outputStream = System.IO.File.OpenWrite("iii.png");
    data.SaveTo(outputStream);
}




pdfProcessor.ExtractText("/media/hsx/j/zhch/苏研院/报销/2025/251112/黄思贤-支付截图440元.jpg");

var ti = new TrainInvoiceInfoExtractor();
ti.GetInfo("/media/hsx/j/zhch/苏研院/报销/2025/temp/26319166100000150548.pdf");
