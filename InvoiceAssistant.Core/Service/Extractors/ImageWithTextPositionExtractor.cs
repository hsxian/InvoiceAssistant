using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.Processors;
using Microsoft.Extensions.Logging;

namespace InvoiceAssistant.Core.Service.Extractors;

public class ImageWithTextPositionExtractor(ILogger<ImageWithTextPositionExtractor> logger,
IInfoExtractUnit extractUnit) : IInfoExtractor
{
    public ProcessType ProcessType => Data.ProcessType.ImageWithTextPosition;
    public void Dispose()
    {

    }

    public async Task<InvoiceInfo?> GetInfo(string filePath, ProcessConfig processConfig)
    {
        if (processConfig.ProcessValue != ProcessType) return null;
        logger.LogInformation("开始执行");
        InvoiceInfo? ret = await extractUnit.ExtractByImg(filePath, processConfig);
        processConfig.CopyTo(ret);
        logger.LogInformation("结束执行");
        return ret;
    }
}
