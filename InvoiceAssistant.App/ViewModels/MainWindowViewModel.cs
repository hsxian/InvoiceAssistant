using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoiceAssistant.App.Views;
using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceAssistant.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
#pragma warning disable CA1822 // Mark members as static
    public string Greeting => "Welcome to Avalonia!";
#pragma warning restore CA1822 // Mark members as static
    private string _processConfigPath = "config/process";
    [ObservableProperty]
    private ObservableCollection<string> _processClassify = [];

    [ObservableProperty]
    private string _currentProcessClassify = string.Empty;
    [ObservableProperty]
    private ObservableCollection<ProcessConfig> _processConfigList = [];
    private readonly IInfoExtractAssembly _infoExtractAssembly;
    public MainWindowViewModel(IInfoExtractAssembly infoExtractAssembly)
    {
        _infoExtractAssembly = infoExtractAssembly;
        ProcessClassify = new ObservableCollection<string>(Directory.GetDirectories(_processConfigPath).Select(x => Path.GetFileName(x)));
        if (ProcessClassify.Count > 0)
        {
            CurrentProcessClassify = ProcessClassify[0];
        }
    }
    [RelayCommand]
    public void EditProcessConfigCommand(ProcessConfig processConfig)
    {
        var vm = ServiceCollectionExtensions.ServiceProvider.GetRequiredService<ProcessConfigViewModel>();
        // 创建并显示第二个窗口
        var secondWindow = new ProcessConfigWindow
        {
            DataContext = vm
        };

        // 设置第二个窗口的所有者为当前主窗口
        secondWindow.Show();
        vm.ProcessConfig = processConfig;
    }

    [RelayCommand]
    public void OpenProcessConfigWindowCommand()
    {
        var vm = ServiceCollectionExtensions.ServiceProvider.GetRequiredService<ProcessConfigViewModel>();
        vm.ProcessConfig = new ProcessConfig();
        // 创建并显示第二个窗口
        var secondWindow = new ProcessConfigWindow
        {
            DataContext = vm
        };

        // 设置第二个窗口的所有者为当前主窗口
        secondWindow.Show();
    }

    [RelayCommand]
    public void AddProcessConfigCommand()
    {
        // 添加新配置
        var newConfig = new ProcessConfig { Title = "新配置", ProcessValue = ProcessType.UnKnown };
        ProcessConfigList.Add(newConfig);
    }

    [RelayCommand]
    public void DeleteProcessConfigCommand()
    {
        // 删除选中的配置
        // 这里需要实现具体的删除逻辑
    }
    partial void OnCurrentProcessClassifyChanged(string value)
    {
        _ = LoadProcessConfig(value);
    }
    private async Task LoadProcessConfig(string classify)
    {
        var dir = Path.Combine(_processConfigPath, classify);
        ProcessConfigList = new ObservableCollection<ProcessConfig>(await _infoExtractAssembly.GetProcessConfigs(dir));
    }
}