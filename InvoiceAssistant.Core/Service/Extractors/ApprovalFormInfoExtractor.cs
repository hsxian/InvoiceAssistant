using System.Text.RegularExpressions;
using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.Images;
using InvoiceAssistant.Core.Service.Processors;
using SkiaSharp;
using Tesseract;

namespace InvoiceAssistant.Core.Service.Extractors;

public class ApprovalFormInfoExtractor : IInfoExtractor
{
    public PdfProcessor? PdfEngine { get; set; }
    public FileInfoTypeBag InfoBag => new() { InfoType = FileInfoType.ApprovalForm, Title = "出差审批单" };

    public void Dispose()
    {
    }
    public InvoiceInfo? GetInfo(string filePath)
    {
        Console.WriteLine(nameof(ApprovalFormInfoExtractor) + " " + nameof(GetInfo));
        var ieu = new InfoExtractUnit();
        //出差审批单
        var matcher = new ExtractMetadata(6f / 15, 1f / 30, 9f / 15, 1f / 15)
        {
            RegexPattern = "出差审批单",
        };
        var xtractors = new List<ExtractMetadata>()
        {
             //申请人
            new(3f / 15, 3f / 30, 5f / 15, 4f / 30) {
                RegexPattern= @"(.*)",
                 RegexMapToProperties=[new RegexMapToPropertyMetadata(1,nameof(InvoiceInfo.PersonName))]
            },
            //地点
            new(59f / 119, 28f / 60, 9f / 15, 30f / 61) {
                RegexPattern= @"(.*)",
                RegexMapToProperties=[new RegexMapToPropertyMetadata(1,nameof(InvoiceInfo.Destination) )]
            },
            //开始时间
            new(3f / 15, 13f / 30, 5f / 15, 14f / 30) {
                RegexPattern=@"(\d{4}-\d{2}-\d{2})",
                 RegexMapToProperties=[new RegexMapToPropertyMetadata(1,nameof(InvoiceInfo.StartTime) )]
            },
            //结束时间
            new(9f / 15, 13f / 30, 12f / 15, 14f / 30) {
                RegexPattern=@"(\d{4}-\d{2}-\d{2})",
                 RegexMapToProperties=[new RegexMapToPropertyMetadata(1,nameof(InvoiceInfo.EndTime) )]
            }
        };
        var imgs = PdfEngine!.ExtractImages(filePath);
        InvoiceInfo? ret = null;
        foreach (var img in imgs)
        {
            using var pix = MyImageConverter.SKBitmapToPix(img);
            ret = ieu.ExtractByImg(pix, matcher, xtractors);
            if (ret != null) break;
        }
        imgs.ForEach(t => t.Dispose());
        InfoBag.CopyTo(ret);
        // var imgs = PdfEngine.ExtractImages(filePath);
        // foreach (var img in imgs)
        // {
        //     using var stream1 = File.OpenWrite("iii.png");
        //     img.Encode(SKEncodedImageFormat.Png, 100).SaveTo(stream1);

        //     //出差审批单
        //     var box = new SKRectI(6 * img.Width / 15, img.Height / 30, 9 * img.Width / 15, img.Height / 15);
        //     var txt = ImageProcessor.GetBoxText(Tesseract, img, box)?.Trim().Replace(" ", "");
        //     if (InfoBag.KeyWords!.Any(t => t == txt))
        //     {
        //         ret = new InvoiceInfo
        //         {
        //             InfoType = InfoBag.InfoType,
        //             Title = InfoBag.Title
        //         };
        //         //申请人
        //         box = new SKRectI(3 * img.Width / 15, 3 * img.Height / 30, 5 * img.Width / 15, 4 * img.Height / 30);
        //         ret.PersonName = txt = ImageProcessor.GetBoxText(Tesseract, img, box)?.Trim().Replace(" ", "") ?? "";
        //         //地点
        //         box = new SKRectI(59 * img.Width / 119, 28 * img.Height / 60, 9 * img.Width / 15, 30 * img.Height / 61);
        //         ret.Destination = txt = ImageProcessor.GetBoxText(Tesseract, img, box)?.Trim().Replace(" ", "") ?? "";
        //         //开始时间
        //         box = new SKRectI(3 * img.Width / 15, 13 * img.Height / 30, 5 * img.Width / 15, 14 * img.Height / 30);
        //         txt = ImageProcessor.GetBoxText(Tesseract, img, box)?.Trim().Replace(" ", "") ?? "";
        //         var match = Regex.Match(txt, @"(\d{4}-\d{2}-\d{2})");
        //         if (match.Success)
        //         {
        //             ret.StartTime = DateTime.Parse(match.Groups[1].Value);
        //         }

        //         //结束时间
        //         box = new SKRectI(9 * img.Width / 15, 13 * img.Height / 30, 12 * img.Width / 15, 14 * img.Height / 30);
        //         txt = ImageProcessor.GetBoxText(Tesseract, img, box)?.Trim().Replace(" ", "") ?? "";
        //         match = Regex.Match(txt, @"(\d{4}-\d{2}-\d{2})");
        //         if (match.Success)
        //         {
        //             ret.EndTime = DateTime.Parse(match.Groups[1].Value);
        //         }
        //         break;
        //     }
        // }
        return ret;
    }
}
