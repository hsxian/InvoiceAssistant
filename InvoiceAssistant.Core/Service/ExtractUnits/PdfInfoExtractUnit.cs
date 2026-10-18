using System.Text.RegularExpressions;
using Docnet.Core.Models;
using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.Processors;
using Microsoft.Extensions.Logging;

namespace InvoiceAssistant.Core.Service.ExtractUnits;

public class PdfInfoExtractUnit(IPdfProcessor pdfProcessor,
ILogger<PdfInfoExtractUnit> logger
) : IPdfInfoExtractUnit
{
    private static readonly System.Reflection.PropertyInfo[] Props =
        typeof(InvoiceInfo).GetProperties();

    public async Task<InvoiceInfo?> ExtractByPdfOnlyText(string filePath, ProcessConfig processConfig)
    {
        InvoiceInfo? ret = null;
        // 页面快照带缓存：同一份 PDF 在多个配置之间只解析一次
        var pages = await pdfProcessor.GetPageSnapshots(filePath);
        foreach (var page in pages)
        {
            var txt = page.Text;
            if (string.IsNullOrEmpty(txt) ||
                !Regex.Match(txt, processConfig.Matcher!.RegexPattern!).Success)
            {
                continue;
            }
            logger.LogDebug("{Pattern} -> [all text]", processConfig.Matcher!.RegexPattern);
            ret = new InvoiceInfo();
            if (processConfig.Xtractors != null)
            {
                var xtractors = processConfig.Xtractors.Where(t => t.Enable != false).ToList();
                foreach (var xtractor in xtractors)
                {
                    var match = Regex.Match(txt, xtractor.RegexPattern!);
                    if (!match.Success) continue;
                    if (false == ret.SetValue(Props, match, xtractor.RegexMapToProperties!))
                    {
                        logger.LogDebug("{Title} set value failed", processConfig.Title);
                    }
                }
            }
        }
        return ret;
    }

    private static string GetTxtByPdf(PdfPageSnapshot page, ExtractMetadata metadata)
        => PdfTextRegion.GetText(page, metadata);

    /// <summary>
    /// 用文本层按配置区域抽取。仅依赖已缓存的页面快照，不再重复打开 PDF。
    /// </summary>
    public async Task<InvoiceInfo?> ExtractByPdf(string filePath, ProcessConfig processConfig)
    {
        InvoiceInfo? ret = null;
        var pages = await pdfProcessor.GetPageSnapshots(filePath);
        foreach (var page in pages)
        {
            var txt = GetTxtByPdf(page, processConfig.Matcher!);
            var match = Regex.Match(txt, processConfig.Matcher!.RegexPattern!);
            if (!match.Success)
            {
                continue;
            }
            logger.LogDebug("{Pattern} -> {Text}", processConfig.Matcher!.RegexPattern, txt);
            ret = new InvoiceInfo();
            if (processConfig.Xtractors != null)
            {
                var xtractors = processConfig.Xtractors.Where(t => t.Enable != false).ToList();
                foreach (var xtractor in xtractors)
                {
                    txt = GetTxtByPdf(page, xtractor);
                    logger.LogDebug("{Pattern} -> {Text}", xtractor.RegexPattern, txt);
                    match = Regex.Match(txt, xtractor.RegexPattern!);
                    if (!match.Success) continue;
                    if (false == ret.SetValue(Props, match, xtractor.RegexMapToProperties!))
                    {
                        logger.LogDebug("{Title} set value failed", processConfig.Title);
                    }
                }
            }
        }
        return ret;
    }
}
