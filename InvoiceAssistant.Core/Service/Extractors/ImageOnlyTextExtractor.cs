using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.Processors;
using Tesseract;

namespace InvoiceAssistant.Core.Service.Extractors;

public class ImageOnlyTextExtractor : IInfoExtractor
{
    public PdfProcessor? PdfEngine { get; set; }

    public void Dispose()
    {

    }


    public InvoiceInfo? GetInfo(string filePath, ProcessConfig processConfig)
    {
        Console.WriteLine(nameof(ImageOnlyTextExtractor) + " " + nameof(GetInfo));
        var ieu = new InfoExtractUnit();
        InvoiceInfo? ret = ieu.ExtractByImgOnlyText(filePath, processConfig);
        processConfig.CopyTo(ret);
        return ret;
    }
}
