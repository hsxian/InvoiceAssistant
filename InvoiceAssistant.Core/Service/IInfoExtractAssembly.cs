using InvoiceAssistant.Core.Data;

namespace InvoiceAssistant.Core.Service;

public interface IInfoExtractAssembly : IDisposable
{
    List<string> PdfExtensions { get; set; }
    List<string> ImageExtensions { get; set; }
    Task<IEnumerable<InvoiceInfo>> Extract(string dir, IEnumerable<ProcessConfig> configs);
    Task<IEnumerable<ProcessConfig>> GetProcessConfigs(string dir);
}
