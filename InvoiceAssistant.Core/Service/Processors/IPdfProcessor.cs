using Docnet.Core.Models;
using Docnet.Core.Readers;
using OpenCvSharp;
using SkiaSharp;

namespace InvoiceAssistant.Core.Service.Processors;

public interface IPdfProcessor : IDisposable
{
    Task<List<SKBitmap>> ExtractImages(string filePath, int dpi = 300);
    Task<List<Mat>> ExtractMatImages(string filePath, int dpi = 300);
    Task<List<string>> ExtractText(string filePath, int dpi = 300);
    IEnumerable<Character> FilterContains(IEnumerable<Character> characters, BoundBox bound);
    IEnumerable<Character> FilterIntersect(IEnumerable<Character> characters, BoundBox bound);
    Task ForeachPdfPage(string filePath, Action<IPageReader> action, int dpi = 300);
    string GetText(IEnumerable<Character> characters, BoundBox bound);
}
