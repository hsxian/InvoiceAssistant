using InvoiceAssistant.Core.Data;
using OpenCvSharp;

namespace InvoiceAssistant.Core.Service.ExtractUnits;

public interface IImageInfoExtractUnit : IDisposable
{
    Task<InvoiceInfo?> ExtractByImg(Mat mat, ProcessConfig processConfig);
    Task<InvoiceInfo?> ExtractByImgOnlyText(Mat mat, ProcessConfig processConfig);
    Task<InvoiceInfo?> ExtractByImg(string filePath, ProcessConfig processConfig);
    Task<InvoiceInfo?> ExtractByImgOnlyText(string filePath, ProcessConfig processConfig);
    Task OutputTestResult(Mat mat);
}
