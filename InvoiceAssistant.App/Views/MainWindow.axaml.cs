using System;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using InvoiceAssistant.App.ViewModels;

namespace InvoiceAssistant.App.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
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
        if (ViewModel != null)
            ViewModel.ViewWindow = this;
    }
}