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
                ret.Add(cfg);
            }
            catch (Exception ex)
            {
                logger.LogError("{}", ex);
            }
        }
        return ret;
    }
    public async Task<IEnumerable<InvoiceInfo>> Extract(string dir, IEnumerable<ProcessConfig> cfgs)
    {
        var files = Directory.EnumerateFiles(dir);
        var extractors = serviceProvider.GetServices<IInfoExtractor>();
        var imgs = extractors.Where(t => t.GetType().Name.StartsWith("Image")).ToList();
        var pdfs = extractors.Where(t => t.GetType().Name.StartsWith("Pdf")).ToList();
        List<InvoiceInfo> ret = [];
        foreach (var item in files)
        {
            logger.LogInformation("开始处理：{}", item);
            if (RenameProcessor.InvoiceOwner != null &&
            Path.GetFileNameWithoutExtension(item).Contains(RenameProcessor.InvoiceOwner))
            {
                logger.LogInformation("因文件“{}”包含InvoiceOwner“{}”，视为处理过，将忽略。", item, RenameProcessor.InvoiceOwner);
                continue;
            }
            InvoiceInfo? info = null;
            var lc = item.ToLower();
            if (ImageExtensions.Any(lc.EndsWith))
            {
                info = await Match(item, imgs, cfgs);
            }
            else if (PdfExtensions.Any(lc.EndsWith))
            {
                info = await Match(item, pdfs, cfgs);
            }
            else
            {
                logger.LogInformation("跳过处理：{}", item);
                continue;
            }
            logger.LogInformation("结束处理：{}", item);
            if (info != null)
            {
                info.FilePath = item;
                ret.Add(info);
            }
        }
        return ret;
    }
    async Task<InvoiceInfo?> Match(string fp, IEnumerable<IInfoExtractor> extractors, IEnumerable<ProcessConfig> configs)
    {

        foreach (var config in configs)
        {
            var extractor = extractors.FirstOrDefault(t => t.ProcessType == config.ProcessValue);
            if (extractor == null)
            {
                // logger.LogWarning("配置{}未找到执行者{}", config.Title, config.ProcessValue);
                continue;
            }
            var ret = await extractor.GetInfo(fp, config);
            if (ret != null)
                return ret;
        }

        return null;
    }
    public void Dispose()
    {
    }
}