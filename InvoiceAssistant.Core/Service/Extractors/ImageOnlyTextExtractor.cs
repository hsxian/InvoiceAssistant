using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.ExtractUnits;
using Microsoft.Extensions.Logging;

namespace InvoiceAssistant.Core.Service.Extractors;

public class ImageOnlyTextExtractor(
 IImageInfoExtractUnit imageInfoExtractUnit) : IInfoExtractor
{

    public void Dispose()
    {

    }


    public async Task<InvoiceInfo?> GetInfo(string filePath, ProcessConfig processConfig)
    {
        if (processConfig.ProcessValue != ProcessType) return null;
        InvoiceInfo? ret = await imageInfoExtractUnit.ExtractByImgOnlyText(filePath, processConfig);
        processConfig.CopyTo(ret);
        return ret;
    }

    public ProcessType ProcessType => ProcessType.ImageOnlyText;
}
