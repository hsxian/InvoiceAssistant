using Ghostscript.NET.PDFA3Converter.ZUGFeRD;
using InvoiceAssistant.Core.Data;
using Microsoft.Extensions.Logging;

namespace InvoiceAssistant.Core.Service.Processors;

public class RenameProcessor(ILogger<RenameProcessor> logger) : IRenameProcessor
{
    public static string? InvoiceOwner { get; set; }

    public void TryMove(string s, string d)
    {
        try
        {
            var dir = Path.GetDirectoryName(d);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir!);
            }
            if (File.Exists(d))
            {
                logger.LogInformation("文件“{}”已经存在”", d);
            }
            else
            {
                File.Move(s, d);
                logger.LogInformation("重命名“{}”为“{}”", s, d);
            }
        }
        catch (Exception ex)
        {
            logger.LogError("{}", ex);
        }
    }
    public string GetNewFilename(InvoiceInfo info, bool datePrefix)
    {
        string? filename;
        if (datePrefix)
        {

            if (info.StartTime == DateTime.MinValue)
            {
                filename = $"0_{DateTime.Now:yyyy-MM-dd_fffffff}-{InvoiceOwner}-";
            }
            else
            {
                filename = $"{info.StartTime:yyyy-MM-dd}-{InvoiceOwner}-";
            }
        }
        else
        {
            filename = $"{InvoiceOwner}-";
        }
        var onull = string.IsNullOrWhiteSpace(info.Origin);
        var dnull = string.IsNullOrWhiteSpace(info.Destination);
        if (!onull && !dnull)
        {
            filename += $"{info.Origin![..2]}到{info.Destination![..2]}";
        }
        else if (!onull)
        {
            filename += $"{info.Origin![..2]}";
        }
        filename += info.Title;
        if (info.TicketPrice > 0)
        {
            filename += $"{info.TicketPrice}元";
        }
        string filePath = info.FilePath!;
        var en = Path.GetExtension(filePath);

        return filename + en;
    }
    public void Rename(InvoiceInfo info, params string[] subDir)
    {
        string filePath = info.FilePath!;
        var dir = Path.GetDirectoryName(filePath);
        var newFn = GetNewFilename(info, true);
        var array = new List<string>
        {
            dir!
        };
        array.AddRange(subDir);
        array.Add(newFn);
        TryMove(filePath, Path.Combine([.. array]));
    }
    class OrderInvoiceInfo
    {
        public string? Key { get; set; }
        public InvoiceInfo? Info { get; set; }
    }
    public void TryGroup(IEnumerable<InvoiceInfo> infos)
    {
        var fs = infos.Where(t => t.GroupFlag).OrderBy(t => t.StartTime).ToList();
        if (fs.Count == 0)
        {
            var ts = infos.Select(t => t.StartTime).Where(t => t != DateTime.MinValue).Order().ToList();
            if (ts.Count == 0)
            {
                foreach (var item in infos)
                {
                    Rename(item);
                }
            }
            else
            {
                var tf = $"{ts.First():yyyy.MM.dd}-{ts.Last():yyyy.MM.dd}";
                foreach (var info in infos)
                {
                    Rename(info,tf);
                }
            }

            return;
        }

        List<DateTime[]> times = [];
        for (int i = 0; i < fs.Count - 1; i++)
        {
            times.Add([fs[i].StartTime, fs[i + 1].StartTime]);
        }
        var fsl = fs.Last();
        times.Add([fsl.StartTime, fsl.EndTime]);

        var oinfos = infos.Select(t =>
        {
            var m = times.FirstOrDefault(tt => tt[0] <= t.StartTime && t.StartTime < tt[1]);
            return new OrderInvoiceInfo { Key = m == null ? "" : $"{m[0]:yyyy.MM.dd}", Info = t };
        }).ToList();
        var ks = oinfos.Where(t => !string.IsNullOrWhiteSpace(t.Key)).ToList();
        var nks = oinfos.Where(t => string.IsNullOrWhiteSpace(t.Key)).ToList();
        foreach (var item in nks)
        {
            var m = ks.FirstOrDefault(t => t.Info!.TicketPrice != 0 && t.Info.TicketPrice == item.Info!.TicketPrice);
            if (m != null)
            {
                item.Key = m.Key;
            }
        }

        ks = [.. oinfos.Where(t => !string.IsNullOrWhiteSpace(t.Key))];
        nks = [.. oinfos.Where(t => string.IsNullOrWhiteSpace(t.Key))];

        foreach (var item in nks)
        {
            Rename(item.Info!);
        }
        var gss = oinfos.Where(t => !string.IsNullOrWhiteSpace(t.Key)).GroupBy(t => t.Key).ToList();
        foreach (var gs in gss)
        {
            var ts = gs.Where(t => t.Info!.StartTime != DateTime.MinValue).Select(t => t.Info!.StartTime).Order();
            var tf = $"{ts.First():yyyy.MM.dd}-{ts.Last():yyyy.MM.dd}";
            foreach (var item in gs)
            {
                Rename(item.Info!,tf);
                //var info = item.Info!;
                //string filePath = info.FilePath!;
                //var dir = Path.GetDirectoryName(filePath);
                //var newFn = GetNewFilename(info, false);
                //TryMove(filePath, Path.Combine(dir!, tf, newFn));
            }
        }
    }
}