namespace InvoiceAssistant.Core.Data;

public class InvoiceInfo
{
    public FileInfoType InfoType { get; set; }
    public string? FilePath { get; set; }
    public string? PersonName { get; set; }
    public string? Title { get; set; }
    public string? Origin { get; set; }
    public string? Destination { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double TicketPrice { get; set; }
}
