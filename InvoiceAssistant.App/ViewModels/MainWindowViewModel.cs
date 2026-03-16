using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoiceAssistant.App.Views;
using InvoiceAssistant.Core.Data;
using InvoiceAssistant.Core.Service;
using InvoiceAssistant.Core.Service.Processors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InvoiceAssistant.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private string _processConfigPath = "config/process";
    [ObservableProperty]
    private ObservableCollection<string> _processClassify = [];

    [ObservableProperty]
    private string _currentProcessClassify = string.Empty;

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(RunProcessCommand))]
    private string _invoiceOwner = string.Empty;
    [ObservableProperty]
    private string _processResult = string.Empty;
    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(RunProcessCommand))]
    private bool _isProcessing = false;
    [ObservableProperty]
    private ObservableCollection<ProcessConfig> _processConfigList = [];
    public MainWindow ViewWindow { get; set; }
    private readonly IInfoExtractAssembly _infoExtractAssembly;
    private readonly IRenameProcessor _renameProcessor;
    private readonly ILogger<MainWindowViewModel> _logger;

    public MainWindowViewModel(IInfoExtractAssembly infoExtractAssembly,
     IRenameProcessor renameProcessor,
     ILogger<MainWindowViewModel> logger)
    {
        _infoExtractAssembly = infoExtractAssembly;
        _renameProcessor = renameProcessor;
        _logger = logger;
        ProcessClassify = new ObservableCollection<string>(Directory.GetDirectories(_processConfigPath).Select(x => Path.GetFileName(x)));
        if (ProcessClassify.Count > 0)
        {
            CurrentProcessClassify = ProcessClassify[0];
        }
        //监听日志事件
        CustomLoggerProvider.OnLogMessageSent += OnLogMessageSent;
    }
    private async void OnLogMessageSent(string logMessage)
    {
        ProcessResult = $"{ProcessResult}\n\n{logMessage}";
        // //在主线程更新UI
        // Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
        // {
        // });
    }


    [RelayCommand]
    public void EditProcessConfig(ProcessConfig processConfig)
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
    public void OpenProcessConfigWindow()
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
    public void AddProcessConfig()
    {
        // 添加新配置
        var newConfig = new ProcessConfig
        {
            Title = "新配置",
            ProcessValue = ProcessType.UnKnown,
            Matcher = new ExtractMetadata { Right = 0.5f, Bottom = 0.1f },
            Xtractors = []
        };
        ProcessConfigList.Add(newConfig);
    }

    [RelayCommand]
    public void DeleteProcessConfig(ProcessConfig config)
    {
        ProcessConfigList.Remove(config);
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
    private bool CanRunProcess() =>
        !string.IsNullOrEmpty(InvoiceOwner) && !IsProcessing;
    [RelayCommand(CanExecute = nameof(CanRunProcess))]
    public async Task RunProcess()
    {
        var topLevel = TopLevel.GetTopLevel(ViewWindow)!;

        var dirs = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = "选择要处理的文件夹",
        });
        if (dirs.Count == 0)
        {
            return;
        }
        var rfh = dirs[0].Path.LocalPath;
        _infoExtractAssembly.PdfExtensions = ["pdf"];
        _infoExtractAssembly.ImageExtensions = ["jpg", "jpeg", "png"];
        RenameProcessor.InvoiceOwner = InvoiceOwner;
        _ = Task.Run(async () =>
        {
            try
            {
                var infos = await _infoExtractAssembly.Extract(rfh, ProcessConfigList);
                _renameProcessor.TryGroup(infos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理文件夹{0}时出错", rfh);
            }
        });
        // _renameProcessor.TryGroup(infos);
        // foreach (var item in infos)
        // {
        //     renameProcessor.Rename(item);
        // }
    }
}