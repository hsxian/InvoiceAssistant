using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.Images;
using InvoiceAssistant.Core.Service.Processors;
using Microsoft.Extensions.Logging;

namespace InvoiceAssistant.Core.Service.Extractors;

public class PdfToImageWithTextPositionExtractor(ILogger<PdfToImageWithTextPositionExtractor> logger,
IInfoExtractUnit extractUnit,
IPdfProcessor pdfProcessor) : IInfoExtractor
{
    public ProcessType ProcessType => Data.ProcessType.PdfToImageWithTextPosition;
    public void Dispose()
    {
    }
    public async Task<InvoiceInfo?> GetInfo(string filePath, ProcessConfig processConfig)
    {
        if (processConfig.ProcessValue != ProcessType) return null;
        logger.LogInformation("开始执行");
        var imgs = await pdfProcessor.ExtractImages(filePath);
        InvoiceInfo? ret = null;
        foreach (var img in imgs)
        {
            using var pix = MyImageConverter.SKBitmapToPix(img);
            ret = await extractUnit.ExtractByImg(pix, processConfig);
            if (ret != null) break;
        }
        imgs.ForEach(t => t.Dispose());
        processConfig.CopyTo(ret);
        logger.LogInformation("结束执行");
        return ret;
    }
}
