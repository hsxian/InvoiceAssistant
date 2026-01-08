using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.Processors;
using Tesseract;

namespace InvoiceAssistant.Core.Service.Extractors;

public class AirplaneInvoiceInfoExtractor : IInfoExtractor
{
    public PdfProcessor? PdfEngine { get; set; }
    public FileInfoTypeBag InfoBag => new() { InfoType = FileInfoType.PlaneTicket, Title = "机票" };

    public void Dispose()
    {
    }

    public InvoiceInfo? GetInfo(string filePath)
    {
        Console.WriteLine(nameof(AirplaneInvoiceInfoExtractor) + " " + nameof(GetInfo));

        var ieu = new InfoExtractUnit();
        var matcher = new ExtractMetadata(200, 40, 430, 60)
        {
            RegexPattern = "航空|民航|行程单",
            ScaleOrAbsolutePosition = false,
        };
        var xtractors = new List<ExtractMetadata>()
        {
            new(40, 150, 170, 161) {
                RegexPattern= @"^自：([\u4e00-\u9fa5]{1,3}) ",
                ScaleOrAbsolutePosition = false,
                RegexMapToProperties=[new RegexMapToPropertyMetadata(1, nameof(InvoiceInfo.Origin))]
            },
            new(270,150, 350, 161) {
                RegexPattern= @"(\d{4}年\d{2}月\d{2}日 *\d{2}:\d{2})",
                ScaleOrAbsolutePosition = false,
                RegexMapToProperties=[new RegexMapToPropertyMetadata(1,nameof(InvoiceInfo.StartTime) )]
            },
            new(520, 235, 610, 260) {
                RegexPattern= @"(\d+\.\d{2})",
                ScaleOrAbsolutePosition = false,
                RegexMapToProperties=[new RegexMapToPropertyMetadata(1,nameof(InvoiceInfo.TicketPrice) )]
            }
        };
        var m = new ExtractMetadata(40, 150, 170, 161)
        {
            RegexPattern = @"^至：([\u4e00-\u9fa5]{1,3}) ",
            ScaleOrAbsolutePosition = false,
            RegexMapToProperties = [new RegexMapToPropertyMetadata(1, nameof(InvoiceInfo.Destination))]
        };
        xtractors.Add(m);
        for (int i = 1; i <= 4; i++)
        {
            m = m.CloneByJson<ExtractMetadata>();
            m.Offset(0, 25);
            xtractors.Add(m);
        }
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
        //     var box = new Docnet.Core.Models.BoundBox(40, 150, 170, 161);
        //     txt = PdfEngine.GetText(characters, box).ReplaceLineEndings("");
        //     Console.WriteLine(txt);
        //     var match = Regex.Match(txt, @"^自：([\u4e00-\u9fa5]{1,3}) ");
        //     if (match.Success)
        //     {
        //         ret.Origin = match.Groups[1].Value;
        //     }
        //     //至：
        //     for (int i = 1; i <= 4; i++)
        //     {
        //         box = box.Plus(new Docnet.Core.Models.BoundBox(0, 25, 0, 25));
        //         txt = PdfEngine.GetText(characters, box).ReplaceLineEndings("");
        //         Console.WriteLine(txt);
        //         match = Regex.Match(txt, @"^至：([\u4e00-\u9fa5]{1,3}) ");
        //         if (match.Success)
        //         {
        //             ret.Destination = match.Groups[1].Value;
        //         }
        //     }
        //     //日期
        //     box = new Docnet.Core.Models.BoundBox(270, 150, 320, 161);
        //     txt = PdfEngine.GetText(characters, box).ReplaceLineEndings("").Trim();
        //     //时间
        //     box = new Docnet.Core.Models.BoundBox(325, 150, 350, 161);
        //     txt += PdfEngine.GetText(characters, box).ReplaceLineEndings("").Trim();
        //     Console.WriteLine(txt);
        //     ret.StartTime = DateTime.Parse(txt);
        //     //合计
        //     box = new Docnet.Core.Models.BoundBox(520, 235, 610, 260);
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
}
