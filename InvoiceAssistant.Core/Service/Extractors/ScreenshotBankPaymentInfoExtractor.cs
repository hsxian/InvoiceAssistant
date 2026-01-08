using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.Images;
using InvoiceAssistant.Core.Service.Processors;
using SkiaSharp;
using Tesseract;

namespace InvoiceAssistant.Core.Service.Extractors;

public class ScreenshotBankPaymentInfoExtractor : IInfoExtractor
{
    public FileInfoTypeBag InfoBag => new() { InfoType = FileInfoType.ScreenshotBankPayment, Title = "支付截图", };
    public PdfProcessor? PdfEngine { get; set; }

    public void Dispose()
    {

    }

    public InvoiceInfo? GetInfo(string filePath)
    {
        Console.WriteLine(nameof(ScreenshotBankPaymentInfoExtractor) + " " + nameof(GetInfo));
        var ieu = new InfoExtractUnit();
        var matcher = new ExtractMetadata();
        var xtractors = new List<ExtractMetadata>();
        InvoiceInfo? ret = ieu.ExtractByImg(filePath, matcher, xtractors);
        InfoBag.CopyTo(ret);
        return ret;
    }
}
