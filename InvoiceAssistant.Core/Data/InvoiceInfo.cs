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

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool SetValue(PropertyInfo[] props, Match match,
       IEnumerable<RegexMapToPropertyMetadata> regexMapToProperties)
    {
        foreach (var item in regexMapToProperties!)
        {
            var prop = props.FirstOrDefault(t => t.Name == item.PropertyName);
            if (prop == null)
            {
                return false;
            }
            SetValue(this, prop, match.Groups[item.GroupsIndex].Value.ReplaceLineEndings().Trim(), item.Format);
        }
        return true;
    }
    private void SetValue(object obj, PropertyInfo prop, string v, string? f)
    {
        if (prop.PropertyType == typeof(string))
        {
            prop.SetValue(obj, v);
        }
        else if (prop.PropertyType == typeof(double))
        {
            var d = double.Parse(v);
            prop.SetValue(obj, d);
        }
        else if (prop.PropertyType == typeof(DateTime))
        {
            var d = string.IsNullOrWhiteSpace(f) ?
             DateTime.Parse(v) :
            DateTime.ParseExact(v, f, CultureInfo.InvariantCulture);
            prop.SetValue(obj, d);
        }
    }
}
