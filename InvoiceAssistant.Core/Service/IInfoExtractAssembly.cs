using InvoiceAssistant.Core.Data;

namespace InvoiceAssistant.Core.Service;

public interface IInfoExtractAssembly : IDisposable
{
    Task<IEnumerable<InvoiceInfo>> Extract(string dir, IEnumerable<ProcessConfig> configs);
    Task<IEnumerable<ProcessConfig>> GetProcessConfigs(string dir);
}
