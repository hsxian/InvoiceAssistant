namespace InvoiceAssistant.Core.Data;

public class ProcessConfig
{
    public string? Title { get; set; }
    public ProcessType ProcessValue { get; set; }
    public ExtractMetadata? Matcher { get; set; }
    public List<ExtractMetadata>? Xtractors { get; set; }
    public void CopyTo(InvoiceInfo? info)
    {
        if (info == null) return;
        info.Title = Title;
    }
}