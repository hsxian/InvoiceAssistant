using System.Text.Json;
using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.Extractors;
using InvoiceAssistant.Core.Service.Processors;

namespace InvoiceAssistant.Core.Service;

public class InfoExtractAssembly : IDisposable
{
    PdfProcessor pdfProcessor;
    private List<IInfoExtractor> PdfExtractors { get; set; }
    private List<IInfoExtractor> ImgExtractors { get; set; }
    public InfoExtractAssembly()
    {
        pdfProcessor = new PdfProcessor();
        PdfExtractors = [
            // new TrainInvoiceInfoExtractor(),
            new PdfWithTextPositionExtractor(),
            new PdfToImageWithTextPositionExtractor(),
            new PdfOnlyTextExtractor()
        ];
        ImgExtractors =
        [
            new ImageOnlyTextExtractor(),
            new ImageWithTextPositionExtractor()
        ];
        ImgExtractors.Concat(PdfExtractors).ToList().ForEach(t =>
        {
            t.PdfEngine = pdfProcessor;
        });
    }

    public IEnumerable<ProcessConfig> GetProcessConfigs(string dir)
    {
        List<ProcessConfig> ret = [];
        var files = Directory.EnumerateFiles(dir);
        foreach (var file in files)
        {
            try
            {
                var txt = File.ReadAllText(file);
                var cfg = JsonSerializer.Deserialize<ProcessConfig>(txt);
                if (cfg == null
                || string.IsNullOrWhiteSpace(cfg.Title)
                || cfg.Matcher == null
                || cfg.ProcessValue == ProcessType.UnKnown)
                {
                    Console.WriteLine("%s为正确配置，Title、Matcher、ProcessValue为必填。", file);
                    continue;
                }
                ret.Add(cfg);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }
        return ret;
    }
    public InvoiceInfo? Extract(string dir)
    {
        var files = Directory.EnumerateFiles(dir);
        var cfgs = GetProcessConfigs("../../../../config/process");
        foreach (var item in files)
        {
            InvoiceInfo? ret = null;
            var lc = item.ToLower();
            if (lc.EndsWith(".png") || lc.EndsWith(".jpg") || lc.EndsWith(".bmp"))
            {
                ret = Match(item, ImgExtractors, cfgs);
                if (ret != null) continue;
            }
            else if (lc.EndsWith(".pdf"))
            {
                ret = Match(item, PdfExtractors, cfgs);
                if (ret != null) continue;
            }
            Console.WriteLine(item);
            if (ret != null)
            {
                ret.FilePath = item;
            }
        }
        return null;
    }
    InvoiceInfo? Match(string fp, IEnumerable<IInfoExtractor> extractors, IEnumerable<ProcessConfig> configs)
    {
        foreach (var extractor in extractors)
        {
            var ret = configs.Select(t => extractor.GetInfo(fp, t)).FirstOrDefault(t => t != null);
            if (ret != null) return ret;
        }
        return null;
    }
    public void Dispose()
    {
    }
}