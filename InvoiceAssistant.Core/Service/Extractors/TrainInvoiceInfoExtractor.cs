using System.Text.RegularExpressions;
using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.Processors;
using Tesseract;

namespace InvoiceAssistant.Core.Service.Extractors;

public class TrainInvoiceInfoExtractor : IInfoExtractor
{
    public PdfProcessor? PdfEngine { get; set; }
    public FileInfoTypeBag InfoBag => new() { InfoType = FileInfoType.TrainTicket, Title = "火车票" };

    public void Dispose()
    {
    }
    public InvoiceInfo? GetInfo(string filePath)
    {
        Console.WriteLine(nameof(TrainInvoiceInfoExtractor) + " " + nameof(GetInfo));

        var ieu = new InfoExtractUnit();
        var matcher = new ExtractMetadata(180, 15, 410, 40)
        {
            RegexPattern = "铁路|电子客票",
            ScaleOrAbsolutePosition = false,
        };
        var xtractors = new List<ExtractMetadata>()
        {
            new(90, 80, 190, 100) {
                RegexPattern= @"^([\u4e00-\u9fa5]+)站",
                ScaleOrAbsolutePosition = false,
                 RegexMapToProperties=[new RegexMapToPropertyMetadata(1,nameof(InvoiceInfo.Origin) )]
            },
            new(390, 80, 490, 100) {
                RegexPattern= @"^([\u4e00-\u9fa5]+)站",
                ScaleOrAbsolutePosition = false,
                RegexMapToProperties=[new RegexMapToPropertyMetadata(1,nameof(InvoiceInfo.Destination))]
            },
            new(20, 130, 160, 150) {
                RegexPattern=  @"(\d{4}年\d{2}月\d{2}日\s*\d{1,2}:\d{2})开",
                ScaleOrAbsolutePosition = false,
                RegexMapToProperties=[new RegexMapToPropertyMetadata(1,nameof(InvoiceInfo.StartTime))]
            },
            new(20, 160, 130, 180) {
                RegexPattern= @"(\d+\.\d{2})",
                ScaleOrAbsolutePosition = false,
                RegexMapToProperties=[new RegexMapToPropertyMetadata(1,nameof(InvoiceInfo.TicketPrice) )]
            }
        };

        InvoiceInfo? ret = ieu.ExtractByPdf(PdfEngine!, filePath, matcher, xtractors);
        InfoBag.CopyTo(ret);

        // PdfEngine.ForeachPdfPage(filePath, page =>
        // {
        //     var txt = page.GetText();
        //     if (!InfoBag.KeyWords!.Any(tt => txt.Contains(tt)))
        //     {
        //         return;
        //     }
        //     ret = new InvoiceInfo()
        //     {
        //         InfoType = InfoBag.InfoType,
        //         Title = InfoBag.Title
        //     };
        //     var characters = page.GetCharacters();
        //     //自：
        //     var box = new Docnet.Core.Models.BoundBox(90, 80, 190, 100);
        //     txt = PdfEngine.GetText(characters, box).ReplaceLineEndings("");
        //     Console.WriteLine(txt);
        //     var match = Regex.Match(txt, @"^([\u4e00-\u9fa5]+)站");
        //     if (match.Success)
        //     {
        //         ret.Origin = match.Groups[1].Value;
        //     }
        //     //至：
        //     box = new Docnet.Core.Models.BoundBox(390, 80, 490, 100);
        //     txt = PdfEngine.GetText(characters, box).ReplaceLineEndings("");
        //     Console.WriteLine(txt);
        //     match = Regex.Match(txt, @"^([\u4e00-\u9fa5]+)站");
        //     if (match.Success)
        //     {
        //         ret.Destination = match.Groups[1].Value;
        //     }
        //     //日期时间
        //     box = new Docnet.Core.Models.BoundBox(20, 130, 160, 150);
        //     txt = PdfEngine.GetText(characters, box).ReplaceLineEndings("").Trim();
        //     Console.WriteLine(txt);
        //     match = Regex.Match(txt, @"(\d{4}年\d{2}月\d{2}日\s*\d{1,2}:\d{2})开");
        //     if (match.Success)
        //     {
        //         ret.StartTime = DateTime.Parse(match.Groups[1].Value);
        //     }
        //     //合计
        //     box = new Docnet.Core.Models.BoundBox(20, 160, 130, 180);
        //     txt = PdfEngine.GetText(characters, box).ReplaceLineEndings("").Trim();
        //     Console.WriteLine(txt);
        //     match = Regex.Match(txt, @"(\d+\.\d{2})");
        //     if (match.Success)
        //     {
        //         ret.TicketPrice = double.Parse(match.Groups[1].Value);
        //     }
        // });
        return ret;
    }
    // public InvoiceInfo? GetInfo2(string filePath)
    // {
    //     Console.WriteLine(nameof(TrainInvoiceInfoExtractor) + " " + nameof(GetInfo2));
    //     var strs = PdfEngine.ExtractText(filePath);
    //     if (!strs.Any(t => InfoBag.KeyWords!.Any(tt => t.Contains(tt)))) return null;
    //     var ret = new InvoiceInfo();
    //     foreach (var item in strs)
    //     {
    //         var lines = item.Split("\r\n");
    //         foreach (var line in lines)
    //         {
    //             // Console.WriteLine(line);
    //             var match = Regex.Match(line, @"^([\u4e00-\u9fa5]+站)\s+[A-Z\d]+\s+([\u4e00-\u9fa5]+站)$");
    //             if (match.Success)
    //             {
    //                 ret.Origin = match.Groups[1].Value;
    //                 ret.Destination = match.Groups[2].Value;
    //                 continue;
    //             }
    //             match = Regex.Match(line, @"(\d{4}年\d{2}月\d{2}日\s+\d{1,2}:\d{2})开");
    //             if (match.Success)
    //             {
    //                 ret.StartTime = DateTime.Parse(match.Groups[1].Value);
    //                 continue;
    //             }
    //             match = Regex.Match(line, @"￥(\d+\.\d{2})");
    //             if (match.Success)
    //             {
    //                 ret.TicketPrice = double.Parse(match.Groups[1].Value);
    //                 continue;
    //             }
    //         }
    //     }
    //     return ret;
    // }
}
