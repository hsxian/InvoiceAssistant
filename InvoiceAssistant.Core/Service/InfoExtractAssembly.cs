using System.Diagnostics;
using System.Text.Json;
using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.Extractors;
using InvoiceAssistant.Core.Service.Processors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InvoiceAssistant.Core.Service;

public class InfoExtractAssembly(ILogger<InfoExtractAssembly> logger,
 IServiceProvider serviceProvider) : IInfoExtractAssembly
{
    public List<string> PdfExtensions { get; set; } = [".pdf"];
    public List<string> ImageExtensions { get; set; } = [".png"];

    public async Task<IEnumerable<ProcessConfig>> GetProcessConfigs(string dir)
    {
        List<ProcessConfig> ret = [];
        var files = Directory.EnumerateFiles(dir);
        foreach (var file in files)
        {
            try
            {
                var txt = await File.ReadAllTextAsync(file);
                var cfg = JsonSerializer.Deserialize<ProcessConfig>(txt);
                if (cfg == null
                || string.IsNullOrWhiteSpace(cfg.Title)
                || cfg.Matcher == null
                || cfg.ProcessValue == ProcessType.UnKnown)
                {
                    logger.LogWarning("{}未正确配置，Title、Matcher、ProcessValue为必填。", file);
                    continue;
                }
                cfg.FilePath = file;
                ret.Add(cfg);
            }
            catch (Exception ex)
            {
                logger.LogError("{}", ex);
            }
        }
        return ret;
    }

    /// <summary>
    /// 预先把“配置 -> 抽取器”配对好。原实现对每个文件、每份配置都做一次线性查找，
    /// 配置多时这层查找本身也是可观开销。
    /// </summary>
    private static List<(ProcessConfig Cfg, IInfoExtractor Ex)> BuildPairs(
        IEnumerable<ProcessConfig> cfgs, IEnumerable<IInfoExtractor> extractors)
    {
        var list = new List<(ProcessConfig, IInfoExtractor)>();
        var byType = extractors.GroupBy(t => t.ProcessType).ToDictionary(g => g.Key, g => g.First());
        foreach (var cfg in cfgs)
        {
            if (byType.TryGetValue(cfg.ProcessValue, out var ex))
            {
                list.Add((cfg, ex));
            }
        }
        return list;
    }

    public async Task<IEnumerable<InvoiceInfo>> Extract(string dir, IEnumerable<ProcessConfig> cfgs)
    {
        var files = Directory.EnumerateFiles(dir).ToList();
        var extractors = serviceProvider.GetServices<IInfoExtractor>().ToList();
        var imgs = extractors.Where(t => t.GetType().Name.StartsWith("Image")).ToList();
        var pdfs = extractors.Where(t => t.GetType().Name.StartsWith("Pdf")).ToList();

        var imgPairs = BuildPairs(cfgs, imgs);
        var pdfPairs = BuildPairs(cfgs, pdfs);

        List<InvoiceInfo> ret = [];
        var index = 0;
        foreach (var item in files)
        {
            index++;
            if (RenameProcessor.InvoiceOwner != null &&
            Path.GetFileNameWithoutExtension(item).Contains(RenameProcessor.InvoiceOwner))
            {
                logger.LogInformation("因文件“{}”包含InvoiceOwner“{}”，视为处理过，将忽略。", item, RenameProcessor.InvoiceOwner);
                continue;
            }

            List<(ProcessConfig Cfg, IInfoExtractor Ex)> pairs;
            var lc = item.ToLower();
            if (ImageExtensions.Any(lc.EndsWith))
            {
                pairs = imgPairs;
            }
            else if (PdfExtensions.Any(lc.EndsWith))
            {
                pairs = pdfPairs;
            }
            else
            {
                logger.LogDebug("跳过处理：{}", item);
                continue;
            }

            if (pairs.Count == 0)
            {
                logger.LogDebug("没有可用于“{}”的抽取器，跳过。", item);
                continue;
            }

            var sw = Stopwatch.StartNew();
            logger.LogDebug("[{Index}/{Total}] 开始处理：{File}", index, files.Count, item);
            var (info, cfg) = await Match(item, pairs);
            sw.Stop();

            if (info != null)
            {
                info.FilePath = item;
                ret.Add(info);
                logger.LogInformation("[{Index}/{Total}] {Elapsed,6} ms  {File}  →  {Result}",
                    index, files.Count, sw.ElapsedMilliseconds, Path.GetFileName(item),
                    PdfHybridExtraction.Describe(info, cfg!));
            }
            else
            {
                logger.LogWarning("[{Index}/{Total}] {Elapsed,6} ms  {File}  →  未识别（{Count} 份配置均未命中）",
                    index, files.Count, sw.ElapsedMilliseconds, Path.GetFileName(item), pairs.Count);
            }
        }
        return ret;
    }

    private async Task<(InvoiceInfo? Info, ProcessConfig? Cfg)> Match(
        string fp, List<(ProcessConfig Cfg, IInfoExtractor Ex)> pairs)
    {
        foreach (var (cfg, ex) in pairs)
        {
            var ret = await ex.GetInfo(fp, cfg);
            if (ret != null)
            {
                ret.ExtractSource ??= ex.ProcessType.ToString();
                return (ret, cfg);
            }
        }
        return (null, null);
    }

    public void Dispose()
    {
    }
}
