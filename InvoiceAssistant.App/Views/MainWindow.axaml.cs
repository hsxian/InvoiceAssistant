using System;
using Avalonia.Controls;
using InvoiceAssistant.App.ViewModels;

namespace InvoiceAssistant.App.Views;

// Avalonia 12 不再提供 Avalonia.ReactiveUI 附属包；
// 本项目 VM 基于 CommunityToolkit.Mvvm，ReactiveWindow 从未被真正用到，改回普通 Window
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            return;
    }
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is MainWindowViewModel vm)
            vm.ViewWindow = this;
    }
}