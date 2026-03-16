using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using InvoiceAssistant.App.ViewModels;
using InvoiceAssistant.App.Views;
using InvoiceAssistant.Core;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceAssistant.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        var collection = new ServiceCollection();
        collection.ConfigInvoiceAssistantCore();
        collection.AddCommonServices();
        var services = collection.BuildServiceProvider();
        ServiceCollectionExtensions.ServiceProvider = services;
        var vm = services.GetRequiredService<MainWindowViewModel>();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = vm
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}