using InvoiceAssistant.Core.Data;

namespace InvoiceAssistant.Core.Service.Processors;

public interface IRenameProcessor
{
    string GetNewFilename(InvoiceInfo info, bool datePrefix);
    void Rename(InvoiceInfo info);
    void TryGroup(IEnumerable<InvoiceInfo> infos);
    void TryMove(string s, string d);
}
