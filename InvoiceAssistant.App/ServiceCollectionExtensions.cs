using InvoiceAssistant.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InvoiceAssistant.App;

public static class ServiceCollectionExtensions
{
    public static ServiceProvider ServiceProvider { get; set; }
    public static void AddCommonServices(this IServiceCollection collection)
    {
        collection.AddLogging(logging =>
        {
            // logging.AddConsole();
            // logging.AddDebug();
            logging.SetMinimumLevel(LogLevel.Debug);
        });
        collection.AddTransient<MainWindowViewModel>();
        collection.AddTransient<ProcessConfigViewModel>();
    }
}
