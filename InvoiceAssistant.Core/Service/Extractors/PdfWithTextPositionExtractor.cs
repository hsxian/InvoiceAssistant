using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.ExtractUnits;
using InvoiceAssistant.Core.Service.Processors;
using Microsoft.Extensions.Logging;

namespace InvoiceAssistant.Core.Service.Extractors;

/// <summary>
/// 语义：PDF 文本层抽取。
///
/// 历史上这里只读文本层，遇到扫描版火车票/住宿发票就整份漏掉（没有任何兜底）。
/// 现在接入统一的 <see cref="PdfHybridExtraction"/>：文本层可用时行为不变（仍然只读文本层），
/// 只有文本层缺失/乱码/字段不全时才回退 OCR。
/// </summary>
public class PdfWithTextPositionExtractor(
 IImageInfoExtractUnit imageInfoExtractUnit,
 IPdfInfoExtractUnit pdfInfoExtractUnit,
 IPdfProcessor pdfProcessor,
 ILogger<PdfWithTextPositionExtractor> logger) : IInfoExtractor
{
    public ProcessType ProcessType => Data.ProcessType.PdfWithTextPosition;

    public void Dispose()
    {
    }

    public async Task<InvoiceInfo?> GetInfo(string filePath, ProcessConfig processConfig)
    {
        if (processConfig.ProcessValue != ProcessType) return null;
        var ret = await PdfHybridExtraction.Extract(
            pdfProcessor, pdfInfoExtractUnit, imageInfoExtractUnit, logger, filePath, processConfig);
        processConfig.CopyTo(ret);
        return ret;
    }
}
