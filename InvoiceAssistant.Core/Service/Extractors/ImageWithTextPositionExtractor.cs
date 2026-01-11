using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.ExtractUnits;
using Microsoft.Extensions.Logging;

namespace InvoiceAssistant.Core.Service.Extractors;

public class ImageWithTextPositionExtractor(ILogger<ImageWithTextPositionExtractor> logger,
IImageInfoExtractUnit imageInfoExtractUnit) : IInfoExtractor
{
    public ProcessType ProcessType => Data.ProcessType.ImageWithTextPosition;
    public void Dispose()
    {

    }

    public async Task<InvoiceInfo?> GetInfo(string filePath, ProcessConfig processConfig)
    {
        if (processConfig.ProcessValue != ProcessType) return null;
        InvoiceInfo? ret = await imageInfoExtractUnit.ExtractByImg(filePath, processConfig);
        processConfig.CopyTo(ret);
        return ret;
    }
}
