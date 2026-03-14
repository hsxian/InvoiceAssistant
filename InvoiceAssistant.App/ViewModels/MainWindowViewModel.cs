using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;

namespace InvoiceAssistant.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
#pragma warning disable CA1822 // Mark members as static
    public string Greeting => "Welcome to Avalonia!";
#pragma warning restore CA1822 // Mark members as static

    public IInteraction<ProcessConfigViewModel, bool> ShowProcessConfigDialog { get; }
    public MainWindowViewModel()
    {
        ShowProcessConfigDialog = new Interaction<ProcessConfigViewModel, bool>();

    }
    [RelayCommand]
    public async Task OpenProcessConfigWindowAsync()
    {
        ShowProcessConfigDialog.Handle(new ProcessConfigViewModel());
    }
}