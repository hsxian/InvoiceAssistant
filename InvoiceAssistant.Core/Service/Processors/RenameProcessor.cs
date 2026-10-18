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
        // 出发地/到达地取前 2 字，需防御 null 与长度不足 2 的情况
        var o2 = string.IsNullOrWhiteSpace(info.Origin) ? null
            : (info.Origin.Length >= 2 ? info.Origin[..2] : info.Origin);
        var d2 = string.IsNullOrWhiteSpace(info.Destination) ? null
            : (info.Destination.Length >= 2 ? info.Destination[..2] : info.Destination);
        if (o2 != null && d2 != null)
        {
            filename += $"{o2}到{d2}";
        }
        else if (o2 != null)
        {
            filename += o2;
        }
        if (!string.IsNullOrWhiteSpace(info.Title))
        {
            filename += info.Title;
        }
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

    /// <summary>
    /// 将单据按“行程”归档到 yyyy.MM.dd[-yyyy.MM.dd] 子目录。
    /// 优先用“出差审批单”等锚点；没有锚点时按“去程/返程”路线切分行程，
    /// 从而修复“多次出差被合并成一组”的缺陷。
    /// </summary>
    public void TryGroup(IEnumerable<InvoiceInfo> infos)
    {
        var list = infos.ToList();
        if (list.Count == 0)
        {
            return;
        }

        // 1) 优先使用“出差审批单”等锚点（GroupFlag=true）划分行程，保留原设计意图。
        var anchors = list
            .Where(t => t.GroupFlag && t.StartTime != DateTime.MinValue)
            .OrderBy(t => t.StartTime)
            .ToList();

        // 2) 没有锚点（最常见：未上传出差审批单）时按“去程/返程”路线切分；
        //    路线信息不足再退回日期间隔聚类兜底。
        var trips = anchors.Count > 0
            ? GroupByAnchors(list, anchors)
            : (GroupByRoute(list) ?? GroupByDateGap(list));

        // 3) 归档：每个行程一个 yyyy.MM.dd[-yyyy.MM.dd] 文件夹。
        foreach (var trip in trips)
        {
            var dated = trip.Where(t => t.StartTime != DateTime.MinValue).ToList();
            DateTime dmin, dmax;
            if (dated.Count > 0)
            {
                dmin = dated.Min(t => t.StartTime).Date;
                dmax = dated.Max(t => t.StartTime).Date;
            }
            else
            {
                dmin = dmax = DateTime.Today;
            }

            var tf = dmin == dmax
                ? $"{dmin:yyyy.MM.dd}"
                : $"{dmin:yyyy.MM.dd}-{dmax:yyyy.MM.dd}";

            foreach (var info in trip)
            {
                Rename(info, tf);
            }
        }
    }

    /// <summary>按“出差审批单”锚点划分行程：相邻锚点之间为一个区间。</summary>
    private static List<List<InvoiceInfo>> GroupByAnchors(
        List<InvoiceInfo> list, List<InvoiceInfo> anchors)
    {
        var bounds = new List<(DateTime Start, DateTime End)>();
        for (var i = 0; i < anchors.Count - 1; i++)
        {
            bounds.Add((anchors[i].StartTime, anchors[i + 1].StartTime));
        }
        var last = anchors[^1];
        var lastEnd = last.EndTime != DateTime.MinValue && last.EndTime > last.StartTime
            ? last.EndTime
            : DateTime.MaxValue;
        bounds.Add((last.StartTime, lastEnd));

        var groups = bounds.Select(_ => new List<InvoiceInfo>()).ToList();
        var leftover = new List<InvoiceInfo>();

        foreach (var info in list)
        {
            if (info.StartTime == DateTime.MinValue)
            {
                leftover.Add(info);
                continue;
            }
            var idx = -1;
            for (var bi = 0; bi < bounds.Count; bi++)
            {
                var (s, e) = bounds[bi];
                // 最后一个区间上界为闭区间，避免终点当天的返程票被漏掉
                var isLast = bi == bounds.Count - 1;
                if (s <= info.StartTime && (info.StartTime < e || (isLast && info.StartTime <= e)))
                {
                    idx = bi;
                    break;
                }
            }
            if (idx >= 0)
            {
                groups[idx].Add(info);
            }
            else
            {
                leftover.Add(info);
            }
        }

        // 未落入任何区间的单据：有日期的按日期就近归入；无日期的按金额匹配，仍不行则单独成组。
        foreach (var info in leftover)
        {
            if (info.StartTime != DateTime.MinValue)
            {
                var idx = NearestRange(bounds, info.StartTime.Date);
                if (idx >= 0)
                {
                    groups[idx].Add(info);
                    continue;
                }
            }
            else if (info.TicketPrice != 0)
            {
                var hit = groups.FirstOrDefault(g => g.Any(x => x.TicketPrice == info.TicketPrice));
                if (hit != null)
                {
                    hit.Add(info);
                    continue;
                }
            }
            groups.Add([info]);
        }

        return groups.Where(g => g.Count > 0).ToList();
    }

    /// <summary>
    /// 无锚点时按“去程/返程”路线切分行程：
    /// 以最早一张车票/机票的出发地作为“家”，每次“从家出发”开启新行程，
    /// 其余单据（住宿/截图等）按日期归入所在行程。路线信息不足时返回 null。
    /// </summary>
    private static List<List<InvoiceInfo>>? GroupByRoute(List<InvoiceInfo> list)
    {
        static bool IsRoute(InvoiceInfo i) =>
            !string.IsNullOrWhiteSpace(i.Origin) &&
            !string.IsNullOrWhiteSpace(i.Destination) &&
            i.Origin != i.Destination &&
            i.StartTime != DateTime.MinValue &&
            i.Title != null &&
            (i.Title.Contains("火车票") || i.Title.Contains("机票")) &&
            !i.Title.Contains("退票");

        var routes = list.Where(IsRoute).OrderBy(t => t.StartTime).ToList();
        if (routes.Count == 0)
        {
            return null;
        }

        var home = routes[0].Origin!;

        var items = new List<List<InvoiceInfo>>();
        var ranges = new List<(DateTime Start, DateTime End)>();
        List<InvoiceInfo>? cur = null;

        foreach (var r in routes)
        {
            var outbound = r.Origin == home && r.Destination != home;
            if (outbound || cur == null)
            {
                cur = [r];
                items.Add(cur);
                ranges.Add((r.StartTime, r.StartTime));
            }
            else
            {
                cur.Add(r);
                var idx = items.Count - 1;
                var range = ranges[idx];
                ranges[idx] = (range.Start,
                    r.StartTime > range.End ? r.StartTime : range.End);
            }
        }

        // 把非路线单据（住宿、支付截图、审批单等）归入日期所在行程，否则归入最近行程。
        var routeSet = new HashSet<InvoiceInfo>(routes);
        foreach (var info in list.Where(t => !routeSet.Contains(t)))
        {
            int idx;
            if (info.StartTime != DateTime.MinValue)
            {
                idx = ranges.FindIndex(r =>
                    r.Start.Date <= info.StartTime.Date && info.StartTime.Date <= r.End.Date);
                if (idx < 0)
                {
                    idx = NearestRange(ranges, info.StartTime.Date);
                }
            }
            else
            {
                idx = info.TicketPrice != 0
                    ? items.FindIndex(g => g.Any(x => x.TicketPrice == info.TicketPrice))
                    : -1;
                if (idx < 0)
                {
                    idx = items.Count - 1;
                }
            }

            if (idx >= 0)
            {
                items[idx].Add(info);
                if (info.StartTime != DateTime.MinValue)
                {
                    var range = ranges[idx];
                    ranges[idx] = (
                        info.StartTime < range.Start ? info.StartTime : range.Start,
                        info.StartTime > range.End ? info.StartTime : range.End);
                }
            }
            else
            {
                items.Add([info]);
                ranges.Add((DateTime.Today, DateTime.Today));
            }
        }

        return items;
    }

    private static int NearestRange(List<(DateTime Start, DateTime End)> ranges, DateTime d)
    {
        var best = -1;
        var bestGap = double.MaxValue;
        for (var i = 0; i < ranges.Count; i++)
        {
            var gap = d < ranges[i].Start
                ? (ranges[i].Start - d).TotalDays
                : d > ranges[i].End
                    ? (d - ranges[i].End).TotalDays
                    : 0;
            if (gap < bestGap)
            {
                bestGap = gap;
                best = i;
            }
        }
        return best;
    }

    /// <summary>最后的兜底：按日期间隔聚类（间隔超过 gapDays 天即视为新行程）。</summary>
    private static List<List<InvoiceInfo>> GroupByDateGap(
        List<InvoiceInfo> list, int gapDays = 14)
    {
        var dated = list.Where(t => t.StartTime != DateTime.MinValue)
                        .OrderBy(t => t.StartTime)
                        .ToList();
        var undated = list.Where(t => t.StartTime == DateTime.MinValue).ToList();

        if (dated.Count == 0)
        {
            if (list.Count == 0)
            {
                return [];
            }
            return [list.ToList()];
        }

        var trips = new List<List<InvoiceInfo>>();
        List<InvoiceInfo>? cur = null;
        foreach (var info in dated)
        {
            if (cur == null ||
                (info.StartTime.Date - cur[^1].StartTime.Date).TotalDays > gapDays)
            {
                cur = [info];
                trips.Add(cur);
            }
            else
            {
                cur.Add(info);
            }
        }

        foreach (var u in undated)
        {
            trips[0].Add(u);
        }
        return trips;
    }
}
