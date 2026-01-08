using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.Extractors;
using InvoiceAssistant.Core.Service.Processors;

namespace InvoiceAssistant.Core.Service;

public class InfoExtractAssembly : IDisposable
{
    PdfProcessor pdfProcessor;
    private List<IInfoExtractor> PdfExtractors { get; set; }
    private List<IInfoExtractor> ImgExtractors { get; set; }
    public InfoExtractAssembly()
    {
        pdfProcessor = new PdfProcessor();
        PdfExtractors = [
        new TrainInvoiceInfoExtractor(),
        new AirplaneInvoiceInfoExtractor(),
        new ApprovalFormInfoExtractor(),
        ];
        ImgExtractors = [new ScreenshotApplyPlaneInfoExtractor()];
        ImgExtractors.Concat(PdfExtractors).ToList().ForEach(t =>
        {
            t.PdfEngine = pdfProcessor;
        });
    }
    public InvoiceInfo? Extract(string dir)
    {
        var files = Directory.EnumerateFiles(dir);
        foreach (var item in files)
        {
            InvoiceInfo? ret = null;
            var lc = item.ToLower();
            if (lc.EndsWith(".png") || lc.EndsWith(".jpg") || lc.EndsWith(".bmp"))
            {
                ret = ImgExtractors.Select(t => t.GetInfo(item)).FirstOrDefault(t => t != null);
            }
            else if (lc.EndsWith(".pdf"))
            {
                ret = PdfExtractors.Select(t => t.GetInfo(item)).FirstOrDefault(t => t != null);
            }
            Console.WriteLine(item);
            if (ret != null)
            {
                ret.FilePath = item;
            }
        }
        return null;
    }

    public void Dispose()
    {
    }
}