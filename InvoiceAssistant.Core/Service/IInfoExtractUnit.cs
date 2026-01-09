using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.Processors;
using Tesseract;

namespace InvoiceAssistant.Core.Service;

public interface IInfoExtractUnit
{
    Task<InvoiceInfo?> ExtractByImg(string filePath, ProcessConfig processConfig);
    Task<InvoiceInfo?> ExtractByImg(Pix pix, ProcessConfig processConfig);
    Task<InvoiceInfo?> ExtractByImgOnlyText(string filePath, ProcessConfig processConfig);
    Task<InvoiceInfo?> ExtractByPdf(string filePath, ProcessConfig processConfig);
    Task<InvoiceInfo?> ExtractByPdfOnlyText(string filePath, ProcessConfig processConfig);
}
