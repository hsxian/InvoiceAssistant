using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.Processors;
using Tesseract;

namespace InvoiceAssistant.Core.Service.Extractors;

public interface IInfoExtractor : IDisposable
{
    PdfProcessor? PdfEngine { get; set; }
    FileInfoTypeBag InfoBag { get; }
    InvoiceInfo? GetInfo(string filePath);
}
