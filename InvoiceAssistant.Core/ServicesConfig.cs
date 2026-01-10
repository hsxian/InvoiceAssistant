using System.Security.Cryptography;
using InvoiceAssistant.Core.Service;
using InvoiceAssistant.Core.Service.Extractors;
using InvoiceAssistant.Core.Service.Processors;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceAssistant.Core;

public static class ServicesConfig
{
    public static IServiceCollection ConfigInvoiceAssistantCore(this IServiceCollection service)
    {
        service.AddScoped<IInfoExtractUnit, InfoExtractUnit>();
        service.AddScoped<IInfoExtractAssembly, InfoExtractAssembly>();
        service.AddScoped<IRenameProcessor, RenameProcessor>();
        service.AddScoped<IImageProcessor, ImageProcessor>();
        service.AddSingleton<IPdfProcessor, PdfProcessor>();
        service.AddTransient<IInfoExtractor, ImageOnlyTextExtractor>();
        service.AddTransient<IInfoExtractor, ImageWithTextPositionExtractor>();
        service.AddTransient<IInfoExtractor, PdfOnlyTextExtractor>();
        service.AddTransient<IInfoExtractor, PdfWithTextPositionExtractor>();
        service.AddTransient<IInfoExtractor, PdfToImageWithTextPositionExtractor>();
        return service;
    }
}
