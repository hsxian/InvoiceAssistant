using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace InvoiceAssistant.Core.Data;

public class InvoiceInfo : INotifyPropertyChanged
{
    public string? FilePath { get; set; }
    public string? PersonName { get; set; }
    public string? Title { get; set; }
    public string? Origin { get; set; }
    public string? Destination { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double TicketPrice { get; set; }
    public bool GroupFlag { get; set; }

    /// <summary>
    /// 这条结果从哪条路径来的：text（PDF 文本层）/ ocr（渲染成图后识别）/ image。
    /// 仅用于诊断与日志，不参与业务逻辑。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? ExtractSource { get; set; }

    /// <summary>
    /// 日期里没有年份（如截图里只写“12月27号”），年份是按当前年补的。
    /// 批处理结束后会参考同批其他单据的日期把年份修正到最接近的那一年。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool DateYearInferred { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool SetValue(PropertyInfo[] props, Match match,
       IEnumerable<RegexMapToPropertyMetadata> regexMapToProperties)
    {
        bool allOk = true;
        foreach (var item in regexMapToProperties!)
        {
            var prop = props.FirstOrDefault(t => t.Name == item.PropertyName);
            if (prop == null)
            {
                return false;
            }
            if (!TrySetValue(this, prop, match.Groups[item.GroupsIndex].Value.ReplaceLineEndings("").Trim(), item.Format))
            {
                // 单个字段解析失败不应中断整批，交由调用方记录 "set value failed"
                allOk = false;
            }
        }
        return allOk;
    }
    private bool TrySetValue(object obj, PropertyInfo prop, string v, string? f)
    {
        try
        {
            if (prop.PropertyType == typeof(string))
            {
                prop.SetValue(obj, v);
            }
            else if (prop.PropertyType == typeof(double))
            {
                prop.SetValue(obj, double.Parse(v, CultureInfo.InvariantCulture));
            }
            else if (prop.PropertyType == typeof(DateTime))
            {
                var d = string.IsNullOrWhiteSpace(f)
                    ? DateTime.Parse(v, CultureInfo.InvariantCulture)
                    : DateTime.ParseExact(v, f, CultureInfo.InvariantCulture);
                prop.SetValue(obj, d);
                // ParseExact 对缺失的成分用“今天”补齐：格式里没有年份时，
                // 年份其实是当前年猜出来的，记下来等批内修正
                if (!string.IsNullOrEmpty(f) && !f.Contains('y'))
                {
                    ((InvoiceInfo)obj).DateYearInferred = true;
                }
            }
            return true;
        }
        catch
        {
            // OCR 抽到脏数据时跳过该字段，保持默认（DateTime.MinValue / 0）
            return false;
        }
    }
}
