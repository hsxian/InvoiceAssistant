using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service.Processors;

namespace InvoiceAssistant.Core.Service.Extractors;

public interface IInfoExtractor : IDisposable
{
    ProcessType ProcessType { get; }
    Task<InvoiceInfo?> GetInfo(string filePath, ProcessConfig processConfig);
}
