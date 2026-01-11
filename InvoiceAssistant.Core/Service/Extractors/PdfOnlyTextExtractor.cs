using System.Formats.Asn1;
using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.ExtractUnits;
using Microsoft.Extensions.Logging;

namespace InvoiceAssistant.Core.Service.Extractors;

public class PdfOnlyTextExtractor(ILogger<PdfOnlyTextExtractor> logger,
 IPdfInfoExtractUnit pdfInfoExtractUnit) : IInfoExtractor
{
    public ProcessType ProcessType => Data.ProcessType.PdfOnlyText;
    public void Dispose()
    {

    }

    public async Task<InvoiceInfo?> GetInfo(string filePath, ProcessConfig processConfig)
    {
        if (processConfig.ProcessValue != ProcessType) return null;
        InvoiceInfo? ret = await pdfInfoExtractUnit.ExtractByPdfOnlyText(filePath, processConfig);
        processConfig.CopyTo(ret);
        return ret;
    }
}
