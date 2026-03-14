using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using InvoiceAssistant.App.ViewModels;
using ReactiveUI;

namespace InvoiceAssistant.App.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    // protected override void OnDataContextEndUpdate()
    // {
    //     base.OnDataContextEndUpdate();
    //     if (DataContext is MainWindowViewModel vm)
    //     {
    //         vm.ShowProcessConfigDialog.RegisterHandler(async interaction =>
    //         {
    //             var dialog = new ProcessConfigWindow
    //             {
    //                 DataContext = interaction.Input
    //             };
    //             var result = await dialog.ShowDialog<bool>(this);
    //             interaction.SetOutput(result);
    //         });
    //     }
    // }
    public MainWindow()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            return;

        this.WhenActivated(action =>
        {
            ViewModel!.ShowProcessConfigDialog.RegisterHandler(ShowProcessConfigDialog);
        });
    }

    private async Task ShowProcessConfigDialog(IInteractionContext<ProcessConfigViewModel,
                                            bool> interaction)
    {
        var dialog = new ProcessConfigWindow
        {
            DataContext = interaction.Input
        };
        var result = await dialog.ShowDialog<bool>(this);
        interaction.SetOutput(result);
    }
}