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
        // Avalonia 12 移除了公开的 BindingPlugins（数据校验插件机制重做）；
        // 原来这里 RemoveAt(0) 去重 Avalonia 与 CommunityToolkit 的校验，12 下无需处理

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