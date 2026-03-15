using System;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;
using InvoiceAssistant.Core.Service;
using InvoiceAssistant.Core.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using InvoiceAssistant.App.Models;
using System.Collections.ObjectModel;
using InvoiceAssistant.App.Views;
using Avalonia;
using System.Collections.Generic;
using InvoiceAssistant.Core.Service.Images;
using InvoiceAssistant.Core.Service.Processors;
using InvoiceAssistant.App.Service;
using System.ComponentModel;

namespace InvoiceAssistant.App.ViewModels;

public partial class ProcessConfigViewModel : ViewModelBase
{
    [ObservableProperty] private string _filePath = string.Empty;

    [ObservableProperty] private Bitmap? _displayBitmap;

    [ObservableProperty] private ProcessConfig? _processConfig;
    [ObservableProperty] private int _pdfToImageDpi = 72;
    [ObservableProperty] private int _imageScale = 1;

    [ObservableProperty]
    private ObservableCollection<EnumOptions.ProcessTypeOption> _processTypeOptions =
        new(EnumOptions.ProcessTypeOptions);

    public event EventHandler<IEnumerable<SelectionBox>>? SelectionBoxesChanged;
    public ProcessConfigWindow Window { get; set; }
    private readonly IInfoExtractAssembly _infoExtractAssembly;
    private readonly IPdfProcessor _pdfProcessor;

    public ProcessConfigViewModel(IInfoExtractAssembly infoExtractAssembly, IPdfProcessor pdfProcessor)
    {
        _infoExtractAssembly = infoExtractAssembly;
        _pdfProcessor = pdfProcessor;
    }
    [RelayCommand]
    public async Task OpenImage()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(Window)!;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = "选择文件",
                SuggestedFileType = FilePickerFileTypes.ImageAll,
            });
            if (files.Count >= 1)
            {
                FilePath = files[0].Path.LocalPath;
                if (FilePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    var sImps = await _pdfProcessor.ExtractImages(FilePath, PdfToImageDpi);
                    if (sImps?.Count > 0)
                    {
                        DisplayBitmap = sImps[0].ConvertSKBitmapToAvaloniaBitmapViaStream();
                    }
                }
                else
                {
                    await using var stream = File.OpenRead(FilePath);
                    DisplayBitmap = new Bitmap(stream);
                }

                UpdateSelectionBoxes();
            }
        }
        catch (Exception ex)
        {
            // 处理错误
            Console.WriteLine($"选择图片失败: {ex.Message}");
        }
    }


    private void UpdateSelectionBoxes()
    {
        if (ProcessConfig is null || DisplayBitmap is null)
        {
            return;
        }

        var boxs = new List<SelectionBox>();
        var m = ProcessConfig.Matcher!;
        var box = SelectionBox.NewSelectionBox(m, DisplayBitmap.Size.Width, DisplayBitmap.Size.Height);
        boxs.Add(box);

        foreach (var item in ProcessConfig.Xtractors!)
        {
            box = SelectionBox.NewSelectionBox(item, DisplayBitmap.Size.Width, DisplayBitmap.Size.Height);
            boxs.Add(box);
        }

        SelectionBoxesChanged?.Invoke(this, boxs);
    }
}