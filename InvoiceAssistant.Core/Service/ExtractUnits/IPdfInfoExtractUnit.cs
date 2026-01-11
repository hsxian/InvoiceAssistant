using InvoiceAssistant.Core.Data;

namespace InvoiceAssistant.Core.Service.ExtractUnits;

public interface IPdfInfoExtractUnit
{
    Task<InvoiceInfo?> ExtractByPdf(string filePath, ProcessConfig processConfig);
    Task<InvoiceInfo?> ExtractByPdfOnlyText(string filePath, ProcessConfig processConfig);
}
