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
    public async Task<InvoiceInfo?> ExtractByPdfOnlyText(string filePath, ProcessConfig processConfig)
    {
        InvoiceInfo? ret = null;
        await pdfProcessor.ForeachPdfPage(filePath, page =>
          {
              var txt = page.GetText();
              if (!Regex.Match(txt, processConfig.Matcher!.RegexPattern!).Success)
              {
                  return;
              }
              logger.LogInformation($"{processConfig.Matcher!.RegexPattern} -> [all text]");
              ret = new InvoiceInfo();
              var props = typeof(InvoiceInfo).GetProperties();
              if (processConfig.Xtractors != null)
              {
                  var xtractors = processConfig.Xtractors.Where(t => t.Enable != false).ToList();
                  foreach (var xtractor in xtractors)
                  {
                      var match = Regex.Match(txt, xtractor.RegexPattern!);
                      if (!match.Success) continue;
                      if (false == ret.SetValue(props, match, xtractor.RegexMapToProperties!))
                      {
                          logger.LogInformation($"{processConfig.Title} set value failed");
                      }
                  }
              }
          });
        return ret;
    }
    private string GetTxtByPdf(IEnumerable<Character> characters, int pw, int ph, ExtractMetadata metadata)
    {
        var box = metadata.ScaleOrAbsolutePosition ?
            new BoundBox((int)(pw * metadata.Left), (int)(ph * metadata.Top),
            (int)(pw * metadata.Right), (int)(ph * metadata.Bottom))
            : new BoundBox((int)metadata.Left, (int)metadata.Top, (int)metadata.Right, (int)metadata.Bottom);

        var txt = pdfProcessor.GetText(characters, box).ReplaceLineEndings("").Trim();
        return txt;
    }
    public async Task<InvoiceInfo?> ExtractByPdf(string filePath, ProcessConfig processConfig)
    {
        InvoiceInfo? ret = null;
        await pdfProcessor.ForeachPdfPage(filePath, page =>
            {
                var characters = page.GetCharacters();

                var txt = GetTxtByPdf(characters, page.GetPageWidth(), page.GetPageHeight(), processConfig.Matcher!);
                var match = Regex.Match(txt, processConfig.Matcher!.RegexPattern!);
                if (!match.Success)
                {
                    return;
                }
                logger.LogInformation($"{processConfig.Matcher!.RegexPattern} -> {txt}");
                ret = new InvoiceInfo();
                var props = typeof(InvoiceInfo).GetProperties();
                if (processConfig.Xtractors != null)
                {
                    var xtractors = processConfig.Xtractors.Where(t => t.Enable != false).ToList();
                    foreach (var xtractor in xtractors)
                    {
                        txt = GetTxtByPdf(characters, page.GetPageWidth(), page.GetPageHeight(), xtractor);
                        logger.LogInformation($"{xtractor.RegexPattern} -> {txt}");
                        match = Regex.Match(txt, xtractor.RegexPattern!);
                        if (!match.Success) continue;
                        if (false == ret.SetValue(props, match, xtractor.RegexMapToProperties!))
                        {
                            logger.LogInformation($"{processConfig.Title} set value failed");
                        }
                    }
                }
            });
        return ret;
    }
}
