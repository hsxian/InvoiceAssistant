using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.ExtractUnits;
using InvoiceAssistant.Core.Service.Processors;
using Microsoft.Extensions.Logging;

namespace InvoiceAssistant.Core.Service.Extractors;

public class PdfToImageWithTextPositionExtractor(ILogger<PdfToImageWithTextPositionExtractor> logger,
IImageInfoExtractUnit imageInfoExtractUnit,
IPdfProcessor pdfProcessor) : IInfoExtractor
{
    public ProcessType ProcessType => Data.ProcessType.PdfToImageWithTextPosition;
    public void Dispose()
    {
    }
    public async Task<InvoiceInfo?> GetInfo(string filePath, ProcessConfig processConfig)
    {
        if (processConfig.ProcessValue != ProcessType) return null;
        var imgs = await pdfProcessor.ExtractMatImages(filePath);
        InvoiceInfo? ret = null;
        foreach (var img in imgs)
        {
            ret = await imageInfoExtractUnit.ExtractByImg(img, processConfig);
            if (ret != null) break;
        }
        imgs.ForEach(t => t.Dispose());
        processConfig.CopyTo(ret);
        return ret;
    }
}
