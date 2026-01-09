using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.Processors;

namespace InvoiceAssistant.Core.Service.Extractors;

public class ImageWithTextPositionExtractor : IInfoExtractor
{
    public PdfProcessor? PdfEngine { get; set; }

    public void Dispose()
    {

    }

    public InvoiceInfo? GetInfo(string filePath, ProcessConfig processConfig)
    {
        Console.WriteLine(nameof(ImageWithTextPositionExtractor) + " " + nameof(GetInfo));
        var ieu = new InfoExtractUnit();
        InvoiceInfo? ret = ieu.ExtractByImg(filePath, processConfig);
        processConfig.CopyTo(ret);
        return ret;
    }
}
