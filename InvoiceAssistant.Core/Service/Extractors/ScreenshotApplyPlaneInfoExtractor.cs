using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.Processors;
using Tesseract;

namespace InvoiceAssistant.Core.Service.Extractors;

public class ScreenshotApplyPlaneInfoExtractor : IInfoExtractor
{
    public PdfProcessor? PdfEngine { get; set; }
    public FileInfoTypeBag InfoBag => new() { InfoType = FileInfoType.ScreenshotApplyPlane, Title = "机票申请截图", };

    public void Dispose()
    {

    }

    public InvoiceInfo? GetInfo(string filePath)
    {
        Console.WriteLine(nameof(ScreenshotApplyPlaneInfoExtractor) + " " + nameof(GetInfo));
        var ieu = new InfoExtractUnit();
        var xtractors = new List<ExtractMetadata>();
        InvoiceInfo? ret = ieu.ExtractByImgOnlyText(filePath, "申请.*\n*.*机票", xtractors);
        InfoBag.CopyTo(ret);
        return ret;
    }
}
