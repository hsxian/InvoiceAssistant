using System.Formats.Asn1;
using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.Processors;
using Microsoft.Extensions.Logging;

namespace InvoiceAssistant.Core.Service.Extractors;

public class PdfOnlyTextExtractor(ILogger<PdfOnlyTextExtractor> logger,
 IInfoExtractUnit extractUnit) : IInfoExtractor
{
    public ProcessType ProcessType => Data.ProcessType.PdfOnlyText;
    public void Dispose()
    {

    }

    public async Task<InvoiceInfo?> GetInfo(string filePath, ProcessConfig processConfig)
    {
        if (processConfig.ProcessValue != ProcessType) return null;
        logger.LogInformation("开始执行");
        InvoiceInfo? ret = await extractUnit.ExtractByPdfOnlyText(filePath, processConfig);
        processConfig.CopyTo(ret);
        logger.LogInformation("结束执行");
        return ret;
    }
}
