using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.ExtractUnits;
using InvoiceAssistant.Core.Service.Processors;
using Microsoft.Extensions.Logging;

namespace InvoiceAssistant.Core.Service.Extractors;

/// <summary>
/// 语义：PDF，允许走“渲染成图 + OCR”。实际策略由 <see cref="PdfHybridExtraction"/> 决定：
/// 文本层可用就先走文本层，缺失/乱码/字段不全时才渲染成图做 OCR。
/// </summary>
public class PdfToImageWithTextPositionExtractor(
 IImageInfoExtractUnit imageInfoExtractUnit,
 IPdfInfoExtractUnit pdfInfoExtractUnit,
 IPdfProcessor pdfProcessor,
 ILogger<PdfToImageWithTextPositionExtractor> logger) : IInfoExtractor
{
    public ProcessType ProcessType => Data.ProcessType.PdfToImageWithTextPosition;

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
