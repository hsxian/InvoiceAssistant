using System.ComponentModel;
using System.Text.Json.Serialization;

namespace InvoiceAssistant.Core.Data;

public class ProcessConfig : INotifyPropertyChanged
{
    public string? Title { get; set; }
    public string? RenameRule { get; set; }
    public bool GroupFlag { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProcessType ProcessValue { get; set; }
    public ExtractMetadata? Matcher { get; set; }
    public List<ExtractMetadata>? Xtractors { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void CopyTo(InvoiceInfo? info)
    {
        if (info == null) return;
        info.Title = Title;
        info.GroupFlag = GroupFlag;
    }
}